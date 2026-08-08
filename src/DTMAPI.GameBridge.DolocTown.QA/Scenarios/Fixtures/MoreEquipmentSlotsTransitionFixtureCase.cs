using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const string MoreEquipmentSlotsTransitionOwnerId =
            "DTMAPI.MoreEquipmentSlotsMod";
        private const long MoreEquipmentSlotsProductScopeClockToleranceSeconds =
            300;
        private string moreEquipmentSlotsTransitionPhase = "None";
        private int moreEquipmentSlotsTransitionStage;
        private int moreEquipmentSlotsTransitionSaveSavedCount;
        private int moreEquipmentSlotsTransitionSaveSavedBaseline;
        private bool moreEquipmentSlotsTransitionSleepEnded;
        private bool moreEquipmentSlotsTransitionSleepReadyLogged;
        private bool moreEquipmentSlotsTransitionNormalStateWaitLogged;
        private MoreEquipmentSlotsCommittedShieldBaseline?
            moreEquipmentSlotsMigratedSaveBaseline;
        private int moreEquipmentSlotsMigratedSaveShieldValue;

        private void ConfigureMoreEquipmentSlotsTransitionForFixture(
            string phase)
        {
            moreEquipmentSlotsTransitionPhase =
                (phase ?? "None").Trim();
            moreEquipmentSlotsTransitionNormalStateWaitLogged = false;
        }

        private void NotifyMoreEquipmentSlotsTransitionSaveSavedForFixture()
        {
            moreEquipmentSlotsTransitionSaveSavedCount++;
        }

        private FixtureAttemptResult
            TryExerciseMoreEquipmentSlotsTransitionForFixture()
        {
            try
            {
                Type dolocApi = ResolveMoreEquipmentSlotsDolocApiType();
                if (!TryGetStaticBoolProperty(
                        dolocApi,
                        "IsNormalState"))
                {
                    if (!moreEquipmentSlotsTransitionNormalStateWaitLogged)
                    {
                        moreEquipmentSlotsTransitionNormalStateWaitLogged =
                            true;
                        runtime.SetHookStatus(
                            "Smoke.MoreEquipmentSlotsTransition",
                            "pending",
                            "NormalGameState before transition mutation",
                            "phase=" +
                            moreEquipmentSlotsTransitionPhase +
                            "; waiting for the native load transition to reach NormalGameState.");
                    }
                    return FixtureAttemptResult.Pending;
                }

                if (moreEquipmentSlotsTransitionPhase.Equals(
                        "U4",
                        StringComparison.Ordinal))
                {
                    VerifyMoreEquipmentSlotsTransitionTerminal(
                        requireOldConsumer: false,
                        expectedBackpackShield: 1,
                        expectedMailShield: 0,
                        expectedSidecarShield: 0,
                        expectedBackpackButton: 0,
                        expectedMailButton: 1,
                        expectedSidecarButton: 0);
                    PublishMoreEquipmentSlotsTransitionVerified(
                        "U4",
                        "NoNativeSave cold restart retained exactly one recovered backpack shield and one unaccepted item mail; terminal sidecar remained empty and no native save was requested.");
                    return FixtureAttemptResult.Succeeded;
                }
                if (moreEquipmentSlotsTransitionPhase.Equals(
                        "ColdObserve",
                        StringComparison.Ordinal))
                {
                    VerifyMoreEquipmentSlotsProductColdRecoveryTerminal(
                        dolocApi,
                        "ColdObserve");
                    PublishMoreEquipmentSlotsTransitionVerified(
                        "ColdObserve",
                        "A second cold NoNativeSave process retained exactly one recovered grandmas_button in the native backpack, observed an empty terminal Product-v3 sidecar, and found no recovery session to replay.");
                    return FixtureAttemptResult.Succeeded;
                }

                switch (moreEquipmentSlotsTransitionStage)
                {
                    case 0:
                        PrepareMoreEquipmentSlotsTransitionMutation();
                        if (!TryPublishMoreEquipmentSlotsNativeSleepMenuReady())
                        {
                            throw new InvalidOperationException(
                                "DolocAPI.ShowSleepMenu(Action) did not synchronously enter the native SleepUiState; the modal would pause later QA updates.");
                        }
                        moreEquipmentSlotsTransitionStage = 2;
                        return FixtureAttemptResult.Pending;
                    case 1:
                        if (!TryPublishMoreEquipmentSlotsNativeSleepMenuReady())
                            return FixtureAttemptResult.Pending;
                        moreEquipmentSlotsTransitionStage = 2;
                        return FixtureAttemptResult.Pending;
                    case 2:
                        if (!moreEquipmentSlotsTransitionSleepEnded ||
                            moreEquipmentSlotsTransitionSaveSavedCount <=
                                moreEquipmentSlotsTransitionSaveSavedBaseline)
                        {
                            return FixtureAttemptResult.Pending;
                        }
                        VerifyMoreEquipmentSlotsTransitionAfterSave();
                        moreEquipmentSlotsTransitionStage = 3;
                        return FixtureAttemptResult.Succeeded;
                    default:
                        return FixtureAttemptResult.Succeeded;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "MoreEquipmentSlots transition acceptance failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.MoreEquipmentSlotsTransition",
                    "failed",
                    "frozen compatibility, cold recovery, and migrated-save transaction",
                    "phase=" +
                    moreEquipmentSlotsTransitionPhase +
                    "; " +
                    ex.GetType().Name +
                    ": " +
                    ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private void PrepareMoreEquipmentSlotsTransitionMutation()
        {
            Type dolocApi = ResolveMoreEquipmentSlotsDolocApiType();

            if (moreEquipmentSlotsTransitionPhase.Equals(
                    "MigratedSave",
                    StringComparison.Ordinal))
            {
                AssertMoreEquipmentSlotsProductAssemblyLoaded();
                AssertMoreEquipmentSlotsOldConsumerAbsent();
                MoreEquipmentSlotsCommittedShieldBaseline baseline =
                    RequireMoreEquipmentSlotsCommittedShieldBaseline(
                        "migrated-save normal-save commit");
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
                        "The migrated ProductNative shield did not receive one non-breaking Working-state hit.");
                }
                AssertMoreEquipmentSlotsCommittedShieldUnchanged(
                    baseline,
                    "migrated normal-save preparation");
                VerifyMoreEquipmentSlotsProductExactlyOnce(
                    dolocApi,
                    baseline.ProductRuntime,
                    baseline.ShieldValue);
                moreEquipmentSlotsMigratedSaveBaseline = baseline;
                moreEquipmentSlotsMigratedSaveShieldValue =
                    damagedShieldValue;
            }
            else if (moreEquipmentSlotsTransitionPhase.Equals(
                         "ColdPrepare",
                         StringComparison.Ordinal))
            {
                AssertMoreEquipmentSlotsProductAssemblyAbsent();
                AssertMoreEquipmentSlotsOldConsumerAbsent();
                AssertMoreEquipmentSlotsProductColdSeedAbsent();
                VerifyMoreEquipmentSlotsTargetMailEmpty(
                    dolocApi,
                    context: "ColdPrepare precondition");
                PrepareMoreEquipmentSlotsOneFreeBackpackSlot(dolocApi);
            }
            else if (moreEquipmentSlotsTransitionPhase.Equals(
                         "ColdCommit",
                         StringComparison.Ordinal))
            {
                AssertMoreEquipmentSlotsProductAssemblyAbsent();
                AssertMoreEquipmentSlotsOldConsumerAbsent();
                VerifyMoreEquipmentSlotsProductColdRecoveryWorking(
                    dolocApi);
            }
            else if (moreEquipmentSlotsTransitionPhase.Equals(
                    "Prepare",
                    StringComparison.Ordinal))
            {
                AssertMoreEquipmentSlotsProductAssemblyAbsent();
                if (CountPendingMoreEquipmentSlotsMail(
                        dolocApi,
                        MoreEquipmentSlotsTransactionItemId) != 0 ||
                    CountPendingMoreEquipmentSlotsMail(
                        dolocApi,
                        MoreEquipmentSlotsShieldItemId) != 0)
                {
                    throw new InvalidOperationException(
                        "The disposable fixture already contained target item mail.");
                }
                PrepareMoreEquipmentSlotsOneFreeBackpackSlot(dolocApi);
            }
            else if (moreEquipmentSlotsTransitionPhase.Equals(
                         "U1",
                         StringComparison.Ordinal))
            {
                AssertMoreEquipmentSlotsProductAssemblyAbsent();
                AssertMoreEquipmentSlotsOldConsumerLoaded();
                string state =
                    ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                        "GetEquipmentSlotsStateSummaryForFixture",
                        MoreEquipmentSlotsTransitionOwnerId);
                RequireMoreEquipmentSlotsU1State(state);
            }
            else if (moreEquipmentSlotsTransitionPhase.Equals(
                         "U3Backpack",
                         StringComparison.Ordinal))
            {
                AssertMoreEquipmentSlotsProductAssemblyAbsent();
                AssertMoreEquipmentSlotsOldConsumerAbsent();
                VerifyMoreEquipmentSlotsRecoveryWorkingState(
                    dolocApi,
                    expectedBackpackShield: 1,
                    expectedBackpackButton: 0,
                    expectedMailButton: 0,
                    requiredSidecarItemId:
                        MoreEquipmentSlotsTransactionItemId);
            }
            else if (moreEquipmentSlotsTransitionPhase.Equals(
                         "U3Mail",
                         StringComparison.Ordinal))
            {
                AssertMoreEquipmentSlotsProductAssemblyAbsent();
                AssertMoreEquipmentSlotsOldConsumerAbsent();
                VerifyMoreEquipmentSlotsRecoveryWorkingState(
                    dolocApi,
                    expectedBackpackShield: 1,
                    expectedBackpackButton: 0,
                    expectedMailButton: 1,
                    requiredSidecarItemId: string.Empty);
            }
            else
            {
                throw new InvalidOperationException(
                    "Unsupported MoreEquipmentSlots transition phase " +
                    moreEquipmentSlotsTransitionPhase +
                    ".");
            }

            moreEquipmentSlotsTransitionSaveSavedBaseline =
                moreEquipmentSlotsTransitionSaveSavedCount;
            moreEquipmentSlotsTransitionSleepEnded = false;
            moreEquipmentSlotsTransitionSleepReadyLogged = false;

            MethodInfo? showSleepMenu =
                dolocApi.GetMethod(
                    "ShowSleepMenu",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    binder: null,
                    types: new[]
                    {
                        typeof(Action)
                    },
                    modifiers: null);
            if (showSleepMenu == null)
            {
                throw new MissingMethodException(
                    "DolocAPI.ShowSleepMenu(Action) was unavailable.");
            }
            showSleepMenu.Invoke(
                null,
                new object[]
                {
                    new Action(
                        () =>
                        {
                            moreEquipmentSlotsTransitionSleepEnded =
                                true;
                        })
                });
            runtime.SetHookStatus(
                "Smoke.MoreEquipmentSlotsTransition",
                "pending",
                "native SleepUiState first option and SaveSaved",
                "phase=" +
                moreEquipmentSlotsTransitionPhase +
                "; waiting for the native sleep menu.");
        }

        private bool
            TryPublishMoreEquipmentSlotsNativeSleepMenuReady()
        {
            object? current = GetCurrentNativeUiStateForFixture();
            if (current == null ||
                !string.Equals(
                    current.GetType().FullName,
                    "DolocTown.SleepUiState",
                    StringComparison.Ordinal))
            {
                return false;
            }
            if (!moreEquipmentSlotsTransitionSleepReadyLogged)
            {
                moreEquipmentSlotsTransitionSleepReadyLogged = true;
                access.Log(
                    "Smoke MoreEquipmentSlotsTransition native sleep menu ready phase=" +
                    moreEquipmentSlotsTransitionPhase +
                    "; selectedOption=0; inputOwner=runner-real-Enter" +
                    ".");
            }
            return true;
        }

        private void VerifyMoreEquipmentSlotsTransitionAfterSave()
        {
            if (moreEquipmentSlotsTransitionSaveSavedCount !=
                moreEquipmentSlotsTransitionSaveSavedBaseline + 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one native SaveSaved notification. baseline=" +
                    moreEquipmentSlotsTransitionSaveSavedBaseline +
                    "; current=" +
                    moreEquipmentSlotsTransitionSaveSavedCount +
                    ".");
            }

            if (moreEquipmentSlotsTransitionPhase.Equals(
                    "Prepare",
                    StringComparison.Ordinal))
            {
                Type dolocApi = ResolveMoreEquipmentSlotsDolocApiType();
                object inventory =
                    ResolveStrongPlantingGunBackpackInventory(
                        dolocApi);
                int empty = CountEmptyInventorySlots(inventory);
                if (empty != 1)
                {
                    throw new InvalidOperationException(
                        "Prepared disposable backpack did not retain exactly one free slot after native save. empty=" +
                        empty +
                        ".");
                }
                PublishMoreEquipmentSlotsTransitionVerified(
                    "Prepare",
                    "Disposable third-save fixture committed exactly one free backpack slot through native SleepUiState.");
                return;
            }

            if (moreEquipmentSlotsTransitionPhase.Equals(
                    "ColdPrepare",
                    StringComparison.Ordinal))
            {
                Type dolocApi = ResolveMoreEquipmentSlotsDolocApiType();
                object inventory =
                    ResolveStrongPlantingGunBackpackInventory(
                        dolocApi);
                if (CountEmptyInventorySlots(inventory) != 1)
                {
                    throw new InvalidOperationException(
                        "ColdPrepare did not retain exactly one free native backpack slot after SaveSaved.");
                }
                VerifyMoreEquipmentSlotsTargetNativeCounts(
                    dolocApi,
                    expectedBackpackButton: 0,
                    expectedMailButton: 0,
                    context: "ColdPrepare post-save");
                StageMoreEquipmentSlotsProductColdSeed(
                    dolocApi);
                PublishMoreEquipmentSlotsTransitionVerified(
                    "ColdPrepare",
                    "A real native SleepUiState save committed the one-free-slot fixture before an exact-scope Product-v3 sidecar with one grandmas_button was staged for the next cold process.");
                return;
            }

            if (moreEquipmentSlotsTransitionPhase.Equals(
                    "ColdCommit",
                    StringComparison.Ordinal))
            {
                VerifyMoreEquipmentSlotsProductColdRecoveryTerminal(
                    ResolveMoreEquipmentSlotsDolocApiType(),
                    "ColdCommit");
                PublishMoreEquipmentSlotsTransitionVerified(
                    "ColdCommit",
                    "The independent cold process recovered exactly one grandmas_button into the sole native backpack slot and a real SleepUiState SaveSaved finalized the Product-v3 journal.");
                return;
            }

            if (moreEquipmentSlotsTransitionPhase.Equals(
                    "U1",
                    StringComparison.Ordinal))
            {
                VerifyMoreEquipmentSlotsTransitionTerminal(
                    requireOldConsumer: true,
                    expectedBackpackShield: 0,
                    expectedMailShield: 0,
                    expectedSidecarShield: 1,
                    expectedBackpackButton: 0,
                    expectedMailButton: 0,
                    expectedSidecarButton: 1);
                string state =
                    ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                        "GetEquipmentSlotsStateSummaryForFixture",
                        MoreEquipmentSlotsTransitionOwnerId);
                RequireMoreEquipmentSlotsU1State(state);
                PublishMoreEquipmentSlotsTransitionVerified(
                    "U1",
                    "Retained 0.3.1 froze IEquipmentSlotsApi registration, three-slot UI, two applied items, partially consumed shield 50/80, scoped flat schema 3, and one normal native sleep save.");
                return;
            }

            if (moreEquipmentSlotsTransitionPhase.Equals(
                    "MigratedSave",
                    StringComparison.Ordinal))
            {
                VerifyMoreEquipmentSlotsMigratedSaveAfterSave();
                return;
            }

            if (moreEquipmentSlotsTransitionPhase.Equals(
                    "U3Backpack",
                    StringComparison.Ordinal))
            {
                VerifyMoreEquipmentSlotsTransitionTerminal(
                    requireOldConsumer: false,
                    expectedBackpackShield: 1,
                    expectedMailShield: 0,
                    expectedSidecarShield: 0,
                    expectedBackpackButton: 0,
                    expectedMailButton: 0,
                    expectedSidecarButton: 1);
                PublishMoreEquipmentSlotsTransitionVerified(
                    "U3Backpack",
                    "Cold Compatibility Host committed the tail-first box_hat to the sole free native backpack slot; one grandmas_button remains in terminal scoped storage.");
                return;
            }

            VerifyMoreEquipmentSlotsTransitionTerminal(
                requireOldConsumer: false,
                expectedBackpackShield: 1,
                expectedMailShield: 0,
                expectedSidecarShield: 0,
                expectedBackpackButton: 0,
                expectedMailButton: 1,
                expectedSidecarButton: 0);
            PublishMoreEquipmentSlotsTransitionVerified(
                "U3Mail",
                "Cold Compatibility Host committed grandmas_button to one unaccepted native item mail after the backpack became full; scoped storage reached the empty terminal.");
        }

        private void PublishMoreEquipmentSlotsTransitionVerified(
            string phase,
            string details)
        {
            string lifecycle =
                ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                    "GetEquipmentSlotsLifecycleSummary");
            string boundary =
                phase.Equals(
                    "MigratedSave",
                    StringComparison.Ordinal)
                    ? "migrated ProductNative save-commit transaction"
                    : phase.StartsWith(
                        "Cold",
                        StringComparison.Ordinal)
                        ? "Product-v3 cold-recovery native commit and replay boundary"
                        : "frozen 0.3.1 compatibility and cold-recovery native sleep transaction";
            runtime.SetHookStatus(
                "Smoke.MoreEquipmentSlotsTransition",
                "verified",
                boundary,
                "phase=" +
                phase +
                "; " +
                details +
                "; lifecycle={" +
                lifecycle +
                "}; ProductNativeLoaded=" +
                (phase.Equals(
                    "MigratedSave",
                    StringComparison.Ordinal)
                    ? "true"
                    : "false") +
                ".");
            access.Log(
                "Smoke exercise MoreEquipmentSlotsTransition OK phase=" +
                phase +
                "; " +
                details +
                "; lifecycle={" +
                lifecycle +
                "}.");
        }

        private void VerifyMoreEquipmentSlotsMigratedSaveAfterSave()
        {
            MoreEquipmentSlotsCommittedShieldBaseline baseline =
                moreEquipmentSlotsMigratedSaveBaseline ??
                throw new InvalidOperationException(
                    "The migrated-save baseline was unavailable after SaveSaved.");
            object committed =
                ReadReflectedMember(
                    baseline.ProductRuntime,
                    "document") ??
                throw new InvalidOperationException(
                    "The migrated ProductNative committed document disappeared after SaveSaved.");
            object[] shields =
                FindEquipmentSlotsByItemId(
                    ReadReflectedMember(
                        committed,
                        "Slots"),
                    MoreEquipmentSlotsShieldItemId);
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
            if (generation <= baseline.Generation ||
                occupied != 2 ||
                shields.Length != 1 ||
                Convert.ToInt32(
                    ReadReflectedMember(
                        shields[0],
                        "ShieldValue") ?? 0) !=
                    moreEquipmentSlotsMigratedSaveShieldValue ||
                ReadReflectedMember(
                    committed,
                    "Journal") != null ||
                ReadReflectedMember(
                    committed,
                    "GameplayCandidate") != null)
            {
                throw new InvalidOperationException(
                    "The normal native save did not commit exactly the migrated two-item ProductNative state. generation=" +
                    baseline.Generation +
                    "->" +
                    generation +
                    "; occupied=" +
                    occupied +
                    "; shield=" +
                    moreEquipmentSlotsMigratedSaveShieldValue +
                    ".");
            }

            VerifyMoreEquipmentSlotsProductExactlyOnce(
                ResolveMoreEquipmentSlotsDolocApiType(),
                baseline.ProductRuntime,
                moreEquipmentSlotsMigratedSaveShieldValue);
            PublishMoreEquipmentSlotsTransitionVerified(
                "MigratedSave",
                "Migrated Host flat schema 3 state committed one real shield hit through native SleepUiState; shield=" +
                baseline.ShieldValue +
                "->" +
                moreEquipmentSlotsMigratedSaveShieldValue +
                "; backpackBaseline=" +
                baseline.BackpackBaseline +
                "; committedGeneration=" +
                generation +
                "; committedOccupied=" +
                occupied +
                "; committedSlots={" +
                slots +
                "}; committedSlotsBase64=" +
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        slots)) +
                "; journal=false; candidate=false; perItemExactlyOnce=true");
        }

        private static void VerifyMoreEquipmentSlotsProductExactlyOnce(
            Type dolocApi,
            object productRuntime,
            int expectedShieldValue)
        {
            object committed =
                ReadReflectedMember(
                    productRuntime,
                    "document") ??
                throw new InvalidOperationException(
                    "The ProductNative committed document was unavailable.");
            object? slots =
                ReadReflectedMember(
                    committed,
                    "Slots");
            object[] shields =
                FindEquipmentSlotsByItemId(
                    slots,
                    MoreEquipmentSlotsShieldItemId);
            object[] buttons =
                FindEquipmentSlotsByItemId(
                    slots,
                    MoreEquipmentSlotsTransactionItemId);
            int backpackShield =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            int backpackButton =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId,
                    checkBox: false);
            int mailShield =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId);
            int mailButton =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId);
            if (shields.Length != 1 ||
                buttons.Length != 1 ||
                backpackShield != 0 ||
                backpackButton != 0 ||
                mailShield != 0 ||
                mailButton != 0 ||
                Convert.ToInt32(
                    ReadReflectedMember(
                        shields[0],
                        "ShieldValue") ?? 0) !=
                    expectedShieldValue ||
                shields.Length +
                    backpackShield +
                    mailShield != 1 ||
                buttons.Length +
                    backpackButton +
                    mailButton != 1)
            {
                throw new InvalidOperationException(
                    "Migrated ProductNative per-item conservation failed. shieldSidecar=" +
                    shields.Length +
                    "; shieldBackpack=" +
                    backpackShield +
                    "; shieldMail=" +
                    mailShield +
                    "; buttonSidecar=" +
                    buttons.Length +
                    "; buttonBackpack=" +
                    backpackButton +
                    "; buttonMail=" +
                    mailButton +
                    "; expectedShieldValue=" +
                    expectedShieldValue +
                    ".");
            }
        }

        private void AssertMoreEquipmentSlotsProductColdSeedAbsent()
        {
            string path = GetMoreEquipmentSlotsProductColdPath();
            if (File.Exists(path) ||
                File.Exists(path + ".previous"))
            {
                throw new InvalidOperationException(
                    "ColdPrepare requires both Product-v3 live and previous sidecars to be absent. path=" +
                    path +
                    ".");
            }
        }

        private void StageMoreEquipmentSlotsProductColdSeed(
            Type dolocApi)
        {
            AssertMoreEquipmentSlotsProductColdSeedAbsent();
            MoreEquipmentSlotsProductScopeProbe scope =
                ReadMoreEquipmentSlotsProductNativeScope(
                    dolocApi);
            var document =
                new MoreEquipmentSlotsProductDocumentProbe
                {
                    SchemaVersion = 3,
                    Scope = scope,
                    Generation = 1,
                    Slots =
                        new List<MoreEquipmentSlotsProductSlotProbe>
                        {
                            new MoreEquipmentSlotsProductSlotProbe
                            {
                                Index = 0,
                                ItemId =
                                    MoreEquipmentSlotsTransactionItemId,
                                DisplayName = "Grandma's Button"
                            },
                            new MoreEquipmentSlotsProductSlotProbe
                            {
                                Index = 1
                            },
                            new MoreEquipmentSlotsProductSlotProbe
                            {
                                Index = 2
                            }
                        }
                };
            string path = GetMoreEquipmentSlotsProductColdPath();
            string directory =
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Product-v3 cold seed had no parent directory.");
            Directory.CreateDirectory(directory);
            string temporaryPath =
                path + ".qa-seed-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    CreateMoreEquipmentSlotsProductSerializer()
                        .WriteObject(
                            stream,
                            document);
                    stream.Flush(flushToDisk: true);
                }

                MoreEquipmentSlotsProductDocumentProbe roundTrip =
                    ReadMoreEquipmentSlotsProductColdDocument(
                        temporaryPath);
                VerifyMoreEquipmentSlotsProductColdSeed(
                    roundTrip,
                    scope,
                    "temporary round trip");
                if (File.Exists(path) ||
                    File.Exists(path + ".previous"))
                {
                    throw new InvalidOperationException(
                        "Product-v3 cold seed authority appeared before publication; refusing to overwrite it.");
                }
                File.Move(
                    temporaryPath,
                    path);
                VerifyMoreEquipmentSlotsProductColdSeed(
                    ReadMoreEquipmentSlotsProductColdDocument(
                        path),
                    scope,
                    "published live document");
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private void VerifyMoreEquipmentSlotsProductColdRecoveryWorking(
            Type dolocApi)
        {
            VerifyMoreEquipmentSlotsTargetNativeCounts(
                dolocApi,
                expectedBackpackButton: 1,
                expectedMailButton: 0,
                context: "ColdCommit working state");
            MoreEquipmentSlotsProductScopeProbe scope =
                ReadMoreEquipmentSlotsProductNativeScope(
                    dolocApi);
            MoreEquipmentSlotsProductDocumentProbe document =
                ReadMoreEquipmentSlotsProductColdDocument(
                    GetMoreEquipmentSlotsProductColdPath());
            VerifyMoreEquipmentSlotsProductDocumentScope(
                document,
                scope,
                "ColdCommit working state");
            if (!AreMoreEquipmentSlotsProductSlotsEmpty(
                    document.Slots) ||
                document.GameplayCandidate != null ||
                document.Journal == null ||
                !AreMoreEquipmentSlotsProductScopesEqual(
                    document.Journal.Scope,
                    document.Scope) ||
                document.Journal.State != 0 ||
                !document.Journal.AttemptStarted ||
                document.Journal.Escrow == null ||
                document.Journal.Escrow.Count != 1 ||
                !string.Equals(
                    document.Journal.Escrow[0].ItemId,
                    MoreEquipmentSlotsTransactionItemId,
                    StringComparison.Ordinal) ||
                !document.Journal.Escrow[0].AttemptCompleted ||
                document.Journal.Escrow[0].Placement != 1)
            {
                throw new InvalidOperationException(
                    "ColdCommit did not observe one completed backpack placement quarantined by a prepared Product-v3 journal.");
            }
            RequireMoreEquipmentSlotsProductColdSessionCount(
                expected: 1,
                context: "ColdCommit working state");
        }

        private void VerifyMoreEquipmentSlotsProductColdRecoveryTerminal(
            Type dolocApi,
            string phase)
        {
            AssertMoreEquipmentSlotsProductAssemblyAbsent();
            AssertMoreEquipmentSlotsOldConsumerAbsent();
            VerifyMoreEquipmentSlotsTargetNativeCounts(
                dolocApi,
                expectedBackpackButton: 1,
                expectedMailButton: 0,
                context: phase + " terminal state");
            MoreEquipmentSlotsProductScopeProbe scope =
                ReadMoreEquipmentSlotsProductNativeScope(
                    dolocApi);
            MoreEquipmentSlotsProductDocumentProbe document =
                ReadMoreEquipmentSlotsProductColdDocument(
                    GetMoreEquipmentSlotsProductColdPath());
            VerifyMoreEquipmentSlotsProductDocumentScope(
                document,
                scope,
                phase + " terminal state");
            if (!AreMoreEquipmentSlotsProductSlotsEmpty(
                    document.Slots) ||
                document.Journal != null ||
                document.GameplayCandidate != null)
            {
                throw new InvalidOperationException(
                    phase +
                    " did not observe an empty terminal Product-v3 document.");
            }
            RequireMoreEquipmentSlotsProductColdSessionCount(
                expected: 0,
                context: phase + " terminal state");
        }

        private static void VerifyMoreEquipmentSlotsTargetNativeCounts(
            Type dolocApi,
            int expectedBackpackButton,
            int expectedMailButton,
            string context)
        {
            int backpackButton =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId,
                    checkBox: false);
            int mailButton =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId);
            int backpackShield =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            int mailShield =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId);
            if (backpackButton != expectedBackpackButton ||
                mailButton != expectedMailButton ||
                backpackShield != 0 ||
                mailShield != 0 ||
                (backpackButton + mailButton != 1 &&
                 expectedBackpackButton + expectedMailButton == 1))
            {
                throw new InvalidOperationException(
                    context +
                    " native item counts were unexpected. buttonBackpack=" +
                    backpackButton +
                    "/" +
                    expectedBackpackButton +
                    "; buttonMail=" +
                    mailButton +
                    "/" +
                    expectedMailButton +
                    "; shieldBackpack=" +
                    backpackShield +
                    "; shieldMail=" +
                    mailShield +
                    ".");
            }
        }

        private static void VerifyMoreEquipmentSlotsTargetMailEmpty(
            Type dolocApi,
            string context)
        {
            int mailButton =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId);
            int mailShield =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId);
            if (mailButton != 0 || mailShield != 0)
            {
                throw new InvalidOperationException(
                    context +
                    " target-item mail was not empty. buttonMail=" +
                    mailButton +
                    "; shieldMail=" +
                    mailShield +
                    ".");
            }
        }

        private void RequireMoreEquipmentSlotsProductColdSessionCount(
            int expected,
            string context)
        {
            string lifecycle =
                ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                    "GetEquipmentSlotsLifecycleSummary");
            string required =
                "equipmentProductColdRecoverySessions=" + expected;
            if (lifecycle.IndexOf(
                    required,
                    StringComparison.Ordinal) < 0 ||
                lifecycle.IndexOf(
                    "equipmentJournals=0",
                    StringComparison.Ordinal) < 0 ||
                lifecycle.IndexOf(
                    "equipmentGameplayCandidates=0",
                    StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    context +
                    " lifecycle counts were not terminal outside the exact Product cold session. required=" +
                    required +
                    "; lifecycle={" +
                    lifecycle +
                    "}.");
            }
        }

        private static bool AreMoreEquipmentSlotsProductSlotsEmpty(
            IList<MoreEquipmentSlotsProductSlotProbe>? slots)
        {
            if (slots == null || slots.Count != 3)
                return false;
            for (int index = 0; index < slots.Count; index++)
            {
                MoreEquipmentSlotsProductSlotProbe? slot = slots[index];
                if (slot == null ||
                    slot.Index != index ||
                    !string.IsNullOrEmpty(slot.ItemId) ||
                    !string.IsNullOrEmpty(slot.DisplayName) ||
                    !string.IsNullOrEmpty(slot.SkillId) ||
                    slot.DefenseBonus != 0 ||
                    slot.IsShield ||
                    slot.ShieldValue != 0 ||
                    slot.ShieldMaxValue != 0 ||
                    slot.ShieldDefend != 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static void VerifyMoreEquipmentSlotsProductColdSeed(
            MoreEquipmentSlotsProductDocumentProbe document,
            MoreEquipmentSlotsProductScopeProbe expectedScope,
            string context)
        {
            VerifyMoreEquipmentSlotsProductDocumentScope(
                document,
                expectedScope,
                context);
            if (document.Generation != 1 ||
                document.Journal != null ||
                document.GameplayCandidate != null ||
                document.Slots == null ||
                document.Slots.Count != 3 ||
                document.Slots[0] == null ||
                document.Slots[0].Index != 0 ||
                !string.Equals(
                    document.Slots[0].ItemId,
                    MoreEquipmentSlotsTransactionItemId,
                    StringComparison.Ordinal) ||
                document.Slots[0].IsShield ||
                document.Slots[1] == null ||
                document.Slots[1].Index != 1 ||
                !string.IsNullOrEmpty(
                    document.Slots[1].ItemId) ||
                document.Slots[2] == null ||
                document.Slots[2].Index != 2 ||
                !string.IsNullOrEmpty(
                    document.Slots[2].ItemId))
            {
                throw new InvalidOperationException(
                    "Product-v3 cold seed failed exact " +
                    context +
                    " verification.");
            }
        }

        private static void VerifyMoreEquipmentSlotsProductDocumentScope(
            MoreEquipmentSlotsProductDocumentProbe document,
            MoreEquipmentSlotsProductScopeProbe expectedScope,
            string context)
        {
            if (document == null ||
                document.SchemaVersion != 3 ||
                document.Generation < 0 ||
                document.Scope == null ||
                document.Scope.ArchiveIndex != 2 ||
                document.Scope.ArchiveIndex !=
                    expectedScope.ArchiveIndex ||
                !string.Equals(
                    document.Scope.PlayerName,
                    expectedScope.PlayerName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    document.Scope.CustomPlayerName,
                    expectedScope.CustomPlayerName,
                    StringComparison.Ordinal) ||
                !document.Scope.TotalGameSeconds.HasValue ||
                document.Scope.TotalGameSeconds.Value < 0 ||
                !expectedScope.TotalGameSeconds.HasValue ||
                expectedScope.TotalGameSeconds.Value < 0 ||
                document.Scope.TotalGameSeconds.Value >
                    expectedScope.TotalGameSeconds.Value ||
                expectedScope.TotalGameSeconds.Value -
                    document.Scope.TotalGameSeconds.Value >
                    MoreEquipmentSlotsProductScopeClockToleranceSeconds)
            {
                throw new InvalidOperationException(
                    "Product-v3 document did not match the exact disposable third-save scope during " +
                    context + ". document=" +
                    FormatMoreEquipmentSlotsProductScope(document?.Scope) +
                    "; native=" +
                    FormatMoreEquipmentSlotsProductScope(expectedScope) +
                    ".");
            }
        }

        private static bool AreMoreEquipmentSlotsProductScopesEqual(
            MoreEquipmentSlotsProductScopeProbe? left,
            MoreEquipmentSlotsProductScopeProbe? right)
        {
            return left != null &&
                right != null &&
                left.ArchiveIndex == right.ArchiveIndex &&
                string.Equals(
                    left.PlayerName,
                    right.PlayerName,
                    StringComparison.Ordinal) &&
                string.Equals(
                    left.CustomPlayerName,
                    right.CustomPlayerName,
                    StringComparison.Ordinal) &&
                left.TotalGameSeconds == right.TotalGameSeconds;
        }

        private static string FormatMoreEquipmentSlotsProductScope(
            MoreEquipmentSlotsProductScopeProbe? scope)
        {
            if (scope == null)
                return "missing";
            return "archive=" + scope.ArchiveIndex +
                ",player=" + scope.PlayerName +
                ",custom=" + scope.CustomPlayerName +
                ",clock=" +
                (scope.TotalGameSeconds.HasValue
                    ? scope.TotalGameSeconds.Value.ToString()
                    : "missing");
        }

        private static MoreEquipmentSlotsProductScopeProbe
            ReadMoreEquipmentSlotsProductNativeScope(
                Type dolocApi)
        {
            object archive =
                ReadStaticMember(
                    dolocApi,
                    "archiveHandle") ??
                throw new InvalidOperationException(
                    "Product-v3 cold acceptance could not read archiveHandle.");
            object farmData =
                ReadMember(
                    archive,
                    "farmData") ??
                throw new InvalidOperationException(
                    "Product-v3 cold acceptance could not read archiveHandle.farmData.");
            object agentData =
                ReadMember(
                    farmData,
                    "agentData") ??
                throw new InvalidOperationException(
                    "Product-v3 cold acceptance could not read farmData.agentData.");
            object baseDataOnLoad =
                ReadMember(
                    archive,
                    "baseDataOnLoad") ??
                throw new InvalidOperationException(
                    "Product-v3 cold acceptance could not read archiveHandle.baseDataOnLoad.");
            int archiveIndex =
                ReadIntMember(
                    archive,
                    "archiveIndex",
                    -1);
            string customPlayerName =
                ReadStringMember(
                    agentData,
                    "customPlayerName",
                    string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(customPlayerName))
            {
                customPlayerName =
                    ReadStringMember(
                        baseDataOnLoad,
                        "customPlayerName",
                        string.Empty).Trim();
            }
            string playerName =
                ReadStringMember(
                    agentData,
                    "playerName",
                    string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = customPlayerName;
            object? totalGameSecondsValue =
                ReadMember(
                    baseDataOnLoad,
                    "totalGameSeconds");
            long totalGameSeconds =
                totalGameSecondsValue == null
                    ? -1
                    : Convert.ToInt64(totalGameSecondsValue);
            if (archiveIndex != 2 ||
                totalGameSeconds < 0)
            {
                throw new InvalidOperationException(
                    "Product-v3 cold acceptance requires exact archiveIndex=2 and a non-negative native save clock. archive=" +
                    archiveIndex +
                    "; clock=" +
                    totalGameSeconds +
                    ".");
            }
            return new MoreEquipmentSlotsProductScopeProbe
            {
                ArchiveIndex = archiveIndex,
                PlayerName = playerName,
                CustomPlayerName = customPlayerName,
                TotalGameSeconds = totalGameSeconds
            };
        }

        private string GetMoreEquipmentSlotsProductColdPath()
        {
            return Path.Combine(
                runtime.Paths.ConfigPath,
                "protected-items",
                "equipment-slots",
                "slot-2",
                "equipment-slots-" +
                    MoreEquipmentSlotsTransitionOwnerId +
                    ".json");
        }

        private static MoreEquipmentSlotsProductDocumentProbe
            ReadMoreEquipmentSlotsProductColdDocument(
                string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Product-v3 cold acceptance sidecar was unavailable.",
                    path);
            }
            using (FileStream stream = File.OpenRead(path))
            {
                return CreateMoreEquipmentSlotsProductSerializer()
                           .ReadObject(stream) as
                       MoreEquipmentSlotsProductDocumentProbe ??
                    throw new InvalidDataException(
                        "Product-v3 cold acceptance sidecar deserialized to null.");
            }
        }

        private static DataContractJsonSerializer
            CreateMoreEquipmentSlotsProductSerializer()
        {
            return new DataContractJsonSerializer(
                typeof(MoreEquipmentSlotsProductDocumentProbe),
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
        }

        private void VerifyMoreEquipmentSlotsTransitionTerminal(
            bool requireOldConsumer,
            int expectedBackpackShield,
            int expectedMailShield,
            int expectedSidecarShield,
            int expectedBackpackButton,
            int expectedMailButton,
            int expectedSidecarButton)
        {
            AssertMoreEquipmentSlotsProductAssemblyAbsent();
            if (requireOldConsumer)
                AssertMoreEquipmentSlotsOldConsumerLoaded();
            else
                AssertMoreEquipmentSlotsOldConsumerAbsent();

            Type dolocApi = ResolveMoreEquipmentSlotsDolocApiType();
            int backpackShield =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            int backpackButton =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId,
                    checkBox: false);
            int mailShield =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId);
            int mailButton =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId);
            string sidecarJson =
                ReadMoreEquipmentSlotsScopedFlatDocument();
            int sidecarShield =
                CountMoreEquipmentSlotsSidecarItem(
                    sidecarJson,
                    MoreEquipmentSlotsShieldItemId);
            int sidecarButton =
                CountMoreEquipmentSlotsSidecarItem(
                    sidecarJson,
                    MoreEquipmentSlotsTransactionItemId);
            if (backpackShield != expectedBackpackShield ||
                mailShield != expectedMailShield ||
                sidecarShield != expectedSidecarShield ||
                backpackButton != expectedBackpackButton ||
                mailButton != expectedMailButton ||
                sidecarButton != expectedSidecarButton)
            {
                throw new InvalidOperationException(
                    "Per-item recovery counts did not match the transition phase. shieldBackpack=" +
                    backpackShield +
                    "/" +
                    expectedBackpackShield +
                    "; shieldMail=" +
                    mailShield +
                    "/" +
                    expectedMailShield +
                    "; shieldSidecar=" +
                    sidecarShield +
                    "/" +
                    expectedSidecarShield +
                    "; buttonBackpack=" +
                    backpackButton +
                    "/" +
                    expectedBackpackButton +
                    "; buttonMail=" +
                    mailButton +
                    "/" +
                    expectedMailButton +
                    "; buttonSidecar=" +
                    sidecarButton +
                    "/" +
                    expectedSidecarButton +
                    ".");
            }
            if (backpackShield + mailShield + sidecarShield != 1 ||
                backpackButton + mailButton + sidecarButton != 1)
            {
                throw new InvalidOperationException(
                    "Per-item recovery conservation failed. box_hat=" +
                    (backpackShield + mailShield + sidecarShield) +
                    "; grandmas_button=" +
                    (backpackButton + mailButton + sidecarButton) +
                    "; expected exactly one active copy of each item.");
            }

            string lifecycle =
                ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                    "GetEquipmentSlotsLifecycleSummary");
            if (lifecycle.IndexOf(
                    "equipmentJournals=0",
                    StringComparison.Ordinal) < 0 ||
                lifecycle.IndexOf(
                    "equipmentGameplayCandidates=0",
                    StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    "Compatibility recovery did not reach a terminal transaction ledger. lifecycle={" +
                    lifecycle +
                    "}.");
            }

            VerifyMoreEquipmentSlotsScopedFlatDocument(sidecarJson);
        }

        private void VerifyMoreEquipmentSlotsRecoveryWorkingState(
            Type dolocApi,
            int expectedBackpackShield,
            int expectedBackpackButton,
            int expectedMailButton,
            string requiredSidecarItemId)
        {
            int backpackShield =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false);
            int backpackButton =
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId,
                    checkBox: false);
            int mailButton =
                CountPendingMoreEquipmentSlotsMail(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId);
            string lifecycle =
                ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                    "GetEquipmentSlotsLifecycleSummary");
            if (backpackShield != expectedBackpackShield ||
                backpackButton != expectedBackpackButton ||
                mailButton != expectedMailButton ||
                lifecycle.IndexOf(
                    "equipmentJournals=1",
                    StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    "Cold recovery Working state did not match the expected one-item transaction. shieldBackpack=" +
                    backpackShield +
                    "; buttonBackpack=" +
                    backpackButton +
                    "; buttonMail=" +
                    mailButton +
                    "; lifecycle={" +
                    lifecycle +
                    "}.");
            }

            if (!string.IsNullOrWhiteSpace(requiredSidecarItemId))
            {
                string json =
                    ReadMoreEquipmentSlotsScopedFlatDocument();
                if (json.IndexOf(
                        "\"" +
                        requiredSidecarItemId +
                        "\"",
                        StringComparison.Ordinal) < 0)
                {
                    throw new InvalidOperationException(
                        "Cold recovery Working sidecar did not retain the deferred item " +
                        requiredSidecarItemId +
                        ".");
                }
            }
        }

        private void VerifyMoreEquipmentSlotsScopedFlatDocument(
            string json)
        {
            string globalPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "equipment-slots-" +
                    MoreEquipmentSlotsTransitionOwnerId +
                    ".json");
            if (File.Exists(globalPath))
            {
                throw new InvalidOperationException(
                    "Legacy global sidecar still exists after the native save commit.");
            }

            if (json.IndexOf(
                    "\"schemaVersion\":3",
                    StringComparison.Ordinal) < 0 ||
                json.IndexOf(
                    "\"storageScope\":\"slot-2\"",
                    StringComparison.Ordinal) < 0 ||
                json.IndexOf(
                    "\"journal\":{",
                    StringComparison.Ordinal) >= 0 ||
                json.IndexOf(
                    "\"gameplayCandidate\":{",
                    StringComparison.Ordinal) >= 0)
            {
                throw new InvalidOperationException(
                    "Scoped compatibility sidecar was not terminal flat schema 3.");
            }
        }

        private static int CountMoreEquipmentSlotsSidecarItem(
            string json,
            string itemId)
        {
            return CountOrdinalOccurrences(
                json,
                "\"itemId\":\"" +
                itemId +
                "\"");
        }

        private string ReadMoreEquipmentSlotsScopedFlatDocument()
        {
            string path =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsTransitionOwnerId +
                    ".json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Canonical scoped compatibility sidecar was unavailable.",
                    path);
            }
            return File.ReadAllText(path);
        }

        private static int CountOrdinalOccurrences(
            string text,
            string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(
                       value,
                       index,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private void PrepareMoreEquipmentSlotsOneFreeBackpackSlot(
            Type dolocApi)
        {
            object inventory =
                ResolveStrongPlantingGunBackpackInventory(
                    dolocApi);
            MethodInfo take =
                FindMethod(
                    inventory.GetType(),
                    "Take",
                    1) ??
                throw new MissingMethodException(
                    "LinearInventory.Take(int) was unavailable.");
            MethodInfo place =
                FindMethod(
                    inventory.GetType(),
                    "PlaceItemAt",
                    2) ??
                throw new MissingMethodException(
                    "LinearInventory.PlaceItemAt(int, Item) was unavailable.");
            int capacity =
                ReadIntMember(
                    inventory,
                    "capacity",
                    0);
            if (capacity <= 1)
            {
                throw new InvalidOperationException(
                    "Native backpack capacity was not usable.");
            }

            for (int index = 0; index < capacity; index++)
            {
                object? item =
                    ReadInventoryItemForFixture(
                        inventory,
                        index);
                string itemId =
                    item == null
                        ? string.Empty
                        : ReadStringMember(
                            item,
                            "name",
                            string.Empty);
                if (itemId.Equals(
                        MoreEquipmentSlotsTransactionItemId,
                        StringComparison.Ordinal) ||
                    itemId.Equals(
                        MoreEquipmentSlotsShieldItemId,
                        StringComparison.Ordinal))
                {
                    take.Invoke(
                        inventory,
                        new object[]
                        {
                            index
                        });
                }
            }

            int keepEmpty =
                Enumerable.Range(0, capacity)
                    .FirstOrDefault(
                        index =>
                            ReadInventoryItemForFixture(
                                inventory,
                                index) == null);
            if (ReadInventoryItemForFixture(
                    inventory,
                    keepEmpty) != null)
            {
                throw new InvalidOperationException(
                    "Disposable backpack had no free slot to retain.");
            }
            for (int index = 0; index < capacity; index++)
            {
                if (index == keepEmpty ||
                    ReadInventoryItemForFixture(
                        inventory,
                        index) != null)
                {
                    continue;
                }
                object filler =
                    GenerateItemForFixture(
                        dolocApi,
                        "stone",
                        1) ??
                    throw new InvalidOperationException(
                        "Could not generate disposable stone filler.");
                object? remainder =
                    place.Invoke(
                        inventory,
                        new[]
                        {
                            (object)index,
                            filler
                        });
                if (remainder != null)
                {
                    throw new InvalidOperationException(
                        "Could not fill disposable backpack slot " +
                        index +
                        ".");
                }
            }

            if (CountEmptyInventorySlots(inventory) != 1 ||
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsTransactionItemId,
                    checkBox: false) != 0 ||
                CountNativeItemForFixture(
                    dolocApi,
                    MoreEquipmentSlotsShieldItemId,
                    checkBox: false) != 0)
            {
                throw new InvalidOperationException(
                    "Disposable backpack preparation did not produce exactly one empty slot and zero target items.");
            }
        }

        private static int CountEmptyInventorySlots(object inventory)
        {
            int capacity =
                ReadIntMember(
                    inventory,
                    "capacity",
                    0);
            int empty = 0;
            for (int index = 0; index < capacity; index++)
            {
                if (ReadInventoryItemForFixture(
                        inventory,
                        index) == null)
                {
                    empty++;
                }
            }
            return empty;
        }

        private static int CountPendingMoreEquipmentSlotsMail(
            Type dolocApi,
            string itemId)
        {
            object? archive =
                ReadStaticMember(
                    dolocApi,
                    "archiveHandle");
            return CountPendingMoreEquipmentSlotsMailFromArchiveForFixture(
                archive,
                itemId);
        }

        internal static int
            CountPendingMoreEquipmentSlotsMailFromArchiveForFixture(
                object? archive,
                string itemId)
        {
            if (archive == null)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots pending-mail observation could not read archiveHandle.");
            }

            object? farmData =
                ReadMember(
                    archive,
                    "farmData");
            if (farmData == null)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots pending-mail observation could not read archiveHandle.farmData.");
            }

            object? emailManager =
                ReadMember(
                    farmData,
                    "emailManager");
            if (emailManager == null)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots pending-mail observation could not read archiveHandle.farmData.emailManager.");
            }

            object? emailsValue =
                ReadMember(
                    emailManager,
                    "emails");
            if (!(emailsValue is IEnumerable emails) ||
                emailsValue is string)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots pending-mail observation could not enumerate archiveHandle.farmData.emailManager.emails.");
            }

            int count = 0;
            int emailIndex = 0;
            foreach (object? email in emails)
            {
                string emailPath =
                    "archiveHandle.farmData.emailManager.emails[" +
                    emailIndex +
                    "]";
                if (email == null)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots pending-mail observation found a null entry at " +
                        emailPath +
                        ".");
                }

                object emailIdValue =
                    ReadRequiredMoreEquipmentSlotsMailMember(
                        email,
                        "Id",
                        emailPath + ".Id");
                if (!(emailIdValue is string emailId) ||
                    string.IsNullOrWhiteSpace(emailId))
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots pending-mail observation requires a non-empty string at " +
                        emailPath +
                        ".Id.");
                }

                if (!emailId.Equals(
                        "send_item_template",
                        StringComparison.Ordinal))
                {
                    emailIndex++;
                    continue;
                }

                IEnumerable attachments =
                    ReadRequiredMoreEquipmentSlotsMailEnumerable(
                        email,
                        "emailAttaches",
                        emailPath + ".emailAttaches");
                int attachmentIndex = 0;
                foreach (object? attachment in attachments)
                {
                    string attachmentPath =
                        emailPath +
                        ".emailAttaches[" +
                        attachmentIndex +
                        "]";
                    if (attachment == null)
                    {
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation found a null entry at " +
                            attachmentPath +
                            ".");
                    }

                    if (!string.Equals(
                            attachment.GetType().FullName,
                            "DolocTown.EmailAttachReward",
                            StringComparison.Ordinal))
                    {
                        attachmentIndex++;
                        continue;
                    }

                    object isAcceptValue =
                        ReadRequiredMoreEquipmentSlotsMailMember(
                            attachment,
                            "isAccept",
                            attachmentPath + ".isAccept");
                    if (!(isAcceptValue is bool isAccept))
                    {
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation requires a boolean at " +
                            attachmentPath +
                            ".isAccept.");
                    }

                    if (isAccept)
                    {
                        attachmentIndex++;
                        continue;
                    }

                    object reward =
                        ReadRequiredMoreEquipmentSlotsMailMember(
                            attachment,
                            "reward",
                            attachmentPath + ".reward");
                    if (!string.Equals(
                            reward.GetType().FullName,
                            "DolocTown.RewardItem",
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation requires an unaccepted " +
                            attachmentPath +
                            ".reward to be DolocTown.RewardItem.");
                    }

                    object itemNameValue =
                        ReadRequiredMoreEquipmentSlotsMailMember(
                            reward,
                            "itemName",
                            attachmentPath + ".reward.itemName");
                    if (!(itemNameValue is string rewardItemId) ||
                        string.IsNullOrWhiteSpace(rewardItemId))
                    {
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation requires a non-empty string at " +
                            attachmentPath +
                            ".reward.itemName.");
                    }

                    object itemCountValue =
                        ReadRequiredMoreEquipmentSlotsMailMember(
                            reward,
                            "itemCount",
                            attachmentPath + ".reward.itemCount");
                    if (!(itemCountValue is int rewardItemCount) ||
                        rewardItemCount < 0)
                    {
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation requires a non-negative integer at " +
                            attachmentPath +
                            ".reward.itemCount.");
                    }

                    if (rewardItemId.Equals(
                            itemId,
                            StringComparison.Ordinal))
                    {
                        count =
                            checked(
                                count +
                                rewardItemCount);
                    }

                    attachmentIndex++;
                }

                emailIndex++;
            }
            return count;
        }

        private static object
            ReadRequiredMoreEquipmentSlotsMailMember(
                object instance,
                string memberName,
                string memberPath)
        {
            for (Type? type = instance.GetType();
                 type != null;
                 type = type.BaseType)
            {
                FieldInfo? field =
                    type.GetField(
                        memberName,
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field.GetValue(
                               instance) ??
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation found null at " +
                            memberPath +
                            ".");
                }

                PropertyInfo? property =
                    type.GetProperty(
                        memberName,
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly);
                if (property != null)
                {
                    if (!property.CanRead ||
                        property.GetIndexParameters().Length != 0)
                    {
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation could not read " +
                            memberPath +
                            ".");
                    }

                    return property.GetValue(
                               instance,
                               null) ??
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots pending-mail observation found null at " +
                            memberPath +
                            ".");
                }
            }

            throw new InvalidOperationException(
                "MoreEquipmentSlots pending-mail observation could not read " +
                memberPath +
                ".");
        }

        private static IEnumerable
            ReadRequiredMoreEquipmentSlotsMailEnumerable(
                object instance,
                string memberName,
                string memberPath)
        {
            object value =
                ReadRequiredMoreEquipmentSlotsMailMember(
                    instance,
                    memberName,
                    memberPath);
            if (!(value is IEnumerable enumerable) ||
                value is string)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots pending-mail observation could not enumerate " +
                    memberPath +
                    ".");
            }

            return enumerable;
        }

        private string
            ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                string methodName,
                params object[] args)
        {
            object proxy =
                bridge.EquipmentSlotsService ??
                throw new InvalidOperationException(
                    "Mandatory EquipmentSlots proxy was unavailable.");
            object backend =
                proxy.GetType().GetProperty(
                    "Backend",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)?.GetValue(
                    proxy,
                    null) ??
                throw new InvalidOperationException(
                    "Compatibility EquipmentSlots backend was unavailable.");
            MethodInfo method =
                backend.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic) ??
                throw new MissingMethodException(
                    backend.GetType().FullName,
                    methodName);
            return method.Invoke(
                       backend,
                       args) as string ??
                string.Empty;
        }

        private static void RequireMoreEquipmentSlotsU1State(
            string state)
        {
            string[] required =
            {
                "extra=3",
                "stored=2",
                "applied=2",
                "box_hat:50/80",
                "dtmapi.more_equipment.0=grandmas_button[applied]",
                "dtmapi.more_equipment.1=box_hat[applied]",
                "dtmapi.more_equipment.2=empty"
            };
            string[] missing =
                required.Where(
                        value =>
                            state.IndexOf(
                                value,
                                StringComparison.Ordinal) < 0)
                    .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Frozen 0.3.1 compatibility state was incomplete. missing=" +
                    string.Join(",", missing) +
                    "; state={" +
                    state +
                    "}.");
            }
        }

        private static void
            AssertMoreEquipmentSlotsProductAssemblyAbsent()
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(
                    assembly =>
                        string.Equals(
                            assembly.GetName().Name,
                            "DTMAPI.MoreEquipmentSlots",
                            StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "ProductNative assembly was loaded during a frozen compatibility transition phase.");
            }
        }

        private static void
            AssertMoreEquipmentSlotsProductAssemblyLoaded()
        {
            int count =
                AppDomain.CurrentDomain.GetAssemblies().Count(
                    assembly =>
                        string.Equals(
                            assembly.GetName().Name,
                            "DTMAPI.MoreEquipmentSlots",
                            StringComparison.Ordinal));
            if (count != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one ProductNative MoreEquipmentSlots assembly, actual=" +
                    count +
                    ".");
            }
        }

        private static void AssertMoreEquipmentSlotsOldConsumerLoaded()
        {
            int count =
                AppDomain.CurrentDomain.GetAssemblies().Count(
                    assembly =>
                        string.Equals(
                            assembly.GetName().Name,
                            "MoreEquipmentSlotsMod",
                            StringComparison.Ordinal));
            if (count != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one retained MoreEquipmentSlots 0.3.1 consumer assembly, actual=" +
                    count +
                    ".");
            }
        }

        private static void AssertMoreEquipmentSlotsOldConsumerAbsent()
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(
                    assembly =>
                        string.Equals(
                            assembly.GetName().Name,
                            "MoreEquipmentSlotsMod",
                            StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Retained MoreEquipmentSlots consumer was loaded during product-absent cold recovery.");
            }
        }

        [DataContract]
        private sealed class MoreEquipmentSlotsProductDocumentProbe
        {
            [DataMember(Name = "schemaVersion", Order = 1)]
            public int SchemaVersion { get; set; }

            [DataMember(Name = "scope", Order = 2)]
            public MoreEquipmentSlotsProductScopeProbe Scope
            {
                get;
                set;
            } = new MoreEquipmentSlotsProductScopeProbe();

            [DataMember(Name = "generation", Order = 3)]
            public long Generation { get; set; }

            [DataMember(Name = "slots", Order = 4)]
            public List<MoreEquipmentSlotsProductSlotProbe> Slots
            {
                get;
                set;
            } = new List<MoreEquipmentSlotsProductSlotProbe>();

            [DataMember(
                Name = "journal",
                Order = 5,
                EmitDefaultValue = false)]
            public MoreEquipmentSlotsProductJournalProbe? Journal
            {
                get;
                set;
            }

            [DataMember(
                Name = "gameplayCandidate",
                Order = 6,
                EmitDefaultValue = false)]
            public MoreEquipmentSlotsProductCandidateProbe?
                GameplayCandidate
            {
                get;
                set;
            }
        }

        [DataContract]
        private sealed class MoreEquipmentSlotsProductScopeProbe
        {
            [DataMember(Name = "archiveIndex", Order = 1)]
            public int ArchiveIndex { get; set; } = -1;

            [DataMember(Name = "playerName", Order = 2)]
            public string PlayerName { get; set; } = string.Empty;

            [DataMember(Name = "customPlayerName", Order = 3)]
            public string CustomPlayerName { get; set; } = string.Empty;

            [DataMember(
                Name = "totalGameSeconds",
                Order = 4,
                EmitDefaultValue = false)]
            public long? TotalGameSeconds { get; set; }
        }

        [DataContract]
        private sealed class MoreEquipmentSlotsProductSlotProbe
        {
            [DataMember(Name = "index", Order = 1)]
            public int Index { get; set; }

            [DataMember(Name = "itemId", Order = 2)]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "displayName", Order = 3)]
            public string DisplayName { get; set; } = string.Empty;

            [DataMember(Name = "skillId", Order = 4)]
            public string SkillId { get; set; } = string.Empty;

            [DataMember(Name = "defenseBonus", Order = 5)]
            public int DefenseBonus { get; set; }

            [DataMember(Name = "isShield", Order = 6)]
            public bool IsShield { get; set; }

            [DataMember(Name = "shieldValue", Order = 7)]
            public int ShieldValue { get; set; }

            [DataMember(Name = "shieldMaxValue", Order = 8)]
            public int ShieldMaxValue { get; set; }

            [DataMember(Name = "shieldDefend", Order = 9)]
            public int ShieldDefend { get; set; }
        }

        [DataContract]
        private sealed class MoreEquipmentSlotsProductJournalProbe
        {
            [DataMember(Name = "phase", Order = 2)]
            public int State { get; set; }

            [DataMember(Name = "scope", Order = 3)]
            public MoreEquipmentSlotsProductScopeProbe? Scope { get; set; }

            [DataMember(Name = "escrow", Order = 5)]
            public List<MoreEquipmentSlotsProductEscrowProbe> Escrow
            {
                get;
                set;
            } = new List<MoreEquipmentSlotsProductEscrowProbe>();

            [DataMember(Name = "attemptStarted", Order = 7)]
            public bool AttemptStarted { get; set; }
        }

        [DataContract]
        private sealed class MoreEquipmentSlotsProductEscrowProbe
        {
            [DataMember(Name = "itemId", Order = 2)]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "placement", Order = 8)]
            public int Placement { get; set; }

            [DataMember(Name = "attemptCompleted", Order = 9)]
            public bool AttemptCompleted { get; set; }
        }

        [DataContract]
        private sealed class MoreEquipmentSlotsProductCandidateProbe
        {
            [DataMember(Name = "transactionId", Order = 1)]
            public string TransactionId { get; set; } = string.Empty;
        }
    }
}
