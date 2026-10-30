using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private bool moreEquipmentSlotsNoNativeSaveCompleted;
        private bool moreEquipmentSlotsNoNativeSaveColdObserverCompleted;
        private int expectedMoreEquipmentSlotsBackpackBaseline = -1;
        private long expectedMoreEquipmentSlotsCommittedGeneration = -1L;
        private int expectedMoreEquipmentSlotsCommittedOccupied = -1;
        private string expectedMoreEquipmentSlotsCommittedSlots =
            string.Empty;

        private void
            ConfigureMoreEquipmentSlotsNoNativeSaveColdObserverForFixture(
                int backpackBaseline,
                long committedGeneration,
                int committedOccupied,
                string committedSlots)
        {
            expectedMoreEquipmentSlotsBackpackBaseline =
                backpackBaseline;
            expectedMoreEquipmentSlotsCommittedGeneration =
                committedGeneration;
            expectedMoreEquipmentSlotsCommittedOccupied =
                committedOccupied;
            expectedMoreEquipmentSlotsCommittedSlots =
                committedSlots ?? string.Empty;
        }

        private FixtureAttemptResult
            TryExerciseMoreEquipmentSlotsNoNativeSaveForFixture()
        {
            if (moreEquipmentSlotsNoNativeSaveCompleted)
                return FixtureAttemptResult.Succeeded;

            try
            {
                Batch6HarmonyOwnerInventory inventory =
                    Batch6AdvancedHarmonyOwnerObserver.Observe(
                        MoreEquipmentSlotsAssemblyName,
                        MoreEquipmentSlotsHarmonyOwner,
                        MoreEquipmentSlotsHarmonyTargets);
                object? productRuntime = ReadStaticCallbackRuntime(
                    MoreEquipmentSlotsAssemblyName,
                    "DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks");
                int loadedOwners =
                    CountLoadedOwners(MoreEquipmentSlotsOwnerId);
                int ownerRoots =
                    runtime.CountCoreOwnerRoots(MoreEquipmentSlotsOwnerId);
                if (!inventory.IsComplete ||
                    inventory.ExactOwnerPatchCount != MoreEquipmentSlotsHarmonyTargets.Length ||
                    productRuntime == null ||
                    !runtime.HasOwnerInstance(MoreEquipmentSlotsOwnerId) ||
                    loadedOwners != 1 ||
                    ownerRoots <= 0)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots NoNativeSave readiness was incomplete. instance=" +
                        runtime.HasOwnerInstance(
                            MoreEquipmentSlotsOwnerId) +
                        "; loaded=" +
                        loadedOwners +
                        "; roots=" +
                        ownerRoots +
                        "; patches=" +
                        inventory.ExactOwnerPatchCount +
                        "; targets=" +
                        inventory.ExactOwnerTargetCount +
                        "/" +
                        inventory.ResolvedTargetCount +
                        "; callback=" +
                        (productRuntime != null) +
                        "; inventory={" +
                        inventory.Details +
                        "}.");
                }

                object committedBefore =
                    ReadReflectedMember(
                        productRuntime,
                        "document") ??
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots in-memory Committed document is unavailable.");
                int committedOccupiedBefore =
                    CountOccupiedEquipmentSlots(
                        ReadReflectedMember(
                            committedBefore,
                            "Slots"));
                int targetSlot =
                    FindFirstEmptyEquipmentSlot(
                        ReadReflectedMember(
                            committedBefore,
                            "Slots"));
                if (targetSlot < 0)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots NoNativeSave requires one empty fixed product slot.");
                }

                long generationBefore =
                    Convert.ToInt64(
                        ReadReflectedMember(
                            committedBefore,
                            "Generation") ?? 0L);
                string committedSlotsBefore =
                    DescribeEquipmentSlots(
                        ReadReflectedMember(
                            committedBefore,
                            "Slots"));
                if (ReadReflectedMember(
                        committedBefore,
                        "Journal") != null ||
                    ReadReflectedMember(
                        committedBefore,
                        "GameplayCandidate") != null)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots NoNativeSave refused a pre-existing durable transaction.");
                }

                int backpackBaseline =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                int shieldBackpackBaseline =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false);
                InventoryGiveResult give =
                    inventoryDebugApi.GiveItem(
                        CreateFixtureManifest(),
                        MoreEquipmentSlotsTransactionItemId,
                        1);
                int afterGive =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                if (!give.Success ||
                    afterGive != backpackBaseline + 1)
                {
                    throw new InvalidOperationException(
                        "Could not stage exactly one grandmas_button in the backpack. baseline=" +
                        backpackBaseline +
                        "; afterGive=" +
                        afterGive +
                        "; give=" +
                        give.Success +
                        "; message=" +
                        give.Message +
                        ".");
                }

                InvokeMoreEquipmentSlotsOperation(
                    productRuntime,
                    "EquipFromBackpack",
                    targetSlot,
                    MoreEquipmentSlotsTransactionItemId,
                    "QA NoNativeSave Working-only equip");
                int afterEquip =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                if (afterEquip != backpackBaseline)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots Working equip did not consume exactly the QA-owned backpack item. baseline=" +
                        backpackBaseline +
                        "; afterEquip=" +
                        afterEquip +
                        ".");
                }

                InventoryGiveResult giveShield =
                    inventoryDebugApi.GiveItem(
                        CreateFixtureManifest(),
                        MoreEquipmentSlotsShieldItemId,
                        1);
                int afterShieldGive =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false);
                if (!giveShield.Success ||
                    afterShieldGive !=
                        shieldBackpackBaseline + 1)
                {
                    throw new InvalidOperationException(
                        "Could not stage exactly one box_hat for the no-save replacement/shield matrix. baseline=" +
                        shieldBackpackBaseline +
                        "; afterGive=" +
                        afterShieldGive +
                        "; message=" +
                        giveShield.Message +
                        ".");
                }
                InvokeMoreEquipmentSlotsOperation(
                    productRuntime,
                    "EquipFromBackpack",
                    targetSlot,
                    MoreEquipmentSlotsShieldItemId,
                    "QA NoNativeSave replacement with shield");
                int grandmasAfterReplacement =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                int shieldAfterReplacement =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false);
                if (grandmasAfterReplacement !=
                        backpackBaseline + 1 ||
                    shieldAfterReplacement !=
                        shieldBackpackBaseline)
                {
                    throw new InvalidOperationException(
                        "The real ProductNative replacement did not place the outgoing item and consume the incoming shield exactly once. grandmas=" +
                        grandmasAfterReplacement +
                        "; shield=" +
                        shieldAfterReplacement +
                        ".");
                }

                object shieldBeforeDamage =
                    RequireEquipmentSlotEntry(
                        ReadReflectedMember(
                            productRuntime,
                            "workingSlots"),
                        targetSlot);
                int shieldValueBefore =
                    Convert.ToInt32(
                        ReadReflectedMember(
                            shieldBeforeDamage,
                            "ShieldValue") ?? 0);
                int shieldDefend =
                    Convert.ToInt32(
                        ReadReflectedMember(
                            shieldBeforeDamage,
                            "ShieldDefend") ?? 0);
                if (!(ReadReflectedMember(
                          shieldBeforeDamage,
                          "IsShield") is bool isShield &&
                      isShield) ||
                    shieldValueBefore <= 1)
                {
                    throw new InvalidOperationException(
                        "The native box_hat did not enter the ProductNative slot with live shield traits.");
                }

                InvokeRealMoreEquipmentSlotsAttack(
                    shieldDefend +
                    Math.Max(
                        1,
                        shieldValueBefore / 2));
                object shieldAfterDamage =
                    RequireEquipmentSlotEntry(
                        ReadReflectedMember(
                            productRuntime,
                            "workingSlots"),
                        targetSlot);
                int damagedShieldValue =
                    Convert.ToInt32(
                        ReadReflectedMember(
                            shieldAfterDamage,
                            "ShieldValue") ?? 0);
                if (damagedShieldValue <= 0 ||
                    damagedShieldValue >= shieldValueBefore)
                {
                    throw new InvalidOperationException(
                        "The real BodyController.OnAttacked ProductNative prefix did not persist a non-breaking Working shield hit. shield=" +
                        shieldValueBefore +
                        "->" +
                        damagedShieldValue +
                        ".");
                }

                InvokeRealMoreEquipmentSlotsAttack(
                    shieldDefend +
                    damagedShieldValue +
                    1);
                object afterBreakEntry =
                    RequireEquipmentSlotEntry(
                        ReadReflectedMember(
                            productRuntime,
                            "workingSlots"),
                        targetSlot);
                if (!string.IsNullOrWhiteSpace(
                        ReadReflectedMember(
                            afterBreakEntry,
                            "ItemId") as string))
                {
                    throw new InvalidOperationException(
                        "The real ProductNative shield-break path did not clear the Working slot.");
                }

                InvokeMoreEquipmentSlotsOperation(
                    productRuntime,
                    "EquipFromBackpack",
                    targetSlot,
                    MoreEquipmentSlotsTransactionItemId,
                    "QA NoNativeSave post-break equip");
                InvokeMoreEquipmentSlotsOperation(
                    productRuntime,
                    "RequestUnequip",
                    targetSlot,
                    MoreEquipmentSlotsTransactionItemId,
                    "QA NoNativeSave Working-only unequip");
                int afterUnequip =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                if (afterUnequip != backpackBaseline + 1)
                {
                    throw new InvalidOperationException(
                        "The real no-save unequip did not return exactly one QA-owned item. backpack=" +
                        afterUnequip +
                        ".");
                }
                bool cleanupRemoved =
                    CostNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        1,
                        checkBox: false);
                int afterCleanup =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                int shieldAfterBreak =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false);
                if (!cleanupRemoved ||
                    afterCleanup != backpackBaseline ||
                    shieldAfterBreak != shieldBackpackBaseline)
                {
                    throw new InvalidOperationException(
                        "The no-save matrix did not finish with exactly the native item baselines before title rollback. grandmas=" +
                        backpackBaseline +
                        "->" +
                        afterCleanup +
                        "; shield=" +
                        shieldBackpackBaseline +
                        "->" +
                        shieldAfterBreak +
                        ".");
                }

                object committedAfter =
                    ReadReflectedMember(
                        productRuntime,
                        "document") ??
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots in-memory Committed document disappeared after Working mutation.");
                long generationAfter =
                    Convert.ToInt64(
                        ReadReflectedMember(
                            committedAfter,
                            "Generation") ?? 0L);
                int committedOccupiedAfter =
                    CountOccupiedEquipmentSlots(
                        ReadReflectedMember(
                            committedAfter,
                            "Slots"));
                string committedSlotsAfter =
                    DescribeEquipmentSlots(
                        ReadReflectedMember(
                            committedAfter,
                            "Slots"));
                int workingOccupied =
                    CountOccupiedEquipmentSlots(
                        ReadReflectedMember(
                            productRuntime,
                            "workingSlots"));
                string workingSlots =
                    DescribeEquipmentSlots(
                        ReadReflectedMember(
                            productRuntime,
                            "workingSlots"));
                bool workingDirty =
                    ReadReflectedMember(
                        productRuntime,
                        "workingDirty") is bool dirty &&
                    dirty;
                if (generationAfter != generationBefore ||
                    committedOccupiedAfter != committedOccupiedBefore ||
                    !string.Equals(
                        committedSlotsAfter,
                        committedSlotsBefore,
                        StringComparison.Ordinal) ||
                    ReadReflectedMember(
                        committedAfter,
                        "Journal") != null ||
                    ReadReflectedMember(
                        committedAfter,
                        "GameplayCandidate") != null ||
                    workingOccupied != committedOccupiedBefore ||
                    !string.Equals(
                        workingSlots,
                        committedSlotsBefore,
                        StringComparison.Ordinal) ||
                    !workingDirty)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots NoNativeSave crossed the Working/Committed boundary. generation=" +
                        generationBefore +
                        "->" +
                        generationAfter +
                        "; committedOccupied=" +
                        committedOccupiedBefore +
                        "->" +
                        committedOccupiedAfter +
                        "; workingOccupied=" +
                        workingOccupied +
                        "; workingDirty=" +
                        workingDirty +
                        "; journal=" +
                        (ReadReflectedMember(
                            committedAfter,
                            "Journal") != null) +
                        "; candidate=" +
                        (ReadReflectedMember(
                            committedAfter,
                            "GameplayCandidate") != null) +
                        ".");
                }

                string summary =
                    "route=ProductNative" +
                    "; saveMode=NoNativeSave" +
                    "; item=" +
                    MoreEquipmentSlotsTransactionItemId +
                    "; targetSlot=" +
                    targetSlot +
                    "; backpackBaseline=" +
                    backpackBaseline +
                    "; afterGive=" +
                    afterGive +
                    "; afterEquip=" +
                    afterEquip +
                    "; replacement=grandmas_button->box_hat" +
                    "; shieldDamage=" +
                    shieldValueBefore +
                    "->" +
                    damagedShieldValue +
                    "; shieldBreak=true" +
                    "; noSaveUnequip=true" +
                    "; nativeItemsRestoredBeforeTitle=true" +
                    "; committedGeneration=" +
                    generationBefore +
                    "; committedOccupied=" +
                    committedOccupiedBefore +
                    "; committedSlots={" +
                    committedSlotsBefore +
                    "}" +
                    "; committedSlotsBase64=" +
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            committedSlotsBefore)) +
                    "; workingOccupied=" +
                    workingOccupied +
                    "; workingDirty=true" +
                    "; nativeSaveRequested=false" +
                    "; exactOwner=" +
                    MoreEquipmentSlotsHarmonyOwner +
                    "; patches=" +
                    inventory.ExactOwnerPatchCount +
                    "; targets=" +
                    inventory.ExactOwnerTargetCount +
                    "/" +
                    inventory.ResolvedTargetCount +
                    "; callback=1" +
                    "; loaded=1" +
                    "; roots=" +
                    ownerRoots;
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise MoreEquipmentSlotsNoNativeSave OK " +
                    summary +
                    ".");
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsNoNativeSave",
                    "verified",
                    "ProductNative equip/replace/shield damage/break/unequip remained Working-only; no SaveGame",
                    summary);
                moreEquipmentSlotsNoNativeSaveCompleted = true;
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke MoreEquipmentSlots NoNativeSave exercise failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsNoNativeSave",
                    "failed",
                    "ProductNative equip/replace/shield damage/break/unequip remained Working-only; no SaveGame",
                    ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult
            TryObserveMoreEquipmentSlotsNoNativeSaveColdForFixture()
        {
            if (moreEquipmentSlotsNoNativeSaveColdObserverCompleted)
                return FixtureAttemptResult.Succeeded;

            try
            {
                Batch6HarmonyOwnerInventory inventory =
                    Batch6AdvancedHarmonyOwnerObserver.Observe(
                        MoreEquipmentSlotsAssemblyName,
                        MoreEquipmentSlotsHarmonyOwner,
                        MoreEquipmentSlotsHarmonyTargets);
                object? productRuntime = ReadStaticCallbackRuntime(
                    MoreEquipmentSlotsAssemblyName,
                    "DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks");
                int loadedOwners =
                    CountLoadedOwners(MoreEquipmentSlotsOwnerId);
                int ownerRoots =
                    runtime.CountCoreOwnerRoots(MoreEquipmentSlotsOwnerId);
                if (!inventory.IsComplete ||
                    inventory.ExactOwnerPatchCount != MoreEquipmentSlotsHarmonyTargets.Length ||
                    productRuntime == null ||
                    !runtime.HasOwnerInstance(MoreEquipmentSlotsOwnerId) ||
                    loadedOwners != 1 ||
                    ownerRoots <= 0)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots NoNativeSave cold readiness was incomplete. instance=" +
                        runtime.HasOwnerInstance(
                            MoreEquipmentSlotsOwnerId) +
                        "; loaded=" +
                        loadedOwners +
                        "; roots=" +
                        ownerRoots +
                        "; patches=" +
                        inventory.ExactOwnerPatchCount +
                        "; targets=" +
                        inventory.ExactOwnerTargetCount +
                        "/" +
                        inventory.ResolvedTargetCount +
                        "; callback=" +
                        (productRuntime != null) +
                        "; inventory={" +
                        inventory.Details +
                        "}.");
                }

                object committed =
                    ReadReflectedMember(
                        productRuntime,
                        "document") ??
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots in-memory Committed document is unavailable.");
                long committedGeneration =
                    Convert.ToInt64(
                        ReadReflectedMember(
                            committed,
                            "Generation") ?? 0L);
                object? committedSlotEntries =
                    ReadReflectedMember(
                        committed,
                        "Slots");
                int committedOccupied =
                    CountOccupiedEquipmentSlots(
                        committedSlotEntries);
                string committedSlots =
                    DescribeEquipmentSlots(
                        committedSlotEntries);
                object? workingSlotEntries =
                    ReadReflectedMember(
                        productRuntime,
                        "workingSlots");
                int workingOccupied =
                    CountOccupiedEquipmentSlots(
                        workingSlotEntries);
                string workingSlots =
                    DescribeEquipmentSlots(
                        workingSlotEntries);
                bool workingDirty =
                    ReadReflectedMember(
                        productRuntime,
                        "workingDirty") is bool dirty &&
                    dirty;
                int backpack =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                int nativeShield =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false);
                int mailButton =
                    CountPendingMoreEquipmentSlotsMail(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId);
                int mailShield =
                    CountPendingMoreEquipmentSlotsMail(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId);
                int committedButtonCount =
                    FindEquipmentSlotsByItemId(
                        committedSlotEntries,
                        MoreEquipmentSlotsTransactionItemId).Length;
                int committedShieldCount =
                    FindEquipmentSlotsByItemId(
                        committedSlotEntries,
                        MoreEquipmentSlotsShieldItemId).Length;
                MoreEquipmentSlotsColdItemDistribution
                    itemDistribution =
                        ValidateMoreEquipmentSlotsColdItemDistribution(
                            expectedMoreEquipmentSlotsBackpackBaseline,
                            expectedMoreEquipmentSlotsCommittedSlots,
                            backpack,
                            nativeShield,
                            mailButton,
                            mailShield,
                            committedButtonCount,
                            committedShieldCount);
                bool journalPresent =
                    ReadReflectedMember(
                        committed,
                        "Journal") != null;
                bool candidatePresent =
                    ReadReflectedMember(
                        committed,
                        "GameplayCandidate") != null;

                if (committedGeneration !=
                        expectedMoreEquipmentSlotsCommittedGeneration ||
                    committedOccupied !=
                        expectedMoreEquipmentSlotsCommittedOccupied ||
                    !string.Equals(
                        committedSlots,
                        expectedMoreEquipmentSlotsCommittedSlots,
                        StringComparison.Ordinal) ||
                    workingOccupied != committedOccupied ||
                    !string.Equals(
                        workingSlots,
                        committedSlots,
                        StringComparison.Ordinal) ||
                    workingDirty ||
                    journalPresent ||
                    candidatePresent)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots NoNativeSave cold state did not match the first-process baseline. backpack=" +
                        expectedMoreEquipmentSlotsBackpackBaseline +
                        "->" +
                        backpack +
                        "; generation=" +
                        expectedMoreEquipmentSlotsCommittedGeneration +
                        "->" +
                        committedGeneration +
                        "; occupied=" +
                        expectedMoreEquipmentSlotsCommittedOccupied +
                        "->" +
                        committedOccupied +
                        "; committedSlotsMatch=" +
                        string.Equals(
                            committedSlots,
                            expectedMoreEquipmentSlotsCommittedSlots,
                            StringComparison.Ordinal) +
                        "; workingMatchesCommitted=" +
                        string.Equals(
                            workingSlots,
                            committedSlots,
                            StringComparison.Ordinal) +
                        "; workingOccupied=" +
                        workingOccupied +
                        "; workingDirty=" +
                        workingDirty +
                        "; journal=" +
                        journalPresent +
                        "; candidate=" +
                        candidatePresent +
                        "; nativeShield=" +
                        nativeShield +
                        "; mailShield=" +
                        mailShield +
                        "; committedShieldCount=" +
                        committedShieldCount +
                        "; backpackButton=" +
                        backpack +
                        "; mailButton=" +
                        mailButton +
                        "; committedButtonCount=" +
                        committedButtonCount +
                        "; expectedBoxHatLogicalTotal=" +
                        itemDistribution.ExpectedShieldTotal +
                        "; boxHatLogicalTotal=" +
                        itemDistribution.ShieldTotal +
                        "; expectedGrandmasButtonLogicalTotal=" +
                        itemDistribution.ExpectedButtonTotal +
                        "; grandmasButtonLogicalTotal=" +
                        itemDistribution.ButtonTotal +
                        ".");
                }

                string summary =
                    "route=ProductNativeReadOnly" +
                    "; saveMode=NoNativeSave" +
                    "; backpackBaseline=" +
                    backpack +
                    "; committedGeneration=" +
                    committedGeneration +
                    "; committedOccupied=" +
                    committedOccupied +
                    "; committedSlots={" +
                    committedSlots +
                    "}" +
                    "; committedSlotsBase64=" +
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            committedSlots)) +
                    "; workingMatchesCommitted=true" +
                    "; workingDirty=false" +
                    "; journal=false" +
                    "; candidate=false" +
                    "; boxHatBackpack=" +
                    nativeShield +
                    "; boxHatMail=" +
                    mailShield +
                    "; boxHatSidecar=" +
                    committedShieldCount +
                    "; boxHatExpectedTotal=" +
                    itemDistribution.ExpectedShieldTotal +
                    "; boxHatLogicalTotal=" +
                    itemDistribution.ShieldTotal +
                    "; grandmasButtonBackpack=" +
                    backpack +
                    "; grandmasButtonMail=" +
                    mailButton +
                    "; grandmasButtonSidecar=" +
                    committedButtonCount +
                    "; grandmasButtonExpectedTotal=" +
                    itemDistribution.ExpectedButtonTotal +
                    "; grandmasButtonLogicalTotal=" +
                    itemDistribution.ButtonTotal +
                    "; itemExpectationsMatch=true" +
                    "; nativeSaveRequested=false" +
                    "; exactOwner=" +
                    MoreEquipmentSlotsHarmonyOwner +
                    "; patches=" +
                    inventory.ExactOwnerPatchCount +
                    "; targets=" +
                    inventory.ExactOwnerTargetCount +
                    "/" +
                    inventory.ResolvedTargetCount +
                    "; callback=1" +
                    "; loaded=1" +
                    "; roots=" +
                    ownerRoots;
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise MoreEquipmentSlotsNoNativeSaveColdObserver OK " +
                    summary +
                    ".");
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsNoNativeSaveColdObserver",
                    "verified",
                    "ProductNative cold Working/Committed read-only observation; no SaveGame",
                    summary);
                moreEquipmentSlotsNoNativeSaveColdObserverCompleted =
                    true;
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke MoreEquipmentSlots NoNativeSave cold observation failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsNoNativeSaveColdObserver",
                    "failed",
                    "ProductNative cold Working/Committed read-only observation; no SaveGame",
                    ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        internal static MoreEquipmentSlotsColdItemDistribution
            ValidateMoreEquipmentSlotsColdItemDistribution(
                int expectedBackpackButtonCount,
                string expectedCommittedSlots,
                int backpackButtonCount,
                int nativeShieldCount,
                int mailButtonCount,
                int mailShieldCount,
                int committedButtonCount,
                int committedShieldCount)
        {
            int expectedCommittedButtonCount =
                CountDescribedEquipmentSlotsByItemId(
                    expectedCommittedSlots,
                    MoreEquipmentSlotsTransactionItemId);
            int expectedCommittedShieldCount =
                CountDescribedEquipmentSlotsByItemId(
                    expectedCommittedSlots,
                    MoreEquipmentSlotsShieldItemId);
            int expectedButtonTotal =
                checked(
                    expectedBackpackButtonCount +
                    expectedCommittedButtonCount);
            int expectedShieldTotal =
                expectedCommittedShieldCount;
            RequireNoPendingMoreEquipmentSlotsMail(
                mailButtonCount,
                mailShieldCount);
            int buttonTotal =
                checked(
                    backpackButtonCount +
                    mailButtonCount +
                    committedButtonCount);
            int shieldTotal =
                checked(
                    nativeShieldCount +
                    mailShieldCount +
                    committedShieldCount);

            if (expectedBackpackButtonCount < 0 ||
                backpackButtonCount < 0 ||
                nativeShieldCount < 0 ||
                mailButtonCount < 0 ||
                mailShieldCount < 0 ||
                committedButtonCount < 0 ||
                committedShieldCount < 0 ||
                backpackButtonCount !=
                    expectedBackpackButtonCount ||
                nativeShieldCount != 0 ||
                committedButtonCount !=
                    expectedCommittedButtonCount ||
                committedShieldCount !=
                    expectedCommittedShieldCount ||
                buttonTotal != expectedButtonTotal ||
                shieldTotal != expectedShieldTotal)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots cold item distribution did not match the supplied baseline. expectedBackpackButton=" +
                    expectedBackpackButtonCount +
                    "; backpackButton=" +
                    backpackButtonCount +
                    "; nativeShield=" +
                    nativeShieldCount +
                    "; mailButton=" +
                    mailButtonCount +
                    "; mailShield=" +
                    mailShieldCount +
                    "; expectedCommittedButton=" +
                    expectedCommittedButtonCount +
                    "; committedButton=" +
                    committedButtonCount +
                    "; expectedCommittedShield=" +
                    expectedCommittedShieldCount +
                    "; committedShield=" +
                    committedShieldCount +
                    "; expectedButtonTotal=" +
                    expectedButtonTotal +
                    "; buttonTotal=" +
                    buttonTotal +
                    "; expectedShieldTotal=" +
                    expectedShieldTotal +
                    "; shieldTotal=" +
                    shieldTotal +
                    ".");
            }

            return new MoreEquipmentSlotsColdItemDistribution(
                expectedCommittedButtonCount,
                expectedCommittedShieldCount,
                expectedButtonTotal,
                expectedShieldTotal,
                buttonTotal,
                shieldTotal);
        }

        internal static void RequireNoPendingMoreEquipmentSlotsMail(
            int mailButtonCount,
            int mailShieldCount)
        {
            if (mailButtonCount != 0)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots cold observation found pending grandmas_button mail. count=" +
                    mailButtonCount +
                    ".");
            }

            if (mailShieldCount != 0)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots cold observation found pending box_hat mail. count=" +
                    mailShieldCount +
                    ".");
            }
        }

        private static int CountDescribedEquipmentSlotsByItemId(
            string descriptions,
            string itemId)
        {
            int count = 0;
            foreach (string description in
                     (descriptions ?? string.Empty).Split('|'))
            {
                int firstSeparator =
                    description.IndexOf(
                        ':');
                if (firstSeparator < 0)
                    continue;

                int itemStart =
                    firstSeparator + 1;
                int secondSeparator =
                    description.IndexOf(
                        ':',
                        itemStart);
                string describedItemId =
                    secondSeparator < 0
                        ? description.Substring(
                            itemStart)
                        : description.Substring(
                            itemStart,
                            secondSeparator - itemStart);
                if (string.Equals(
                        describedItemId,
                        itemId,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountOccupiedEquipmentSlots(
            object? slots)
        {
            int occupied = 0;
            if (slots is IEnumerable entries)
            {
                foreach (object? entry in entries)
                {
                    string itemId =
                        ReadReflectedMember(
                            entry,
                            "ItemId") as string ??
                        string.Empty;
                    if (!string.IsNullOrWhiteSpace(itemId))
                        occupied++;
                }
            }

            return occupied;
        }

        private static int FindFirstEmptyEquipmentSlot(
            object? slots)
        {
            if (slots is IEnumerable entries)
            {
                foreach (object? entry in entries)
                {
                    string itemId =
                        ReadReflectedMember(
                            entry,
                            "ItemId") as string ??
                        string.Empty;
                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        return Convert.ToInt32(
                            ReadReflectedMember(
                                entry,
                                "Index") ?? -1);
                    }
                }
            }

            return -1;
        }

        private static string DescribeEquipmentSlots(
            object? slots)
        {
            var descriptions = new List<string>();
            if (slots is IEnumerable entries)
            {
                foreach (object? entry in entries)
                {
                    descriptions.Add(
                        string.Join(
                            ":",
                            Convert.ToString(
                                ReadReflectedMember(
                                    entry,
                                    "Index")) ?? string.Empty,
                            ReadReflectedMember(
                                entry,
                                "ItemId") as string ?? string.Empty,
                            ReadReflectedMember(
                                entry,
                                "DisplayName") as string ?? string.Empty,
                            ReadReflectedMember(
                                entry,
                                "SkillId") as string ?? string.Empty,
                            Convert.ToString(
                                ReadReflectedMember(
                                    entry,
                                    "DefenseBonus")) ?? string.Empty,
                            Convert.ToString(
                                ReadReflectedMember(
                                    entry,
                                    "IsShield")) ?? string.Empty,
                            Convert.ToString(
                                ReadReflectedMember(
                                    entry,
                                    "ShieldValue")) ?? string.Empty,
                            Convert.ToString(
                                ReadReflectedMember(
                                    entry,
                                    "ShieldMaxValue")) ?? string.Empty,
                            Convert.ToString(
                                ReadReflectedMember(
                                    entry,
                                    "ShieldDefend")) ?? string.Empty));
                }
            }

            return string.Join("|", descriptions);
        }
    }

    internal readonly struct MoreEquipmentSlotsColdItemDistribution
    {
        internal MoreEquipmentSlotsColdItemDistribution(
            int expectedCommittedButtonCount,
            int expectedCommittedShieldCount,
            int expectedButtonTotal,
            int expectedShieldTotal,
            int buttonTotal,
            int shieldTotal)
        {
            ExpectedCommittedButtonCount =
                expectedCommittedButtonCount;
            ExpectedCommittedShieldCount =
                expectedCommittedShieldCount;
            ExpectedButtonTotal =
                expectedButtonTotal;
            ExpectedShieldTotal =
                expectedShieldTotal;
            ButtonTotal =
                buttonTotal;
            ShieldTotal =
                shieldTotal;
        }

        internal int ExpectedCommittedButtonCount { get; }

        internal int ExpectedCommittedShieldCount { get; }

        internal int ExpectedButtonTotal { get; }

        internal int ExpectedShieldTotal { get; }

        internal int ButtonTotal { get; }

        internal int ShieldTotal { get; }
    }
}
