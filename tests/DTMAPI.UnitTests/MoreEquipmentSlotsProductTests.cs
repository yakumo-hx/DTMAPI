using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DTMAPI.MoreEquipmentSlots;

namespace DTMAPI.UnitTests
{
    internal static class MoreEquipmentSlotsProductTests
    {
        internal static void RunAll()
        {
            FixedThreeSlotAndEffectPolicy();
            HookOwnershipRejectsBothOrdersAndResidue();
            PlacementDistinguishesBackpackMailAndFailure();
            GameplayCandidateSeparatesWorkingAndCommitted();
            GameplayEquipReplaceUnequipAndBreakDiscardUnsaved();
            GameplayCandidateCoversNativeCommitWindows();
            IdenticalNativeBytesStillProveRewrite();
            UnknownJournalOriginFailsClosed();
            PreparedBeforeAttemptRetriesWithoutDuplication();
            NativeSaveBeforePromotionRecoversExactDestination();
            CommittedTombstoneBeforeClearFinalizesOnce();
            AmbiguousOrExcessDestinationFailsClosed();
            SameItemSplitAcrossBackpackAndMailIsExact();
            ScopeMismatchFailsClosed();
            ReplacementCommitsIncomingAndOutgoingTogether();
            ReplacementOutgoingFailurePreservesIncoming();
            ReplacementIncomingFailurePreservesBothItems();
            ReplacementCrashWindowsPreserveExactOwnership();
            SameItemReplacementEvidenceCoversAllCrashWindows();
            DurableStorePreservesLiveAndJournalOnFailure();
            GameplayCandidateStoreIsDurableAndFailClosed();
            LegacyFlatSchemasAndHistoricalHostShapeMigrateExactly();
            LegacyPreSchemaGlobalIsClaimedExactlyOnce();
            ConcurrentPreSchemaClaimPublicationIsSerialized();
            CrossProcessLateClaimIsWithdrawn();
            PreSchemaClaimPublicationWindowsFailClosed();
            PreSchemaPendingClaimBlocksCrossSaveCrashOrder();
            PendingClaimBlocksEmptyAuthorityCreation();
            ClaimCompletionRevalidatesArchiveAndProduct();
            HistoricalGameRootWinnerRejectsDifferentSource();
            CrossProcessHistoricalWinnerIsSingleton();
            PendingEvidenceStillRunsPreWinnerCensus();
            PendingWinnerStillRunsPreWinnerCensus();
            CompletedWinnerIgnoresRetainedLateLoserEvidence();
            PreWinnerCensusIncludesEligiblePreviousAndDifferentHash();
            ClaimCompletionNeverOverwritesDifferentPendingOwner();
            CompletedClaimAcceptsEligiblePreviousAuthority();
            LegacyMigrationPreservesTransactionsAndCandidate();
            LegacyMigrationCrashWindowsAndDriftFailClosed();
            InterruptedGlobalCaptureResumesExactly();
            CaptureResidueBlocksEmptyAuthorityCreation();
            GlobalFlatFinalStateRejectsRecreatedAuthority();
            GlobalFlatTerminalArchiveAllowsAnotherSaveEmpty();
            LegacyBackupPublicationRetriesEveryWindow();
            LegacyMigrationRejectsWrongScopeFutureAndExcessSlots();
            LegacyMigrationRejectsIdentityAndMalformedSlots();
            ProductRevisionAndPreviousAuthorityRules();
            ProductV3RequiresNativeSaveClock();
            PlayerUiRoutesHeldNativeItemThroughProductionEquip();
            ReflectionAccessPrefersTheNearestHiddenMember();
            AtomicInstallerOwnsRollbackAndDeactivationPaths();
        }

