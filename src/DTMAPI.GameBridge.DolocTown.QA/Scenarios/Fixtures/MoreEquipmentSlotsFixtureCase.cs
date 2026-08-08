using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const string MoreEquipmentSlotsTransactionItemId =
            "grandmas_button";
        private const string MoreEquipmentSlotsShieldItemId =
            "box_hat";
        private int moreEquipmentSlotsTransactionStage;
        private int moreEquipmentSlotsBackpackBaseline;
        private int moreEquipmentSlotsShieldBackpackBaseline;
        private int moreEquipmentSlotsTargetSlot = -1;
        private int moreEquipmentSlotsShieldValueBefore;
        private int moreEquipmentSlotsShieldValueAfterDamage;
        private object? moreEquipmentSlotsProductRuntime;
        private DateTimeOffset moreEquipmentSlotsStageStartedAt;
        private bool
            moreEquipmentSlotsInterruptedCandidateObservationCompleted;
        private bool
            moreEquipmentSlotsInterruptedCandidateObservationEnabled;
        private long
            expectedMoreEquipmentSlotsCandidatePreGeneration = -1L;
        private int
            expectedMoreEquipmentSlotsCandidateCommittedOccupied = -1;
        private string
            expectedMoreEquipmentSlotsCandidateCommittedSlots =
                string.Empty;

        private void
            ConfigureMoreEquipmentSlotsInterruptedCandidateObserverForFixture(
                bool enabled,
                long candidatePreGeneration,
                int committedOccupied,
                string committedSlots)
        {
            moreEquipmentSlotsInterruptedCandidateObservationEnabled =
                enabled;
            expectedMoreEquipmentSlotsCandidatePreGeneration =
                candidatePreGeneration;
            expectedMoreEquipmentSlotsCandidateCommittedOccupied =
                committedOccupied;
            expectedMoreEquipmentSlotsCandidateCommittedSlots =
                committedSlots ?? string.Empty;
        }

        private FixtureAttemptResult TryExerciseMoreEquipmentSlotsForFixture()
        {
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
                    inventory.ExactOwnerPatchCount != 4 ||
                    productRuntime == null ||
                    !runtime.HasOwnerInstance(MoreEquipmentSlotsOwnerId) ||
                    loadedOwners != 1 ||
                    ownerRoots <= 0)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots ProductNative readiness was incomplete. instance=" +
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

                if (moreEquipmentSlotsInterruptedCandidateObservationEnabled &&
                    !moreEquipmentSlotsInterruptedCandidateObservationCompleted)
                {
                    FixtureAttemptResult candidateObservation =
                        TryObserveMoreEquipmentSlotsInterruptedCandidateRecovery(
                            productRuntime,
                            inventory,
                            loadedOwners,
                            ownerRoots);
                    if (candidateObservation !=
                        FixtureAttemptResult.Succeeded)
                    {
                        return candidateObservation;
                    }

                    // Keep the first real mutation on a later Unity frame so
                    // the independent marker is ordered before GiveItem and
                    // every native SaveGame request in this route.
                    return FixtureAttemptResult.Pending;
                }

                moreEquipmentSlotsProductRuntime = productRuntime;
                switch (moreEquipmentSlotsTransactionStage)
                {
                    case 0:
                        return BeginMoreEquipmentSlotsProtectedTransaction(
                            productRuntime);
                    case 1:
                        return CommitMoreEquipmentSlotsEquippedSidecar(
                            productRuntime);
                    case 2:
                        return BeginMoreEquipmentSlotsUnequipTransaction(
                            productRuntime);
                    case 3:
                        return CompleteMoreEquipmentSlotsProtectedTransaction(
                            productRuntime,
                            inventory,
                            ownerRoots);
                    default:
                        return FixtureAttemptResult.Succeeded;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke MoreEquipmentSlots ProductNative transaction exercise failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlots",
                    "failed",
                    "MoreEquipmentSlots ProductNative real EquipFromBackpack/RequestUnequip -> DolocAPI.SaveGame",
                    ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult
            TryObserveMoreEquipmentSlotsInterruptedCandidateRecovery(
                object productRuntime,
                Batch6HarmonyOwnerInventory inventory,
                int loadedOwners,
                int ownerRoots)
        {
            try
            {
                object committed =
                    FindMoreEquipmentSlotsDocument(productRuntime) ??
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots committed sidecar is unavailable after interrupted-candidate recovery.");
                long generation =
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
                bool journalPresent =
                    ReadReflectedMember(
                        committed,
                        "Journal") != null;
                bool candidatePresent =
                    ReadReflectedMember(
                        committed,
                        "GameplayCandidate") != null;
                long expectedGeneration =
                    checked(
                        expectedMoreEquipmentSlotsCandidatePreGeneration +
                        1L);

                if (generation != expectedGeneration ||
                    committedOccupied !=
                        expectedMoreEquipmentSlotsCandidateCommittedOccupied ||
                    !string.Equals(
                        committedSlots,
                        expectedMoreEquipmentSlotsCandidateCommittedSlots,
                        StringComparison.Ordinal) ||
                    journalPresent ||
                    candidatePresent)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots interrupted gameplay candidate did not resolve as one persisted discard before product mutation. generation=" +
                        expectedMoreEquipmentSlotsCandidatePreGeneration +
                        "->" +
                        generation +
                        "; expectedPostGeneration=" +
                        expectedGeneration +
                        "; occupied=" +
                        expectedMoreEquipmentSlotsCandidateCommittedOccupied +
                        "->" +
                        committedOccupied +
                        "; committedSlotsMatch=" +
                        string.Equals(
                            committedSlots,
                            expectedMoreEquipmentSlotsCandidateCommittedSlots,
                            StringComparison.Ordinal) +
                        "; journal=" +
                        journalPresent +
                        "; candidate=" +
                        candidatePresent +
                        ".");
                }

                string summary =
                    "route=ProductNativeReadOnlyPreflight" +
                    "; saveMode=NativeSaveExpected" +
                    "; candidatePreGeneration=" +
                    expectedMoreEquipmentSlotsCandidatePreGeneration +
                    "; committedGeneration=" +
                    generation +
                    "; committedOccupied=" +
                    committedOccupied +
                    "; committedSlots={" +
                    committedSlots +
                    "}" +
                    "; committedSlotsBase64=" +
                    Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(
                            committedSlots)) +
                    "; persistedCleanupWrites=1" +
                    "; journal=false" +
                    "; candidate=false" +
                    "; beforeGiveItem=true" +
                    "; beforeNativeSave=true" +
                    "; exactOwner=" +
                    MoreEquipmentSlotsHarmonyOwner +
                    "; patches=" +
                    inventory.ExactOwnerPatchCount +
                    "; targets=" +
                    inventory.ExactOwnerTargetCount +
                    "/" +
                    inventory.ResolvedTargetCount +
                    "; loaded=" +
                    loadedOwners +
                    "; roots=" +
                    ownerRoots;
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise MoreEquipmentSlotsInterruptedCandidateRecovery OK " +
                    summary +
                    ".");
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsInterruptedCandidateRecovery",
                    "verified",
                    "ProductNative SaveLoaded interrupted-candidate cleanup before QA mutation",
                    summary);
                moreEquipmentSlotsInterruptedCandidateObservationCompleted =
                    true;
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke MoreEquipmentSlots interrupted-candidate observation failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsInterruptedCandidateRecovery",
                    "failed",
                    "ProductNative SaveLoaded interrupted-candidate cleanup before QA mutation",
                    ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult BeginMoreEquipmentSlotsProtectedTransaction(
            object productRuntime)
        {
            object committed =
                FindMoreEquipmentSlotsDocument(
                    productRuntime) ??
                throw new InvalidOperationException(
                    "MoreEquipmentSlots committed sidecar is unavailable before the protected transaction.");
            moreEquipmentSlotsTargetSlot =
                FindFirstEmptyEquipmentSlot(
                    ReadReflectedMember(
                        committed,
                        "Slots"));
            if (moreEquipmentSlotsTargetSlot < 0)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots protected transaction requires one empty fixed product slot.");
            }

            moreEquipmentSlotsBackpackBaseline =
                CountNativeItemForFixture(
                    ResolveMoreEquipmentSlotsDolocApiType(),
                    MoreEquipmentSlotsTransactionItemId,
                    checkBox: false);
            moreEquipmentSlotsShieldBackpackBaseline =
                CountNativeItemForFixture(
                    ResolveMoreEquipmentSlotsDolocApiType(),
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            InventoryGiveResult give = inventoryDebugApi.GiveItem(
                CreateFixtureManifest(),
                MoreEquipmentSlotsTransactionItemId,
                1);
            int afterGive = CountNativeItemForFixture(
                ResolveMoreEquipmentSlotsDolocApiType(),
                MoreEquipmentSlotsTransactionItemId,
                checkBox: false);
            if (!give.Success ||
                afterGive != moreEquipmentSlotsBackpackBaseline + 1)
            {
                throw new InvalidOperationException(
                    "Could not stage exactly one backpack item for the protected equipment transaction. item=" +
                    MoreEquipmentSlotsTransactionItemId +
                    "; give=" +
                    give.Success +
                    "; backpack=" +
                    moreEquipmentSlotsBackpackBaseline +
                    "->" +
                    afterGive +
                    "; message=" +
                    give.Message +
                    ".");
            }

            InvokeMoreEquipmentSlotsOperation(
                productRuntime,
                "EquipFromBackpack",
                moreEquipmentSlotsTargetSlot,
                MoreEquipmentSlotsTransactionItemId,
                "QA protected transaction equip");
            int afterEquip = CountNativeItemForFixture(
                ResolveMoreEquipmentSlotsDolocApiType(),
                MoreEquipmentSlotsTransactionItemId,
                checkBox: false);
            if (afterEquip != moreEquipmentSlotsBackpackBaseline)
            {
                throw new InvalidOperationException(
                    "Product EquipFromBackpack did not move exactly one in-memory item from the native backpack into the Working equipment slot before SaveSaving. backpack=" +
                    moreEquipmentSlotsBackpackBaseline +
                        "->" +
                        afterEquip +
                        ".");
            }

            InventoryGiveResult shieldGive =
                inventoryDebugApi.GiveItem(
                    CreateFixtureManifest(),
                    MoreEquipmentSlotsShieldItemId,
                    1);
            int afterShieldGive =
                CountNativeItemForFixture(
                    ResolveMoreEquipmentSlotsDolocApiType(),
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            if (!shieldGive.Success ||
                afterShieldGive !=
                    moreEquipmentSlotsShieldBackpackBaseline + 1)
            {
                throw new InvalidOperationException(
                    "Could not stage exactly one box_hat for the protected replacement/shield transaction. backpack=" +
                    moreEquipmentSlotsShieldBackpackBaseline +
                    "->" +
                    afterShieldGive +
                    "; message=" +
                    shieldGive.Message +
                    ".");
            }

            InvokeMoreEquipmentSlotsOperation(
                productRuntime,
                "EquipFromBackpack",
                moreEquipmentSlotsTargetSlot,
                MoreEquipmentSlotsShieldItemId,
                "QA protected replacement with shield");
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
                    moreEquipmentSlotsBackpackBaseline + 1 ||
                shieldAfterReplacement !=
                    moreEquipmentSlotsShieldBackpackBaseline)
            {
                throw new InvalidOperationException(
                    "The protected ProductNative replacement did not move exactly one outgoing item and one incoming shield. grandmas=" +
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
                    moreEquipmentSlotsTargetSlot);
            moreEquipmentSlotsShieldValueBefore =
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
                moreEquipmentSlotsShieldValueBefore <= 1)
            {
                throw new InvalidOperationException(
                    "The protected native box_hat did not expose live ProductNative shield state.");
            }

            InvokeRealMoreEquipmentSlotsAttack(
                shieldDefend +
                Math.Max(
                    1,
                    moreEquipmentSlotsShieldValueBefore / 2));
            object shieldAfterDamage =
                RequireEquipmentSlotEntry(
                    ReadReflectedMember(
                        productRuntime,
                        "workingSlots"),
                    moreEquipmentSlotsTargetSlot);
            moreEquipmentSlotsShieldValueAfterDamage =
                Convert.ToInt32(
                    ReadReflectedMember(
                        shieldAfterDamage,
                        "ShieldValue") ?? 0);
            if (moreEquipmentSlotsShieldValueAfterDamage <= 0 ||
                moreEquipmentSlotsShieldValueAfterDamage >=
                    moreEquipmentSlotsShieldValueBefore)
            {
                throw new InvalidOperationException(
                    "The real protected BodyController.OnAttacked route did not leave a non-breaking Working shield hit. shield=" +
                    moreEquipmentSlotsShieldValueBefore +
                    "->" +
                    moreEquipmentSlotsShieldValueAfterDamage +
                    ".");
            }

            RequestMoreEquipmentSlotsNativeSave();
            moreEquipmentSlotsTransactionStage = 1;
            moreEquipmentSlotsStageStartedAt = DateTimeOffset.UtcNow;
            runtime.SetHookStatus(
                "Smoke.MoreEquipmentSlots",
                "pending",
                "ProductNative replacement + real shield damage -> DolocAPI.SaveGame(2)",
                "Waiting for SaveSaved to commit the damaged shield sidecar.");
            return FixtureAttemptResult.Pending;
        }

        private FixtureAttemptResult CommitMoreEquipmentSlotsEquippedSidecar(
            object productRuntime)
        {
            ReadMoreEquipmentSlotsDocumentCounts(
                productRuntime,
                out int occupied,
                out int journalCount);
            if (occupied != 1 || journalCount != 0)
                return WaitForMoreEquipmentSlotsStage(
                    "damaged shield sidecar commit",
                    "occupied=" + occupied + "; journal=" + journalCount);

            object committed =
                FindMoreEquipmentSlotsDocument(
                    productRuntime) ??
                throw new InvalidOperationException(
                    "MoreEquipmentSlots committed sidecar disappeared after the first SaveSaved.");
            object committedShield =
                RequireEquipmentSlotEntry(
                    ReadReflectedMember(
                        committed,
                        "Slots"),
                    moreEquipmentSlotsTargetSlot);
            string committedItemId =
                ReadReflectedMember(
                    committedShield,
                    "ItemId") as string ?? string.Empty;
            int committedShieldValue =
                Convert.ToInt32(
                    ReadReflectedMember(
                        committedShield,
                        "ShieldValue") ?? 0);
            if (!string.Equals(
                    committedItemId,
                    MoreEquipmentSlotsShieldItemId,
                    StringComparison.Ordinal) ||
                committedShieldValue !=
                    moreEquipmentSlotsShieldValueAfterDamage)
            {
                throw new InvalidOperationException(
                    "SaveSaved did not commit the real damaged shield state. item=" +
                    committedItemId +
                    "; shield=" +
                    committedShieldValue +
                    "; expected=" +
                    moreEquipmentSlotsShieldValueAfterDamage +
                    ".");
            }

            moreEquipmentSlotsTransactionStage = 2;
            moreEquipmentSlotsStageStartedAt = DateTimeOffset.UtcNow;
            return FixtureAttemptResult.Pending;
        }

        private FixtureAttemptResult BeginMoreEquipmentSlotsUnequipTransaction(
            object productRuntime)
        {
            object shieldBeforeBreak =
                RequireEquipmentSlotEntry(
                    ReadReflectedMember(
                        productRuntime,
                        "workingSlots"),
                    moreEquipmentSlotsTargetSlot);
            int shieldDefend =
                Convert.ToInt32(
                    ReadReflectedMember(
                        shieldBeforeBreak,
                        "ShieldDefend") ?? 0);
            int shieldValue =
                Convert.ToInt32(
                    ReadReflectedMember(
                        shieldBeforeBreak,
                        "ShieldValue") ?? 0);
            InvokeRealMoreEquipmentSlotsAttack(
                shieldDefend +
                shieldValue +
                1);
            object afterBreak =
                RequireEquipmentSlotEntry(
                    ReadReflectedMember(
                        productRuntime,
                        "workingSlots"),
                    moreEquipmentSlotsTargetSlot);
            if (!string.IsNullOrWhiteSpace(
                    ReadReflectedMember(
                        afterBreak,
                        "ItemId") as string))
            {
                throw new InvalidOperationException(
                    "The real protected shield-break route did not clear the Working slot.");
            }

            InvokeMoreEquipmentSlotsOperation(
                productRuntime,
                "EquipFromBackpack",
                moreEquipmentSlotsTargetSlot,
                MoreEquipmentSlotsTransactionItemId,
                "QA protected post-break equip");
            InvokeMoreEquipmentSlotsOperation(
                productRuntime,
                "RequestUnequip",
                moreEquipmentSlotsTargetSlot,
                MoreEquipmentSlotsTransactionItemId,
                "QA protected transaction unequip");
            RequestMoreEquipmentSlotsNativeSave();
            moreEquipmentSlotsTransactionStage = 3;
            moreEquipmentSlotsStageStartedAt = DateTimeOffset.UtcNow;
            runtime.SetHookStatus(
                "Smoke.MoreEquipmentSlots",
                "pending",
                "ProductNative real shield break + equip/unequip -> DolocAPI.SaveGame(2)",
                "Waiting for the empty committed sidecar after the second SaveSaved.");
            return FixtureAttemptResult.Pending;
        }

        private FixtureAttemptResult CompleteMoreEquipmentSlotsProtectedTransaction(
            object productRuntime,
            Batch6HarmonyOwnerInventory inventory,
            int ownerRoots)
        {
            ReadMoreEquipmentSlotsDocumentCounts(
                productRuntime,
                out int occupied,
                out int journalCount);
            int backpack = CountNativeItemForFixture(
                ResolveMoreEquipmentSlotsDolocApiType(),
                MoreEquipmentSlotsTransactionItemId,
                checkBox: false);
            int shieldBackpack =
                CountNativeItemForFixture(
                    ResolveMoreEquipmentSlotsDolocApiType(),
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            if (occupied != 0 ||
                journalCount != 0 ||
                backpack != moreEquipmentSlotsBackpackBaseline + 1 ||
                shieldBackpack !=
                    moreEquipmentSlotsShieldBackpackBaseline)
            {
                return WaitForMoreEquipmentSlotsStage(
                    "post-break empty sidecar commit",
                    "occupied=" +
                    occupied +
                    "; journal=" +
                    journalCount +
                    "; backpack=" +
                    backpack +
                    "; expectedBackpack=" +
                    (moreEquipmentSlotsBackpackBaseline + 1) +
                    "; shieldBackpack=" +
                    shieldBackpack +
                    "; expectedShieldBackpack=" +
                    moreEquipmentSlotsShieldBackpackBaseline);
            }

            bool removed = CostNativeItemForFixture(
                ResolveMoreEquipmentSlotsDolocApiType(),
                MoreEquipmentSlotsTransactionItemId,
                1,
                checkBox: false);
            int afterCleanup = CountNativeItemForFixture(
                ResolveMoreEquipmentSlotsDolocApiType(),
                MoreEquipmentSlotsTransactionItemId,
                checkBox: false);
            if (!removed ||
                afterCleanup != moreEquipmentSlotsBackpackBaseline)
            {
                throw new InvalidOperationException(
                    "QA could not remove its recovered item after proving the protected transaction. backpack=" +
                    backpack +
                    "->" +
                    afterCleanup +
                    ".");
            }

            string transaction =
                "destination=Backpack" +
                " item=" +
                MoreEquipmentSlotsTransactionItemId +
                " native=1 committedSidecar=0 journal=0 logicalItems=1" +
                " replacement=grandmas_button->box_hat" +
                " shieldDamage=" +
                moreEquipmentSlotsShieldValueBefore +
                "->" +
                moreEquipmentSlotsShieldValueAfterDamage +
                " shieldBreak=true equipAfterBreak=true unequipAfterBreak=true";
            string summary =
                "route=ProductNative" +
                "; fixedExtraSlots=3" +
                "; exactOwner=" +
                MoreEquipmentSlotsHarmonyOwner +
                "; patches=4" +
                "; targets=4/4" +
                "; callback=1" +
                "; loaded=1" +
                "; roots=" +
                ownerRoots +
                "; " +
                transaction +
                "; inventory={" +
                inventory.Details +
                "}";
            runtime.RuntimeMonitor.Log(
                "MoreEquipmentSlots protected transaction committed " +
                transaction +
                ".");
            runtime.RuntimeMonitor.Log(
                "Smoke exercise MoreEquipmentSlots OK " +
                summary +
                ".");
            runtime.SetHookStatus(
                "Smoke.MoreEquipmentSlots",
                "verified",
                "MoreEquipmentSlots ProductNative replacement/damage/break/equip/unequip -> two native saves",
                summary);
            moreEquipmentSlotsTransactionStage = 4;
            return FixtureAttemptResult.Succeeded;
        }

        private FixtureAttemptResult WaitForMoreEquipmentSlotsStage(
            string stage,
            string observation)
        {
            if ((DateTimeOffset.UtcNow -
                moreEquipmentSlotsStageStartedAt).TotalSeconds > 30d)
            {
                throw new TimeoutException(
                    "MoreEquipmentSlots protected transaction timed out waiting for " +
                    stage +
                    "; " +
                    observation +
                    ".");
            }

            return FixtureAttemptResult.Pending;
        }

        private static void InvokeMoreEquipmentSlotsOperation(
            object productRuntime,
            string operation,
            int slotIndex,
            string itemId,
            string reason)
        {
            MethodInfo[] candidates = productRuntime.GetType().GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Where(method =>
                    method.Name.Equals(
                        operation,
                        StringComparison.Ordinal))
                .ToArray();
            if (candidates.Length != 1)
            {
                throw new MissingMethodException(
                    productRuntime.GetType().FullName,
                    operation +
                    " (expected exactly one UI-shared production method; found " +
                    candidates.Length +
                    ")");
            }

            MethodInfo method = candidates[0];
            ParameterInfo[] parameters = method.GetParameters();
            var arguments = new object?[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                ParameterInfo parameter = parameters[index];
                Type parameterType = parameter.ParameterType.IsByRef
                    ? parameter.ParameterType.GetElementType()!
                    : parameter.ParameterType;
                string name = parameter.Name ?? string.Empty;
                if (parameterType == typeof(int))
                {
                    arguments[index] = slotIndex;
                }
                else if (parameterType == typeof(string))
                {
                    arguments[index] =
                        name.IndexOf(
                            "item",
                            StringComparison.OrdinalIgnoreCase) >= 0
                            ? itemId
                            : reason;
                }
                else if (parameterType == typeof(bool))
                {
                    arguments[index] = false;
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[index] = parameter.DefaultValue;
                }
                else
                {
                    throw new InvalidOperationException(
                        "QA cannot bind production operation " +
                        operation +
                        " parameter " +
                        name +
                        ":" +
                        parameterType.FullName +
                        ".");
                }
            }

            object? result;
            try
            {
                result = method.Invoke(productRuntime, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }

            if (method.ReturnType == typeof(bool) &&
                result is bool succeeded &&
                !succeeded)
            {
                string outMessage = arguments
                    .Where((value, index) =>
                        parameters[index].ParameterType.IsByRef &&
                        parameters[index].ParameterType.GetElementType() ==
                        typeof(string))
                    .OfType<string>()
                    .FirstOrDefault() ?? string.Empty;
                throw new InvalidOperationException(
                    "MoreEquipmentSlots production " +
                    operation +
                    " rejected the QA-owned transaction. " +
                    outMessage);
            }
        }

        private static object RequireEquipmentSlotEntry(
            object? slots,
            int slotIndex)
        {
            if (!(slots is IEnumerable entries))
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots Working slot collection is unavailable.");
            }

            foreach (object? entry in entries)
            {
                if (entry != null &&
                    Convert.ToInt32(
                        ReadReflectedMember(
                            entry,
                            "Index") ?? -1) == slotIndex)
                {
                    return entry;
                }
            }

            throw new InvalidOperationException(
                "MoreEquipmentSlots Working slot " +
                slotIndex +
                " is unavailable.");
        }

        private void InvokeRealMoreEquipmentSlotsAttack(
            int damageAfterNativeDefense)
        {
            Type dolocApi =
                ResolveMoreEquipmentSlotsDolocApiType();
            object? body =
                dolocApi.GetField(
                    "agent",
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)?.GetValue(null) ??
                dolocApi.GetProperty(
                    "agent",
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)?.GetValue(null);
            if (body == null)
            {
                throw new InvalidOperationException(
                    "DolocAPI.agent is unavailable for the real ProductNative shield attack route.");
            }

            MethodInfo[] targets = body.GetType().GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Where(method =>
                {
                    if (!method.Name.Equals(
                            "OnAttacked",
                            StringComparison.Ordinal))
                    {
                        return false;
                    }

                    ParameterInfo[] parameters =
                        method.GetParameters();
                    return parameters.Length == 4 &&
                        parameters[0].ParameterType ==
                            typeof(float) &&
                        parameters[1].ParameterType ==
                            typeof(bool) &&
                        parameters[2].ParameterType.FullName ==
                            "UnityEngine.Vector2" &&
                        parameters[3].ParameterType.IsByRef &&
                        parameters[3].ParameterType
                            .GetElementType() == typeof(bool);
                })
                .ToArray();
            if (targets.Length != 1)
            {
                throw new MissingMethodException(
                    body.GetType().FullName,
                    "OnAttacked(float,bool,Vector2,out bool) (found " +
                    targets.Length +
                    ")");
            }

            float nativeDefense = Convert.ToSingle(
                ReadReflectedMember(
                    body,
                    "CurrentDefend") ?? 0f);
            object position =
                Activator.CreateInstance(
                    targets[0].GetParameters()[2].ParameterType) ??
                throw new InvalidOperationException(
                    "Could not construct the native Vector2 attack position.");
            object?[] arguments =
            {
                nativeDefense +
                    Math.Max(
                        1,
                        damageAfterNativeDefense),
                false,
                position,
                false
            };
            object? result;
            try
            {
                result = targets[0].Invoke(
                    body,
                    arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }

            if (!(result is bool handled) ||
                !handled ||
                arguments[3] is bool isDead &&
                isDead)
            {
                throw new InvalidOperationException(
                    "The real BodyController.OnAttacked route did not complete as a non-fatal handled ProductNative attack. handled=" +
                    (result is bool value && value) +
                    "; isDead=" +
                    (arguments[3] is bool dead && dead) +
                    ".");
            }
        }

        private static void ReadMoreEquipmentSlotsDocumentCounts(
            object productRuntime,
            out int occupied,
            out int journalCount)
        {
            object? document = FindMoreEquipmentSlotsDocument(
                productRuntime);
            if (document == null)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots runtime did not expose its current normal storage document.");
            }

            object? slots = ReadReflectedMember(document, "Slots");
            occupied = 0;
            if (slots is IEnumerable items)
            {
                foreach (object? item in items)
                {
                    string itemId =
                        ReadReflectedMember(item, "ItemId") as string ??
                        string.Empty;
                    if (!string.IsNullOrWhiteSpace(itemId))
                        occupied++;
                }
            }
            journalCount =
                ReadReflectedMember(document, "Journal") == null ? 0 : 1;
        }

        private static object? FindMoreEquipmentSlotsDocument(
            object productRuntime)
        {
            object? store =
                ReadReflectedMember(productRuntime, "store");
            object? scope =
                ReadReflectedMember(productRuntime, "scope");
            string sidecarPath =
                ReadReflectedMember(
                    productRuntime,
                    "sidecarPath") as string ??
                string.Empty;
            if (store == null ||
                scope == null ||
                string.IsNullOrWhiteSpace(sidecarPath))
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots durable document identity is incomplete. path=" +
                    sidecarPath +
                    "; store=" +
                    (store != null) +
                    "; scope=" +
                    (scope != null) +
                    ".");
            }

            MethodInfo[] readers = store.GetType().GetMethods(
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Where(method =>
                    method.Name.Equals(
                        "TryReadValidated",
                        StringComparison.Ordinal) &&
                    method.GetParameters().Length == 4)
                .ToArray();
            if (readers.Length != 1)
            {
                throw new MissingMethodException(
                    store.GetType().FullName,
                    "TryReadValidated(path, scope, out document, out failure)");
            }

            object?[] arguments =
            {
                sidecarPath,
                scope,
                null,
                string.Empty
            };
            object? result;
            try
            {
                result = readers[0].Invoke(null, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
            if (!(result is bool succeeded) ||
                !succeeded ||
                arguments[2] == null)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots durable sidecar validation failed. path=" +
                    sidecarPath +
                    "; failure=" +
                    (arguments[3] as string ?? "unknown") +
                    ".");
            }

            return arguments[2];
        }

        private static object? ReadReflectedMember(
            object? instance,
            string name)
        {
            if (instance == null)
                return null;
            Type type = instance.GetType();
            return type.GetField(
                       name,
                       BindingFlags.Instance |
                       BindingFlags.Public |
                       BindingFlags.NonPublic |
                       BindingFlags.IgnoreCase)?.GetValue(instance) ??
                type.GetProperty(
                       name,
                       BindingFlags.Instance |
                       BindingFlags.Public |
                       BindingFlags.NonPublic |
                       BindingFlags.IgnoreCase)?.GetValue(instance);
        }

        private Type ResolveMoreEquipmentSlotsDolocApiType()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            return patcher.ResolveType("DolocAPI, Assembly-CSharp") ??
                throw new TypeLoadException(
                    "DolocAPI, Assembly-CSharp was not found.");
        }

        private void RequestMoreEquipmentSlotsNativeSave()
        {
            Type dolocApi = ResolveMoreEquipmentSlotsDolocApiType();
            MethodInfo? saveGame = dolocApi.GetMethod(
                "SaveGame",
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(int) },
                modifiers: null);
            if (saveGame == null)
            {
                throw new MissingMethodException(
                    dolocApi.FullName,
                    "SaveGame(int)");
            }

            object? result;
            try
            {
                result = saveGame.Invoke(null, new object[] { 2 });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
            if (result is bool saved && !saved)
            {
                throw new InvalidOperationException(
                    "DolocAPI.SaveGame(2) rejected the protected equipment transaction.");
            }
        }
    }
}
