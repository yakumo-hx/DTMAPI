using System;
using System.Collections;
using System.Linq;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private int moreEquipmentSlotsCommittedShieldSetupStage;
        private int moreEquipmentSlotsCommittedShieldSetupTargetSlot = -1;
        private int moreEquipmentSlotsCommittedShieldSetupValue;
        private DateTimeOffset moreEquipmentSlotsCommittedShieldSetupStartedAt;
        private bool moreEquipmentSlotsCommittedShieldDamageCompleted;
        private bool moreEquipmentSlotsCommittedShieldBreakReplaceCompleted;

        private FixtureAttemptResult
            TrySetupMoreEquipmentSlotsCommittedShieldForFixture()
        {
            try
            {
                MoreEquipmentSlotsQaReadiness readiness =
                    RequireMoreEquipmentSlotsQaReadiness(
                        "committed-shield setup");
                object productRuntime = readiness.ProductRuntime;
                if (moreEquipmentSlotsCommittedShieldSetupStage == 0)
                {
                    object committed =
                        ReadReflectedMember(
                            productRuntime,
                            "document") ??
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots committed document is unavailable before shield setup.");
                    object? committedSlots =
                        ReadReflectedMember(
                            committed,
                            "Slots");
                    if (FindEquipmentSlotsByItemId(
                            committedSlots,
                            MoreEquipmentSlotsShieldItemId).Length != 0)
                    {
                        throw new InvalidOperationException(
                            "Committed-shield setup requires no pre-existing box_hat in the ProductNative sidecar.");
                    }

                    moreEquipmentSlotsCommittedShieldSetupTargetSlot =
                        FindFirstEmptyEquipmentSlot(committedSlots);
                    if (moreEquipmentSlotsCommittedShieldSetupTargetSlot < 0)
                    {
                        throw new InvalidOperationException(
                            "Committed-shield setup requires one empty fixed product slot.");
                    }

                    int initialNativeShield =
                        CountNativeItemForFixture(
                            ResolveMoreEquipmentSlotsDolocApiType(),
                            MoreEquipmentSlotsShieldItemId,
                            checkBox: false);
                    if (initialNativeShield != 0)
                    {
                        throw new InvalidOperationException(
                            "Committed-shield setup requires a disposable fixture with no native box_hat.");
                    }

                    InventoryGiveResult give =
                        inventoryDebugApi.GiveItem(
                            CreateFixtureManifest(),
                            MoreEquipmentSlotsShieldItemId,
                            1);
                    int afterGive =
                        CountNativeItemForFixture(
                            ResolveMoreEquipmentSlotsDolocApiType(),
                            MoreEquipmentSlotsShieldItemId,
                            checkBox: false);
                    if (!give.Success || afterGive != 1)
                    {
                        throw new InvalidOperationException(
                            "Could not stage exactly one QA-owned box_hat. backpack=0->" +
                            afterGive +
                            "; message=" +
                            give.Message +
                            ".");
                    }

                    InvokeMoreEquipmentSlotsOperation(
                        productRuntime,
                        "EquipFromBackpack",
                        moreEquipmentSlotsCommittedShieldSetupTargetSlot,
                        MoreEquipmentSlotsShieldItemId,
                        "QA committed-shield setup");
                    object workingShield =
                        RequireEquipmentSlotEntry(
                            ReadReflectedMember(
                                productRuntime,
                                "workingSlots"),
                            moreEquipmentSlotsCommittedShieldSetupTargetSlot);
                    moreEquipmentSlotsCommittedShieldSetupValue =
                        Convert.ToInt32(
                            ReadReflectedMember(
                                workingShield,
                                "ShieldValue") ?? 0);
                    if (moreEquipmentSlotsCommittedShieldSetupValue <= 1 ||
                        CountNativeItemForFixture(
                            ResolveMoreEquipmentSlotsDolocApiType(),
                            MoreEquipmentSlotsShieldItemId,
                            checkBox: false) != 0)
                    {
                        throw new InvalidOperationException(
                            "Committed-shield setup did not move one live shield exclusively into Working state.");
                    }

                    RequestMoreEquipmentSlotsNativeSave();
                    moreEquipmentSlotsCommittedShieldSetupStage = 1;
                    moreEquipmentSlotsCommittedShieldSetupStartedAt =
                        DateTimeOffset.UtcNow;
                    runtime.SetHookStatus(
                        "Smoke.MoreEquipmentSlotsCommittedShieldSetup",
                        "pending",
                        "ProductNative box_hat -> native SaveGame",
                        "Waiting for SaveSaved to promote the non-empty committed shield.");
                    return FixtureAttemptResult.Pending;
                }

                object durable =
                    FindMoreEquipmentSlotsDocument(productRuntime) ??
                    throw new InvalidOperationException(
                        "Committed-shield setup sidecar is unavailable after SaveSaved.");
                object[] durableShields =
                    FindEquipmentSlotsByItemId(
                        ReadReflectedMember(
                            durable,
                            "Slots"),
                        MoreEquipmentSlotsShieldItemId);
                if (durableShields.Length != 1 ||
                    Convert.ToInt32(
                        ReadReflectedMember(
                            durableShields[0],
                            "Index") ?? -1) !=
                        moreEquipmentSlotsCommittedShieldSetupTargetSlot ||
                    Convert.ToInt32(
                        ReadReflectedMember(
                            durableShields[0],
                            "ShieldValue") ?? 0) !=
                        moreEquipmentSlotsCommittedShieldSetupValue ||
                    ReadReflectedMember(durable, "Journal") != null ||
                    ReadReflectedMember(
                        durable,
                        "GameplayCandidate") != null)
                {
                    if ((DateTimeOffset.UtcNow -
                        moreEquipmentSlotsCommittedShieldSetupStartedAt)
                        .TotalSeconds <= 30d)
                    {
                        return FixtureAttemptResult.Pending;
                    }

                    throw new TimeoutException(
                        "SaveSaved did not promote exactly one non-empty committed shield without a journal/candidate.");
                }

                int nativeShield =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false);
                if (nativeShield != 0)
                {
                    throw new InvalidOperationException(
                        "Committed-shield setup left a duplicate native box_hat. native=" +
                        nativeShield +
                        ".");
                }

                long generation =
                    Convert.ToInt64(
                        ReadReflectedMember(
                            durable,
                            "Generation") ?? 0L);
                int occupied =
                    CountOccupiedEquipmentSlots(
                        ReadReflectedMember(
                            durable,
                            "Slots"));
                string slots =
                    DescribeEquipmentSlots(
                        ReadReflectedMember(
                            durable,
                            "Slots"));
                int grandmasBackpack =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                string summary =
                    "route=ProductNative" +
                    "; saveMode=NativeSaveExpected" +
                    "; logicalShieldItems=1" +
                    "; nativeShieldItems=0" +
                    "; committedShieldItems=1" +
                    "; committedShieldValue=" +
                    moreEquipmentSlotsCommittedShieldSetupValue +
                    "; targetSlot=" +
                    moreEquipmentSlotsCommittedShieldSetupTargetSlot +
                    "; backpackBaseline=" +
                    grandmasBackpack +
                    "; committedGeneration=" +
                    generation +
                    "; committedOccupied=" +
                    occupied +
                    "; committedSlots={" +
                    slots +
                    "}" +
                    "; committedSlotsBase64=" +
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(slots)) +
                    "; journal=false; candidate=false" +
                    "; exactOwner=" +
                    MoreEquipmentSlotsHarmonyOwner +
                    "; patches=" +
                    MoreEquipmentSlotsHarmonyTargets.Length +
                    "; targets=" +
                    MoreEquipmentSlotsHarmonyTargets.Length +
                    "/" +
                    MoreEquipmentSlotsHarmonyTargets.Length +
                    "; callback=1; loaded=1; roots=" +
                    readiness.OwnerRoots;
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise MoreEquipmentSlotsCommittedShieldSetup OK " +
                    summary +
                    ".");
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsCommittedShieldSetup",
                    "verified",
                    "ProductNative one-shield committed baseline",
                    summary);
                moreEquipmentSlotsCommittedShieldSetupStage = 2;
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke MoreEquipmentSlots committed-shield setup failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsCommittedShieldSetup",
                    "failed",
                    "ProductNative one-shield committed baseline",
                    ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult
            TryDamageMoreEquipmentSlotsCommittedShieldNoNativeSaveForFixture()
        {
            if (moreEquipmentSlotsCommittedShieldDamageCompleted)
                return FixtureAttemptResult.Succeeded;

            try
            {
                MoreEquipmentSlotsCommittedShieldBaseline baseline =
                    RequireMoreEquipmentSlotsCommittedShieldBaseline(
                        "damage rollback");
                InvokeRealMoreEquipmentSlotsAttack(
                    baseline.ShieldDefend +
                    Math.Max(
                        1,
                        baseline.ShieldValue / 2));
                object damagedShield =
                    RequireEquipmentSlotEntry(
                        ReadReflectedMember(
                            baseline.ProductRuntime,
                            "workingSlots"),
                        baseline.TargetSlot);
                int damagedShieldValue =
                    Convert.ToInt32(
                        ReadReflectedMember(
                            damagedShield,
                            "ShieldValue") ?? 0);
                if (damagedShieldValue <= 0 ||
                    damagedShieldValue >= baseline.ShieldValue)
                {
                    throw new InvalidOperationException(
                        "The real ProductNative attack did not damage the committed shield in Working state only.");
                }

                AssertMoreEquipmentSlotsCommittedShieldUnchanged(
                    baseline,
                    "damage");
                string summary =
                    BuildMoreEquipmentSlotsCommittedShieldSummary(
                        baseline,
                        "Damage",
                        "workingShieldDamage=" +
                        baseline.ShieldValue +
                        "->" +
                        damagedShieldValue);
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise MoreEquipmentSlotsCommittedShieldDamageNoNativeSave OK " +
                    summary +
                    ".");
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsCommittedShieldDamageNoNativeSave",
                    "verified",
                    "Committed ProductNative shield damage remained Working-only",
                    summary);
                moreEquipmentSlotsCommittedShieldDamageCompleted = true;
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke MoreEquipmentSlots committed-shield damage rollback failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsCommittedShieldDamageNoNativeSave",
                    "failed",
                    "Committed ProductNative shield damage remained Working-only",
                    ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult
            TryBreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSaveForFixture()
        {
            if (moreEquipmentSlotsCommittedShieldBreakReplaceCompleted)
                return FixtureAttemptResult.Succeeded;

            try
            {
                MoreEquipmentSlotsCommittedShieldBaseline baseline =
                    RequireMoreEquipmentSlotsCommittedShieldBaseline(
                        "break/replace/unequip rollback");
                InvokeMoreEquipmentSlotsOperation(
                    baseline.ProductRuntime,
                    "RequestUnequip",
                    baseline.TargetSlot,
                    MoreEquipmentSlotsShieldItemId,
                    "QA no-save committed shield unequip");
                if (CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false) != 1)
                {
                    throw new InvalidOperationException(
                        "Unsaved committed-shield unequip did not produce exactly one native shield.");
                }
                InvokeMoreEquipmentSlotsOperation(
                    baseline.ProductRuntime,
                    "EquipFromBackpack",
                    baseline.TargetSlot,
                    MoreEquipmentSlotsShieldItemId,
                    "QA no-save committed shield re-equip");

                InventoryGiveResult giveReplacement =
                    inventoryDebugApi.GiveItem(
                        CreateFixtureManifest(),
                        MoreEquipmentSlotsTransactionItemId,
                        1);
                if (!giveReplacement.Success)
                {
                    throw new InvalidOperationException(
                        "Could not stage the transient replacement item: " +
                        giveReplacement.Message +
                        ".");
                }
                InvokeMoreEquipmentSlotsOperation(
                    baseline.ProductRuntime,
                    "EquipFromBackpack",
                    baseline.TargetSlot,
                    MoreEquipmentSlotsTransactionItemId,
                    "QA no-save replacement of committed shield");
                if (CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false) != 1)
                {
                    throw new InvalidOperationException(
                        "Replacing the committed shield did not return exactly one shield to native inventory.");
                }

                InvokeMoreEquipmentSlotsOperation(
                    baseline.ProductRuntime,
                    "RequestUnequip",
                    baseline.TargetSlot,
                    MoreEquipmentSlotsTransactionItemId,
                    "QA no-save replacement unequip");
                InvokeMoreEquipmentSlotsOperation(
                    baseline.ProductRuntime,
                    "EquipFromBackpack",
                    baseline.TargetSlot,
                    MoreEquipmentSlotsShieldItemId,
                    "QA no-save shield re-equip before break");
                InvokeRealMoreEquipmentSlotsAttack(
                    baseline.ShieldDefend +
                    baseline.ShieldValue +
                    1);

                object afterBreak =
                    RequireEquipmentSlotEntry(
                        ReadReflectedMember(
                            baseline.ProductRuntime,
                            "workingSlots"),
                        baseline.TargetSlot);
                int nativeShieldAfterBreak =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsShieldItemId,
                        checkBox: false);
                int grandmasAfterUnequip =
                    CountNativeItemForFixture(
                        ResolveMoreEquipmentSlotsDolocApiType(),
                        MoreEquipmentSlotsTransactionItemId,
                        checkBox: false);
                if (!string.IsNullOrWhiteSpace(
                        ReadReflectedMember(
                            afterBreak,
                            "ItemId") as string) ||
                    nativeShieldAfterBreak != 0 ||
                    grandmasAfterUnequip !=
                        baseline.BackpackBaseline + 1)
                {
                    throw new InvalidOperationException(
                        "The no-save unequip/replace/re-equip/break route did not end in the expected uncommitted Working state.");
                }

                AssertMoreEquipmentSlotsCommittedShieldUnchanged(
                    baseline,
                    "unequip/replace/break");
                string summary =
                    BuildMoreEquipmentSlotsCommittedShieldSummary(
                        baseline,
                        "BreakReplaceUnequip",
                        "workingShieldBroken=true; replacement=true; unequip=true; transientGrandmasItems=1");
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave OK " +
                    summary +
                    ".");
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave",
                    "verified",
                    "Committed ProductNative shield break/replace/unequip remained Working-only",
                    summary);
                moreEquipmentSlotsCommittedShieldBreakReplaceCompleted = true;
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke MoreEquipmentSlots committed-shield break/replace rollback failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave",
                    "failed",
                    "Committed ProductNative shield break/replace/unequip remained Working-only",
                    ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private MoreEquipmentSlotsCommittedShieldBaseline
            RequireMoreEquipmentSlotsCommittedShieldBaseline(
                string scenario)
        {
            MoreEquipmentSlotsQaReadiness readiness =
                RequireMoreEquipmentSlotsQaReadiness(scenario);
            object productRuntime = readiness.ProductRuntime;
            object committed =
                ReadReflectedMember(
                    productRuntime,
                    "document") ??
                throw new InvalidOperationException(
                    "Committed-shield rollback requires the in-memory committed document.");
            object[] shields =
                FindEquipmentSlotsByItemId(
                    ReadReflectedMember(
                        committed,
                        "Slots"),
                    MoreEquipmentSlotsShieldItemId);
            if (shields.Length != 1)
            {
                throw new InvalidOperationException(
                    "Committed-shield rollback requires exactly one committed box_hat; found " +
                    shields.Length +
                    ".");
            }

            int targetSlot =
                Convert.ToInt32(
                    ReadReflectedMember(
                        shields[0],
                        "Index") ?? -1);
            int shieldValue =
                Convert.ToInt32(
                    ReadReflectedMember(
                        shields[0],
                        "ShieldValue") ?? 0);
            int shieldDefend =
                Convert.ToInt32(
                    ReadReflectedMember(
                        shields[0],
                        "ShieldDefend") ?? 0);
            long generation =
                Convert.ToInt64(
                    ReadReflectedMember(
                        committed,
                        "Generation") ?? 0L);
            int occupied =
                CountOccupiedEquipmentSlots(
                    ReadReflectedMember(
                        committed,
                        "Slots"));
            string slots =
                DescribeEquipmentSlots(
                    ReadReflectedMember(
                        committed,
                        "Slots"));
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
            int nativeShield =
                CountNativeItemForFixture(
                    ResolveMoreEquipmentSlotsDolocApiType(),
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            int backpack =
                CountNativeItemForFixture(
                    ResolveMoreEquipmentSlotsDolocApiType(),
                    MoreEquipmentSlotsTransactionItemId,
                    checkBox: false);
            if (targetSlot < 0 ||
                shieldValue <= 1 ||
                ReadReflectedMember(
                    committed,
                    "Journal") != null ||
                ReadReflectedMember(
                    committed,
                    "GameplayCandidate") != null ||
                nativeShield != 0 ||
                workingDirty ||
                !string.Equals(
                    workingSlots,
                    slots,
                    StringComparison.Ordinal) ||
                generation !=
                    expectedMoreEquipmentSlotsCommittedGeneration ||
                occupied !=
                    expectedMoreEquipmentSlotsCommittedOccupied ||
                !string.Equals(
                    slots,
                    expectedMoreEquipmentSlotsCommittedSlots,
                    StringComparison.Ordinal) ||
                backpack !=
                    expectedMoreEquipmentSlotsBackpackBaseline)
            {
                throw new InvalidOperationException(
                    "Committed-shield rollback did not begin from the exact normally saved cold baseline.");
            }

            return new MoreEquipmentSlotsCommittedShieldBaseline(
                productRuntime,
                readiness.OwnerRoots,
                targetSlot,
                shieldValue,
                shieldDefend,
                generation,
                occupied,
                slots,
                backpack);
        }

        private static void
            AssertMoreEquipmentSlotsCommittedShieldUnchanged(
                MoreEquipmentSlotsCommittedShieldBaseline baseline,
                string scenario)
        {
            object committed =
                ReadReflectedMember(
                    baseline.ProductRuntime,
                    "document") ??
                throw new InvalidOperationException(
                    "Committed document disappeared after " +
                    scenario +
                    ".");
            object[] shields =
                FindEquipmentSlotsByItemId(
                    ReadReflectedMember(
                        committed,
                        "Slots"),
                    MoreEquipmentSlotsShieldItemId);
            if (Convert.ToInt64(
                    ReadReflectedMember(
                        committed,
                        "Generation") ?? 0L) !=
                    baseline.Generation ||
                !string.Equals(
                    DescribeEquipmentSlots(
                        ReadReflectedMember(
                            committed,
                            "Slots")),
                    baseline.Slots,
                    StringComparison.Ordinal) ||
                shields.Length != 1 ||
                Convert.ToInt32(
                    ReadReflectedMember(
                        shields[0],
                        "ShieldValue") ?? 0) !=
                    baseline.ShieldValue ||
                ReadReflectedMember(
                    committed,
                    "Journal") != null ||
                ReadReflectedMember(
                    committed,
                    "GameplayCandidate") != null)
            {
                throw new InvalidOperationException(
                    "No-save " +
                    scenario +
                    " crossed the Working/Committed boundary.");
            }
        }

        private static string
            BuildMoreEquipmentSlotsCommittedShieldSummary(
                MoreEquipmentSlotsCommittedShieldBaseline baseline,
                string operation,
                string operationDetails) =>
            "route=ProductNative" +
            "; saveMode=NoNativeSave" +
            "; operation=" +
            operation +
            "; " +
            operationDetails +
            "; committedShieldValue=" +
            baseline.ShieldValue +
            "; committedShieldItems=1" +
            "; nativeShieldItems=0" +
            "; logicalCommittedShieldItems=1" +
            "; cleanupBeforeTitle=false" +
            "; backpackBaseline=" +
            baseline.BackpackBaseline +
            "; committedGeneration=" +
            baseline.Generation +
            "; committedOccupied=" +
            baseline.Occupied +
            "; committedSlots={" +
            baseline.Slots +
            "}" +
            "; committedSlotsBase64=" +
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    baseline.Slots)) +
            "; workingDirty=true" +
            "; journal=false; candidate=false" +
            "; nativeSaveRequested=false" +
            "; exactOwner=" +
            MoreEquipmentSlotsHarmonyOwner +
            "; patches=" +
            MoreEquipmentSlotsHarmonyTargets.Length +
            "; targets=" +
            MoreEquipmentSlotsHarmonyTargets.Length +
            "/" +
            MoreEquipmentSlotsHarmonyTargets.Length +
            "; callback=1; loaded=1; roots=" +
            baseline.OwnerRoots;

        private MoreEquipmentSlotsQaReadiness
            RequireMoreEquipmentSlotsQaReadiness(string scenario)
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
                    "MoreEquipmentSlots " +
                    scenario +
                    " readiness was incomplete. instance=" +
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

            return new MoreEquipmentSlotsQaReadiness(
                productRuntime,
                ownerRoots);
        }

        private static object[] FindEquipmentSlotsByItemId(
            object? slots,
            string itemId)
        {
            if (!(slots is IEnumerable entries))
                return Array.Empty<object>();

            return entries
                .Cast<object?>()
                .Where(entry =>
                    entry != null &&
                    string.Equals(
                        ReadReflectedMember(
                            entry,
                            "ItemId") as string,
                        itemId,
                        StringComparison.Ordinal))
                .Cast<object>()
                .ToArray();
        }

        private sealed class MoreEquipmentSlotsQaReadiness
        {
            internal MoreEquipmentSlotsQaReadiness(
                object productRuntime,
                int ownerRoots)
            {
                ProductRuntime =
                    productRuntime ??
                    throw new ArgumentNullException(
                        nameof(productRuntime));
                OwnerRoots = ownerRoots;
            }

            internal object ProductRuntime { get; }

            internal int OwnerRoots { get; }
        }

        private sealed class
            MoreEquipmentSlotsCommittedShieldBaseline
        {
            internal MoreEquipmentSlotsCommittedShieldBaseline(
                object productRuntime,
                int ownerRoots,
                int targetSlot,
                int shieldValue,
                int shieldDefend,
                long generation,
                int occupied,
                string slots,
                int backpackBaseline)
            {
                ProductRuntime = productRuntime;
                OwnerRoots = ownerRoots;
                TargetSlot = targetSlot;
                ShieldValue = shieldValue;
                ShieldDefend = shieldDefend;
                Generation = generation;
                Occupied = occupied;
                Slots = slots;
                BackpackBaseline = backpackBaseline;
            }

            internal object ProductRuntime { get; }

            internal int OwnerRoots { get; }

            internal int TargetSlot { get; }

            internal int ShieldValue { get; }

            internal int ShieldDefend { get; }

            internal long Generation { get; }

            internal int Occupied { get; }

            internal string Slots { get; }

            internal int BackpackBaseline { get; }
        }
    }
}