        internal static void RunCrossProcessClaimActor()
        {
            if (string.Equals(
                Environment.GetEnvironmentVariable(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_MODE"),
                "late-evidence-writer",
                StringComparison.Ordinal))
            {
                RunLateEvidenceWriterActor();
                return;
            }
            if (string.Equals(
                Environment.GetEnvironmentVariable(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_MODE"),
                "historical-backfill",
                StringComparison.Ordinal))
            {
                RunCrossProcessHistoricalBackfillActor();
                return;
            }
            if (string.Equals(
                Environment.GetEnvironmentVariable(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_MODE"),
                "capture-crash",
                StringComparison.Ordinal))
            {
                RunCrossProcessCaptureCrashActor();
                return;
            }

            string globalPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_GLOBAL");
            string productPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_PRODUCT");
            string winningProductPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_WINNER");
            string readyPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_READY");
            string releasePath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_RELEASE");
            var scope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = 3,
                    PlayerName = "fourth-save",
                    CustomPlayerName = "fourth-save",
                    TotalGameSeconds = 100
                };
            string sourceSha256 =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(globalPath);
            string claimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        sourceSha256);
            string winnerPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        globalPath);
            const string permanentWinnerRejection =
                "Another save or source revision already owns the pending pre-schema global migration claim.";
            string? rejectionReason = null;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-claim-file-publish-before-revalidation" &&
                            string.Equals(
                                Environment.GetEnvironmentVariable(
                                    "DTMAPI_MORE_EQUIPMENT_CLAIM_CRASH_AFTER_PUBLISH"),
                                "1",
                                StringComparison.Ordinal))
                        {
                            Environment.Exit(86);
                        }
                        if (stage !=
                            "after-preschema-claim-state-check-before-publish")
                        {
                            return;
                        }
                        File.WriteAllText(
                            readyPath,
                            "ready");
                        WaitForFile(
                            releasePath,
                            "The winning claim process did not release the late publisher.");
                    }).LoadOrMigrate(
                        productPath,
                        globalPath,
                        scope,
                        out _);
            }
            catch (InvalidDataException ex)
            {
                rejectionReason = ex.Message;
            }
            Assert(
                string.Equals(
                    rejectionReason,
                    permanentWinnerRejection,
                    StringComparison.Ordinal) &&
                File.Exists(winningProductPath) &&
                !File.Exists(productPath) &&
                !File.Exists(globalPath) &&
                IsCompletedClaim(winnerPath) &&
                !File.Exists(claimPath),
                "A late process must encounter the winner's durable completed claim before it can publish or crash with a stale loser claim.");
            Console.WriteLine(
                "DTMAPI_MORE_EQUIPMENT_CLAIM_REJECTION=permanent-winner");
        }

        private static void
            RunCrossProcessHistoricalBackfillActor()
        {
            string globalPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_GLOBAL");
            string productPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_PRODUCT");
            string readyPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_READY");
            string releasePath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_RELEASE");
            string outcomePath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_OUTCOME");
            var scope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex =
                        int.Parse(
                            RequiredEnvironment(
                                "DTMAPI_MORE_EQUIPMENT_CLAIM_ARCHIVE_INDEX"),
                            System.Globalization
                                .CultureInfo.InvariantCulture),
                    PlayerName =
                        RequiredEnvironment(
                            "DTMAPI_MORE_EQUIPMENT_CLAIM_PLAYER"),
                    CustomPlayerName =
                        RequiredEnvironment(
                            "DTMAPI_MORE_EQUIPMENT_CLAIM_PLAYER"),
                    TotalGameSeconds = 100
                };
            File.WriteAllText(
                readyPath,
                "ready");
            WaitForFile(
                releasePath,
                "The historical backfill actor was not released.");
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        scope,
                        out _);
                File.WriteAllText(
                    outcomePath,
                    "success");
            }
            catch (InvalidDataException)
            {
                File.WriteAllText(
                    outcomePath,
                    "blocked");
            }
        }

        private static void RunCrossProcessCaptureCrashActor()
        {
            string globalPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_GLOBAL");
            string productPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_PRODUCT");
            new EquipmentSlotDocumentStore(
                stage =>
                {
                    if (stage ==
                        "after-global-capture-before-hash")
                    {
                        Environment.Exit(87);
                    }
                }).LoadOrMigrate(
                    productPath,
                    globalPath,
                    Scope(),
                    out _);
            throw new InvalidOperationException(
                "The capture crash actor did not reach its process-exit needle.");
        }

        private static void RunLateEvidenceWriterActor()
        {
            string targetPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_TARGET");
            string readyPath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_READY");
            string releasePath =
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_RELEASE");
            string temporaryPath =
                targetPath +
                ".old-actor-" +
                Guid.NewGuid().ToString("N");
            File.WriteAllText(
                temporaryPath,
                RequiredEnvironment(
                    "DTMAPI_MORE_EQUIPMENT_CLAIM_JSON"));
            File.WriteAllText(
                readyPath,
                "ready");
            WaitForFile(
                releasePath,
                "The late evidence writer was not released.");
            File.Move(
                temporaryPath,
                targetPath);
        }

        private static void FixedThreeSlotAndEffectPolicy()
        {
            Assert(
                MoreEquipmentSlotsProductContract.FixedSlotCount == 3,
                "ProductNative equipment policy must remain fixed at exactly three slots.");
            var slots =
                EquipmentSlotStorageDocument.CreateEmptySlots();
            slots[0].ItemId = "passive";
            slots[0].SkillId = "passive-skill";
            slots[1].ItemId = "defense-hat";
            slots[1].DefenseBonus = 2;
            slots[2].ItemId = "shield-hat";
            slots[2].IsShield = true;
            slots[2].ShieldValue = 5;
            slots[2].DefenseBonus = 3;
            Assert(
                MoreEquipmentSlotsEffectPolicy
                    .ShouldCreateNativeFunction(false, "passive-skill") &&
                !MoreEquipmentSlotsEffectPolicy
                    .ShouldCreateNativeFunction(true, "shield") &&
                MoreEquipmentSlotsEffectPolicy
                    .SumHatDefense(slots) == 5,
                "Passive functions, defensive hats, and product shield hats must remain separate effect paths.");
            Assert(
                MoreEquipmentSlotsEffectPolicy
                    .FindTailShieldIndex(slots) == 2 &&
                MoreEquipmentSlotsEffectPolicy
                    .PreservesNativeHatVisual,
                "Tail shield selection and native hat appearance ownership regressed.");
        }

        private static void HookOwnershipRejectsBothOrdersAndResidue()
        {
            bool[] none = { false, false, false, false };
            bool[] compatibility =
                { true, true, true, true };
            bool[] product =
                { true, true, true, true };
            bool[] residue =
                { true, false, true, false };
            Assert(
                MoreEquipmentSlotsHookOwnership.DecideInstall(
                    none,
                    none) ==
                    EquipmentSlotsInstallDecision.Install,
                "Empty owner topology must admit one atomic product installation.");
            Assert(
                MoreEquipmentSlotsHookOwnership.DecideInstall(
                    compatibility,
                    none) ==
                    EquipmentSlotsInstallDecision
                        .RejectCompatibilityOwner,
                "Compatibility-first load order must fail closed.");
            Assert(
                MoreEquipmentSlotsHookOwnership.DecideInstall(
                    none,
                    product) ==
                    EquipmentSlotsInstallDecision
                        .RejectDuplicateProductOwner,
                "Product-first duplicate load order must fail closed.");
            Assert(
                MoreEquipmentSlotsHookOwnership.DecideInstall(
                    none,
                    residue) ==
                    EquipmentSlotsInstallDecision
                        .RejectPartialProductOwner &&
                MoreEquipmentSlotsHookOwnership
                    .Observe(residue).HasPartialOwner,
                "A failed rollback or residual exact owner must not be treated as installed or clean.");
            EquipmentSlotsObservedHookState clean =
                MoreEquipmentSlotsHookOwnership.Observe(none);
            Assert(
                !clean.IsInstalled &&
                clean.InstalledPatchCount == 0 &&
                !clean.HasPartialOwner,
                "Real owner deactivation must observe four exact-owner hooks removed.");
        }

        private static void
            ReflectionAccessPrefersTheNearestHiddenMember()
        {
            var value =
                new DerivedReflectionFixture();
            Assert(
                (string?)MoreEquipmentSlotsReflectionAccess
                    .Read(
                        value,
                        "Value") == "derived",
                "Native item reflection must choose the nearest hidden member without GetProperty ambiguity.");
            Assert(
                MoreEquipmentSlotsReflectionAccess.Set(
                    value,
                    "Value",
                    "updated") &&
                string.Equals(
                    value.Value as string,
                    "updated",
                    StringComparison.Ordinal) &&
                ((BaseReflectionFixture)value).Value ==
                    "base",
                "Native item reflection writes must target the same deterministic nearest member.");
        }

        private static void PlacementDistinguishesBackpackMailAndFailure()
        {
            var backpack = new FakeGateway
            {
                BackpackSucceeds = true
            };
            NativePlacementResult backpackResult =
                new EquipmentSlotNativePlacement(backpack)
                    .PlaceOne("hat");
            Assert(
                backpackResult.Kind ==
                    NativePlacementKind.Backpack &&
                backpack.BackpackCount == 1 &&
                backpack.MailCount == 0,
                "Backpack success must be one exact backpack delta.");

            var mail = new FakeGateway
            {
                MailSucceeds = true
            };
            NativePlacementResult mailResult =
                new EquipmentSlotNativePlacement(mail)
                    .PlaceOne("hat");
            Assert(
                mailResult.Kind == NativePlacementKind.Mail &&
                mail.BackpackCount == 0 &&
                mail.MailCount == 1,
                "Mail success must remain distinct from the official TryPlaceInBackpack false return.");

            NativePlacementResult failed =
                new EquipmentSlotNativePlacement(
                    new FakeGateway()).PlaceOne("hat");
            Assert(
                failed.Kind == NativePlacementKind.Failure,
                "True native placement failure must remain fail-closed escrow.");

            var excessBackpackDelta = new FakeGateway
            {
                BackpackSucceeds = true,
                BackpackPlacementDelta = 2
            };
            bool excessRejectedAsUnknown = false;
            try
            {
                new EquipmentSlotNativePlacement(
                    excessBackpackDelta).PlaceOne("hat");
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException)
            {
                excessRejectedAsUnknown = true;
            }
            Assert(
                excessRejectedAsUnknown &&
                excessBackpackDelta.BackpackCount == 2 &&
                excessBackpackDelta.MailCount == 0,
                "A reported backpack success with an excess +2 native delta must become outcome-unknown, not an ordinary retryable failure.");

            var unreadablePreflight = new FakeGateway
            {
                MailSucceeds = true,
                ThrowMailReadAtCall = 1
            };
            bool preflightFailedClosed = false;
            try
            {
                new EquipmentSlotNativePlacement(
                    unreadablePreflight).PlaceOne("hat");
            }
            catch (InvalidDataException)
            {
                preflightFailedClosed = true;
            }
            Assert(
                preflightFailedClosed &&
                unreadablePreflight.MailSendCallCount == 0 &&
                unreadablePreflight.MailCount == 0,
                "Unreadable mail preflight must stop before every native placement call.");

            var unreadableAfterMail = new FakeGateway
            {
                MailSucceeds = true,
                ThrowMailReadAtCall = 3
            };
            var placement =
                new EquipmentSlotNativePlacement(
                    unreadableAfterMail);
            NativePlacementResult reconciled =
                placement.PlaceOneWithImmediateEvidence(
                    "hat");
            Assert(
                reconciled.Kind == NativePlacementKind.Mail &&
                unreadableAfterMail.MailSendCallCount == 1 &&
                unreadableAfterMail.MailCount == 1,
                "One unreadable post-call observation must reconcile from the one immediate same-call observation without replay.");

            var unreadableWithoutMutation = new FakeGateway
            {
                ThrowMailReadAtCall = 3
            };
            var noMutationPlacement =
                new EquipmentSlotNativePlacement(
                    unreadableWithoutMutation);
            NativePlacementResult unchanged =
                noMutationPlacement
                    .PlaceOneWithImmediateEvidence(
                        "hat");
            Assert(
                unchanged.Kind == NativePlacementKind.Failure &&
                unreadableWithoutMutation.MailSendCallCount == 1 &&
                unreadableWithoutMutation.MailCount == 0,
                "An unreadable post-call with an unchanged immediate observation must retain one sidecar item and zero native mail.");

            var persistentlyUnreadable = new FakeGateway
            {
                MailSucceeds = true,
                ThrowMailReadFromCall = 3
            };
            bool persistentAmbiguityRejected = false;
            try
            {
                new EquipmentSlotNativePlacement(
                    persistentlyUnreadable)
                    .PlaceOneWithImmediateEvidence(
                        "hat");
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException)
            {
                persistentAmbiguityRejected = true;
            }
            Assert(
                persistentAmbiguityRejected &&
                persistentlyUnreadable.MailSendCallCount == 1 &&
                persistentlyUnreadable.MailCount == 1,
                "Persistent evidence ambiguity must remain outcome-unknown after exactly one native call and one immediate observation.");

            var falseButExactWithdrawal = new FakeGateway
            {
                CostReportedOverride = false,
                CostDelta = -1
            };
            falseButExactWithdrawal.SetBackpackCount(1);
            Assert(
                new EquipmentSlotNativePlacement(
                    falseButExactWithdrawal)
                    .TryWithdrawOne("hat") &&
                falseButExactWithdrawal.BackpackCount == 0,
                "An exact observed -1 backpack delta must authorize withdrawal even when the native Boolean is false.");

            var trueWithoutWithdrawal = new FakeGateway
            {
                CostReportedOverride = true,
                CostDelta = 0
            };
            trueWithoutWithdrawal.SetBackpackCount(1);
            bool contradictoryWithdrawalRejected = false;
            try
            {
                new EquipmentSlotNativePlacement(
                    trueWithoutWithdrawal)
                    .TryWithdrawOne("hat");
            }
            catch (InvalidDataException)
            {
                contradictoryWithdrawalRejected = true;
            }
            Assert(
                contradictoryWithdrawalRejected &&
                trueWithoutWithdrawal.BackpackCount == 1,
                "A true withdrawal Boolean without the exact -1 count delta must fail closed.");
        }

        private static void
            GameplayCandidateSeparatesWorkingAndCommitted()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("shield");
            document.Slots[0].IsShield = true;
            document.Slots[0].ShieldValue = 10;
            document.Slots[0].ShieldMaxValue = 10;
            List<EquipmentSlotStorageEntry> working =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            working[0].ShieldValue = 4;

            EquipmentSlotGameplayCandidate candidate =
                EquipmentSlotGameplayCandidateCoordinator.Prepare(
                    document,
                    working,
                    "save-a",
                    item => Observation("save-a", 0, 0));
            Assert(
                candidate.Origin ==
                    EquipmentSlotTransactionOrigin
                        .GameplayMutation &&
                candidate.State ==
                    EquipmentSlotGameplayCandidateState.Prepared &&
                document.Slots[0].ShieldValue == 10 &&
                candidate.WorkingSlots[0].ShieldValue == 4 &&
                document.Journal == null,
                "SaveSaving must persist Working as a typed gameplay candidate without advancing committed slots.");

            EquipmentSlotGameplayRecoveryDecision noSave =
                EquipmentSlotGameplayCandidateCoordinator
                    .DecideRecovery(
                        document,
                        "save-a",
                        item => Observation(
                            "save-a",
                            0,
                            0));
            Assert(
                noSave.Action ==
                    EquipmentSlotGameplayRecoveryAction
                        .DiscardUncommitted,
                "A native-save failure or no-save title return must discard the prepared gameplay candidate.");
            EquipmentSlotGameplayCandidateCoordinator
                .DiscardUncommitted(document);
            Assert(
                document.GameplayCandidate == null &&
                document.Slots[0].ShieldValue == 10,
                "Discarding unsaved shield damage must retain the prior committed durability.");

            EquipmentSlotGameplayCandidateCoordinator.Prepare(
                document,
                working,
                "save-a",
                item => Observation("save-a", 0, 0));
            EquipmentSlotGameplayCandidateCoordinator
                .PromoteCommitted(
                    document,
                    "save-a",
                    nativeSaveConfirmed: true);
            EquipmentSlotGameplayCandidateCoordinator
                .FinalizeCommitted(document);
            Assert(
                document.Slots[0].ShieldValue == 4,
                "A real SaveSaved notification must commit shield-only Working state even when native save bytes hash identically.");
        }

        private static void
            GameplayCandidateCoversNativeCommitWindows()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("old-hat");
            List<EquipmentSlotStorageEntry> working =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            working[0].Clear();
            EquipmentSlotGameplayCandidateCoordinator.Prepare(
                document,
                working,
                "save-a",
                item => Observation("save-a", 1, 0));

            Assert(
                EquipmentSlotGameplayCandidateCoordinator
                    .DecideRecovery(
                        document,
                        "save-b",
                        item => Observation(
                            "save-b",
                            1,
                            0)).Action ==
                    EquipmentSlotGameplayRecoveryAction
                        .PromoteCommitted,
                "Native success before sidecar promotion must be recoverable from exact scope, fingerprint, and native counts.");
            Assert(
                EquipmentSlotGameplayCandidateCoordinator
                    .DecideRecovery(
                        document,
                        "save-b",
                        item => Observation(
                            "save-b",
                            2,
                            0)).Action ==
                    EquipmentSlotGameplayRecoveryAction
                        .FailClosed,
                "A mismatched native count must fail closed instead of duplicating a gameplay item.");

            EquipmentSlotGameplayCandidateCoordinator
                .PromoteCommitted(document, "save-b");
            Assert(
                !document.Slots[0].IsOccupied &&
                document.GameplayCandidate?.State ==
                    EquipmentSlotGameplayCandidateState
                        .CommittedTombstone &&
                EquipmentSlotGameplayCandidateCoordinator
                    .DecideRecovery(
                        document,
                        "save-b",
                        item => Observation(
                            "save-b",
                            1,
                            0)).Action ==
                    EquipmentSlotGameplayRecoveryAction
                        .FinalizeCommitted,
                "A journal-cleanup interruption must retain the promoted committed projection and be idempotently finalizable.");
            EquipmentSlotGameplayCandidateCoordinator
                .FinalizeCommitted(document);
            Assert(
                document.GameplayCandidate == null &&
                !document.Slots[0].IsOccupied,
                "Final gameplay cleanup must leave exactly the native-owned unequipped item.");
        }

        private static void
            GameplayEquipReplaceUnequipAndBreakDiscardUnsaved()
        {
            foreach (GameplayDiscardScenario scenario in
                new[]
                {
                    new GameplayDiscardScenario(
                        committedItem: string.Empty,
                        workingItem: "new-hat",
                        name: "equip"),
                    new GameplayDiscardScenario(
                        committedItem: "old-hat",
                        workingItem: "new-hat",
                        name: "replace"),
                    new GameplayDiscardScenario(
                        committedItem: "old-hat",
                        workingItem: string.Empty,
                        name: "unequip"),
                    new GameplayDiscardScenario(
                        committedItem: "shield",
                        workingItem: string.Empty,
                        name: "shield-break")
                })
            {
                EquipmentSlotStorageDocument document =
                    string.IsNullOrWhiteSpace(
                        scenario.CommittedItem)
                        ? new EquipmentSlotStorageDocument
                        {
                            Scope = Scope(),
                            Generation = 1
                        }
                        : CreateOccupiedDocument(
                            scenario.CommittedItem);
                List<EquipmentSlotStorageEntry> working =
                    EquipmentSlotGameplayCandidateCoordinator
                        .CloneSlots(document.Slots);
                working[0].Clear();
                if (!string.IsNullOrWhiteSpace(
                    scenario.WorkingItem))
                {
                    working[0].ItemId =
                        scenario.WorkingItem;
                    working[0].DisplayName =
                        scenario.WorkingItem;
                }
                EquipmentSlotGameplayCandidateCoordinator
                    .Prepare(
                        document,
                        working,
                        "save-a",
                        item => Observation(
                            "save-a",
                            0,
                            0));
                EquipmentSlotGameplayCandidateCoordinator
                    .DiscardUncommitted(document);
                Assert(
                    string.Equals(
                        document.Slots[0].ItemId,
                        scenario.CommittedItem,
                        StringComparison.Ordinal),
                    "No-save " +
                    scenario.Name +
                    " must restore the exact committed product slot projection.");
            }
        }

        private static void
            IdenticalNativeBytesStillProveRewrite()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Native fingerprint fixtures require the managed DTMAPI test session.");
            string root = Path.Combine(
                sessionRoot,
                "moreequipment-identical-native-rewrite");
            Directory.CreateDirectory(root);
            string currentPath =
                Path.Combine(root, "archive-2.data");
            byte[] bytes = { 1, 2, 3, 4, 5 };
            File.WriteAllBytes(currentPath, bytes);
            DateTime firstWrite =
                new DateTime(
                    638890000000000000L,
                    DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(
                currentPath,
                firstWrite);
            string before =
                EquipmentSlotNativeCommitFingerprint
                    .Compute(currentPath, backupCount: 5);

            File.WriteAllBytes(currentPath, bytes);
            File.SetLastWriteTimeUtc(
                currentPath,
                firstWrite.AddSeconds(2));
            string after =
                EquipmentSlotNativeCommitFingerprint
                    .Compute(currentPath, backupCount: 5);

            for (int index = 0; index < 5; index++)
            {
                File.WriteAllText(
                    currentPath + ".prev" + index,
                    "previous-" + index);
            }
            File.WriteAllText(
                currentPath + ".bak",
                "backup");
            string fullFamily =
                EquipmentSlotNativeCommitFingerprint
                    .Compute(currentPath, backupCount: 5);
            string legacyV2 =
                "native-v2|current=" +
                FingerprintValue(fullFamily, "current") +
                "|prev=" +
                FingerprintValue(fullFamily, "prev0") +
                "|bak=" +
                FingerprintValue(fullFamily, "bak");
            Assert(
                EquipmentSlotNativeCommitFingerprint
                    .Equivalent(legacyV2, fullFamily),
                "A historical native-v2 fingerprint must match only its exact current/prev0/bak projection inside native-v3.");
            File.WriteAllText(
                currentPath + ".prev4",
                "previous-4-changed");
            string changedLaterBackup =
                EquipmentSlotNativeCommitFingerprint
                    .Compute(currentPath, backupCount: 5);
            Assert(
                !string.Equals(
                    fullFamily,
                    changedLaterBackup,
                    StringComparison.Ordinal) &&
                EquipmentSlotNativeCommitFingerprint
                    .Equivalent(
                        legacyV2,
                        changedLaterBackup),
                "native-v3 must fingerprint every configured prev generation while the explicit legacy-v2 alias remains limited to prev0.");
            File.Delete(currentPath);
            bool missingCurrentRejected = false;
            try
            {
                EquipmentSlotNativeCommitFingerprint
                    .Compute(currentPath, backupCount: 5);
            }
            catch (FileNotFoundException)
            {
                missingCurrentRejected = true;
            }
            Assert(
                missingCurrentRejected &&
                !EquipmentSlotNativeCommitFingerprint
                    .Equivalent(string.Empty, string.Empty),
                "A missing native current archive must never produce or equal commit authority.");
            File.WriteAllBytes(currentPath, bytes);

            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("shield");
            List<EquipmentSlotStorageEntry> working =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            working[0].ShieldValue = 3;
            EquipmentSlotGameplayCandidateCoordinator
                .Prepare(
                    document,
                    working,
                    before,
                    item => Observation(
                        before,
                        0,
                        0));
            Assert(
                !string.Equals(
                    before,
                    after,
                    StringComparison.Ordinal) &&
                EquipmentSlotGameplayCandidateCoordinator
                    .DecideRecovery(
                        document,
                        after,
                        item => Observation(
                            after,
                            0,
                            0)).Action ==
                    EquipmentSlotGameplayRecoveryAction
                        .PromoteCommitted,
                "A native save rewrite with identical bytes must remain distinguishable across the SaveSaved crash gap.");
        }

        private static string FingerprintValue(
            string fingerprint,
            string label)
        {
            string marker = "|" + label + "=";
            int start = fingerprint.IndexOf(
                marker,
                StringComparison.Ordinal);
            if (start < 0)
            {
                throw new InvalidDataException(
                    "Fingerprint did not contain " + label + ".");
            }
            start += marker.Length;
            int end = fingerprint.IndexOf('|', start);
            return end < 0
                ? fingerprint.Substring(start)
                : fingerprint.Substring(start, end - start);
        }

        private static void UnknownJournalOriginFailsClosed()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("hat");
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareRecovery(
                        document,
                        new[] { 0 },
                        EquipmentSlotTransactionOrigin
                            .OwnerRecovery);
            journal.Origin =
                EquipmentSlotTransactionOrigin.Unknown;
            Assert(
                EquipmentSlotTransactionCoordinator
                    .DecideRecovery(
                        document,
                        "save-a",
                        item => Observation(
                            "save-a",
                            0,
                            0)).Action ==
                    EquipmentSlotRecoveryAction.FailClosed,
                "A legacy journal without a typed origin must remain Unknown and fail closed.");
        }

        private static void PreparedBeforeAttemptRetriesWithoutDuplication()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("hat");
            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                document,
                new[] { 0 });
            EquipmentSlotRecoveryDecision decision =
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:10:A",
                    _ => Observation("exists:10:A", 0, 0));
            Assert(
                decision.Action ==
                    EquipmentSlotRecoveryAction.RetryPlacement &&
                document.Journal?.Escrow.Count == 1 &&
                !document.Slots[0].IsOccupied,
                "Crash after prepared write must leave exactly one journal-owned logical item.");
        }

        private static void NativeSaveBeforePromotionRecoversExactDestination()
        {
            EquipmentSlotStorageDocument document =
                PrepareAttemptedSingle(
                    NativePlacementKind.Backpack,
                    1,
                    0);
            EquipmentSlotRecoveryDecision decision =
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:11:B",
                    _ => Observation("exists:11:B", 1, 0));
            Assert(
                decision.Action ==
                    EquipmentSlotRecoveryAction.FinalizeCommitted,
                "Crash after native save and before sidecar promotion must infer one exact native destination.");
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "exists:11:B");
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            Assert(
                document.Journal == null &&
                !document.Slots[0].IsOccupied,
                "Recovered native placement must clear escrow without restoring or duplicating the item.");
        }

        private static void CommittedTombstoneBeforeClearFinalizesOnce()
        {
            EquipmentSlotStorageDocument document =
                PrepareAttemptedSingle(
                    NativePlacementKind.Mail,
                    0,
                    1);
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "exists:11:B");
            EquipmentSlotRecoveryDecision decision =
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:11:B",
                    _ => Observation("exists:11:B", 0, 1));
            Assert(
                decision.Action ==
                    EquipmentSlotRecoveryAction.FinalizeCommitted &&
                document.Journal?.State ==
                    EquipmentSlotJournalState
                        .CommittedTombstone,
                "Crash after promotion and before tombstone clear must be idempotently finalizable.");
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            Assert(
                document.Journal == null,
                "Committed tombstone must clear exactly once.");
        }

        private static void AmbiguousOrExcessDestinationFailsClosed()
        {
            EquipmentSlotStorageDocument dual =
                PrepareAttemptedSingle(
                    NativePlacementKind.Backpack,
                    1,
                    0);
            EquipmentSlotStorageDocument excess =
                PrepareAttemptedSingle(
                    NativePlacementKind.Backpack,
                    1,
                    0);
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    dual,
                    "exists:11:B",
                    _ => Observation("exists:11:B", 1, 1))
                    .Action ==
                    EquipmentSlotRecoveryAction.FailClosed &&
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    excess,
                    "exists:11:B",
                    _ => Observation("exists:11:B", 2, 0))
                    .Action ==
                    EquipmentSlotRecoveryAction.FailClosed,
                "A single escrow appearing in both targets or with an excess delta must stay fail-closed.");
        }

        private static void SameItemSplitAcrossBackpackAndMailIsExact()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("hat");
            document.Slots[1].ItemId = "hat";
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator.PrepareRecovery(
                    document,
                    new[] { 0, 1 });
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "exists:10:A",
                _ => Observation("exists:10:A", 0, 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                Result(NativePlacementKind.Backpack),
                Observation("exists:10:A", 1, 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                1,
                Result(NativePlacementKind.Mail),
                Observation("exists:10:A", 1, 1));
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:11:B",
                    _ => Observation("exists:11:B", 1, 1))
                    .Action ==
                    EquipmentSlotRecoveryAction
                        .FinalizeCommitted,
                "Two identical items may validly split one exact item to backpack and one to mail.");
        }

        private static void ScopeMismatchFailsClosed()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("hat");
            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                document,
                new[] { 0 });
            document.Scope.PlayerName = "other-player";
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:10:A",
                    _ => Observation("exists:10:A", 0, 0))
                    .Action ==
                    EquipmentSlotRecoveryAction.FailClosed,
                "Archive/player scope mismatch must never consume another save's escrow.");
        }

        private static void ReplacementCommitsIncomingAndOutgoingTogether()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("old-hat");
            var replacement =
                new EquipmentSlotStorageEntry
                {
                    Index = 0,
                    ItemId = "new-passive",
                    SkillId = "passive"
                };
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        replacement);
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "exists:10:A",
                item => item == "new-passive"
                    ? Observation("exists:10:A", 1, 0)
                    : Observation("exists:10:A", 0, 0));
            EquipmentSlotTransactionCoordinator
                .RecordIncomingWithdrawal(journal, true, 0);
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                Result(NativePlacementKind.Backpack),
                Observation("exists:10:A", 1, 0));
            Assert(
                EquipmentSlotTransactionCoordinator
                    .CanWithdrawIncoming(journal),
                "Incoming replacement withdrawal must wait until every outgoing item has an exact native destination.");
            Func<string, NativeRecoveryObservation> observe =
                item => item == "new-passive"
                    ? Observation("exists:11:B", 0, 0)
                    : Observation("exists:11:B", 1, 0);
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:11:B",
                    observe).Action ==
                    EquipmentSlotRecoveryAction
                        .FinalizeCommitted,
                "Replacement must prove the incoming withdrawal and outgoing native placement together.");
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "exists:11:B");
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            Assert(
                document.Slots[0].ItemId == "new-passive" &&
                document.Journal == null,
                "Compound replacement must end with the new product item and no outgoing escrow.");
        }

        private static void ReplacementOutgoingFailurePreservesIncoming()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("old-hat");
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        Replacement("new-passive"));
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "exists:10:A",
                item => item == "new-passive"
                    ? Observation("exists:10:A", 1, 0)
                    : Observation("exists:10:A", 0, 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                Result(NativePlacementKind.Failure),
                Observation("exists:10:A", 0, 0));
            Assert(
                !EquipmentSlotTransactionCoordinator
                    .CanWithdrawIncoming(journal),
                "A failed outgoing placement must block incoming withdrawal.");
            EquipmentSlotTransactionCoordinator
                .RecordIncomingWithdrawal(journal, false, 1);

            Func<string, NativeRecoveryObservation> observe =
                item => item == "new-passive"
                    ? Observation("exists:11:B", 1, 0)
                    : Observation("exists:11:B", 0, 0);
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:11:B",
                    observe).Action ==
                    EquipmentSlotRecoveryAction
                        .FinalizeCommitted,
                "A saved failed-outgoing attempt must have an exact unchanged incoming item and journal-owned outgoing item.");
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "exists:11:B");
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            Assert(
                document.Slots[0].ItemId == "old-hat" &&
                document.Journal == null &&
                observe("new-passive").BackpackCount == 1 &&
                observe("old-hat").BackpackCount == 0,
                "Outgoing failure must restore the old sidecar item while leaving exactly one incoming item in native storage.");
        }

        private static void ReplacementIncomingFailurePreservesBothItems()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("old-hat");
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        Replacement("new-passive"));
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "exists:10:A",
                item => item == "new-passive"
                    ? Observation("exists:10:A", 1, 0)
                    : Observation("exists:10:A", 0, 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                Result(NativePlacementKind.Backpack),
                Observation("exists:10:A", 1, 0));
            Assert(
                EquipmentSlotTransactionCoordinator
                    .CanWithdrawIncoming(journal),
                "An exact outgoing destination must permit the incoming withdrawal attempt.");
            EquipmentSlotTransactionCoordinator
                .RecordIncomingWithdrawal(journal, false, 1);

            Func<string, NativeRecoveryObservation> observe =
                item => item == "new-passive"
                    ? Observation("exists:11:B", 1, 0)
                    : Observation("exists:11:B", 1, 0);
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:11:B",
                    observe).Action ==
                    EquipmentSlotRecoveryAction
                        .FinalizeCommitted,
                "Incoming failure with an exact outgoing destination must remain recoverable without losing either item.");
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "exists:11:B");
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            Assert(
                !document.Slots[0].IsOccupied &&
                document.Journal == null &&
                observe("new-passive").BackpackCount == 1 &&
                observe("old-hat").BackpackCount == 1,
                "Incoming failure must leave one incoming and one outgoing item in native storage.");
        }

        private static void ReplacementCrashWindowsPreserveExactOwnership()
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("old-hat");
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        Replacement("new-passive"));
            Assert(
                !document.Slots[0].IsOccupied &&
                journal.Escrow.Count == 1 &&
                journal.Replacements.Count == 1,
                "Prepared replacement must durably own exactly one outgoing item and identify one still-native incoming item.");

            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "exists:10:A",
                item => item == "new-passive"
                    ? Observation("exists:10:A", 1, 0)
                    : Observation("exists:10:A", 0, 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                Result(NativePlacementKind.Mail),
                Observation("exists:10:A", 0, 1));
            EquipmentSlotTransactionCoordinator
                .RecordIncomingWithdrawal(journal, true, 0);
            Func<string, NativeRecoveryObservation> observe =
                item => item == "new-passive"
                    ? Observation("exists:11:B", 0, 0)
                    : Observation("exists:11:B", 0, 1);
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    document,
                    "exists:11:B",
                    observe).Action ==
                    EquipmentSlotRecoveryAction
                        .FinalizeCommitted &&
                journal.Escrow[0].Placement ==
                    NativePlacementKind.Mail &&
                journal.Replacements.Count == 1,
                "After native save and before sidecar promotion, outgoing mail and journal-owned incoming must each have one exact owner.");

            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "exists:11:B");
            Assert(
                document.Slots[0].ItemId == "new-passive" &&
                journal.State ==
                    EquipmentSlotJournalState
                        .CommittedTombstone,
                "Committed tombstone must project the incoming item into the sidecar while retaining only destination evidence.");
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            Assert(
                document.Slots[0].ItemId == "new-passive" &&
                document.Journal == null &&
                observe("old-hat").MailCount == 1,
                "Finalization must leave exactly one incoming sidecar item and one outgoing native-mail item.");
        }

        private static void
            SameItemReplacementEvidenceCoversAllCrashWindows()
        {
            foreach (SameItemReplacementScenario scenario in
                new[]
                {
                    CreateSameItemReplacementScenario(
                        NativePlacementKind.Backpack,
                        fromNativeBuffer: false),
                    CreateSameItemReplacementScenario(
                        NativePlacementKind.Mail,
                        fromNativeBuffer: false),
                    CreateSameItemReplacementScenario(
                        NativePlacementKind.Backpack,
                        fromNativeBuffer: true)
                })
            {
                Assert(
                    EquipmentSlotTransactionCoordinator
                        .DecideRecovery(
                            scenario.Document,
                            "save-a",
                            _ => scenario.Before)
                        .Action ==
                        EquipmentSlotRecoveryAction
                            .RetryPlacement,
                    "A same-item replacement interrupted before native commit must retry from the durable prepared journal without treating in-memory deltas as saved.");
                Assert(
                    EquipmentSlotTransactionCoordinator
                        .DecideRecovery(
                            scenario.Document,
                            "save-b",
                            _ => scenario.After)
                        .Action ==
                        EquipmentSlotRecoveryAction
                            .FinalizeCommitted,
                    "A same-item replacement must recognize its exact net native delta after native save, including backpack, mail, and native-buffer withdrawal paths.");

                EquipmentSlotTransactionCoordinator
                    .PromoteCommittedTombstone(
                        scenario.Document,
                        "save-b");
                Assert(
                    scenario.Document.Slots[0].ItemId ==
                        "same-item" &&
                    scenario.Journal.State ==
                        EquipmentSlotJournalState
                            .CommittedTombstone &&
                    EquipmentSlotTransactionCoordinator
                        .DecideRecovery(
                            scenario.Document,
                            "save-b",
                            _ => scenario.After)
                        .Action ==
                        EquipmentSlotRecoveryAction
                            .FinalizeCommitted,
                    "A same-item committed tombstone must preserve one replacement sidecar owner and exact native destination evidence.");
            }

            SameItemReplacementScenario ambiguous =
                CreateSameItemReplacementScenario(
                    NativePlacementKind.Backpack,
                    fromNativeBuffer: false);
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    ambiguous.Document,
                    "save-b",
                    _ => Observation(
                        "save-b",
                        backpack: 0,
                        mail: 0)).Action ==
                    EquipmentSlotRecoveryAction.FailClosed &&
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    ambiguous.Document,
                    "save-b",
                    _ => Observation(
                        "save-b",
                        backpack: 2,
                        mail: 0)).Action ==
                    EquipmentSlotRecoveryAction.FailClosed,
                "Missing or excess same-item native counts must never be accepted as the net-zero replacement commit.");

            SameItemReplacementScenario mismatchedBaseline =
                CreateSameItemReplacementScenario(
                    NativePlacementKind.Backpack,
                    fromNativeBuffer: false);
            mismatchedBaseline.Journal
                .IncomingBeforeBackpackCount++;
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    mismatchedBaseline.Document,
                    "save-b",
                    _ => mismatchedBaseline.After).Action ==
                    EquipmentSlotRecoveryAction.FailClosed,
                "A same-item replacement whose incoming and escrow baselines disagree must fail closed.");
        }

        private static void DurableStorePreservesLiveAndJournalOnFailure()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Equipment-slot store fixtures require the managed DTMAPI test session.");
            string root = Path.Combine(
                sessionRoot,
                "moreequipment-store");
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "sidecar.json");
            var store = new EquipmentSlotDocumentStore();
            EquipmentSlotStorageDocument live =
                CreateOccupiedDocument("hat");
            live.Generation = 1;
            store.WriteAtomic(path, live);
            byte[] before = File.ReadAllBytes(path);

            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                live,
                new[] { 0 });
            live.Generation = 2;
            live.Scope.PlayerName = "wrong-scope";
            bool failed = false;
            try
            {
                store.WriteAtomic(path, live);
            }
            catch (InvalidDataException)
            {
                failed = true;
            }
            byte[] after = File.ReadAllBytes(path);
            Assert(
                failed &&
                EqualBytes(before, after) &&
                live.Journal?.Escrow.Count == 1,
                "Atomic write failure must leave the live sidecar byte-identical and retain dirty journal escrow.");
        }

        private static void
            GameplayCandidateStoreIsDurableAndFailClosed()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Gameplay candidate store fixtures require the managed DTMAPI test session.");
            string root = Path.Combine(
                sessionRoot,
                "moreequipment-gameplay-candidate-store");
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "sidecar.json");
            var store = new EquipmentSlotDocumentStore();
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("shield");
            document.Slots[0].IsShield = true;
            document.Slots[0].ShieldValue = 5;
            document.Slots[0].ShieldMaxValue = 5;
            store.WriteAtomic(path, document);

            List<EquipmentSlotStorageEntry> working =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            working[0].ShieldValue = 2;
            EquipmentSlotGameplayCandidateCoordinator.Prepare(
                document,
                working,
                "save-a",
                item => Observation("save-a", 0, 0));
            document.Generation = 2;
            store.WriteAtomic(path, document);
            EquipmentSlotStorageDocument roundTrip =
                store.Load(path, Scope());
            Assert(
                roundTrip.Slots[0].ShieldValue == 5 &&
                roundTrip.GameplayCandidate?.WorkingSlots[0]
                    .ShieldValue == 2 &&
                roundTrip.GameplayCandidate.Origin ==
                    EquipmentSlotTransactionOrigin
                        .GameplayMutation,
                "The atomic store must round-trip committed and Working candidate projections separately.");

            byte[] before = File.ReadAllBytes(path);
            roundTrip.Scope.PlayerName = "wrong-scope";
            roundTrip.Generation++;
            bool failed = false;
            try
            {
                store.WriteAtomic(path, roundTrip);
            }
            catch (InvalidDataException)
            {
                failed = true;
            }
            Assert(
                failed &&
                EqualBytes(before, File.ReadAllBytes(path)),
                "A candidate write failure must leave the last committed/candidate authority byte-identical.");
        }

        private static void
            LegacyFlatSchemasAndHistoricalHostShapeMigrateExactly()
        {
            string root = CreateMigrationRoot("flat-generations");
            foreach (int schema in new[] { 1, 2, 3 })
            {
                string path = Path.Combine(
                    root,
                    "schema-" + schema + ".json");
                File.WriteAllText(
                    path,
                    LegacyFlatJson(schema));
                byte[] legacyBytes = File.ReadAllBytes(path);
                var store = new EquipmentSlotDocumentStore();
                EquipmentSlotStorageDocument migrated =
                    store.LoadOrMigrate(
                        path,
                        Path.Combine(root, "missing-global.json"),
                        Scope(),
                        out string message);
                Assert(
                    migrated.SchemaVersion == 3 &&
                    migrated.Generation == 8 &&
                    migrated.Scope.ArchiveIndex == 2 &&
                    migrated.Scope.PlayerName == "third-save" &&
                    migrated.Scope.CustomPlayerName ==
                        "third-save" &&
                    migrated.Scope.TotalGameSeconds == 100 &&
                    migrated.Slots.Count == 3 &&
                    migrated.Slots[0].ItemId == "passive-hat" &&
                    migrated.Slots[0].DisplayName == "Passive" &&
                    migrated.Slots[0].SkillId == "passive-skill" &&
                    migrated.Slots[0].DefenseBonus == 0 &&
                    !migrated.Slots[0].IsShield &&
                    migrated.Slots[0].ShieldValue == 0 &&
                    migrated.Slots[0].ShieldMaxValue == 0 &&
                    migrated.Slots[0].ShieldDefend == 0 &&
                    migrated.Slots[1].ItemId == "defense-hat" &&
                    migrated.Slots[1].DisplayName == "Defense" &&
                    migrated.Slots[1].SkillId == string.Empty &&
                    migrated.Slots[1].DefenseBonus == 2 &&
                    !migrated.Slots[1].IsShield &&
                    migrated.Slots[1].ShieldValue == 0 &&
                    migrated.Slots[1].ShieldMaxValue == 0 &&
                    migrated.Slots[1].ShieldDefend == 0 &&
                    migrated.Slots[2].ItemId == "shield-hat" &&
                    migrated.Slots[2].DisplayName == "Shield" &&
                    migrated.Slots[2].SkillId == string.Empty &&
                    migrated.Slots[2].DefenseBonus == 0 &&
                    migrated.Slots[2].IsShield &&
                    migrated.Slots[2].ShieldValue == 7 &&
                    migrated.Slots[2].ShieldMaxValue == 10 &&
                    migrated.Slots[2].ShieldDefend == 3,
                    "Legacy schema " + schema +
                    " must preserve all three items and map isShieldHat to Product isShield.");
                Assert(
                    migrated.LegacyMigration?.SourceSchema == schema &&
                    migrated.LegacyMigration.SourceKind ==
                        EquipmentSlotLegacyMigrationSourceKinds
                            .ScopedFlat &&
                    EquipmentSlotStorageFormatClassifier.Probe(path)
                        .Format ==
                        EquipmentSlotStorageFormat.ProductV3 &&
                    message.Contains(
                        "Migrated scoped flat schema",
                        StringComparison.Ordinal),
                    "Legacy schema " + schema +
                    " must publish one classified Product v3 authority with exact migration provenance.");
                string backupPath =
                    EquipmentSlotLegacyMigration.GetLegacyBackupPath(
                        path,
                        migrated.LegacyMigration!.SourceSha256);
                Assert(
                    File.Exists(backupPath) &&
                    string.Equals(
                        Path.GetFileName(backupPath),
                        migrated.LegacyMigration.SourceSha256 +
                            ".flat.json",
                        StringComparison.Ordinal) &&
                    EqualBytes(
                        legacyBytes,
                        File.ReadAllBytes(backupPath)),
                    "Legacy schema " + schema +
                    " must retain one exact, short-named non-authoritative source backup before replacement.");
                EquipmentSlotStorageDocument restarted =
                    store.LoadOrMigrate(
                        path,
                        Path.Combine(root, "missing-global.json"),
                        Scope(),
                        out string restartMessage);
                Assert(
                    restarted.Generation == 8 &&
                    restarted.Slots[0].ItemId == "passive-hat" &&
                    restarted.Slots[1].ItemId == "defense-hat" &&
                    restarted.Slots[2].ItemId == "shield-hat" &&
                    restarted.Slots[2].ShieldValue == 7 &&
                    restarted.Slots[2].ShieldMaxValue == 10 &&
                    restarted.Slots[2].ShieldDefend == 3 &&
                    string.IsNullOrEmpty(restartMessage),
                    "A migrated Product v3 sidecar must reload idempotently without re-running legacy conversion.");
            }

            string realHostPath =
                Path.Combine(root, "real-host-schema-3.json");
            File.WriteAllText(
                realHostPath,
                RealHostFlatJson(3));
            byte[] realHostBytes =
                File.ReadAllBytes(realHostPath);
            var realHostStore =
                new EquipmentSlotDocumentStore();
            EquipmentSlotStorageDocument realHostMigrated =
                realHostStore.LoadOrMigrate(
                    realHostPath,
                    Path.Combine(root, "missing-global.json"),
                    Scope(),
                    out string realHostMessage);
            Assert(
                realHostMigrated.SchemaVersion == 3 &&
                realHostMigrated.Generation == 2 &&
                realHostMigrated.Scope.ArchiveIndex == 2 &&
                realHostMigrated.Scope.TotalGameSeconds == 100 &&
                realHostMigrated.Slots.Count == 3 &&
                realHostMigrated.Slots[0].ItemId ==
                    "grandmas_button" &&
                realHostMigrated.Slots[0].DisplayName ==
                    "grandmas_button" &&
                realHostMigrated.Slots[0].SkillId == string.Empty &&
                realHostMigrated.Slots[0].DefenseBonus == 0 &&
                !realHostMigrated.Slots[0].IsShield &&
                realHostMigrated.Slots[1].ItemId ==
                    "box_hat" &&
                realHostMigrated.Slots[1].DisplayName ==
                    "box_hat" &&
                realHostMigrated.Slots[1].SkillId == string.Empty &&
                realHostMigrated.Slots[1].DefenseBonus == 0 &&
                realHostMigrated.Slots[1].IsShield &&
                realHostMigrated.Slots[1].ShieldValue == 80 &&
                realHostMigrated.Slots[1].ShieldMaxValue == 100 &&
                realHostMigrated.Slots[1].ShieldDefend == 3 &&
                realHostMigrated.Slots[2].ItemId == string.Empty &&
                realHostMigrated.Slots[2].DisplayName ==
                    string.Empty &&
                realHostMigrated.Slots[2].SkillId == string.Empty &&
                realHostMigrated.Slots[2].DefenseBonus == 0 &&
                !realHostMigrated.Slots[2].IsShield &&
                realHostMigrated.Slots[2].ShieldValue == 0 &&
                realHostMigrated.Slots[2].ShieldMaxValue == 0 &&
                realHostMigrated.Slots[2].ShieldDefend == 0,
                "The full Compatibility Host flat shape, including its historical extra fields, must migrate without item or shield-trait loss.");
            Assert(
                realHostMigrated.LegacyMigration?.SourceSchema == 3 &&
                realHostMigrated.LegacyMigration.SourceKind ==
                    EquipmentSlotLegacyMigrationSourceKinds
                        .ScopedFlat &&
                EquipmentSlotStorageFormatClassifier
                    .Probe(realHostPath).Format ==
                    EquipmentSlotStorageFormat.ProductV3 &&
                realHostMessage.Contains(
                    "Migrated scoped flat schema",
                    StringComparison.Ordinal),
                "The historical Host fixture must publish one Product v3 authority with exact scoped-flat provenance.");
            string realHostBackupPath =
                EquipmentSlotLegacyMigration.GetLegacyBackupPath(
                    realHostPath,
                    realHostMigrated.LegacyMigration!.SourceSha256);
            Assert(
                File.Exists(realHostBackupPath) &&
                EqualBytes(
                    realHostBytes,
                    File.ReadAllBytes(realHostBackupPath)),
                "The historical Host fixture must retain its exact source bytes before Product v3 publication.");
            EquipmentSlotStorageDocument realHostRestarted =
                realHostStore.LoadOrMigrate(
                    realHostPath,
                    Path.Combine(root, "missing-global.json"),
                    Scope(),
                    out string realHostRestartMessage);
            Assert(
                realHostRestarted.Generation == 2 &&
                realHostRestarted.Slots[0].ItemId ==
                    "grandmas_button" &&
                realHostRestarted.Slots[1].ItemId ==
                    "box_hat" &&
                realHostRestarted.Slots[1].ShieldValue == 80 &&
                realHostRestarted.Slots[1].ShieldMaxValue == 100 &&
                realHostRestarted.Slots[1].ShieldDefend == 3 &&
                string.IsNullOrEmpty(realHostRestartMessage),
                "The migrated historical Host fixture must reload idempotently without a second conversion.");
        }

        private static void LegacyPreSchemaGlobalIsClaimedExactlyOnce()
        {
            string root = CreateMigrationRoot("pre-schema-global");
            string globalPath = Path.Combine(
                root,
                "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string firstProductPath = Path.Combine(
                root,
                "slot-2",
                "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string json = PreSchemaGlobalJson();
            File.WriteAllText(globalPath, json);
            byte[] sourceBytes = File.ReadAllBytes(globalPath);
            foreach (
                (string name, string malformed) fixture in
                new[]
                {
                    (
                        "unknown-top.json",
                        json.Replace(
                            "\"savedAt\":",
                            "\"unexpected\":true,\"savedAt\":",
                            StringComparison.Ordinal)),
                    (
                        "unknown-slot.json",
                        json.Replace(
                            "\"index\":0,",
                            "\"unexpected\":true,\"index\":0,",
                            StringComparison.Ordinal)),
                    (
                        "owner-case.json",
                        json.Replace(
                            "DTMAPI.MoreEquipmentSlotsMod",
                            "dtmapi.moreequipmentslotsmod",
                            StringComparison.Ordinal))
                })
            {
                string malformedPath =
                    Path.Combine(root, fixture.name);
                File.WriteAllText(
                    malformedPath,
                    fixture.malformed);
                Assert(
                    EquipmentSlotStorageFormatClassifier
                        .Probe(malformedPath).Format !=
                        EquipmentSlotStorageFormat
                            .PreSchemaGlobal,
                    fixture.name +
                    " must not be broadened into the exact historical pre-schema generation.");
            }
            Assert(
                EquipmentSlotStorageFormatClassifier
                    .Probe(globalPath).Format ==
                    EquipmentSlotStorageFormat.PreSchemaGlobal,
                "The exact historical ownerId/savedAt/slots document must be classified separately from arbitrary schema zero JSON.");

            EquipmentSlotStorageDocument migrated =
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    firstProductPath,
                    globalPath,
                    Scope(),
                    out string message);
            Assert(
                migrated.SchemaVersion == 3 &&
                migrated.Generation == 1 &&
                migrated.Slots.Count == 3 &&
                migrated.Slots[0].ItemId ==
                    "historical-button" &&
                migrated.Slots[0].DisplayName ==
                    "Historical Button" &&
                migrated.Slots[0].SkillId ==
                    "historical-skill" &&
                migrated.Slots[0].DefenseBonus == 0 &&
                !migrated.Slots[0].IsShield &&
                migrated.Slots[0].ShieldValue == 0 &&
                migrated.Slots[0].ShieldMaxValue == 0 &&
                migrated.Slots[0].ShieldDefend == 0 &&
                migrated.Slots[1].ItemId == string.Empty &&
                migrated.Slots[2].ItemId == string.Empty &&
                migrated.LegacyMigration?.SourceSchema == 0 &&
                migrated.LegacyMigration.SourceKind ==
                    EquipmentSlotLegacyMigrationSourceKinds
                        .PreSchemaGlobal &&
                migrated.Scope.TotalGameSeconds == 100 &&
                message.Contains(
                    "pre-schema global",
                    StringComparison.Ordinal),
                "The exact pre-schema global document must preserve its item fields and claim the current native save revision once.");
            EquipmentSlotLegacyMigrationStamp migrationStamp =
                migrated.LegacyMigration ??
                throw new InvalidOperationException(
                    "Pre-schema migration stamp was missing.");
            string archivePath =
                EquipmentSlotLegacyMigration.GetGlobalArchivePath(
                    globalPath,
                    migrationStamp.SourceSha256);
            string backupPath =
                EquipmentSlotLegacyMigration.GetLegacyBackupPath(
                    firstProductPath,
                    migrationStamp.SourceSha256);
            Assert(
                !File.Exists(globalPath) &&
                File.Exists(archivePath) &&
                File.Exists(backupPath) &&
                EqualBytes(
                    sourceBytes,
                    File.ReadAllBytes(archivePath)) &&
                EqualBytes(
                    sourceBytes,
                    File.ReadAllBytes(backupPath)),
                "Pre-schema adoption must preserve exact source bytes in the scoped backup and deterministic global claim archive.");

            File.WriteAllText(globalPath, json);
            string secondProductPath = Path.Combine(
                root,
                "slot-3",
                "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            bool secondClaimFailed = false;
            try
            {
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    secondProductPath,
                    globalPath,
                    new EquipmentSlotSaveScope
                    {
                        ArchiveIndex = 3,
                        PlayerName = "fourth-save",
                        CustomPlayerName = "fourth-save",
                        TotalGameSeconds = 100
                    },
                    out _);
            }
            catch (InvalidDataException)
            {
                secondClaimFailed = true;
            }
            Assert(
                secondClaimFailed &&
                !File.Exists(secondProductPath) &&
                File.Exists(globalPath),
                "A previously claimed pre-schema source hash must not be adopted by another save even if the global bytes reappear.");
        }

        private static void
            ConcurrentPreSchemaClaimPublicationIsSerialized()
        {
            string root =
                CreateMigrationRoot(
                    "pre-schema-concurrent-claim");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string firstProductPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string secondProductPath =
                Path.Combine(
                    root,
                    "slot-3",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            var secondScope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = 3,
                    PlayerName = "fourth-save",
                    CustomPlayerName = "fourth-save",
                    TotalGameSeconds = 100
                };

            using var secondEnumerated =
                new ManualResetEvent(false);
            using var releaseSecond =
                new ManualResetEvent(false);
            using var firstCompleted =
                new ManualResetEvent(false);
            Exception? firstFailure = null;
            Exception? secondFailure = null;

            var secondThread =
                new Thread(
                    () =>
                    {
                        try
                        {
                            new EquipmentSlotDocumentStore(
                                stage =>
                                {
                                    if (stage ==
                                        "after-preschema-claim-enumeration")
                                    {
                                        secondEnumerated.Set();
                                        if (!releaseSecond.WaitOne(
                                            TimeSpan.FromSeconds(5)))
                                        {
                                            throw new TimeoutException(
                                                "The concurrent claim fixture did not release the enumerated migration.");
                                        }
                                    }
                                    if (stage ==
                                        "after-preschema-claim-publish")
                                    {
                                        throw new IOException(
                                            "injected-second-claim-only");
                                    }
                                }).LoadOrMigrate(
                                    secondProductPath,
                                    globalPath,
                                    secondScope,
                                    out _);
                        }
                        catch (Exception ex)
                        {
                            secondFailure = ex;
                        }
                    });
            secondThread.Start();
            Assert(
                secondEnumerated.WaitOne(
                    TimeSpan.FromSeconds(5)),
                "The second save must reach the deterministic post-enumeration claim window.");

            var firstThread =
                new Thread(
                    () =>
                    {
                        try
                        {
                            new EquipmentSlotDocumentStore()
                                .LoadOrMigrate(
                                    firstProductPath,
                                    globalPath,
                                    Scope(),
                                    out _);
                        }
                        catch (Exception ex)
                        {
                            firstFailure = ex;
                        }
                        finally
                        {
                            firstCompleted.Set();
                        }
                    });
            firstThread.Start();
            bool firstStayedPending;
            try
            {
                firstStayedPending =
                    !firstCompleted.WaitOne(
                        TimeSpan.FromMilliseconds(100));
            }
            finally
            {
                releaseSecond.Set();
            }
            Assert(
                firstStayedPending,
                "A competing save must not pass claim enumeration/publication while another scope owns the claim critical section.");

            Assert(
                secondThread.Join(
                    TimeSpan.FromSeconds(5)) &&
                firstThread.Join(
                    TimeSpan.FromSeconds(5)),
                "Both deterministic claim race threads must terminate.");
            Assert(
                secondFailure is IOException &&
                firstFailure is InvalidDataException &&
                !File.Exists(firstProductPath) &&
                !File.Exists(secondProductPath),
                "The enumerating save may publish only its pending claim; the competing scope must fail before either Product authority is published.");

            EquipmentSlotStorageDocument resumed =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        secondProductPath,
                        globalPath,
                        secondScope,
                        out _);
            Assert(
                resumed.Scope.Matches(secondScope.Clone()) &&
                File.Exists(secondProductPath) &&
                !File.Exists(firstProductPath) &&
                !File.Exists(globalPath),
                "Only the scope that won the serialized claim publication may resume and publish Product authority.");
        }

        private static void CrossProcessLateClaimIsWithdrawn()
        {
            string root =
                CreateMigrationRoot(
                    "pre-schema-cross-process-late-claim");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string winningProductPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string lateProductPath =
                Path.Combine(
                    root,
                    "slot-3",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string readyPath =
                Path.Combine(
                    root,
                    "late-publisher.ready");
            string releasePath =
                Path.Combine(
                    root,
                    "late-publisher.release");
            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            string sourceSha256 =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(globalPath);
            string claimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        sourceSha256);
            string winnerPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        globalPath);

            string executable =
                Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    "The cross-process claim fixture could not resolve the current Unit executable.");
            var start =
                new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(
                        executable),
                    "dotnet",
                    StringComparison.OrdinalIgnoreCase))
            {
                start.ArgumentList.Add(
                    typeof(MoreEquipmentSlotsProductTests)
                        .Assembly
                        .Location);
            }
            start.Environment[
                "DTMAPI_UNIT_TEST_FOCUS"] =
                "moreequipment-claim-process";
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_GLOBAL"] =
                globalPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_PRODUCT"] =
                lateProductPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_WINNER"] =
                winningProductPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_READY"] =
                readyPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_RELEASE"] =
                releasePath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_CRASH_AFTER_PUBLISH"] =
                "1";

            using Process lateProcess =
                Process.Start(start)
                ?? throw new InvalidOperationException(
                    "The cross-process late claim fixture could not start.");
            try
            {
                WaitForFile(
                    readyPath,
                    "The late claim process did not reach the post-validation pre-publication window.");

                EquipmentSlotStorageDocument winning =
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            winningProductPath,
                            globalPath,
                            Scope(),
                            out _);
                Assert(
                    winning.Scope.Matches(
                        Scope().Clone()) &&
                    File.Exists(winningProductPath) &&
                    !File.Exists(globalPath) &&
                    IsCompletedClaim(claimPath),
                    "The winning process must publish Product, archive global, and durably retain its completed claim before releasing the late publisher.");
                File.Delete(
                    claimPath);
                EquipmentSlotStorageDocument historicalWinner =
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            winningProductPath,
                            globalPath,
                            Scope(),
                            out _);
                Assert(
                    historicalWinner.Scope.Matches(
                        Scope().Clone()) &&
                    IsCompletedClaim(claimPath),
                    "A historical Product plus exact archive with no claim must atomically backfill its durable completed tombstone before a paused loser is released.");
                File.Delete(claimPath);
                Assert(
                    IsCompletedClaim(winnerPath) &&
                    !File.Exists(claimPath),
                    "The real cross-process loser must be released with only the permanent winner present, so optional per-hash evidence cannot substitute for winner enforcement.");

                File.WriteAllText(
                    releasePath,
                    "release");
                if (!lateProcess.WaitForExit(
                    20000))
                {
                    throw new TimeoutException(
                        "The cross-process late claim fixture did not exit.");
                }
                string output =
                    lateProcess.StandardOutput.ReadToEnd();
                string error =
                    lateProcess.StandardError.ReadToEnd();
                Assert(
                    lateProcess.ExitCode == 0 &&
                    output.Contains(
                        "DTMAPI.UnitTests: OK (moreequipment-claim-process)",
                        StringComparison.Ordinal) &&
                    output.Contains(
                        "DTMAPI_MORE_EQUIPMENT_CLAIM_REJECTION=permanent-winner",
                        StringComparison.Ordinal) &&
                    File.Exists(winningProductPath) &&
                    !File.Exists(lateProductPath) &&
                    !File.Exists(globalPath) &&
                    IsCompletedClaim(winnerPath) &&
                    !File.Exists(claimPath),
                    "The cross-process loser must be rejected before its post-publish crash point while only the permanent winner remains. exit=" +
                    lateProcess.ExitCode +
                    ", output=" +
                    output +
                    ", error=" +
                    error);
            }
            finally
            {
                if (!File.Exists(releasePath))
                    File.WriteAllText(releasePath, "release");
                if (!lateProcess.HasExited)
                {
                    try
                    {
                        lateProcess.Kill(
                            entireProcessTree: true);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void
            PreSchemaClaimPublicationWindowsFailClosed()
        {
            var otherScope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = 3,
                    PlayerName = "fourth-save",
                    CustomPlayerName = "fourth-save",
                    TotalGameSeconds = 100
                };

            string claimRoot =
                CreateMigrationRoot(
                    "pre-schema-claim-publication");
            string claimGlobal =
                Path.Combine(
                    claimRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string claimProduct =
                Path.Combine(
                    claimRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string otherProduct =
                Path.Combine(
                    claimRoot,
                    "slot-3",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                claimGlobal,
                PreSchemaGlobalJson());
            byte[] claimSource =
                File.ReadAllBytes(claimGlobal);
            string claimHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(claimGlobal);
            string claimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        claimGlobal,
                        claimHash);
            bool stoppedAfterClaim = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-claim-publish")
                        {
                            throw new IOException(
                                "injected-after-preschema-claim-publish");
                        }
                    }).LoadOrMigrate(
                        claimProduct,
                        claimGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
                stoppedAfterClaim = true;
            }
            Assert(
                stoppedAfterClaim &&
                File.Exists(claimPath) &&
                !File.Exists(claimProduct) &&
                File.Exists(claimGlobal) &&
                EqualBytes(
                    claimSource,
                    File.ReadAllBytes(claimGlobal)),
                "A stop immediately after claim publication must leave the exact source and one durable claim without exposing Product authority.");
            string legacyPendingClaim =
                File.ReadAllText(claimPath)
                    .Replace(
                        ",\"state\":\"pending\"",
                        string.Empty,
                        StringComparison.Ordinal);
            Assert(
                !legacyPendingClaim.Contains(
                    "\"state\"",
                    StringComparison.Ordinal),
                "The compatibility fixture must remove the new optional state field from the historical schema-1 pending claim.");
            File.WriteAllText(
                claimPath,
                legacyPendingClaim);

            bool otherSaveBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        otherProduct,
                        claimGlobal,
                        otherScope,
                        out _);
            }
            catch (InvalidDataException)
            {
                otherSaveBlocked = true;
            }
            Assert(
                otherSaveBlocked &&
                !File.Exists(otherProduct) &&
                File.Exists(claimPath),
                "Another save must fail closed while the first save owns a claim-only interrupted migration.");

            new EquipmentSlotDocumentStore()
                .LoadOrMigrate(
                    claimProduct,
                    claimGlobal,
                    Scope(),
                    out _);
            Assert(
                File.Exists(claimProduct) &&
                !File.Exists(claimGlobal) &&
                IsCompletedClaim(claimPath),
                "The original save must resume through Product publication, exact global archival and a durable completed claim.");

            string corruptRoot =
                CreateMigrationRoot(
                    "pre-schema-corrupt-claim");
            string corruptGlobal =
                Path.Combine(
                    corruptRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string corruptProduct =
                Path.Combine(
                    corruptRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                corruptGlobal,
                PreSchemaGlobalJson());
            byte[] corruptSource =
                File.ReadAllBytes(corruptGlobal);
            string corruptHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(corruptGlobal);
            string corruptClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        corruptGlobal,
                        corruptHash);
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-claim-publish")
                        {
                            throw new IOException(
                                "injected-corrupt-claim");
                        }
                    }).LoadOrMigrate(
                        corruptProduct,
                        corruptGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
            }
            File.WriteAllText(
                corruptClaim,
                "{broken");
            byte[] corruptEvidence =
                File.ReadAllBytes(corruptClaim);
            EquipmentSlotStorageDocument
                corruptClaimResumed =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        corruptProduct,
                        corruptGlobal,
                        Scope(),
                        out _);
            Assert(
                corruptClaimResumed.Scope.Matches(
                    Scope().Clone()) &&
                File.Exists(corruptProduct) &&
                !File.Exists(corruptGlobal) &&
                IsCompletedClaim(
                    EquipmentSlotLegacyMigration
                        .GetPreSchemaGlobalWinnerPath(
                            corruptGlobal)) &&
                EqualBytes(
                    corruptEvidence,
                    File.ReadAllBytes(corruptClaim)),
                "A valid pending winner must remain authoritative when optional same-hash evidence is corrupt; the evidence bytes stay retained instead of being adopted or overwritten.");

            string driftRoot =
                CreateMigrationRoot(
                    "pre-schema-claim-source-drift");
            string driftGlobal =
                Path.Combine(
                    driftRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string driftProduct =
                Path.Combine(
                    driftRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                driftGlobal,
                PreSchemaGlobalJson());
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-claim-publish")
                        {
                            throw new IOException(
                                "injected-drift-claim");
                        }
                    }).LoadOrMigrate(
                        driftProduct,
                        driftGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
            }
            File.AppendAllText(
                driftGlobal,
                " ");
            byte[] driftedSource =
                File.ReadAllBytes(driftGlobal);
            bool driftBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        driftProduct,
                        driftGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                driftBlocked = true;
            }
            Assert(
                driftBlocked &&
                !File.Exists(driftProduct) &&
                EqualBytes(
                    driftedSource,
                    File.ReadAllBytes(driftGlobal)),
                "Source drift after claim publication must retain the drifted bytes and require explicit reconciliation.");

            string archiveRoot =
                CreateMigrationRoot(
                    "pre-schema-conflicting-archive");
            string archiveGlobal =
                Path.Combine(
                    archiveRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string archiveProduct =
                Path.Combine(
                    archiveRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                archiveGlobal,
                PreSchemaGlobalJson());
            byte[] archiveSource =
                File.ReadAllBytes(archiveGlobal);
            string archiveHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(archiveGlobal);
            File.WriteAllText(
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        archiveGlobal,
                        archiveHash),
                "different-bytes");
            bool archiveConflictBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        archiveProduct,
                        archiveGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                archiveConflictBlocked = true;
            }
            Assert(
                archiveConflictBlocked &&
                !File.Exists(archiveProduct) &&
                EqualBytes(
                    archiveSource,
                    File.ReadAllBytes(archiveGlobal)),
                "A conflicting deterministic archive must block claim publication without changing the active source.");
        }

        private static void
            PreSchemaPendingClaimBlocksCrossSaveCrashOrder()
        {
            string root =
                CreateMigrationRoot(
                    "pre-schema-cross-save-crash");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string firstProductPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string secondProductPath =
                Path.Combine(
                    root,
                    "slot-3",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            byte[] globalBefore =
                File.ReadAllBytes(globalPath);
            string sourceHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(globalPath);
            string claimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        sourceHash);
            bool firstLoadInterrupted = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage == "after-product-publish")
                        {
                            throw new IOException(
                                "injected-after-product-publish");
                        }
                    }).LoadOrMigrate(
                        firstProductPath,
                        globalPath,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
                firstLoadInterrupted = true;
            }
            Assert(
                firstLoadInterrupted &&
                File.Exists(firstProductPath) &&
                File.Exists(globalPath) &&
                File.Exists(claimPath) &&
                EqualBytes(
                    globalBefore,
                    File.ReadAllBytes(globalPath)),
                "Publishing Product before global archival must retain one durable game-root pending claim bound to the first save.");

            var otherScope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = 3,
                    PlayerName = "fourth-save",
                    CustomPlayerName = "fourth-save",
                    TotalGameSeconds = 100
                };
            bool otherSaveBlockedByClaim = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        secondProductPath,
                        globalPath,
                        otherScope,
                        out _);
            }
            catch (InvalidDataException)
            {
                otherSaveBlockedByClaim = true;
            }
            Assert(
                otherSaveBlockedByClaim &&
                !File.Exists(secondProductPath) &&
                File.Exists(firstProductPath) &&
                EqualBytes(
                    globalBefore,
                    File.ReadAllBytes(globalPath)),
                "A different save loaded before the original restart must not adopt a pre-schema global source with a pending first-save claim.");

            File.Delete(claimPath);
            bool legacyInterruptedStateBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        secondProductPath,
                        globalPath,
                        otherScope,
                        out _);
            }
            catch (InvalidDataException)
            {
                legacyInterruptedStateBlocked = true;
            }
            Assert(
                legacyInterruptedStateBlocked &&
                !File.Exists(secondProductPath) &&
                File.Exists(firstProductPath),
                "A pre-fix interrupted Product authority without a claim file must still block another save by its exact migration stamp.");

            EquipmentSlotStorageDocument resumed =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProductPath,
                        globalPath,
                        Scope(),
                        out string resumeMessage);
            string archivePath =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        globalPath,
                        sourceHash);
            Assert(
                resumed.Slots[0].ItemId ==
                    "historical-button" &&
                !File.Exists(globalPath) &&
                File.Exists(archivePath) &&
                IsCompletedClaim(claimPath) &&
                EqualBytes(
                    globalBefore,
                    File.ReadAllBytes(archivePath)) &&
                resumeMessage.Contains(
                    "archived",
                    StringComparison.Ordinal),
                "Only the original save may resume, archive the exact global bytes, and publish its completed claim.");

            string afterArchiveRoot =
                CreateMigrationRoot(
                    "pre-schema-after-archive-crash");
            string afterArchiveGlobal =
                Path.Combine(
                    afterArchiveRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string afterArchiveProduct =
                Path.Combine(
                    afterArchiveRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                afterArchiveGlobal,
                PreSchemaGlobalJson());
            string afterArchiveHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(afterArchiveGlobal);
            string afterArchiveClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        afterArchiveGlobal,
                        afterArchiveHash);
            bool afterArchiveInterrupted = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage == "after-global-archive")
                        {
                            throw new IOException(
                                "injected-after-global-archive");
                        }
                    }).LoadOrMigrate(
                        afterArchiveProduct,
                        afterArchiveGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
                afterArchiveInterrupted = true;
            }
            Assert(
                afterArchiveInterrupted &&
                !File.Exists(afterArchiveGlobal) &&
                File.Exists(afterArchiveClaim),
                "A crash after archive publication must retain the pending claim until the original Product verifies the completed archive.");
            bool completedPublishInterrupted = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-completed-claim-publish")
                        {
                            throw new IOException(
                                "injected-after-completed-claim-publish");
                        }
                    }).LoadOrMigrate(
                        afterArchiveProduct,
                        afterArchiveGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
                completedPublishInterrupted = true;
            }
            Assert(
                completedPublishInterrupted &&
                IsCompletedClaim(afterArchiveClaim),
                "A crash after completed-claim publication must leave the winner's durable completed identity.");
            new EquipmentSlotDocumentStore()
                .LoadOrMigrate(
                    afterArchiveProduct,
                    afterArchiveGlobal,
                    Scope(),
                    out _);
            Assert(
                IsCompletedClaim(afterArchiveClaim),
                "The original save restart must verify and retain the completed claim without another migration.");
        }

        private static void
            PendingClaimBlocksEmptyAuthorityCreation()
        {
            string missingRoot =
                CreateMigrationRoot(
                    "pre-schema-claim-source-product-missing");
            string missingGlobal =
                Path.Combine(
                    missingRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string missingProduct =
                Path.Combine(
                    missingRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                missingGlobal,
                PreSchemaGlobalJson());
            CreateClaimOnlyInterruption(
                missingGlobal,
                missingProduct,
                Scope());
            File.Delete(missingGlobal);
            bool missingBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        missingProduct,
                        missingGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                missingBlocked = true;
            }
            Assert(
                missingBlocked &&
                !File.Exists(missingProduct),
                "A pending claim with both source and Product absent must block empty Product state creation.");

            string archivedRoot =
                CreateMigrationRoot(
                    "pre-schema-claim-archive-product-missing");
            string archivedGlobal =
                Path.Combine(
                    archivedRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string archivedProduct =
                Path.Combine(
                    archivedRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                archivedGlobal,
                PreSchemaGlobalJson());
            string archivedHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(archivedGlobal);
            CreateClaimOnlyInterruption(
                archivedGlobal,
                archivedProduct,
                Scope());
            File.Move(
                archivedGlobal,
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        archivedGlobal,
                        archivedHash));
            bool exactArchiveBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        archivedProduct,
                        archivedGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                exactArchiveBlocked = true;
            }
            Assert(
                exactArchiveBlocked &&
                !File.Exists(archivedProduct),
                "An exact completed archive without its claimed Product authority must remain recovery-required instead of becoming empty state.");

            string conflictingRoot =
                CreateMigrationRoot(
                    "pre-schema-claim-conflicting-archive-product-missing");
            string conflictingGlobal =
                Path.Combine(
                    conflictingRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string conflictingProduct =
                Path.Combine(
                    conflictingRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                conflictingGlobal,
                PreSchemaGlobalJson());
            string conflictingHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(conflictingGlobal);
            CreateClaimOnlyInterruption(
                conflictingGlobal,
                conflictingProduct,
                Scope());
            File.Delete(conflictingGlobal);
            File.WriteAllText(
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        conflictingGlobal,
                        conflictingHash),
                "mismatched-archive");
            bool conflictingArchiveBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        conflictingProduct,
                        conflictingGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                conflictingArchiveBlocked = true;
            }
            Assert(
                conflictingArchiveBlocked &&
                !File.Exists(conflictingProduct),
                "A mismatched archive plus pending claim must fail closed without creating empty Product authority.");

            string freshRoot =
                CreateMigrationRoot(
                    "pre-schema-no-claim-fresh-empty");
            string freshGlobal =
                Path.Combine(
                    freshRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string freshProduct =
                Path.Combine(
                    freshRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            EquipmentSlotStorageDocument fresh =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        freshProduct,
                        freshGlobal,
                        Scope(),
                        out string freshMessage);
            Assert(
                fresh.Slots.TrueForAll(
                    slot => !slot.IsOccupied) &&
                string.IsNullOrEmpty(freshMessage) &&
                !File.Exists(freshProduct),
                "A true first install with no Product, source or pending claim must still return an in-memory empty state.");
        }

        private static void
            ClaimCompletionRevalidatesArchiveAndProduct()
        {
            string archiveRoot =
                CreateMigrationRoot(
                    "pre-schema-completion-archive-drift");
            string archiveGlobal =
                Path.Combine(
                    archiveRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string archiveProduct =
                Path.Combine(
                    archiveRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                archiveGlobal,
                PreSchemaGlobalJson());
            string archiveHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(archiveGlobal);
            string archivePath =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        archiveGlobal,
                        archiveHash);
            string archiveClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        archiveGlobal,
                        archiveHash);
            bool archiveDriftBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-global-archive")
                        {
                            File.AppendAllText(
                                archivePath,
                                " ");
                        }
                    }).LoadOrMigrate(
                        archiveProduct,
                        archiveGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                archiveDriftBlocked = true;
            }
            Assert(
                archiveDriftBlocked &&
                File.Exists(archiveProduct) &&
                File.Exists(archiveClaim) &&
                File.Exists(archivePath),
                "Archive drift after global move must retain the pending claim and fail before completion.");

            string productRoot =
                CreateMigrationRoot(
                    "pre-schema-completion-product-delete");
            string productGlobal =
                Path.Combine(
                    productRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string productPath =
                Path.Combine(
                    productRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                productGlobal,
                PreSchemaGlobalJson());
            string productHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(productGlobal);
            string productClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        productGlobal,
                        productHash);
            bool productDeleteBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-global-archive")
                        {
                            File.Delete(productPath);
                        }
                    }).LoadOrMigrate(
                        productPath,
                        productGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                productDeleteBlocked = true;
            }
            Assert(
                productDeleteBlocked &&
                !File.Exists(productPath) &&
                File.Exists(productClaim),
                "Product deletion after archive publication must retain the pending claim and fail before completion.");

            bool emptyAfterProductDeleteBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        productGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                emptyAfterProductDeleteBlocked = true;
            }
            Assert(
                emptyAfterProductDeleteBlocked &&
                !File.Exists(productPath) &&
                File.Exists(productClaim),
                "The retained claim must continue blocking empty authority after completed-archive Product loss.");

            string recreatedRoot =
                CreateMigrationRoot(
                    "pre-schema-completion-global-recreated");
            string recreatedGlobal =
                Path.Combine(
                    recreatedRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string recreatedProduct =
                Path.Combine(
                    recreatedRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                recreatedGlobal,
                PreSchemaGlobalJson());
            byte[] recreatedSource =
                File.ReadAllBytes(recreatedGlobal);
            string recreatedHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(recreatedGlobal);
            string recreatedClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        recreatedGlobal,
                        recreatedHash);
            bool recreatedGlobalBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-global-archive")
                        {
                            File.WriteAllBytes(
                                recreatedGlobal,
                                recreatedSource);
                        }
                    }).LoadOrMigrate(
                        recreatedProduct,
                        recreatedGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                recreatedGlobalBlocked = true;
            }
            Assert(
                recreatedGlobalBlocked &&
                File.Exists(recreatedProduct) &&
                File.Exists(recreatedGlobal) &&
                File.Exists(recreatedClaim),
                "A global source recreated after archival must retain its claim and fail before completion instead of reporting a false terminal state.");

            string completionWindowRoot =
                CreateMigrationRoot(
                    "pre-schema-completion-global-toctou");
            string completionWindowGlobal =
                Path.Combine(
                    completionWindowRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string completionWindowProduct =
                Path.Combine(
                    completionWindowRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                completionWindowGlobal,
                PreSchemaGlobalJson());
            byte[] completionWindowSource =
                File.ReadAllBytes(
                    completionWindowGlobal);
            string completionWindowHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        completionWindowGlobal);
            string completionWindowClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        completionWindowGlobal,
                        completionWindowHash);
            bool completionWindowBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-completion-validation-before-publish")
                        {
                            File.WriteAllBytes(
                                completionWindowGlobal,
                                completionWindowSource);
                        }
                    }).LoadOrMigrate(
                        completionWindowProduct,
                        completionWindowGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                completionWindowBlocked = true;
            }
            Assert(
                completionWindowBlocked &&
                File.Exists(completionWindowProduct) &&
                File.Exists(completionWindowGlobal) &&
                File.Exists(completionWindowClaim) &&
                !IsCompletedClaim(
                    completionWindowClaim),
                "Global recreation after completion validation must retain a pending claim and refuse completed-state publication.");
        }

        private static void
            HistoricalGameRootWinnerRejectsDifferentSource()
        {
            string root =
                CreateMigrationRoot(
                    "pre-schema-historical-cross-source-winner");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string firstProductPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string secondProductPath =
                Path.Combine(
                    root,
                    "slot-3",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string winnerPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        globalPath);

            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            EquipmentSlotStorageDocument first =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProductPath,
                        globalPath,
                        Scope(),
                        out _);
            string firstHash =
                first.LegacyMigration!.SourceSha256;
            string firstClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        firstHash);
            File.Delete(
                winnerPath);
            File.Delete(
                firstClaim);

            EquipmentSlotStorageDocument second =
                StageIndependentHistoricalAuthority(
                    "pre-schema-historical-cross-source-winner-second",
                    globalPath,
                    secondProductPath,
                    FourthScope(),
                    PreSchemaGlobalJson()
                        .Replace(
                            "historical-button",
                            "historical-button-v2",
                            StringComparison.Ordinal));
            string secondHash =
                second.LegacyMigration!.SourceSha256;
            string secondClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        secondHash);
            File.Delete(
                winnerPath);
            File.Delete(
                secondClaim);

            bool firstBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProductPath,
                        globalPath,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                firstBlocked = true;
            }
            byte[] secondProductBytes =
                File.ReadAllBytes(
                    secondProductPath);
            bool secondBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        secondProductPath,
                        globalPath,
                        FourthScope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                secondBlocked = true;
            }
            Assert(
                firstBlocked &&
                secondBlocked &&
                !File.Exists(winnerPath) &&
                !File.Exists(firstClaim) &&
                !File.Exists(secondClaim) &&
                EqualBytes(
                    secondProductBytes,
                    File.ReadAllBytes(
                        secondProductPath)),
                "Different historical pre-schema Product authorities without an existing winner must both remain fail-closed; load order cannot select one automatically.");
        }

        private static void
            CrossProcessHistoricalWinnerIsSingleton()
        {
            string root =
                CreateMigrationRoot(
                    "pre-schema-historical-cross-process-winner");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string firstProductPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string secondProductPath =
                Path.Combine(
                    root,
                    "slot-3",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string winnerPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        globalPath);

            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            EquipmentSlotStorageDocument first =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProductPath,
                        globalPath,
                        Scope(),
                        out _);
            string firstClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        first.LegacyMigration!
                            .SourceSha256);
            File.Delete(
                winnerPath);
            File.Delete(
                firstClaim);

            EquipmentSlotStorageDocument second =
                StageIndependentHistoricalAuthority(
                    "pre-schema-historical-cross-process-winner-second",
                    globalPath,
                    secondProductPath,
                    FourthScope(),
                    PreSchemaGlobalJson()
                        .Replace(
                            "historical-button",
                            "historical-button-v3",
                            StringComparison.Ordinal));
            string secondClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        second.LegacyMigration!
                            .SourceSha256);
            File.Delete(
                winnerPath);
            File.Delete(
                secondClaim);
            byte[] firstBefore =
                File.ReadAllBytes(
                    firstProductPath);
            byte[] secondBefore =
                File.ReadAllBytes(
                    secondProductPath);

            string releasePath =
                Path.Combine(
                    root,
                    "backfill.release");
            string firstReady =
                Path.Combine(
                    root,
                    "backfill-first.ready");
            string secondReady =
                Path.Combine(
                    root,
                    "backfill-second.ready");
            string firstOutcome =
                Path.Combine(
                    root,
                    "backfill-first.outcome");
            string secondOutcome =
                Path.Combine(
                    root,
                    "backfill-second.outcome");
            using Process firstProcess =
                StartHistoricalBackfillActor(
                    globalPath,
                    firstProductPath,
                    Scope(),
                    firstReady,
                    releasePath,
                    firstOutcome);
            using Process secondProcess =
                StartHistoricalBackfillActor(
                    globalPath,
                    secondProductPath,
                    FourthScope(),
                    secondReady,
                    releasePath,
                    secondOutcome);
            WaitForFile(
                firstReady,
                "The first historical backfill process did not become ready.");
            WaitForFile(
                secondReady,
                "The second historical backfill process did not become ready.");
            File.WriteAllText(
                releasePath,
                "release");
            Assert(
                firstProcess.WaitForExit(
                    20000) &&
                secondProcess.WaitForExit(
                    20000),
                "Both historical backfill processes must terminate.");
            string firstStatus =
                File.ReadAllText(
                    firstOutcome);
            string secondStatus =
                File.ReadAllText(
                    secondOutcome);
            Assert(
                firstProcess.ExitCode == 0 &&
                secondProcess.ExitCode == 0 &&
                firstStatus == "blocked" &&
                secondStatus == "blocked" &&
                !File.Exists(winnerPath) &&
                EqualBytes(
                    firstBefore,
                    File.ReadAllBytes(
                        firstProductPath)) &&
                EqualBytes(
                    secondBefore,
                    File.ReadAllBytes(
                        secondProductPath)),
                "Two real processes observing different pre-existing historical Product authorities must both fail closed without publishing a load-order winner.");
        }

        private static void
            PendingEvidenceStillRunsPreWinnerCensus()
        {
            foreach (bool usePrevious in
                new[] { false, true })
            {
                string root =
                    CreateMigrationRoot(
                        usePrevious
                            ? "pending-evidence-previous-census"
                            : "pending-evidence-live-census");
                string globalPath =
                    Path.Combine(
                        root,
                        "equipment-slots-" +
                        MoreEquipmentSlotsProductContract
                            .UniqueId +
                        ".json");
                string productPath =
                    Path.Combine(
                        root,
                        "slot-2",
                        Path.GetFileName(globalPath));
                string otherProduct =
                    Path.Combine(
                        root,
                        "slot-3",
                        Path.GetFileName(globalPath));
                File.WriteAllText(
                    globalPath,
                    PreSchemaGlobalJson());
                byte[] globalBytes =
                    File.ReadAllBytes(globalPath);
                string sourceHash =
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(globalPath);
                EquipmentSlotStorageDocument other =
                    StageIndependentHistoricalAuthority(
                        usePrevious
                            ? "pending-evidence-previous-source"
                            : "pending-evidence-live-source",
                        globalPath,
                        otherProduct,
                        FourthScope(),
                        PreSchemaGlobalJson()
                            .Replace(
                                "historical-button",
                                usePrevious
                                    ? "other-previous"
                                    : "other-live",
                                StringComparison.Ordinal));
                File.Delete(
                    EquipmentSlotLegacyMigration
                        .GetGlobalArchivePath(
                            globalPath,
                            other.LegacyMigration!
                                .SourceSha256));
                string authorityPath =
                    otherProduct;
                if (usePrevious)
                {
                    authorityPath =
                        otherProduct + ".previous";
                    File.Move(
                        otherProduct,
                        authorityPath);
                    File.WriteAllText(
                        otherProduct,
                        "{recoverably-corrupt-live");
                }
                byte[] authorityBytes =
                    File.ReadAllBytes(authorityPath);
                string claimPath =
                    EquipmentSlotLegacyMigration
                        .GetPreSchemaGlobalClaimPath(
                            globalPath,
                            sourceHash);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(claimPath)!);
                string pending =
                    PreSchemaClaimJson(
                        globalPath,
                        productPath,
                        Scope(),
                        sourceHash,
                        "pending");
                File.WriteAllText(
                    claimPath,
                    pending);

                bool blocked = false;
                try
                {
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            productPath,
                            globalPath,
                            Scope(),
                            out _);
                }
                catch (InvalidDataException)
                {
                    blocked = true;
                }
                Assert(
                    blocked &&
                    !File.Exists(productPath) &&
                    EqualBytes(
                        globalBytes,
                        File.ReadAllBytes(globalPath)) &&
                    EqualBytes(
                        authorityBytes,
                        File.ReadAllBytes(authorityPath)) &&
                    string.Equals(
                        File.ReadAllText(claimPath),
                        pending,
                        StringComparison.Ordinal) &&
                    !File.Exists(
                        EquipmentSlotLegacyMigration
                            .GetPreSchemaGlobalWinnerPath(
                                globalPath)) &&
                    !File.Exists(
                        EquipmentSlotLegacyMigration
                            .GetGlobalArchivePath(
                                globalPath,
                                sourceHash)) &&
                    !File.Exists(
                        EquipmentSlotLegacyMigration
                            .GetLegacyBackupPath(
                                productPath,
                                sourceHash)),
                    "Existing pending evidence without a winner must still run the live/eligible-previous Product census before any Product, archive or backup mutation.");
            }
        }

        private static void
            PendingWinnerStillRunsPreWinnerCensus()
        {
            foreach (bool sameSource in
                new[] { true, false })
            {
                foreach (bool usePrevious in
                    new[] { false, true })
                {
                    string variant =
                        (sameSource ? "same" : "different") +
                        "-" +
                        (usePrevious ? "previous" : "live");
                    string root =
                        CreateMigrationRoot(
                            "pending-winner-" +
                            variant +
                            "-census");
                    string globalPath =
                        Path.Combine(
                            root,
                            "equipment-slots-" +
                            MoreEquipmentSlotsProductContract
                                .UniqueId +
                            ".json");
                    string productPath =
                        Path.Combine(
                            root,
                            "slot-2",
                            Path.GetFileName(globalPath));
                    string otherProduct =
                        Path.Combine(
                            root,
                            "slot-3",
                            Path.GetFileName(globalPath));
                    File.WriteAllText(
                        globalPath,
                        PreSchemaGlobalJson());
                    CreateClaimOnlyInterruption(
                        globalPath,
                        productPath,
                        Scope());

                    byte[] globalBytes =
                        File.ReadAllBytes(globalPath);
                    string sourceHash =
                        EquipmentSlotLegacyMigration
                            .ComputeFileSha256(globalPath);
                    string winnerPath =
                        EquipmentSlotLegacyMigration
                            .GetPreSchemaGlobalWinnerPath(
                                globalPath);
                    string evidencePath =
                        EquipmentSlotLegacyMigration
                            .GetPreSchemaGlobalClaimPath(
                                globalPath,
                                sourceHash);
                    byte[] winnerBytes =
                        File.ReadAllBytes(winnerPath);
                    byte[] evidenceBytes =
                        File.ReadAllBytes(evidencePath);

                    string otherSourceJson =
                        sameSource
                            ? PreSchemaGlobalJson()
                            : PreSchemaGlobalJson()
                                .Replace(
                                    "historical-button",
                                    "late-" + variant,
                                    StringComparison.Ordinal);
                    EquipmentSlotStorageDocument other =
                        StageIndependentHistoricalAuthority(
                            "pending-winner-" +
                                variant +
                                "-source",
                            globalPath,
                            otherProduct,
                            FourthScope(),
                            otherSourceJson);
                    File.Delete(
                        EquipmentSlotLegacyMigration
                            .GetGlobalArchivePath(
                                globalPath,
                                other.LegacyMigration!
                                    .SourceSha256));
                    string authorityPath =
                        otherProduct;
                    if (usePrevious)
                    {
                        authorityPath =
                            otherProduct +
                            ".previous";
                        File.Move(
                            otherProduct,
                            authorityPath);
                        File.WriteAllText(
                            otherProduct,
                            "{recoverably-corrupt-live");
                    }
                    byte[] authorityBytes =
                        File.ReadAllBytes(authorityPath);

                    bool blocked = false;
                    try
                    {
                        new EquipmentSlotDocumentStore()
                            .LoadOrMigrate(
                                productPath,
                                globalPath,
                                Scope(),
                                out _);
                    }
                    catch (InvalidDataException)
                    {
                        blocked = true;
                    }
                    Assert(
                        blocked &&
                        !File.Exists(productPath) &&
                        EqualBytes(
                            globalBytes,
                            File.ReadAllBytes(globalPath)) &&
                        EqualBytes(
                            winnerBytes,
                            File.ReadAllBytes(winnerPath)) &&
                        EqualBytes(
                            evidenceBytes,
                            File.ReadAllBytes(evidencePath)) &&
                        EqualBytes(
                            authorityBytes,
                            File.ReadAllBytes(authorityPath)) &&
                        !File.Exists(
                            EquipmentSlotLegacyMigration
                                .GetGlobalArchivePath(
                                    globalPath,
                                    sourceHash)) &&
                        !File.Exists(
                            EquipmentSlotLegacyMigration
                                .GetLegacyBackupPath(
                                    productPath,
                                    sourceHash)),
                        "A durable pending winner must re-run the live/eligible-previous Product census for same- and different-source authorities before Product, backup or archive mutation.");
                }
            }
        }

        private static void
            CompletedWinnerIgnoresRetainedLateLoserEvidence()
        {
            string root =
                CreateMigrationRoot(
                    "completed-winner-retains-late-losers");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string productPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            EquipmentSlotStorageDocument winner =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            string winnerHash =
                winner.LegacyMigration!.SourceSha256;
            string winnerPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        globalPath);
            string evidencePath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        winnerHash);

            string alternateSource =
                Path.Combine(
                    root,
                    "alternate-source.json");
            File.WriteAllText(
                alternateSource,
                PreSchemaGlobalJson()
                    .Replace(
                        "historical-button",
                        "late-loser",
                        StringComparison.Ordinal));
            string alternateHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        alternateSource);
            File.Delete(alternateSource);
            string lateClaimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        alternateHash);
            string lateReady =
                Path.Combine(root, "late.ready");
            string lateRelease =
                Path.Combine(root, "late.release");
            using Process lateActor =
                StartLateEvidenceWriterActor(
                    lateClaimPath,
                    PreSchemaClaimJson(
                        globalPath,
                        Path.Combine(
                            root,
                            "slot-3",
                            Path.GetFileName(productPath)),
                        FourthScope(),
                        alternateHash,
                        "pending"),
                    lateReady,
                    lateRelease);
            WaitForFile(
                lateReady,
                "The old late-evidence actor did not become ready.");
            File.WriteAllText(
                lateRelease,
                "release");
            Assert(
                lateActor.WaitForExit(20000) &&
                lateActor.ExitCode == 0 &&
                File.Exists(lateClaimPath),
                "A real old actor must publish its late loser evidence after the completed winner exists.");
            string lateTransition =
                lateClaimPath +
                ".transition-retained";
            File.Copy(
                lateClaimPath,
                lateTransition);

            EquipmentSlotStorageDocument resumed =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            Assert(
                resumed.Scope.Matches(Scope().Clone()) &&
                IsCompletedClaim(winnerPath) &&
                File.Exists(lateClaimPath) &&
                File.Exists(lateTransition),
                "The exact completed winner must remain readable while different-hash loser claim and transition bytes stay retained.");

            var advancedScope = Scope();
            advancedScope.TotalGameSeconds = 1000;
            winner.Scope =
                advancedScope.Clone();
            winner.Generation++;
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    productPath,
                    winner);
            File.Delete(evidencePath);
            byte[] sameHashLoser =
                System.Text.Encoding.UTF8.GetBytes(
                    PreSchemaClaimJson(
                        globalPath,
                        Path.Combine(
                            root,
                            "slot-3",
                            Path.GetFileName(productPath)),
                        FourthScope(),
                        winnerHash,
                        "pending"));
            resumed =
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "before-preschema-optional-evidence-publish")
                        {
                            File.WriteAllBytes(
                                evidencePath,
                                sameHashLoser);
                        }
                    })
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        advancedScope,
                        out _);
            Assert(
                resumed.Scope.Matches(
                    advancedScope.Clone()) &&
                EqualBytes(
                    sameHashLoser,
                    File.ReadAllBytes(evidencePath)) &&
                IsCompletedClaim(winnerPath),
                "A completed T0 winner with a valid T1 Product must not fall back to strict T0 Product validation when a late same-hash evidence writer wins the optional publication race.");
        }

        private static void
            PreWinnerCensusIncludesEligiblePreviousAndDifferentHash()
        {
            string root =
                CreateMigrationRoot(
                    "pre-winner-previous-census");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string firstProduct =
                Path.Combine(
                    root,
                    "slot-2",
                    Path.GetFileName(globalPath));
            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            EquipmentSlotStorageDocument first =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProduct,
                        globalPath,
                        Scope(),
                        out _);
            string firstHash =
                first.LegacyMigration!.SourceSha256;
            string firstArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        globalPath,
                        firstHash);

            string secondRoot =
                CreateMigrationRoot(
                    "pre-winner-previous-census-source-2");
            string secondGlobal =
                Path.Combine(
                    secondRoot,
                    Path.GetFileName(globalPath));
            string secondSourceProduct =
                Path.Combine(
                    secondRoot,
                    "slot-3",
                    Path.GetFileName(globalPath));
            File.WriteAllText(
                secondGlobal,
                PreSchemaGlobalJson()
                    .Replace(
                        "historical-button",
                        "different-revision-item",
                        StringComparison.Ordinal));
            EquipmentSlotStorageDocument second =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        secondSourceProduct,
                        secondGlobal,
                        FourthScope(),
                        out _);
            string secondHash =
                second.LegacyMigration!.SourceSha256;
            string secondSourceArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        secondGlobal,
                        secondHash);
            string secondProduct =
                Path.Combine(
                    root,
                    "slot-3",
                    Path.GetFileName(globalPath));
            Directory.CreateDirectory(
                Path.GetDirectoryName(secondProduct)!);
            File.Copy(
                secondSourceProduct,
                secondProduct);
            string secondArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        globalPath,
                        secondHash);
            File.Copy(
                secondSourceArchive,
                secondArchive);

            string claimDirectory =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimDirectory(
                        globalPath);
            Directory.Delete(
                claimDirectory,
                recursive: true);
            string firstPrevious =
                firstProduct + ".previous";
            File.Move(
                firstProduct,
                firstPrevious);
            File.WriteAllText(
                firstProduct,
                "{recoverably-corrupt-live");
            byte[] firstPreviousBytes =
                File.ReadAllBytes(firstPrevious);
            byte[] secondProductBytes =
                File.ReadAllBytes(secondProduct);
            byte[] firstArchiveBytes =
                File.ReadAllBytes(firstArchive);
            byte[] secondArchiveBytes =
                File.ReadAllBytes(secondArchive);

            bool firstBlocked = false;
            bool secondBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProduct,
                        globalPath,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                firstBlocked = true;
            }
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        secondProduct,
                        globalPath,
                        FourthScope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                secondBlocked = true;
            }

            string release =
                Path.Combine(root, "census.release");
            string firstReady =
                Path.Combine(root, "census-first.ready");
            string secondReady =
                Path.Combine(root, "census-second.ready");
            string firstOutcome =
                Path.Combine(root, "census-first.outcome");
            string secondOutcome =
                Path.Combine(root, "census-second.outcome");
            using Process firstProcess =
                StartHistoricalBackfillActor(
                    globalPath,
                    firstProduct,
                    Scope(),
                    firstReady,
                    release,
                    firstOutcome);
            using Process secondProcess =
                StartHistoricalBackfillActor(
                    globalPath,
                    secondProduct,
                    FourthScope(),
                    secondReady,
                    release,
                    secondOutcome);
            WaitForFile(
                firstReady,
                "The first census process did not become ready.");
            WaitForFile(
                secondReady,
                "The second census process did not become ready.");
            File.WriteAllText(release, "release");
            Assert(
                firstProcess.WaitForExit(20000) &&
                secondProcess.WaitForExit(20000) &&
                firstProcess.ExitCode == 0 &&
                secondProcess.ExitCode == 0 &&
                File.ReadAllText(firstOutcome) == "blocked" &&
                File.ReadAllText(secondOutcome) == "blocked" &&
                firstBlocked &&
                secondBlocked &&
                !File.Exists(
                    EquipmentSlotLegacyMigration
                        .GetPreSchemaGlobalWinnerPath(
                            globalPath)) &&
                EqualBytes(
                    firstPreviousBytes,
                    File.ReadAllBytes(firstPrevious)) &&
                EqualBytes(
                    secondProductBytes,
                    File.ReadAllBytes(secondProduct)) &&
                EqualBytes(
                    firstArchiveBytes,
                    File.ReadAllBytes(firstArchive)) &&
                EqualBytes(
                    secondArchiveBytes,
                    File.ReadAllBytes(secondArchive)),
                "Eligible previous and different-hash live Product authorities must block automatic winner selection in both orders and in real concurrent processes without changing authority bytes.");
        }

        private static void
            ClaimCompletionNeverOverwritesDifferentPendingOwner()
        {
            string root =
                CreateMigrationRoot(
                    "pre-schema-completion-owner-replacement");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string productPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            string sourceHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        globalPath);
            string claimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        sourceHash);
            string winnerPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        globalPath);
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-global-archive")
                        {
                            throw new IOException(
                                "injected-before-completion");
                        }
                    }).LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
            }
            Assert(
                File.Exists(claimPath) &&
                !IsCompletedClaim(claimPath),
                "The mixed-owner fixture requires one pending evidence claim.");

            string differentClaim =
                PreSchemaClaimJson(
                    globalPath,
                    productPath,
                    FourthScope(),
                    sourceHash,
                    "pending");
            bool replacementBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-completion-validation-before-publish")
                        {
                            File.Delete(
                                claimPath);
                            File.WriteAllText(
                                claimPath,
                                differentClaim);
                        }
                    }).LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                replacementBlocked = true;
            }
            Assert(
                !replacementBlocked &&
                IsCompletedClaim(winnerPath) &&
                File.Exists(claimPath) &&
                string.Equals(
                    File.ReadAllText(
                        claimPath),
                    differentClaim,
                    StringComparison.Ordinal) &&
                !IsCompletedClaim(
                    claimPath),
                "Completion must never replace a different late pending evidence owner; the exact completed winner remains usable while conflicting evidence bytes stay retained.");

            string collisionRoot =
                CreateMigrationRoot(
                    "pre-schema-completion-create-collision");
            string collisionGlobal =
                Path.Combine(
                    collisionRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string collisionProduct =
                Path.Combine(
                    collisionRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                collisionGlobal,
                PreSchemaGlobalJson());
            string collisionHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        collisionGlobal);
            string collisionClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        collisionGlobal,
                        collisionHash);
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-global-archive")
                        {
                            throw new IOException(
                                "injected-before-collision");
                        }
                    }).LoadOrMigrate(
                        collisionProduct,
                        collisionGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
            }
            string collisionBytes =
                PreSchemaClaimJson(
                    collisionGlobal,
                    collisionProduct,
                    FourthScope(),
                    collisionHash,
                    "pending");
            bool collisionBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-evidence-transition-capture")
                        {
                            File.WriteAllText(
                                collisionClaim,
                                collisionBytes);
                        }
                    }).LoadOrMigrate(
                        collisionProduct,
                        collisionGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                collisionBlocked = true;
            }
            string[] transitionPaths =
                Directory.GetFiles(
                    Path.GetDirectoryName(
                        collisionClaim)!,
                    Path.GetFileName(
                        collisionClaim) +
                    ".transition-*",
                    SearchOption.TopDirectoryOnly);
            bool collisionRestartBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        collisionProduct,
                        collisionGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                collisionRestartBlocked = true;
            }
            Assert(
                !collisionBlocked &&
                !collisionRestartBlocked &&
                IsCompletedClaim(
                    EquipmentSlotLegacyMigration
                        .GetPreSchemaGlobalWinnerPath(
                            collisionGlobal)) &&
                transitionPaths.Length == 1 &&
                File.Exists(
                    transitionPaths[0]) &&
                string.Equals(
                    File.ReadAllText(
                        collisionClaim),
                    collisionBytes,
                    StringComparison.Ordinal),
                "A different owner appearing after optional evidence capture must leave both byte authorities discoverable without making the exact completed winner unreadable.");
        }

        private static void
            CompletedClaimAcceptsEligiblePreviousAuthority()
        {
            string root =
                CreateMigrationRoot(
                    "pre-schema-completed-previous");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string productPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                globalPath,
                PreSchemaGlobalJson());
            string sourceSha256 =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        globalPath);
            string claimPath =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        globalPath,
                        sourceSha256);
            EquipmentSlotStorageDocument migrated =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            Assert(
                migrated.Slots[0].ItemId ==
                    "historical-button" &&
                IsCompletedClaim(claimPath),
                "The previous-authority fixture must begin from one completed pre-schema migration.");

            string previousPath =
                productPath +
                ".previous";
            File.Copy(
                productPath,
                previousPath,
                overwrite: true);
            File.Delete(
                productPath);
            EquipmentSlotStorageDocument missingLive =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            Assert(
                missingLive.Slots[0].ItemId ==
                    "historical-button" &&
                IsCompletedClaim(claimPath),
                "A missing live Product must keep the existing eligible stamped previous authority reachable through completed-claim validation.");

            File.WriteAllText(
                productPath,
                "{broken");
            EquipmentSlotStorageDocument corruptLive =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            Assert(
                corruptLive.Slots[0].ItemId ==
                    "historical-button" &&
                IsCompletedClaim(claimPath),
                "A recoverably corrupt live Product must keep the eligible stamped previous authority reachable.");

            File.Delete(
                productPath);
            File.WriteAllText(
                previousPath,
                "{}");
            bool invalidPreviousBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                invalidPreviousBlocked = true;
            }
            Assert(
                invalidPreviousBlocked &&
                IsCompletedClaim(claimPath),
                "An invalid previous document must remain fail-closed without weakening or deleting the completed claim.");
        }

        private static void CreateClaimOnlyInterruption(
            string globalPath,
            string productPath,
            EquipmentSlotSaveScope scope)
        {
            bool interrupted = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-preschema-claim-publish")
                        {
                            throw new IOException(
                                "injected-claim-only");
                        }
                    }).LoadOrMigrate(
                        productPath,
                        globalPath,
                        scope,
                        out _);
            }
            catch (IOException)
            {
                interrupted = true;
            }
            Assert(
                interrupted &&
                File.Exists(globalPath) &&
                !File.Exists(productPath),
                "The helper must stop after durable claim publication and before Product publication.");
        }

        private static void
            LegacyMigrationPreservesTransactionsAndCandidate()
        {
            string root = CreateMigrationRoot("transactions");
            string journalPath =
                Path.Combine(root, "journal.json");
            File.WriteAllText(
                journalPath,
                LegacyJournalJson());
            EquipmentSlotStorageDocument journalDocument =
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    journalPath,
                    Path.Combine(root, "missing-global.json"),
                    Scope(),
                    out _);
            Assert(
                journalDocument.Journal != null &&
                journalDocument.Journal.State ==
                    EquipmentSlotJournalState.Prepared &&
                !journalDocument.Journal.AttemptStarted &&
                string.IsNullOrEmpty(
                    journalDocument.Journal
                        .PreSaveFingerprint) &&
                journalDocument.Journal.Escrow.Count == 1 &&
                journalDocument.Journal.Escrow[0].ItemId ==
                    "journal-shield" &&
                journalDocument.Journal.Escrow[0].IsShield &&
                !journalDocument.Slots[1].IsOccupied,
                "A legacy prepared owner-recovery journal must remain durable escrow, clear incompatible pre-attempt observations, and preserve the shield exactly once.");

            string candidatePath =
                Path.Combine(root, "candidate.json");
            File.WriteAllText(
                candidatePath,
                LegacyCandidateJson());
            EquipmentSlotStorageDocument candidateDocument =
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    candidatePath,
                    Path.Combine(root, "missing-global.json"),
                    Scope(),
                    out _);
            Assert(
                candidateDocument.GameplayCandidate != null &&
                candidateDocument.GameplayCandidate.State ==
                    EquipmentSlotGameplayCandidateState.Prepared &&
                candidateDocument.GameplayCandidate
                    .WorkingSlots.Count == 3 &&
                candidateDocument.GameplayCandidate
                    .WorkingSlots[0].ItemId == "working-hat" &&
                candidateDocument.GameplayCandidate
                    .NativeExpectations.Count == 1 &&
                candidateDocument.Slots[0].ItemId ==
                    "committed-hat",
                "A legacy gameplay candidate must preserve separate committed and Working projections plus exact native expectations.");
        }

        private static void
            LegacyMigrationCrashWindowsAndDriftFailClosed()
        {
            string root = CreateMigrationRoot("crash-windows");
            string scopedPath =
                Path.Combine(root, "scoped.json");
            File.WriteAllText(scopedPath, LegacyFlatJson(2));
            byte[] scopedBefore = File.ReadAllBytes(scopedPath);
            var prePublishStore =
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage == "before-product-publish")
                        {
                            throw new IOException(
                                "injected-before-publish");
                        }
                    });
            bool prePublishFailed = false;
            try
            {
                prePublishStore.LoadOrMigrate(
                    scopedPath,
                    Path.Combine(root, "missing-global.json"),
                    Scope(),
                    out _);
            }
            catch (IOException)
            {
                prePublishFailed = true;
            }
            Assert(
                prePublishFailed &&
                EqualBytes(
                    scopedBefore,
                    File.ReadAllBytes(scopedPath)) &&
                EquipmentSlotStorageFormatClassifier.Probe(scopedPath)
                    .Format ==
                    EquipmentSlotStorageFormat.LegacyFlat,
                "A failure before Product publication must leave the flat live authority byte-identical.");

            string globalPath =
                Path.Combine(root, "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string productPath =
                Path.Combine(
                    root,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(globalPath, LegacyFlatJson(3));
            byte[] globalBefore = File.ReadAllBytes(globalPath);
            bool postPublishFailed = false;
            var postPublishStore =
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage == "after-product-publish")
                        {
                            throw new IOException(
                                "injected-after-publish");
                        }
                    });
            try
            {
                postPublishStore.LoadOrMigrate(
                    productPath,
                    globalPath,
                    Scope(),
                    out _);
            }
            catch (IOException)
            {
                postPublishFailed = true;
            }
            Assert(
                postPublishFailed &&
                File.Exists(productPath) &&
                File.Exists(globalPath) &&
                EqualBytes(
                    globalBefore,
                    File.ReadAllBytes(globalPath)) &&
                EquipmentSlotStorageFormatClassifier.Probe(productPath)
                    .Format ==
                    EquipmentSlotStorageFormat.ProductV3,
                "A failure after Product publication must retain the exact global source while leaving a valid retryable Product authority.");
            EquipmentSlotStorageDocument finalized =
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    productPath,
                    globalPath,
                    Scope(),
                    out string finalizationMessage);
            string archivePath =
                EquipmentSlotLegacyMigration.GetGlobalArchivePath(
                    globalPath,
                    finalized.LegacyMigration!.SourceSha256);
            Assert(
                !File.Exists(globalPath) &&
                File.Exists(archivePath) &&
                EqualBytes(
                    globalBefore,
                    File.ReadAllBytes(archivePath)) &&
                finalizationMessage.Contains(
                    "archived",
                    StringComparison.Ordinal),
                "Restart after Product publication must archive only the exact global source and converge on one live authority.");

            string driftGlobal =
                Path.Combine(root, "drift-global.json");
            string driftProduct =
                Path.Combine(root, "slot-2", "drift-product.json");
            File.WriteAllText(driftGlobal, LegacyFlatJson(3));
            bool driftFailed = false;
            var driftStore =
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage == "after-product-publish")
                            File.AppendAllText(driftGlobal, " ");
                    });
            try
            {
                driftStore.LoadOrMigrate(
                    driftProduct,
                    driftGlobal,
                    Scope(),
                    out _);
            }
            catch (InvalidDataException)
            {
                driftFailed = true;
            }
            Assert(
                driftFailed &&
                File.Exists(driftProduct) &&
                File.Exists(driftGlobal),
                "External global drift after Product publication must retain both files fail-closed instead of archiving changed bytes.");
            bool retryDriftFailed = false;
            try
            {
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    driftProduct,
                    driftGlobal,
                    Scope(),
                    out _);
            }
            catch (InvalidDataException)
            {
                retryDriftFailed = true;
            }
            Assert(
                retryDriftFailed,
                "A later restart must continue to reject a drifted pending global source until it is explicitly reconciled.");

            string lateArchiveGlobal =
                Path.Combine(
                    root,
                    "late-archive-global.json");
            string lateArchiveProduct =
                Path.Combine(
                    root,
                    "slot-2",
                    "late-archive-product.json");
            File.WriteAllText(
                lateArchiveGlobal,
                LegacyFlatJson(3));
            byte[] lateArchiveOriginal =
                File.ReadAllBytes(
                    lateArchiveGlobal);
            string lateArchiveHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        lateArchiveGlobal);
            string lateArchivePath =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        lateArchiveGlobal,
                        lateArchiveHash);
            byte[] lateArchiveReplacement =
                System.Text.Encoding.UTF8.GetBytes(
                    LegacyFlatJson(3)
                        .Replace(
                            "passive-hat",
                            "late-passive-hat",
                            StringComparison.Ordinal));
            bool lateArchiveBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "before-global-archive")
                        {
                            File.WriteAllBytes(
                                lateArchivePath,
                                lateArchiveOriginal);
                            File.WriteAllBytes(
                                lateArchiveGlobal,
                                lateArchiveReplacement);
                        }
                    }).LoadOrMigrate(
                        lateArchiveProduct,
                        lateArchiveGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                lateArchiveBlocked = true;
            }
            Assert(
                lateArchiveBlocked &&
                File.Exists(lateArchiveProduct) &&
                File.Exists(lateArchiveGlobal) &&
                File.Exists(lateArchivePath) &&
                EqualBytes(
                    lateArchiveReplacement,
                    File.ReadAllBytes(
                        lateArchiveGlobal)) &&
                EqualBytes(
                    lateArchiveOriginal,
                    File.ReadAllBytes(
                        lateArchivePath)),
                "A late exact archive plus replacement global must retain both byte authorities fail-closed instead of deleting the new global.");

            string preCaptureGlobal =
                Path.Combine(
                    root,
                    "pre-capture-drift-global.json");
            string preCaptureProduct =
                Path.Combine(
                    root,
                    "slot-2",
                    "pre-capture-drift-product.json");
            File.WriteAllText(
                preCaptureGlobal,
                LegacyFlatJson(3));
            string preCaptureHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        preCaptureGlobal);
            string preCaptureArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        preCaptureGlobal,
                        preCaptureHash);
            byte[] preCaptureReplacement =
                System.Text.Encoding.UTF8.GetBytes(
                    LegacyFlatJson(3)
                        .Replace(
                            "defense-hat",
                            "late-defense-hat",
                            StringComparison.Ordinal));
            bool preCaptureBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "before-global-archive")
                        {
                            File.WriteAllBytes(
                                preCaptureGlobal,
                                preCaptureReplacement);
                        }
                    }).LoadOrMigrate(
                        preCaptureProduct,
                        preCaptureGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                preCaptureBlocked = true;
            }
            Assert(
                preCaptureBlocked &&
                File.Exists(preCaptureProduct) &&
                File.Exists(preCaptureGlobal) &&
                !File.Exists(preCaptureArchive) &&
                EqualBytes(
                    preCaptureReplacement,
                    File.ReadAllBytes(
                        preCaptureGlobal)),
                "Pre-capture global drift must restore the captured replacement bytes and must not publish them under the old deterministic archive hash.");
        }

        private static void
            InterruptedGlobalCaptureResumesExactly()
        {
            string exactRoot =
                CreateMigrationRoot(
                    "interrupted-global-capture-exact");
            string exactGlobal =
                Path.Combine(
                    exactRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string exactProduct =
                Path.Combine(
                    exactRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                exactGlobal,
                PreSchemaGlobalJson());
            byte[] exactBytes =
                File.ReadAllBytes(
                    exactGlobal);
            string exactHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        exactGlobal);
            string exactArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        exactGlobal,
                        exactHash);
            string exactClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        exactGlobal,
                        exactHash);
            string exactWinner =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        exactGlobal);
            using Process captureProcess =
                StartCaptureCrashActor(
                    exactGlobal,
                    exactProduct);
            Assert(
                captureProcess.WaitForExit(
                    20000) &&
                captureProcess.ExitCode == 87,
                "The real capture-crash process must terminate immediately after atomic global capture.");
            string[] exactCaptures =
                Directory.GetFiles(
                    exactRoot,
                    Path.GetFileName(
                        exactGlobal) +
                    ".migration-capture-*",
                    SearchOption.TopDirectoryOnly);
            Assert(
                exactCaptures.Length == 1 &&
                !File.Exists(exactGlobal) &&
                !File.Exists(exactArchive),
                "The real crash fixture must leave Product, pending winner/evidence and one exact capture before restart.");
            EquipmentSlotStorageDocument resumed =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        exactProduct,
                        exactGlobal,
                        Scope(),
                        out _);
            Assert(
                resumed.Scope.Matches(
                    Scope().Clone()) &&
                !File.Exists(exactGlobal) &&
                !File.Exists(
                    exactCaptures[0]) &&
                File.Exists(exactArchive) &&
                IsCompletedClaim(exactClaim) &&
                IsCompletedClaim(exactWinner) &&
                EqualBytes(
                    exactBytes,
                    File.ReadAllBytes(
                        exactArchive)),
                "One exact interrupted capture with its Product and scoped backup must resume to the deterministic archive.");

            string mismatchRoot =
                CreateMigrationRoot(
                    "interrupted-global-capture-mismatch");
            string mismatchGlobal =
                Path.Combine(
                    mismatchRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string mismatchProduct =
                Path.Combine(
                    mismatchRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                mismatchGlobal,
                LegacyFlatJson(3));
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-product-publish")
                        {
                            throw new IOException(
                                "stage-mismatch-capture");
                        }
                    }).LoadOrMigrate(
                        mismatchProduct,
                        mismatchGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
            }
            string mismatchHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        mismatchGlobal);
            string mismatchCapture =
                mismatchGlobal +
                ".migration-capture-mismatch";
            File.Move(
                mismatchGlobal,
                mismatchCapture);
            File.AppendAllText(
                mismatchCapture,
                " ");
            bool mismatchBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        mismatchProduct,
                        mismatchGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                mismatchBlocked = true;
            }
            Assert(
                mismatchBlocked &&
                File.Exists(mismatchCapture) &&
                !File.Exists(
                    EquipmentSlotLegacyMigration
                        .GetGlobalArchivePath(
                            mismatchGlobal,
                            mismatchHash)),
                "A drifted interrupted capture must remain byte-preserved and must never publish under the Product source hash.");

            string multipleRoot =
                CreateMigrationRoot(
                    "interrupted-global-capture-multiple");
            string multipleGlobal =
                Path.Combine(
                    multipleRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string multipleProduct =
                Path.Combine(
                    multipleRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                multipleGlobal,
                LegacyFlatJson(3));
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-product-publish")
                        {
                            throw new IOException(
                                "stage-multiple-capture");
                        }
                    }).LoadOrMigrate(
                        multipleProduct,
                        multipleGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
            }
            string firstCapture =
                multipleGlobal +
                ".migration-capture-first";
            string secondCapture =
                multipleGlobal +
                ".migration-capture-second";
            File.Move(
                multipleGlobal,
                firstCapture);
            File.Copy(
                firstCapture,
                secondCapture);
            bool multipleBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        multipleProduct,
                        multipleGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                multipleBlocked = true;
            }
            Assert(
                multipleBlocked &&
                File.Exists(firstCapture) &&
                File.Exists(secondCapture),
                "Multiple capture authorities must remain untouched and fail closed.");

            string replacementRoot =
                CreateMigrationRoot(
                    "interrupted-global-capture-replacement");
            string replacementGlobal =
                Path.Combine(
                    replacementRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string replacementProduct =
                Path.Combine(
                    replacementRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                replacementGlobal,
                LegacyFlatJson(3));
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "after-product-publish")
                        {
                            throw new IOException(
                                "stage-replacement-capture");
                        }
                    }).LoadOrMigrate(
                        replacementProduct,
                        replacementGlobal,
                        Scope(),
                        out _);
            }
            catch (IOException)
            {
            }
            string replacementCapture =
                replacementGlobal +
                ".migration-capture-replacement";
            File.Move(
                replacementGlobal,
                replacementCapture);
            byte[] replacementBytes =
                System.Text.Encoding.UTF8.GetBytes(
                    LegacyFlatJson(3)
                        .Replace(
                            "passive-hat",
                            "replacement-passive-hat",
                            StringComparison.Ordinal));
            File.WriteAllBytes(
                replacementGlobal,
                replacementBytes);
            bool replacementBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        replacementProduct,
                        replacementGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                replacementBlocked = true;
            }
            Assert(
                replacementBlocked &&
                File.Exists(replacementCapture) &&
                File.Exists(replacementGlobal) &&
                EqualBytes(
                    replacementBytes,
                    File.ReadAllBytes(
                        replacementGlobal)),
                "A replacement active global must not be consumed while an interrupted exact capture is awaiting recovery.");
        }

        private static void
            CaptureResidueBlocksEmptyAuthorityCreation()
        {
            string root =
                CreateMigrationRoot(
                    "capture-only-empty-authority");
            string globalPath =
                Path.Combine(
                    root,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string productPath =
                Path.Combine(
                    root,
                    "slot-2",
                    Path.GetFileName(globalPath));
            string capturePath =
                globalPath +
                ".migration-capture-orphan";
            File.WriteAllText(
                capturePath,
                LegacyFlatJson(3));
            byte[] captureBytes =
                File.ReadAllBytes(capturePath);
            bool blocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        productPath,
                        globalPath,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                blocked = true;
            }
            Assert(
                blocked &&
                !File.Exists(productPath) &&
                EqualBytes(
                    captureBytes,
                    File.ReadAllBytes(capturePath)),
                "Capture-only residue without Product/global must forbid empty Product authority and retain the captured item bytes.");

            foreach (bool usePrevious in
                new[] { false, true })
            {
                string authorityRoot =
                    CreateMigrationRoot(
                        usePrevious
                            ? "empty-other-previous-authority"
                            : "empty-other-live-authority");
                string authorityGlobal =
                    Path.Combine(
                        authorityRoot,
                        Path.GetFileName(globalPath));
                string emptyTarget =
                    Path.Combine(
                        authorityRoot,
                        "slot-2",
                        Path.GetFileName(globalPath));
                string otherProduct =
                    Path.Combine(
                        authorityRoot,
                        "slot-3",
                        Path.GetFileName(globalPath));
                EquipmentSlotStorageDocument other =
                    StageIndependentHistoricalAuthority(
                        usePrevious
                            ? "empty-other-previous-source"
                            : "empty-other-live-source",
                        authorityGlobal,
                        otherProduct,
                        FourthScope(),
                        PreSchemaGlobalJson()
                            .Replace(
                                "historical-button",
                                usePrevious
                                    ? "empty-previous-item"
                                    : "empty-live-item",
                                StringComparison.Ordinal));
                File.Delete(
                    EquipmentSlotLegacyMigration
                        .GetGlobalArchivePath(
                            authorityGlobal,
                            other.LegacyMigration!
                                .SourceSha256));
                string retainedPath =
                    otherProduct;
                if (usePrevious)
                {
                    retainedPath =
                        otherProduct + ".previous";
                    File.Move(
                        otherProduct,
                        retainedPath);
                    File.WriteAllText(
                        otherProduct,
                        "{recoverably-corrupt-live");
                }
                byte[] retainedBytes =
                    File.ReadAllBytes(retainedPath);
                EquipmentSlotStorageDocument emptyBesideAuthority =
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            emptyTarget,
                            authorityGlobal,
                            Scope(),
                            out _);
                Assert(
                    emptyBesideAuthority.Slots.TrueForAll(
                        slot => !slot.IsOccupied) &&
                    !File.Exists(emptyTarget) &&
                    EqualBytes(
                        retainedBytes,
                        File.ReadAllBytes(retainedPath)),
                    "A valid Product authority in another canonical scope must not turn terminal per-save storage into a one-save-only product or forbid an in-memory empty state for a new save.");
            }

            string archiveRoot =
                CreateMigrationRoot(
                    "archive-only-empty-authority");
            string archiveGlobal =
                Path.Combine(
                    archiveRoot,
                    Path.GetFileName(globalPath));
            string archiveProduct =
                Path.Combine(
                    archiveRoot,
                    "slot-2",
                    Path.GetFileName(globalPath));
            string archiveResidue =
                archiveGlobal +
                ".migrated-product-v3-" +
                new string('A', 64);
            File.WriteAllText(
                archiveResidue,
                PreSchemaGlobalJson());
            byte[] archiveBytes =
                File.ReadAllBytes(archiveResidue);
            bool archiveBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        archiveProduct,
                        archiveGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                archiveBlocked = true;
            }
            Assert(
                archiveBlocked &&
                !File.Exists(archiveProduct) &&
                EqualBytes(
                    archiveBytes,
                    File.ReadAllBytes(archiveResidue)),
                "An unscoped deterministic global archive must remain unclaimed but forbid silent empty authority creation.");

            foreach (int backupSlot in
                new[] { 2, 3 })
            {
                string backupRoot =
                    CreateMigrationRoot(
                        "backup-only-empty-authority-" +
                        backupSlot);
                string backupGlobal =
                    Path.Combine(
                        backupRoot,
                        Path.GetFileName(globalPath));
                string backupProduct =
                    Path.Combine(
                        backupRoot,
                        "slot-2",
                        Path.GetFileName(globalPath));
                string backupPath =
                    Path.Combine(
                        backupRoot,
                        "slot-" + backupSlot,
                        ".legacy-migrations",
                        new string(
                            backupSlot == 2
                                ? 'B'
                                : 'C',
                            64) +
                        ".flat.json");
                Directory.CreateDirectory(
                    Path.GetDirectoryName(backupPath)!);
                File.WriteAllText(
                    backupPath,
                    LegacyFlatJson(
                        3,
                        archiveIndex: backupSlot));
                byte[] backupBytes =
                    File.ReadAllBytes(backupPath);
                bool backupBlocked = false;
                EquipmentSlotStorageDocument? backupEmpty =
                    null;
                try
                {
                    backupEmpty =
                        new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            backupProduct,
                            backupGlobal,
                            Scope(),
                            out _);
                }
                catch (InvalidDataException)
                {
                    backupBlocked = true;
                }
                Assert(
                    (backupSlot == 2
                        ? backupBlocked
                        : !backupBlocked &&
                            backupEmpty != null &&
                            backupEmpty.Slots.TrueForAll(
                                slot => !slot.IsOccupied)) &&
                    !File.Exists(backupProduct) &&
                    EqualBytes(
                        backupBytes,
                        File.ReadAllBytes(backupPath)),
                    backupSlot == 2
                        ? "A current-scope legacy backup must remain byte-preserved and forbid silent empty Product creation."
                        : "Another scope's terminal legacy backup must remain byte-preserved without forbidding an in-memory empty state for the current save.");
            }

            string completedRoot =
                CreateMigrationRoot(
                    "completed-other-scope-allows-empty");
            string completedGlobal =
                Path.Combine(
                    completedRoot,
                    Path.GetFileName(globalPath));
            string completedProduct =
                Path.Combine(
                    completedRoot,
                    "slot-2",
                    Path.GetFileName(globalPath));
            string newSaveProduct =
                Path.Combine(
                    completedRoot,
                    "slot-3",
                    Path.GetFileName(globalPath));
            File.WriteAllText(
                completedGlobal,
                PreSchemaGlobalJson());
            EquipmentSlotStorageDocument completed =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        completedProduct,
                        completedGlobal,
                        Scope(),
                        out _);
            string completedHash =
                completed.LegacyMigration!
                    .SourceSha256;
            string completedArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        completedGlobal,
                        completedHash);
            string completedWinner =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalWinnerPath(
                        completedGlobal);
            string completedEvidence =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        completedGlobal,
                        completedHash);
            string completedBackup =
                EquipmentSlotLegacyMigration
                    .GetLegacyBackupPath(
                        completedProduct,
                        completedHash);
            completed.Scope.TotalGameSeconds =
                1000;
            completed.Generation++;
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    completedProduct,
                    completed);
            string completedPrevious =
                completedProduct +
                ".previous";
            byte[] completedProductBytes =
                File.ReadAllBytes(completedProduct);
            byte[] completedPreviousBytes =
                File.ReadAllBytes(completedPrevious);
            byte[] completedArchiveBytes =
                File.ReadAllBytes(completedArchive);
            byte[] completedWinnerBytes =
                File.ReadAllBytes(completedWinner);
            byte[] completedEvidenceBytes =
                File.ReadAllBytes(completedEvidence);
            byte[] completedBackupBytes =
                File.ReadAllBytes(completedBackup);
            EquipmentSlotSaveScope completedT1Scope =
                Scope();
            completedT1Scope.TotalGameSeconds =
                1000;
            EquipmentSlotStorageDocument reloadedCompleted =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        completedProduct,
                        completedGlobal,
                        completedT1Scope,
                        out _);
            Assert(
                reloadedCompleted.Scope.TotalGameSeconds ==
                    1000 &&
                EqualBytes(
                    completedWinnerBytes,
                    File.ReadAllBytes(completedWinner)),
                "The original save must cold-load its T1 Product through an immutable completed T0 winner without rewriting that winner.");

            string lateLoserHash =
                new string('D', 64);
            string lateLoserClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        completedGlobal,
                        lateLoserHash);
            File.WriteAllText(
                lateLoserClaim,
                PreSchemaClaimJson(
                    completedGlobal,
                    newSaveProduct,
                    FourthScope(),
                    lateLoserHash,
                    "pending"));
            string lateLoserTransition =
                lateLoserClaim +
                ".transition-retained";
            File.Copy(
                lateLoserClaim,
                lateLoserTransition);
            string corruptLoserClaim =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimPath(
                        completedGlobal,
                        new string('E', 64));
            File.WriteAllText(
                corruptLoserClaim,
                "{corrupt-optional-loser");
            byte[] lateLoserClaimBytes =
                File.ReadAllBytes(lateLoserClaim);
            byte[] lateLoserTransitionBytes =
                File.ReadAllBytes(lateLoserTransition);
            byte[] corruptLoserBytes =
                File.ReadAllBytes(corruptLoserClaim);
            EquipmentSlotStorageDocument newSaveEmpty =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        newSaveProduct,
                        completedGlobal,
                        FourthScope(),
                        out _);
            Assert(
                newSaveEmpty.Slots.TrueForAll(
                    slot => !slot.IsOccupied) &&
                !File.Exists(newSaveProduct) &&
                !File.Exists(completedGlobal) &&
                EqualBytes(
                    completedProductBytes,
                    File.ReadAllBytes(completedProduct)) &&
                EqualBytes(
                    completedPreviousBytes,
                    File.ReadAllBytes(completedPrevious)) &&
                EqualBytes(
                    completedArchiveBytes,
                    File.ReadAllBytes(completedArchive)) &&
                EqualBytes(
                    completedWinnerBytes,
                    File.ReadAllBytes(completedWinner)) &&
                EqualBytes(
                    completedEvidenceBytes,
                    File.ReadAllBytes(completedEvidence)) &&
                EqualBytes(
                    completedBackupBytes,
                    File.ReadAllBytes(completedBackup)) &&
                EqualBytes(
                    lateLoserClaimBytes,
                    File.ReadAllBytes(lateLoserClaim)) &&
                EqualBytes(
                    lateLoserTransitionBytes,
                    File.ReadAllBytes(lateLoserTransition)) &&
                EqualBytes(
                    corruptLoserBytes,
                    File.ReadAllBytes(corruptLoserClaim)),
                "A validated completed winner at T0 must remain winner-first after A reloads T1, retaining B-targeting and corrupt late-loser evidence byte-exact while permitting B's in-memory empty state.");

            foreach (bool identityDrift in
                new[] { false, true })
            {
                string invalidRoot =
                    CreateMigrationRoot(
                        identityDrift
                            ? "completed-winner-identity-drift"
                            : "completed-winner-stale-product");
                string invalidGlobal =
                    Path.Combine(
                        invalidRoot,
                        Path.GetFileName(globalPath));
                string invalidProduct =
                    Path.Combine(
                        invalidRoot,
                        "slot-2",
                        Path.GetFileName(globalPath));
                string invalidNewSave =
                    Path.Combine(
                        invalidRoot,
                        "slot-3",
                        Path.GetFileName(globalPath));
                File.WriteAllText(
                    invalidGlobal,
                    PreSchemaGlobalJson());
                EquipmentSlotStorageDocument invalid =
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            invalidProduct,
                            invalidGlobal,
                            Scope(),
                            out _);
                if (identityDrift)
                {
                    string driftedJson =
                        File.ReadAllText(
                            invalidProduct)
                        .Replace(
                            "\"playerName\":\"third-save\"",
                            "\"playerName\":\"different-player\"",
                            StringComparison.Ordinal);
                    File.WriteAllText(
                        invalidProduct,
                        driftedJson);
                }
                else
                {
                    invalid.Scope.TotalGameSeconds =
                        99;
                    invalid.Generation++;
                    new EquipmentSlotDocumentStore()
                        .WriteAtomic(
                            invalidProduct,
                            invalid);
                }
                byte[] invalidProductBytes =
                    File.ReadAllBytes(invalidProduct);
                bool invalidBlocked = false;
                try
                {
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            invalidNewSave,
                            invalidGlobal,
                            FourthScope(),
                            out _);
                }
                catch (InvalidDataException)
                {
                    invalidBlocked = true;
                }
                Assert(
                    invalidBlocked &&
                    !File.Exists(invalidNewSave) &&
                    EqualBytes(
                        invalidProductBytes,
                        File.ReadAllBytes(invalidProduct)),
                    identityDrift
                        ? "A completed winner must fail closed when its Product authority drifts to another save identity."
                        : "A completed winner must fail closed when its Product authority is older than the immutable migration revision.");
            }

            string emptyRoot =
                CreateMigrationRoot(
                    "capture-empty-negative-control");
            string emptyGlobal =
                Path.Combine(
                    emptyRoot,
                    Path.GetFileName(globalPath));
            string emptyProduct =
                Path.Combine(
                    emptyRoot,
                    "slot-2",
                    Path.GetFileName(globalPath));
            EquipmentSlotStorageDocument empty =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        emptyProduct,
                        emptyGlobal,
                        Scope(),
                        out _);
            Assert(
                empty.Slots.TrueForAll(
                    slot =>
                        string.IsNullOrWhiteSpace(
                            slot.ItemId)) &&
                !File.Exists(emptyProduct) &&
                Directory.GetFiles(
                    emptyRoot,
                    "*",
                    SearchOption.AllDirectories).Length == 0 &&
                Directory.GetDirectories(
                    emptyRoot,
                    "*",
                    SearchOption.AllDirectories).Length == 0,
                "A true empty installation must return only an in-memory empty state and must not retain an operation lock or empty claim directories.");
        }

        private static void
            GlobalFlatFinalStateRejectsRecreatedAuthority()
        {
            foreach (bool sameBytes in
                new[] { true, false })
            {
                string root =
                    CreateMigrationRoot(
                        sameBytes
                            ? "global-flat-final-same"
                            : "global-flat-final-different");
                string globalPath =
                    Path.Combine(
                        root,
                        "equipment-slots-" +
                        MoreEquipmentSlotsProductContract.UniqueId +
                        ".json");
                string productPath =
                    Path.Combine(
                        root,
                        "slot-2",
                        "equipment-slots-" +
                        MoreEquipmentSlotsProductContract.UniqueId +
                        ".json");
                File.WriteAllText(
                    globalPath,
                    LegacyFlatJson(3));
                byte[] sourceBytes =
                    File.ReadAllBytes(
                        globalPath);
                string sourceHash =
                    EquipmentSlotLegacyMigration
                        .ComputeFileSha256(
                            globalPath);
                string archivePath =
                    EquipmentSlotLegacyMigration
                        .GetGlobalArchivePath(
                            globalPath,
                            sourceHash);
                byte[] replacementBytes =
                    sameBytes
                        ? sourceBytes
                        : System.Text.Encoding.UTF8.GetBytes(
                            LegacyFlatJson(3)
                                .Replace(
                                    "defense-hat",
                                    "replacement-defense-hat",
                                    StringComparison.Ordinal));
                bool blocked = false;
                try
                {
                    new EquipmentSlotDocumentStore(
                        stage =>
                        {
                            if (stage ==
                                "after-global-archive")
                            {
                                File.WriteAllBytes(
                                    globalPath,
                                    replacementBytes);
                            }
                        }).LoadOrMigrate(
                            productPath,
                            globalPath,
                            Scope(),
                            out _);
                }
                catch (InvalidDataException)
                {
                    blocked = true;
                }
                Assert(
                    blocked &&
                    File.Exists(productPath) &&
                    File.Exists(globalPath) &&
                    File.Exists(archivePath) &&
                    EqualBytes(
                        replacementBytes,
                        File.ReadAllBytes(
                            globalPath)) &&
                    EqualBytes(
                        sourceBytes,
                        File.ReadAllBytes(
                            archivePath)),
                    "GlobalFlat must not report terminal success when an " +
                    (sameBytes ? "exact" : "different") +
                    " active global is recreated after archival.");
            }

            string conflictRoot =
                CreateMigrationRoot(
                    "global-flat-existing-different-archive");
            string conflictGlobal =
                Path.Combine(
                    conflictRoot,
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string conflictProduct =
                Path.Combine(
                    conflictRoot,
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                conflictGlobal,
                LegacyFlatJson(3));
            byte[] conflictSource =
                File.ReadAllBytes(
                    conflictGlobal);
            string conflictHash =
                EquipmentSlotLegacyMigration
                    .ComputeFileSha256(
                        conflictGlobal);
            string conflictArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        conflictGlobal,
                        conflictHash);
            byte[] differentArchive =
                System.Text.Encoding.UTF8.GetBytes(
                    "different-archive");
            bool archiveBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore(
                    stage =>
                    {
                        if (stage ==
                            "before-global-archive")
                        {
                            File.WriteAllBytes(
                                conflictArchive,
                                differentArchive);
                        }
                    }).LoadOrMigrate(
                        conflictProduct,
                        conflictGlobal,
                        Scope(),
                        out _);
            }
            catch (InvalidDataException)
            {
                archiveBlocked = true;
            }
            Assert(
                archiveBlocked &&
                File.Exists(conflictProduct) &&
                File.Exists(conflictGlobal) &&
                File.Exists(conflictArchive) &&
                EqualBytes(
                    conflictSource,
                    File.ReadAllBytes(
                        conflictGlobal)) &&
                EqualBytes(
                    differentArchive,
                    File.ReadAllBytes(
                        conflictArchive)),
                "A different existing GlobalFlat archive must retain both byte authorities and restore the captured source fail-closed.");
        }

        private static void
            GlobalFlatTerminalArchiveAllowsAnotherSaveEmpty()
        {
            string root =
                CreateMigrationRoot(
                    "global-flat-terminal-other-save-empty");
            string fileName =
                "equipment-slots-" +
                MoreEquipmentSlotsProductContract.UniqueId +
                ".json";
            string globalPath =
                Path.Combine(root, fileName);
            string firstProductPath =
                Path.Combine(
                    root,
                    "slot-2",
                    fileName);
            string secondProductPath =
                Path.Combine(
                    root,
                    "slot-3",
                    fileName);
            File.WriteAllText(
                globalPath,
                LegacyFlatJson(3));
            EquipmentSlotStorageDocument first =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProductPath,
                        globalPath,
                        Scope(),
                        out _);
            string sourceSha256 =
                first.LegacyMigration!.SourceSha256;
            string archivePath =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        globalPath,
                        sourceSha256);
            byte[] productBytes =
                File.ReadAllBytes(firstProductPath);
            byte[] archiveBytes =
                File.ReadAllBytes(archivePath);

            EquipmentSlotStorageDocument second =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        secondProductPath,
                        globalPath,
                        FourthScope(),
                        out _);
            EquipmentSlotStorageDocument reloadedFirst =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        firstProductPath,
                        globalPath,
                        Scope(),
                        out _);
            Assert(
                second.Slots.TrueForAll(
                    slot => !slot.IsOccupied) &&
                !File.Exists(secondProductPath) &&
                reloadedFirst.Slots[2].ItemId ==
                    "shield-hat" &&
                EqualBytes(
                    productBytes,
                    File.ReadAllBytes(firstProductPath)) &&
                EqualBytes(
                    archiveBytes,
                    File.ReadAllBytes(archivePath)),
                "One exact canonical GlobalFlat Product must bind its deterministic archive without preventing another save from establishing an in-memory empty state.");

            foreach (string invalidKind in
                new[] { "missing", "wrong-stamp", "ambiguous" })
            {
                string invalidRoot =
                    CreateMigrationRoot(
                        "global-flat-terminal-" +
                        invalidKind);
                string invalidGlobal =
                    Path.Combine(
                        invalidRoot,
                        fileName);
                string invalidFirst =
                    Path.Combine(
                        invalidRoot,
                        "slot-2",
                        fileName);
                string invalidSecond =
                    Path.Combine(
                        invalidRoot,
                        "slot-3",
                        fileName);
                File.WriteAllText(
                    invalidGlobal,
                    LegacyFlatJson(3));
                EquipmentSlotStorageDocument authority =
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            invalidFirst,
                            invalidGlobal,
                            Scope(),
                            out _);
                string invalidHash =
                    authority.LegacyMigration!
                        .SourceSha256;
                string invalidArchive =
                    EquipmentSlotLegacyMigration
                        .GetGlobalArchivePath(
                            invalidGlobal,
                            invalidHash);
                if (invalidKind == "missing")
                {
                    File.Delete(invalidFirst);
                }
                else if (invalidKind == "wrong-stamp")
                {
                    string json =
                        File.ReadAllText(invalidFirst);
                    File.WriteAllText(
                        invalidFirst,
                        json.Replace(
                            invalidHash,
                            new string('A', 64),
                            StringComparison.Ordinal));
                }
                else
                {
                    var fifthScope =
                        new EquipmentSlotSaveScope
                        {
                            ArchiveIndex = 4,
                            PlayerName = "fifth-save",
                            CustomPlayerName = "fifth-save",
                            TotalGameSeconds = 100
                        };
                    authority.Scope =
                        fifthScope.Clone();
                    authority.Generation++;
                    new EquipmentSlotDocumentStore()
                        .WriteAtomic(
                            Path.Combine(
                                invalidRoot,
                                "slot-4",
                                fileName),
                            authority);
                }

                byte[] invalidArchiveBytes =
                    File.ReadAllBytes(invalidArchive);
                bool blocked = false;
                try
                {
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            invalidSecond,
                            invalidGlobal,
                            FourthScope(),
                            out _);
                }
                catch (InvalidDataException)
                {
                    blocked = true;
                }
                Assert(
                    blocked &&
                    !File.Exists(invalidSecond) &&
                    EqualBytes(
                        invalidArchiveBytes,
                        File.ReadAllBytes(invalidArchive)),
                    "A " +
                    invalidKind +
                    " GlobalFlat Product binding must leave the deterministic archive byte-exact and fail closed instead of authorizing another save's empty state.");
            }
        }

        private static void
            LegacyBackupPublicationRetriesEveryWindow()
        {
            string root = CreateMigrationRoot("backup-windows");
            foreach (string stage in new[]
            {
                "before-backup-temp-write",
                "after-backup-temp-flush",
                "before-backup-publish",
                "after-backup-publish"
            })
            {
                string stageRoot =
                    Path.Combine(root, stage);
                Directory.CreateDirectory(stageRoot);
                string path = Path.Combine(
                    stageRoot,
                    "equipment-slots-" +
                        MoreEquipmentSlotsProductContract.UniqueId +
                        ".json");
                File.WriteAllText(path, LegacyFlatJson(2));
                byte[] source = File.ReadAllBytes(path);
                bool failed = false;
                try
                {
                    new EquipmentSlotDocumentStore(
                        observed =>
                        {
                            if (observed == stage)
                                throw new IOException(stage);
                        }).LoadOrMigrate(
                            path,
                            Path.Combine(root, "missing-global.json"),
                            Scope(),
                            out _);
                }
                catch (IOException)
                {
                    failed = true;
                }
                Assert(
                    failed &&
                    EqualBytes(source, File.ReadAllBytes(path)),
                    stage +
                    " must leave the scoped legacy authority byte-identical.");

                EquipmentSlotStorageDocument retried =
                    new EquipmentSlotDocumentStore().LoadOrMigrate(
                        path,
                        Path.Combine(root, "missing-global.json"),
                        Scope(),
                        out _);
                string backup =
                    EquipmentSlotLegacyMigration.GetLegacyBackupPath(
                        path,
                        retried.LegacyMigration!.SourceSha256);
                Assert(
                    retried.Slots[2].ItemId == "shield-hat" &&
                    File.Exists(backup) &&
                    EqualBytes(source, File.ReadAllBytes(backup)) &&
                    Directory.GetFiles(
                            Path.GetDirectoryName(backup)!,
                            ".tmp-*")
                        .Length == 0 &&
                    Directory.GetFiles(
                            Path.GetDirectoryName(backup)!,
                            ".invalid-*")
                        .Length == 0,
                    stage +
                    " must converge on retry with one exact published backup and no partial publication files.");
            }

            string corruptPath =
                Path.Combine(root, "corrupt-final-backup.json");
            File.WriteAllText(corruptPath, LegacyFlatJson(3));
            byte[] corruptSource = File.ReadAllBytes(corruptPath);
            string corruptHash =
                EquipmentSlotLegacyMigration.ComputeFileSha256(
                    corruptPath);
            string corruptBackup =
                EquipmentSlotLegacyMigration.GetLegacyBackupPath(
                    corruptPath,
                    corruptHash);
            Directory.CreateDirectory(
                Path.GetDirectoryName(corruptBackup)!);
            File.WriteAllText(corruptBackup, "partial");
            EquipmentSlotStorageDocument recovered =
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    corruptPath,
                    Path.Combine(root, "missing-global.json"),
                    Scope(),
                    out _);
            Assert(
                recovered.Slots[0].ItemId == "passive-hat" &&
                EqualBytes(
                    corruptSource,
                    File.ReadAllBytes(corruptBackup)),
                "A partial non-authoritative final backup must be isolated and replaced from the unchanged legacy authority.");
        }

        private static void
            LegacyMigrationRejectsWrongScopeFutureAndExcessSlots()
        {
            string root = CreateMigrationRoot("fail-closed");
            foreach (
                (string name, string json) fixture in
                new[]
                {
                    (
                        "wrong-scope.json",
                        LegacyFlatJson(2, archiveIndex: 1)),
                    (
                        "future.json",
                        LegacyFlatJson(4)),
                    (
                        "fourth-occupied.json",
                        LegacyFlatJson(
                            3,
                            appendFourthOccupied: true))
                })
            {
                string path = Path.Combine(root, fixture.name);
                File.WriteAllText(path, fixture.json);
                byte[] before = File.ReadAllBytes(path);
                bool failed = false;
                try
                {
                    new EquipmentSlotDocumentStore()
                        .LoadOrMigrate(
                            path,
                            Path.Combine(root, "missing-global.json"),
                            Scope(),
                            out _);
                }
                catch (InvalidDataException)
                {
                    failed = true;
                }
                Assert(
                    failed &&
                    EqualBytes(before, File.ReadAllBytes(path)),
                    fixture.name +
                    " must fail closed without replacing the legacy authority or publishing an empty Product document.");
            }
        }

        private static void
            LegacyMigrationRejectsIdentityAndMalformedSlots()
        {
            string root = CreateMigrationRoot("identity-and-shape");
            string baseline = LegacyFlatJson(2);
            var fixtures =
                new Dictionary<string, string>
                {
                    ["empty-owner.json"] =
                        baseline.Replace(
                            "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"",
                            "\"ownerId\":\"\"",
                            StringComparison.Ordinal),
                    ["wrong-owner.json"] =
                        baseline.Replace(
                            "DTMAPI.MoreEquipmentSlotsMod",
                            "Other.Owner",
                            StringComparison.Ordinal),
                    ["wrong-storage-scope.json"] =
                        baseline.Replace(
                            "\"storageScope\":\"slot-2\"",
                            "\"storageScope\":\"slot-1\"",
                            StringComparison.Ordinal),
                    ["wrong-player.json"] =
                        baseline.Replace(
                            "\"customPlayerName\":\"third-save\"",
                            "\"customPlayerName\":\"other-save\"",
                            StringComparison.Ordinal),
                    ["save-clock-regressed.json"] =
                        baseline.Replace(
                            "\"savedTotalGameSeconds\":100",
                            "\"savedTotalGameSeconds\":401",
                            StringComparison.Ordinal),
                    ["duplicate-slot.json"] =
                        baseline.Replace(
                            "{\"index\":1,\"itemId\":\"defense-hat\"",
                            "{\"index\":0,\"itemId\":\"defense-hat\"",
                            StringComparison.Ordinal),
                    ["negative-slot.json"] =
                        baseline.Replace(
                            "{\"index\":0,\"itemId\":\"passive-hat\"",
                            "{\"index\":-1,\"itemId\":\"passive-hat\"",
                            StringComparison.Ordinal),
                    ["invalid-shield.json"] =
                        baseline.Replace(
                            "\"shieldValue\":7",
                            "\"shieldValue\":11",
                            StringComparison.Ordinal),
                    ["bad-json.json"] = "{ invalid-json ]"
                };

            foreach (KeyValuePair<string, string> fixture in fixtures)
            {
                string path = Path.Combine(root, fixture.Key);
                File.WriteAllText(path, fixture.Value);
                byte[] before = File.ReadAllBytes(path);
                bool failed = false;
                try
                {
                    new EquipmentSlotDocumentStore().LoadOrMigrate(
                        path,
                        Path.Combine(root, "missing-global.json"),
                        Scope(),
                        out _);
                }
                catch (InvalidDataException)
                {
                    failed = true;
                }
                Assert(
                    failed &&
                    EqualBytes(before, File.ReadAllBytes(path)),
                    fixture.Key +
                    " must remain byte-identical and publish no Product authority.");
            }

            string globalPath = Path.Combine(
                root,
                "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            string productPath = Path.Combine(
                root,
                "slot-2",
                "equipment-slots-" +
                    MoreEquipmentSlotsProductContract.UniqueId +
                    ".json");
            File.WriteAllText(
                globalPath,
                LegacyFlatJson(3, archiveIndex: 1));
            byte[] globalBefore = File.ReadAllBytes(globalPath);
            bool wrongGlobalFailed = false;
            try
            {
                new EquipmentSlotDocumentStore().LoadOrMigrate(
                    productPath,
                    globalPath,
                    Scope(),
                    out _);
            }
            catch (InvalidDataException)
            {
                wrongGlobalFailed = true;
            }
            Assert(
                wrongGlobalFailed &&
                !File.Exists(productPath) &&
                EqualBytes(
                    globalBefore,
                    File.ReadAllBytes(globalPath)),
                "A global flat document with explicit other-save identity must not bypass scope validation.");
        }

        private static void ProductRevisionAndPreviousAuthorityRules()
        {
            string root =
                CreateMigrationRoot("revision-and-previous");
            string revisionPath =
                Path.Combine(root, "revision.json");
            EquipmentSlotStorageDocument ahead =
                CreateOccupiedDocument("revision-item");
            ahead.Generation = 1;
            ahead.Scope.TotalGameSeconds = 401;
            new EquipmentSlotDocumentStore().WriteAtomic(
                revisionPath,
                ahead);
            bool revisionFailed = false;
            try
            {
                new EquipmentSlotDocumentStore().Load(
                    revisionPath,
                    Scope());
            }
            catch (InvalidDataException)
            {
                revisionFailed = true;
            }
            Assert(
                revisionFailed,
                "A Product committed revision more than the frozen tolerance ahead of the native save must fail closed.");

            foreach (
                (string name, string live, bool expectedSuccess)
                fixture in
                new[]
                {
                    ("missing", string.Empty, true),
                    ("corrupt", "{ invalid-json ]", true),
                    (
                        "future",
                        "{\"schemaVersion\":4,\"scope\":{\"archiveIndex\":2,\"playerName\":\"third-save\",\"customPlayerName\":\"third-save\",\"totalGameSeconds\":100}}",
                        false),
                    (
                        "ambiguous",
                        "{\"schemaVersion\":3,\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"scope\":{\"archiveIndex\":2,\"playerName\":\"third-save\",\"customPlayerName\":\"third-save\",\"totalGameSeconds\":100}}",
                        false)
                })
            {
                string path = Path.Combine(
                    root,
                    fixture.name + ".json");
                EquipmentSlotStorageDocument previous =
                    CreateOccupiedDocument(
                        fixture.name + "-previous");
                previous.Generation = 2;
                previous.Scope.TotalGameSeconds = 100;
                new EquipmentSlotDocumentStore().WriteAtomic(
                    path + ".previous",
                    previous);
                if (fixture.live.Length > 0)
                    File.WriteAllText(path, fixture.live);

                bool loaded = false;
                EquipmentSlotStorageDocument? result = null;
                try
                {
                    result =
                        new EquipmentSlotDocumentStore()
                            .LoadOrMigrate(
                                path,
                                Path.Combine(
                                    root,
                                    "missing-global.json"),
                                Scope(),
                                out _);
                    loaded = true;
                }
                catch (InvalidDataException)
                {
                }
                Assert(
                    loaded == fixture.expectedSuccess &&
                    (!loaded ||
                     result!.Slots[0].ItemId ==
                        fixture.name + "-previous"),
                    fixture.name +
                    " must obey the reachable Product previous-generation authority rule.");
            }
        }

        private static void ProductV3RequiresNativeSaveClock()
        {
            string root =
                CreateMigrationRoot(
                    "product-save-clock-required");
            string productPath =
                Path.Combine(
                    root,
                    "product.json");
            EquipmentSlotStorageDocument valid =
                CreateOccupiedDocument(
                    "clocked-item");
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    productPath,
                    valid);
            string validJson =
                File.ReadAllText(productPath);
            string missingClockJson =
                validJson.Replace(
                    ",\"totalGameSeconds\":100",
                    string.Empty,
                    StringComparison.Ordinal);
            Assert(
                !string.Equals(
                    validJson,
                    missingClockJson,
                    StringComparison.Ordinal),
                "The missing-clock fixture must remove the Product v3 native save clock.");
            File.WriteAllText(
                productPath,
                missingClockJson);
            byte[] missingClockBytes =
                File.ReadAllBytes(productPath);
            bool storedClockBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .Load(
                        productPath,
                        Scope());
            }
            catch (InvalidDataException)
            {
                storedClockBlocked = true;
            }
            Assert(
                storedClockBlocked &&
                EqualBytes(
                    missingClockBytes,
                    File.ReadAllBytes(productPath)),
                "A Product v3 authority without TotalGameSeconds must fail closed and remain byte-identical.");

            File.WriteAllText(
                productPath,
                validJson);
            var missingCurrentClock =
                Scope();
            missingCurrentClock.TotalGameSeconds =
                null;
            bool currentClockBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .Load(
                        productPath,
                        missingCurrentClock);
            }
            catch (InvalidDataException)
            {
                currentClockBlocked = true;
            }
            Assert(
                currentClockBlocked,
                "A Product v3 authority must not load when the current native scope cannot prove TotalGameSeconds.");

            string preSchemaRoot =
                CreateMigrationRoot(
                    "preschema-save-clock-required");
            string preSchemaGlobal =
                Path.Combine(
                    preSchemaRoot,
                    Path.GetFileName(productPath));
            string preSchemaProduct =
                Path.Combine(
                    preSchemaRoot,
                    "slot-2",
                    Path.GetFileName(productPath));
            File.WriteAllText(
                preSchemaGlobal,
                PreSchemaGlobalJson());
            byte[] preSchemaBytes =
                File.ReadAllBytes(preSchemaGlobal);
            bool preSchemaBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        preSchemaProduct,
                        preSchemaGlobal,
                        missingCurrentClock,
                        out _);
            }
            catch (InvalidDataException)
            {
                preSchemaBlocked = true;
            }
            string claimDirectory =
                EquipmentSlotLegacyMigration
                    .GetPreSchemaGlobalClaimDirectory(
                        preSchemaGlobal);
            Assert(
                preSchemaBlocked &&
                !File.Exists(preSchemaProduct) &&
                EqualBytes(
                    preSchemaBytes,
                    File.ReadAllBytes(preSchemaGlobal)) &&
                !Directory.Exists(claimDirectory),
                "A pre-schema global migration without a current native save clock must fail before publishing claim, backup, Product or archive authority.");

            string missingWritePath =
                Path.Combine(
                    root,
                    "missing-write.json");
            valid.Scope.TotalGameSeconds =
                null;
            bool writeBlocked = false;
            try
            {
                new EquipmentSlotDocumentStore()
                    .WriteAtomic(
                        missingWritePath,
                        valid);
            }
            catch (InvalidDataException)
            {
                writeBlocked = true;
            }
            Assert(
                writeBlocked &&
                !File.Exists(missingWritePath),
                "A Product v3 write without TotalGameSeconds must publish no authority.");
        }

        private static void PlayerUiRoutesHeldNativeItemThroughProductionEquip()
        {
            string source = File.ReadAllText(
                Path.Combine(
                    Environment.CurrentDirectory,
                    "products",
                    "first-party",
                    "MoreEquipmentSlots",
                    "src",
                    "Native",
                    "MoreEquipmentSlotsNativeRuntime.cs"));
            Assert(
                source.Contains(
                    "TryEquipFromNativeBuffer(slotIndex)",
                    StringComparison.Ordinal) &&
                source.Contains(
                    "EquipFromBackpack(itemId, slotIndex)",
                    StringComparison.Ordinal) &&
                source.Contains(
                    "EquipmentSlotGameplayCandidateCoordinator",
                    StringComparison.Ordinal) &&
                source.Contains(
                    "TryTakeMatchingNativeBuffer(",
                    StringComparison.Ordinal) &&
                source.Contains(
                    "MarkWorkingDirty(\"EquipFromBackpack\")",
                    StringComparison.Ordinal),
                "The real empty-slot click path must mutate only Working/native memory during the day and prepare its durable gameplay candidate at SaveSaving.");
        }

        private static void AtomicInstallerOwnsRollbackAndDeactivationPaths()
        {
            string nativeRoot = Path.Combine(
                Environment.CurrentDirectory,
                "products",
                "first-party",
                "MoreEquipmentSlots",
                "src",
                "Native");
            string installer = File.ReadAllText(
                Path.Combine(
                    nativeRoot,
                    "MoreEquipmentSlotsHookInstaller.cs"));
            string runtime = File.ReadAllText(
                Path.Combine(
                    nativeRoot,
                    "MoreEquipmentSlotsNativeRuntime.cs"));
            string callbacks = File.ReadAllText(
                Path.Combine(
                    nativeRoot,
                    "MoreEquipmentSlotsCallbacks.cs"));
            Assert(
                installer.Contains(
                    "catch (Exception installFailure)",
                    StringComparison.Ordinal) &&
                installer.Contains(
                    "UnpatchExactOwner();",
                    StringComparison.Ordinal) &&
                installer.Contains(
                    "finally",
                    StringComparison.Ordinal) &&
                installer.Contains(
                    "MoreEquipmentSlotsCallbacks.Detach(",
                    StringComparison.Ordinal) &&
                installer.Contains(
                    "ResolveTryGetShieldItem()",
                    StringComparison.Ordinal) &&
                installer.Contains(
                    "TryGetShieldItem(out IAgentEquipmentShieldItem)",
                    StringComparison.Ordinal) &&
                callbacks.Contains(
                    "AgentEquipmentManagerTryGetShieldItemPostfix",
                    StringComparison.Ordinal) &&
                callbacks.Contains(
                    "ref DolocTown.IAgentEquipmentShieldItem __0",
                    StringComparison.Ordinal) &&
                runtime.Contains(
                    "hooks.UnpatchOwnedHooks(this);",
                    StringComparison.Ordinal) &&
                !installer.Contains(
                    "ResolveBodyController",
                    StringComparison.Ordinal) &&
                !runtime.Contains(
                    "HandleAttackPrefix(",
                    StringComparison.Ordinal) &&
                !runtime.Contains(
                    "ApplyNativeAttackTail(",
                    StringComparison.Ordinal),
                "Atomic Hook installation must own exact-owner rollback, provide the product shield only through the native shield-provider boundary, and leave the official attack tail unreplicated.");
        }

        private static EquipmentSlotStorageDocument
            CreateOccupiedDocument(string itemId)
        {
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope = Scope(),
                    Generation = 1
                };
            document.Slots[0].ItemId = itemId;
            document.Slots[0].DisplayName = itemId;
            return document;
        }

        private static EquipmentSlotStorageEntry
            Replacement(string itemId) =>
            new EquipmentSlotStorageEntry
            {
                Index = 0,
                ItemId = itemId,
                DisplayName = itemId,
                SkillId = "passive"
            };

        private static SameItemReplacementScenario
            CreateSameItemReplacementScenario(
                NativePlacementKind outgoingPlacement,
                bool fromNativeBuffer)
        {
            const int beforeBackpack = 1;
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("same-item");
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        Replacement("same-item"));
            NativeRecoveryObservation before =
                Observation(
                    "save-a",
                    beforeBackpack,
                    mail: 0);
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "save-a",
                _ => before);
            int outgoingBackpack =
                beforeBackpack +
                (outgoingPlacement ==
                    NativePlacementKind.Backpack
                        ? 1
                        : 0);
            int outgoingMail =
                outgoingPlacement ==
                    NativePlacementKind.Mail
                    ? 1
                    : 0;
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                Result(outgoingPlacement),
                Observation(
                    "save-a",
                    outgoingBackpack,
                    outgoingMail));
            int committedBackpack =
                outgoingBackpack -
                (fromNativeBuffer ? 0 : 1);
            EquipmentSlotTransactionCoordinator
                .RecordIncomingWithdrawal(
                    journal,
                    succeeded: true,
                    committedBackpack,
                    fromNativeBuffer);
            return new SameItemReplacementScenario(
                document,
                journal,
                before,
                Observation(
                    "save-b",
                    committedBackpack,
                    outgoingMail));
        }

        private static EquipmentSlotStorageDocument
            PrepareAttemptedSingle(
                NativePlacementKind kind,
                int backpack,
                int mail)
        {
            EquipmentSlotStorageDocument document =
                CreateOccupiedDocument("hat");
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator.PrepareRecovery(
                    document,
                    new[] { 0 });
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "exists:10:A",
                _ => Observation("exists:10:A", 0, 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                Result(kind),
                Observation(
                    "exists:10:A",
                    backpack,
                    mail));
            return document;
        }

        private static EquipmentSlotSaveScope Scope() =>
            new EquipmentSlotSaveScope
            {
                ArchiveIndex = 2,
                PlayerName = "third-save",
                CustomPlayerName = "third-save",
                TotalGameSeconds = 100
            };

        private static EquipmentSlotSaveScope FourthScope() =>
            new EquipmentSlotSaveScope
            {
                ArchiveIndex = 3,
                PlayerName = "fourth-save",
                CustomPlayerName = "fourth-save",
                TotalGameSeconds = 100
            };

        private static string PreSchemaClaimJson(
            string globalPath,
            string targetPath,
            EquipmentSlotSaveScope scope,
            string sourceSha256,
            string state) =>
            "{" +
            "\"schemaVersion\":1," +
            "\"sourceSha256\":\"" +
            sourceSha256 +
            "\"," +
            "\"globalFileName\":\"" +
            Path.GetFileName(globalPath) +
            "\"," +
            "\"targetFileName\":\"" +
            Path.GetFileName(targetPath) +
            "\"," +
            "\"scope\":{" +
            "\"archiveIndex\":" +
            scope.ArchiveIndex +
            "," +
            "\"playerName\":\"" +
            scope.PlayerName +
            "\"," +
            "\"customPlayerName\":\"" +
            scope.CustomPlayerName +
            "\"," +
            "\"totalGameSeconds\":" +
            scope.TotalGameSeconds +
            "}," +
            "\"state\":\"" +
            state +
            "\"}";

        private static string CreateMigrationRoot(string name)
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Legacy migration fixtures require the managed DTMAPI test session.");
            string root = Path.Combine(
                sessionRoot,
                "moreequipment-migration",
                name);
            Directory.CreateDirectory(root);
            return root;
        }

        private static EquipmentSlotStorageDocument
            StageIndependentHistoricalAuthority(
                string fixtureName,
                string targetGlobalPath,
                string targetProductPath,
                EquipmentSlotSaveScope scope,
                string sourceJson)
        {
            string sourceRoot =
                CreateMigrationRoot(fixtureName);
            string sourceGlobal =
                Path.Combine(
                    sourceRoot,
                    Path.GetFileName(targetGlobalPath));
            string sourceProduct =
                Path.Combine(
                    sourceRoot,
                    "slot-" +
                        scope.ArchiveIndex,
                    Path.GetFileName(targetProductPath));
            File.WriteAllText(
                sourceGlobal,
                sourceJson);
            EquipmentSlotStorageDocument document =
                new EquipmentSlotDocumentStore()
                    .LoadOrMigrate(
                        sourceProduct,
                        sourceGlobal,
                        scope,
                        out _);
            Directory.CreateDirectory(
                Path.GetDirectoryName(targetProductPath)!);
            File.Copy(
                sourceProduct,
                targetProductPath);
            string sourceArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        sourceGlobal,
                        document.LegacyMigration!
                            .SourceSha256);
            string targetArchive =
                EquipmentSlotLegacyMigration
                    .GetGlobalArchivePath(
                        targetGlobalPath,
                        document.LegacyMigration!
                            .SourceSha256);
            File.Copy(
                sourceArchive,
                targetArchive);
            return document;
        }

        private static string LegacyFlatJson(
            int schema,
            int archiveIndex = 2,
            bool appendFourthOccupied = false)
        {
            string fourth =
                appendFourthOccupied
                    ? ",{\"index\":3,\"itemId\":\"overflow-hat\",\"displayName\":\"Overflow\"}"
                    : string.Empty;
            return
                "{" +
                "\"schemaVersion\":" + schema + "," +
                "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"," +
                "\"storageScope\":\"slot-" + archiveIndex + "\"," +
                "\"archiveIndex\":" + archiveIndex + "," +
                "\"playerName\":\"third-save\"," +
                "\"customPlayerName\":\"third-save\"," +
                "\"savedTotalGameSeconds\":100," +
                "\"generation\":7," +
                "\"slots\":[" +
                "{\"index\":0,\"itemId\":\"passive-hat\",\"displayName\":\"Passive\",\"skillId\":\"passive-skill\",\"defenseBonus\":0,\"isShieldHat\":false,\"shieldMaxValue\":0,\"shieldValue\":0,\"shieldDefend\":0}," +
                "{\"index\":1,\"itemId\":\"defense-hat\",\"displayName\":\"Defense\",\"skillId\":\"\",\"defenseBonus\":2,\"isShieldHat\":false,\"shieldMaxValue\":0,\"shieldValue\":0,\"shieldDefend\":0}," +
                "{\"index\":2,\"itemId\":\"shield-hat\",\"displayName\":\"Shield\",\"skillId\":\"\",\"defenseBonus\":0,\"isShieldHat\":true,\"shieldMaxValue\":10,\"shieldValue\":7,\"shieldDefend\":3}" +
                fourth +
                "]}";
        }

        private static string PreSchemaGlobalJson() =>
            "{" +
            "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"," +
            "\"savedAt\":\"2026-06-01T01:02:03.0000000+00:00\"," +
            "\"slots\":[" +
            "{\"index\":0,\"slotId\":\"dtmapi.extra.1\",\"itemId\":\"historical-button\",\"displayName\":\"Historical Button\",\"skillId\":\"historical-skill\",\"lastMessage\":\"stored\"}," +
            "{\"index\":1,\"slotId\":\"dtmapi.extra.2\",\"itemId\":\"\",\"displayName\":\"\",\"skillId\":\"\",\"lastMessage\":\"empty\"}," +
            "{\"index\":2,\"slotId\":\"dtmapi.extra.3\",\"itemId\":\"\",\"displayName\":\"\",\"skillId\":\"\",\"lastMessage\":\"empty\"}" +
            "]}";

        private static string RealHostFlatJson(int schema) =>
            "{" +
            "\"schemaVersion\":" + schema + "," +
            "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"," +
            "\"storageScope\":\"slot-2\"," +
            "\"archiveIndex\":2," +
            "\"playerName\":\"third-save\"," +
            "\"customPlayerName\":\"third-save\"," +
            "\"currentScene\":\"Farm\"," +
            "\"savedTotalGameSeconds\":100," +
            "\"savedAt\":\"2026-07-29T11:46:26.2812321+00:00\"," +
            "\"generation\":1," +
            "\"slots\":[" +
            "{\"index\":0,\"tailIndexFromEnd\":2,\"slotId\":\"dtmapi.extra.1\",\"itemId\":\"grandmas_button\",\"displayName\":\"grandmas_button\",\"skillId\":\"\",\"defenseBonus\":0,\"isShieldHat\":false,\"shieldMaxValue\":0,\"shieldValue\":0,\"shieldDefend\":0,\"lastMessage\":\"normal\"}," +
            "{\"index\":1,\"tailIndexFromEnd\":1,\"slotId\":\"dtmapi.extra.2\",\"itemId\":\"box_hat\",\"displayName\":\"box_hat\",\"skillId\":\"\",\"defenseBonus\":0,\"isShieldHat\":true,\"shieldMaxValue\":100,\"shieldValue\":80,\"shieldDefend\":3,\"lastMessage\":\"shield\"}," +
            "{\"index\":2,\"tailIndexFromEnd\":0,\"slotId\":\"dtmapi.extra.3\",\"itemId\":\"\",\"displayName\":\"\",\"skillId\":\"\",\"defenseBonus\":0,\"isShieldHat\":false,\"shieldMaxValue\":0,\"shieldValue\":0,\"shieldDefend\":0,\"lastMessage\":\"empty\"}" +
            "]}";

        private static string LegacyJournalJson() =>
            "{" +
            "\"schemaVersion\":3," +
            "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"," +
            "\"storageScope\":\"slot-2\"," +
            "\"archiveIndex\":2," +
            "\"playerName\":\"third-save\"," +
            "\"customPlayerName\":\"third-save\"," +
            "\"savedTotalGameSeconds\":100," +
            "\"generation\":9," +
            "\"slots\":[" +
            "{\"index\":0,\"itemId\":\"committed-hat\",\"displayName\":\"Committed\"}," +
            "{\"index\":1,\"itemId\":\"\",\"displayName\":\"\"}," +
            "{\"index\":2,\"itemId\":\"\",\"displayName\":\"\"}]," +
            "\"journal\":{" +
            "\"transactionId\":\"11111111111111111111111111111111\"," +
            "\"state\":0," +
            "\"archiveIndex\":2," +
            "\"slotIndex\":1," +
            "\"slotId\":\"dtmapi.extra.2\"," +
            "\"itemId\":\"journal-shield\"," +
            "\"displayName\":\"Journal Shield\"," +
            "\"skillId\":\"\"," +
            "\"defenseBonus\":0," +
            "\"isShieldHat\":true," +
            "\"shieldMaxValue\":10," +
            "\"shieldValue\":6," +
            "\"shieldDefend\":2," +
            "\"attemptStarted\":false," +
            "\"preSaveFingerprint\":\"old-preimage\"," +
            "\"postSaveFingerprint\":\"\"," +
            "\"beforeBackpackCount\":0," +
            "\"beforeMailCount\":0," +
            "\"placement\":0," +
            "\"origin\":2}}";

        private static string LegacyCandidateJson() =>
            "{" +
            "\"schemaVersion\":3," +
            "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"," +
            "\"storageScope\":\"slot-2\"," +
            "\"archiveIndex\":2," +
            "\"playerName\":\"third-save\"," +
            "\"customPlayerName\":\"third-save\"," +
            "\"savedTotalGameSeconds\":100," +
            "\"generation\":4," +
            "\"slots\":[" +
            "{\"index\":0,\"itemId\":\"committed-hat\",\"displayName\":\"Committed\"}," +
            "{\"index\":1,\"itemId\":\"\",\"displayName\":\"\"}," +
            "{\"index\":2,\"itemId\":\"\",\"displayName\":\"\"}]," +
            "\"gameplayCandidate\":{" +
            "\"transactionId\":\"22222222222222222222222222222222\"," +
            "\"phase\":0," +
            "\"origin\":1," +
            "\"scope\":{\"archiveIndex\":2,\"playerName\":\"third-save\",\"customPlayerName\":\"third-save\"}," +
            "\"baseGeneration\":4," +
            "\"preSaveFingerprint\":\"candidate-preimage\"," +
            "\"postSaveFingerprint\":\"\"," +
            "\"workingSlots\":[" +
            "{\"index\":0,\"itemId\":\"working-hat\",\"displayName\":\"Working\"}," +
            "{\"index\":1,\"itemId\":\"\",\"displayName\":\"\"}," +
            "{\"index\":2,\"itemId\":\"\",\"displayName\":\"\"}]," +
            "\"nativeExpectations\":[{" +
            "\"itemId\":\"working-hat\"," +
            "\"backpackCount\":1," +
            "\"mailCount\":0}]}}";

        private static NativeRecoveryObservation Observation(
            string fingerprint,
            int backpack,
            int mail) =>
            new NativeRecoveryObservation(
                fingerprint,
                backpack,
                mail);

        private static NativePlacementResult Result(
            NativePlacementKind kind) =>
            new NativePlacementResult(
                kind,
                kind == NativePlacementKind.Backpack ? 1 : 0,
                kind == NativePlacementKind.Mail ? 1 : 0,
                kind.ToString());

        private static bool EqualBytes(
            byte[] first,
            byte[] second)
        {
            if (first.Length != second.Length)
                return false;
            for (int index = 0; index < first.Length; index++)
            {
                if (first[index] != second[index])
                    return false;
            }
            return true;
        }

        private static bool IsCompletedClaim(
            string path) =>
            File.Exists(path) &&
            File.ReadAllText(path).Contains(
                "\"state\":\"completed\"",
                StringComparison.Ordinal);

        private static Process StartHistoricalBackfillActor(
            string globalPath,
            string productPath,
            EquipmentSlotSaveScope scope,
            string readyPath,
            string releasePath,
            string outcomePath)
        {
            string executable =
                Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    "The historical backfill fixture could not resolve the current Unit executable.");
            var start =
                new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(
                        executable),
                    "dotnet",
                    StringComparison.OrdinalIgnoreCase))
            {
                start.ArgumentList.Add(
                    typeof(MoreEquipmentSlotsProductTests)
                        .Assembly
                        .Location);
            }
            start.Environment[
                "DTMAPI_UNIT_TEST_FOCUS"] =
                "moreequipment-claim-process";
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_MODE"] =
                "historical-backfill";
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_GLOBAL"] =
                globalPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_PRODUCT"] =
                productPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_READY"] =
                readyPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_RELEASE"] =
                releasePath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_OUTCOME"] =
                outcomePath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_ARCHIVE_INDEX"] =
                scope.ArchiveIndex.ToString(
                    System.Globalization
                        .CultureInfo.InvariantCulture);
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_PLAYER"] =
                scope.PlayerName;
            return Process.Start(start)
                ?? throw new InvalidOperationException(
                    "The historical backfill actor could not start.");
        }

        private static Process StartLateEvidenceWriterActor(
            string targetPath,
            string claimJson,
            string readyPath,
            string releasePath)
        {
            string executable =
                Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    "The late evidence fixture could not resolve the current Unit executable.");
            var start =
                new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(
                        executable),
                    "dotnet",
                    StringComparison.OrdinalIgnoreCase))
            {
                start.ArgumentList.Add(
                    typeof(MoreEquipmentSlotsProductTests)
                        .Assembly
                        .Location);
            }
            start.Environment[
                "DTMAPI_UNIT_TEST_FOCUS"] =
                "moreequipment-claim-process";
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_MODE"] =
                "late-evidence-writer";
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_TARGET"] =
                targetPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_JSON"] =
                claimJson;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_READY"] =
                readyPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_RELEASE"] =
                releasePath;
            return Process.Start(start)
                ?? throw new InvalidOperationException(
                    "The late evidence actor could not start.");
        }

        private static Process StartCaptureCrashActor(
            string globalPath,
            string productPath)
        {
            string executable =
                Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    "The capture crash fixture could not resolve the current Unit executable.");
            var start =
                new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(
                        executable),
                    "dotnet",
                    StringComparison.OrdinalIgnoreCase))
            {
                start.ArgumentList.Add(
                    typeof(MoreEquipmentSlotsProductTests)
                        .Assembly
                        .Location);
            }
            start.Environment[
                "DTMAPI_UNIT_TEST_FOCUS"] =
                "moreequipment-claim-process";
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_MODE"] =
                "capture-crash";
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_GLOBAL"] =
                globalPath;
            start.Environment[
                "DTMAPI_MORE_EQUIPMENT_CLAIM_PRODUCT"] =
                productPath;
            return Process.Start(start)
                ?? throw new InvalidOperationException(
                    "The capture crash actor could not start.");
        }

        private static string RequiredEnvironment(
            string name) =>
            Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException(
                "The cross-process claim fixture requires " +
                name +
                ".");

        private static void WaitForFile(
            string path,
            string timeoutMessage)
        {
            var timeout =
                Stopwatch.StartNew();
            while (!File.Exists(path))
            {
                if (timeout.Elapsed >
                    TimeSpan.FromSeconds(10))
                {
                    throw new TimeoutException(
                        timeoutMessage);
                }
                Thread.Sleep(10);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private class BaseReflectionFixture
        {
            public string Value { get; set; } =
                "base";
        }

        private sealed class DerivedReflectionFixture :
            BaseReflectionFixture
        {
            public new object Value { get; set; } =
                "derived";
        }

        private sealed class FakeGateway :
            INativeItemPlacementGateway
        {
            internal bool BackpackSucceeds { get; set; }

            internal bool MailSucceeds { get; set; }

            internal int BackpackPlacementDelta { get; set; } = 1;

            internal bool? CostReportedOverride { get; set; }

            internal int CostDelta { get; set; } = -1;

            internal int BackpackCount { get; private set; }

            internal int MailCount { get; private set; }

            internal int ThrowMailReadAtCall { get; set; }

            internal int ThrowMailReadFromCall { get; set; }

            internal int MailReadCallCount { get; private set; }

            internal int MailSendCallCount { get; private set; }

            internal void SetBackpackCount(int count) =>
                BackpackCount = Math.Max(0, count);

            public int CountBackpack(string itemId) =>
                BackpackCount;

            public int CountUnacceptedMail(string itemId)
            {
                MailReadCallCount++;
                if (ThrowMailReadAtCall > 0 &&
                    MailReadCallCount == ThrowMailReadAtCall ||
                    ThrowMailReadFromCall > 0 &&
                    MailReadCallCount >= ThrowMailReadFromCall)
                {
                    throw new InvalidDataException(
                        "Injected unreadable native mail authority.");
                }
                return MailCount;
            }

            public bool TryPlaceBackpackOnly(string itemId)
            {
                if (!BackpackSucceeds)
                    return false;
                BackpackCount += BackpackPlacementDelta;
                return true;
            }

            public bool TrySendMail(string itemId)
            {
                MailSendCallCount++;
                if (!MailSucceeds)
                    return false;
                MailCount++;
                return true;
            }

            public bool TryCostBackpack(string itemId)
            {
                if (BackpackCount <= 0)
                    return CostReportedOverride ?? false;
                BackpackCount = Math.Max(
                    0,
                    BackpackCount + CostDelta);
                return CostReportedOverride ??
                    CostDelta == -1;
            }
        }

        private sealed class SameItemReplacementScenario
        {
            internal SameItemReplacementScenario(
                EquipmentSlotStorageDocument document,
                EquipmentSlotTransactionJournal journal,
                NativeRecoveryObservation before,
                NativeRecoveryObservation after)
            {
                Document = document;
                Journal = journal;
                Before = before;
                After = after;
            }

            internal EquipmentSlotStorageDocument Document
            {
                get;
            }

            internal EquipmentSlotTransactionJournal Journal
            {
                get;
            }

            internal NativeRecoveryObservation Before
            {
                get;
            }

            internal NativeRecoveryObservation After
            {
                get;
            }
        }

        private sealed class GameplayDiscardScenario
        {
            internal GameplayDiscardScenario(
                string committedItem,
                string workingItem,
                string name)
            {
                CommittedItem = committedItem;
                WorkingItem = workingItem;
                Name = name;
            }

            internal string CommittedItem { get; }

            internal string WorkingItem { get; }

            internal string Name { get; }
        }
    }
}
