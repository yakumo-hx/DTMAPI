using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const string StrongPlantingGunOwnerId =
            "DTMAPI.StrongPlantingGunMod";
        private const string StrongPlantingGunAssemblyName =
            "DTMAPI.StrongPlantingGun";
        private const string StrongPlantingGunHarmonyOwner =
            "dtmapi.mod.dtmapi.strongplantinggunmod";
        private const string StrongPlantingGunCallbackTypeName =
            "DTMAPI.StrongPlantingGun.StrongPlantingGunCallbacks";
        private static readonly Batch6HarmonyPatchTarget[]
            StrongPlantingGunHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget(
                "DolocTown.ItemFarmingGun",
                ".ctor",
                2),
            new Batch6HarmonyPatchTarget(
                "DolocTown.ItemFarmingGun",
                ".ctor",
                3),
            new Batch6HarmonyPatchTarget(
                "DolocTown.ItemFarmingGun",
                "OnUseAsTool",
                0),
            new Batch6HarmonyPatchTarget(
                "DolocTown.FarmingGunUiState",
                "HandlePlaceToOtherSide",
                1),
            new Batch6HarmonyPatchTarget(
                "DolocTown.FarmingGunUiState",
                "HandleSwapOneItem",
                1)
        };
        private bool strongPlantingGunNativeSavePrepared;
        private bool strongPlantingGunReentryLoadRequested;
        private bool strongPlantingGunReentrySaveLoadedObserved;
        private bool strongPlantingGunReentryVerified;
        private string strongPlantingGunReentryFailure =
            string.Empty;
        private int strongPlantingGunSavedQuickSlot = -1;
        private string[] strongPlantingGunSavedItemNames =
            Array.Empty<string>();
        private int[] strongPlantingGunSavedItemCounts =
            Array.Empty<int>();

        private FixtureAttemptResult
            TryExerciseStrongPlantingGunForFixture()
        {
            object? transientBasin = null;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            Type? dolocApiForCleanup = null;
            bool preserveSavedGun = false;
            object? ownedUiState = null;

            try
            {
                if (strongPlantingGunNativeSavePrepared)
                    return FixtureAttemptResult.Succeeded;

                Batch6HarmonyOwnerInventory ownerBefore =
                    ObserveStrongPlantingGunOwnerForFixture();
                object? productRuntime = ReadStaticCallbackRuntime(
                    StrongPlantingGunAssemblyName,
                    StrongPlantingGunCallbackTypeName);
                int loadedOwners =
                    CountLoadedOwners(StrongPlantingGunOwnerId);
                int ownerRoots =
                    runtime.CountCoreOwnerRoots(
                        StrongPlantingGunOwnerId);
                if (!ownerBefore.ProductAssemblyLoaded ||
                    !ownerBefore.IsComplete ||
                    ownerBefore.ExactOwnerPatchCount != 5 ||
                    productRuntime == null ||
                    !runtime.HasOwnerInstance(
                        StrongPlantingGunOwnerId) ||
                    loadedOwners != 1 ||
                    ownerRoots <= 0)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun ProductNative readiness was incomplete; the QA route does not fall back to the frozen API or Compatibility Host. instance=" +
                        runtime.HasOwnerInstance(
                            StrongPlantingGunOwnerId) +
                        "; loaded=" +
                        loadedOwners +
                        "; roots=" +
                        ownerRoots +
                        "; patches=" +
                        ownerBefore.ExactOwnerPatchCount +
                        "; targets=" +
                        ownerBefore.ExactOwnerTargetCount +
                        "/" +
                        ownerBefore.ResolvedTargetCount +
                        "; callback=" +
                        (productRuntime != null) +
                        "; inventory={" +
                        ownerBefore.Details +
                        "}.");
                }

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new InvalidOperationException("DolocAPI was not available.");
                dolocApiForCleanup = dolocApi;

                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    runtime.SetHookStatus(
                        "Smoke.StrongPlantingGun",
                        "pending",
                        "StrongPlantingGun ProductNative exact owner",
                        "Waiting for CurrentRoom after SaveLoaded.");
                    return FixtureAttemptResult.Pending;
                }

                transientBasin = TryCreateTransientEquipmentForFixture(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (transientBasin == null)
                    throw new InvalidOperationException("No PlantBasin target available. source=" + basinSource);

                object? gun = GenerateItemForFixture(dolocApi, "farming_gun", 1);
                if (gun == null || !IsTypeOrBase(gun.GetType(), "DolocTown.ItemFarmingGun"))
                    throw new InvalidOperationException("Could not generate official farming_gun item.");

                object? gunInventory = ReadMember(gun, "inventory");
                if (gunInventory == null)
                    throw new InvalidOperationException("Generated farming gun did not expose inventory.");

                int inventoryCapacity = ReadIntMember(gunInventory, "capacity", 0);
                int totalCapacity = ReadIntMember(gun, "totalCapacity", 0);
                int lineCapacity = ReadIntMember(gun, "lineCapacity", 0);
                if (inventoryCapacity < 3 || totalCapacity < 3 || lineCapacity < 3)
                    throw new InvalidOperationException("Farming gun capacity was not expanded to three visible slots. inventory=" + inventoryCapacity + ", total=" + totalCapacity + ", line=" + lineCapacity + ".");

                object? seed = FindStrongPlantingGunSeedForFixture(dolocApi, transientBasin, out string seedSummary);
                object? film = GenerateItemForFixture(dolocApi, "plastic_film", 2);
                object? fertilizer = GenerateItemForFixture(dolocApi, "fertilizer", 2);
                if (seed == null || film == null || fertilizer == null)
                    throw new InvalidOperationException("Could not generate seed/film/fertilizer. seed={" + seedSummary + "}, film=" + (film != null) + ", fertilizer=" + (fertilizer != null));

                seed = CloneItemForFixture(seed, 2) ?? seed;
                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, gun, quickSlot, out inventory, out originalSlotItem, out string quickSlotSummary))
                    throw new InvalidOperationException("Could not quick-slot generated farming gun. " + quickSlotSummary);

                object backpackInventory =
                    ResolveStrongPlantingGunBackpackInventory(
                        dolocApi);
                string seedName =
                    ReadStringMember(seed, "name", string.Empty);
                if (string.IsNullOrWhiteSpace(seedName))
                {
                    throw new InvalidOperationException(
                        "Generated seed did not expose a stable native item name.");
                }
                int backpackSeedBaseline =
                    CountInventoryItemByNameForFixture(
                        backpackInventory,
                        seedName);
                int backpackSeedSlot =
                    FindEmptyInventorySlotForFixture(
                        backpackInventory,
                        quickSlot);
                string backpackSeedPlace = "not-attempted";
                if (backpackSeedSlot < 0 ||
                    !SwapInventoryItemAtForFixture(
                        backpackInventory,
                        backpackSeedSlot,
                        seed,
                        out backpackSeedPlace))
                {
                    throw new InvalidOperationException(
                        "Could not stage the seed in one real backpack slot for FarmingGunUiState transfer. slot=" +
                        backpackSeedSlot +
                        "; place={" +
                        (backpackSeedSlot < 0
                            ? "no-empty-slot"
                            : backpackSeedPlace) +
                        "}.");
                }

                ownedUiState =
                    EnterStrongPlantingGunUiForFixture(gun);
                if (ownedUiState == null)
                {
                    throw new InvalidOperationException(
                        "Could not enter the real FarmingGunUiState.");
                }
                InvokeStrongPlantingGunUiTransferForFixture(
                    ownedUiState,
                    "backpackPanel",
                    "HandlePlaceToOtherSide",
                    backpackSeedSlot);
                int seedInGunAfterPlace =
                    ReadInventoryItemCount(gunInventory, 0);
                if (seedInGunAfterPlace != 2)
                {
                    throw new InvalidOperationException(
                        "FarmingGunUiState ProductNative place path did not move the two-count seed stack into slot zero. count=" +
                        seedInGunAfterPlace +
                        ".");
                }

                InvokeStrongPlantingGunUiTransferForFixture(
                    ownedUiState,
                    "containerWidget",
                    "HandlePlaceToOtherSide",
                    0);
                int backpackSeedAfterTake =
                    CountInventoryItemByNameForFixture(
                        backpackInventory,
                        seedName);
                if (ReadInventoryItemCount(gunInventory, 0) != 0 ||
                    backpackSeedAfterTake !=
                        backpackSeedBaseline + 2)
                {
                    throw new InvalidOperationException(
                        "FarmingGunUiState native take path did not conserve the seed stack. gun=" +
                        ReadInventoryItemCount(gunInventory, 0) +
                        "; backpack=" +
                        backpackSeedAfterTake +
                        "; baseline=" +
                        backpackSeedBaseline +
                        ".");
                }

                backpackSeedSlot =
                    FindInventoryItemSlotByNameForFixture(
                        backpackInventory,
                        seedName);
                InvokeStrongPlantingGunUiTransferForFixture(
                    ownedUiState,
                    "backpackPanel",
                    "HandleSwapOneItem",
                    backpackSeedSlot);
                InvokeStrongPlantingGunUiTransferForFixture(
                    ownedUiState,
                    "backpackPanel",
                    "HandleSwapOneItem",
                    backpackSeedSlot);
                bool seedPlaced =
                    ReadInventoryItemCount(gunInventory, 0) == 2 &&
                    CountInventoryItemByNameForFixture(
                        backpackInventory,
                        seedName) == backpackSeedBaseline;
                string seedPlace =
                    "uiPlaceTakeSwapOneTwice=True, backpackSlot=" +
                    backpackSeedSlot +
                    ", baseline=" +
                    backpackSeedBaseline +
                    ", staged={" +
                    backpackSeedPlace +
                    "}";
                bool filmPlaced = SwapInventoryItemAtForFixture(gunInventory, 1, film, out string filmPlace);
                bool fertilizerPlaced = SwapInventoryItemAtForFixture(gunInventory, 2, fertilizer, out string fertilizerPlace);
                if (!seedPlaced || !filmPlaced || !fertilizerPlaced)
                    throw new InvalidOperationException("Could not place strong planting gun contents. seed={" + seedPlace + "}, film={" + filmPlace + "}, fertilizer={" + fertilizerPlace + "}");

                CloseStrongPlantingGunUiForFixture(
                    dolocApi,
                    ownedUiState);
                ownedUiState = null;

                object? selectedGun = ReadStaticMember(dolocApi, "SelectedItem");
                if (selectedGun == null || !IsTypeOrBase(selectedGun.GetType(), "DolocTown.ItemFarmingGun"))
                    throw new InvalidOperationException("SelectedItem was not the generated farming gun after quick-slot placement. selected=" + (selectedGun == null ? "null" : selectedGun.GetType().FullName));

                if (!PrimeStrongPlantingGunCellTipForFixture(
                        dolocApi,
                        selectedGun,
                        transientBasin,
                        out string tipPrimeSummary))
                {
                    throw new InvalidOperationException(
                        "Could not initialize the official farming-gun cell-tip area for the protected fixture. " +
                        tipPrimeSummary);
                }
                if (!TryPointAgentCellTipAtEquipmentForFixture(dolocApi, transientBasin, out string tipSummary))
                    throw new InvalidOperationException("Could not point farming gun cell tip at transient basin. " + tipSummary);

                MethodInfo? onUseAsTool = FindMethod(selectedGun.GetType(), "OnUseAsTool", 0);
                if (onUseAsTool == null)
                    throw new MissingMethodException("ItemFarmingGun.OnUseAsTool was not found.");

                int beforeSeedCount = ReadInventoryItemCount(gunInventory, 0);
                int beforeFilmCount = ReadInventoryItemCount(gunInventory, 1);
                int beforeFertilizerCount = ReadInventoryItemCount(gunInventory, 2);
                onUseAsTool.Invoke(selectedGun, null);

                bool planted = ReadBoolMember(transientBasin, "IsPlanted", false);
                bool protectedByFilm = ReadBoolMember(transientBasin, "IsProtected", false);
                bool fertilized = ReadBoolMember(transientBasin, "IsFertilizerd", false);
                int afterSeedCount = ReadInventoryItemCount(gunInventory, 0);
                int afterFilmCount = ReadInventoryItemCount(gunInventory, 1);
                int afterFertilizerCount = ReadInventoryItemCount(gunInventory, 2);

                if (!planted || !protectedByFilm || !fertilized ||
                    afterSeedCount != beforeSeedCount - 1 ||
                    afterFilmCount != beforeFilmCount - 1 ||
                    afterFertilizerCount !=
                        beforeFertilizerCount - 1)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun ProductNative did not apply and conserve all three visible actions. planted=" +
                        planted +
                        ", protected=" +
                        protectedByFilm +
                        ", fertilized=" +
                        fertilized +
                        ", seed=" +
                        beforeSeedCount +
                        "->" +
                        afterSeedCount +
                        ", film=" +
                        beforeFilmCount +
                        "->" +
                        afterFilmCount +
                        ", fertilizer=" +
                        beforeFertilizerCount +
                        "->" +
                        afterFertilizerCount +
                        ".");
                }

                string basinDescription =
                    DescribeEquipmentForFixture(transientBasin);
                TryRemoveTransientEquipmentForFixture(
                    room,
                    transientBasin);
                transientBasin = null;
                strongPlantingGunSavedQuickSlot = quickSlot;
                strongPlantingGunSavedItemNames =
                    ReadStrongPlantingGunSlotNamesForFixture(
                        gunInventory);
                strongPlantingGunSavedItemCounts =
                    ReadStrongPlantingGunSlotCountsForFixture(
                        gunInventory);
                RequestStrongPlantingGunNativeSave(dolocApi);
                strongPlantingGunNativeSavePrepared = true;
                preserveSavedGun = true;

                Batch6HarmonyOwnerInventory ownerAfter =
                    ObserveStrongPlantingGunOwnerForFixture();
                if (!ownerAfter.IsComplete ||
                    ownerAfter.ExactOwnerPatchCount != 5)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun ProductNative behavior completed without exactly five retained owner patches. inventory={" +
                        ownerAfter.Details +
                        "}.");
                }

                string summary = "seed={" + seedSummary + "}" +
                    ", basin={" + basinDescription + " source=" + basinSource + "}" +
                    ", capacities=inventory:" + inventoryCapacity + "/total:" + totalCapacity + "/line:" + lineCapacity +
                    ", counts=seed:" + beforeSeedCount + "->" + afterSeedCount +
                    ", film:" + beforeFilmCount + "->" + afterFilmCount +
                    ", fertilizer:" + beforeFertilizerCount + "->" + afterFertilizerCount +
                    ", basinState=planted:" + planted + ",protected:" + protectedByFilm + ",fertilized:" + fertilized +
                    ", quickSlot={" + quickSlotSummary + "}" +
                    ", tip={" + tipPrimeSummary + "; " + tipSummary + "}" +
                    ", uiPlaceTake=True" +
                    ", nativeSave=True" +
                    ", patches=5" +
                    ", route=ProductNative" +
                    ", inventory={" + ownerAfter.Details + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise StrongPlantingGun OK " + summary);
                runtime.SetHookStatus(
                    "Smoke.StrongPlantingGun",
                    "verified",
                    "StrongPlantingGun ProductNative five exact Hooks -> real FarmingGunUiState place/take -> ItemFarmingGun.OnUseAsTool -> DolocAPI.SaveGame(2)",
                    summary);
                return FixtureAttemptResult.Succeeded;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke strong planting gun exercise failed.", ex.InnerException.ToString());
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "failed", "StrongPlantingGun ProductNative only; no frozen-ABI fallback", ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                return FixtureAttemptResult.Failed;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke strong planting gun exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "failed", "StrongPlantingGun ProductNative only; no frozen-ABI fallback", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
            finally
            {
                if (ownedUiState != null &&
                    dolocApiForCleanup != null)
                {
                    TryCloseStrongPlantingGunUiForFixture(
                        dolocApiForCleanup,
                        ownedUiState);
                }
                if (!preserveSavedGun &&
                    dolocApiForCleanup != null)
                {
                    RestoreSmokeQuickSlot(dolocApiForCleanup, inventory, quickSlot, originalSlotItem);
                }
                if (transientBasin != null && dolocApiForCleanup != null)
                    TryRemoveTransientEquipmentForFixture(ReadStaticMember(dolocApiForCleanup, "CurrentRoom"), transientBasin);
            }
        }

        private Batch6HarmonyOwnerInventory
            ObserveStrongPlantingGunOwnerForFixture() =>
            Batch6AdvancedHarmonyOwnerObserver.Observe(
                StrongPlantingGunAssemblyName,
                StrongPlantingGunHarmonyOwner,
                StrongPlantingGunHarmonyTargets);

        internal G4FixtureStepResult
            AdvanceStrongPlantingGunReentryLoad(
                int humanSlot)
        {
            if (!string.IsNullOrWhiteSpace(
                    strongPlantingGunReentryFailure))
            {
                return G4FixtureStepResult.Failed(
                    "StrongPlantingGun native-save re-entry failed: " +
                    strongPlantingGunReentryFailure);
            }
            if (strongPlantingGunReentryVerified)
            {
                return G4FixtureStepResult.Verified(
                    "StrongPlantingGun third-save native JSON re-entry is verified.");
            }
            if (!strongPlantingGunNativeSavePrepared)
            {
                return G4FixtureStepResult.Pending(
                    "Waiting for the StrongPlantingGun in-save fixture to commit its protected native save before title re-entry.");
            }
            if (!strongPlantingGunReentryLoadRequested)
            {
                strongPlantingGunReentryLoadRequested =
                    TryAutoLoadSave(humanSlot);
                return G4FixtureStepResult.Pending(
                    strongPlantingGunReentryLoadRequested
                        ? "Requested the one bounded third-save reload for StrongPlantingGun native JSON re-entry."
                        : "Waiting for the official third-save UI route before requesting StrongPlantingGun native JSON re-entry.");
            }

            if (strongPlantingGunReentrySaveLoadedObserved)
            {
                ObserveStrongPlantingGunReentryAfterSaveLoadedForFixture();
                if (!string.IsNullOrWhiteSpace(
                        strongPlantingGunReentryFailure))
                {
                    return G4FixtureStepResult.Failed(
                        "StrongPlantingGun native-save re-entry failed: " +
                        strongPlantingGunReentryFailure);
                }
                if (strongPlantingGunReentryVerified)
                {
                    return G4FixtureStepResult.Verified(
                        "StrongPlantingGun third-save native JSON re-entry is verified.");
                }
            }

            ContinueInitialSaveLoad();
            return G4FixtureStepResult.Pending(
                "Waiting for the bounded third-save reload to publish SaveLoaded and verify StrongPlantingGun native JSON re-entry.");
        }

        private void
            MarkStrongPlantingGunReentrySaveLoadedForFixture()
        {
            if (strongPlantingGunNativeSavePrepared &&
                strongPlantingGunReentryLoadRequested)
            {
                strongPlantingGunReentrySaveLoadedObserved =
                    true;
            }
        }

        private void
            ObserveStrongPlantingGunReentryAfterSaveLoadedForFixture()
        {
            if (!strongPlantingGunNativeSavePrepared ||
                strongPlantingGunReentryVerified ||
                !string.IsNullOrWhiteSpace(
                    strongPlantingGunReentryFailure))
            {
                return;
            }

            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type dolocApi =
                    patcher.ResolveType(
                        "DolocAPI, Assembly-CSharp") ??
                    throw new TypeLoadException(
                        "DolocAPI was unavailable after StrongPlantingGun save re-entry.");
                object backpack =
                    ResolveStrongPlantingGunBackpackInventory(
                        dolocApi);
                object gun =
                    ReadInventoryItemForFixture(
                        backpack,
                        strongPlantingGunSavedQuickSlot) ??
                    throw new InvalidOperationException(
                        "Saved farming gun was absent from its protected quick slot after title re-entry.");
                if (!IsTypeOrBase(
                    gun.GetType(),
                    "DolocTown.ItemFarmingGun"))
                {
                    throw new InvalidOperationException(
                        "Protected quick slot did not deserialize as ItemFarmingGun after title re-entry.");
                }

                object gunInventory =
                    ReadMember(gun, "inventory") ??
                    throw new InvalidOperationException(
                        "Re-entered ItemFarmingGun had no native inventory.");
                string[] names =
                    ReadStrongPlantingGunSlotNamesForFixture(
                        gunInventory);
                int[] counts =
                    ReadStrongPlantingGunSlotCountsForFixture(
                        gunInventory);
                int inventoryCapacity =
                    ReadIntMember(
                        gunInventory,
                        "capacity",
                        0);
                int totalCapacity =
                    ReadIntMember(
                        gun,
                        "totalCapacity",
                        0);
                int lineCapacity =
                    ReadIntMember(
                        gun,
                        "lineCapacity",
                        0);
                Batch6HarmonyOwnerInventory owner =
                    ObserveStrongPlantingGunOwnerForFixture();
                if (!strongPlantingGunSavedItemNames
                        .SequenceEqual(
                            names,
                            StringComparer.Ordinal) ||
                    !strongPlantingGunSavedItemCounts
                        .SequenceEqual(counts) ||
                    inventoryCapacity < 3 ||
                    totalCapacity < 3 ||
                    lineCapacity < 3 ||
                    !owner.IsComplete ||
                    owner.ExactOwnerPatchCount != 5)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun native JSON/title re-entry did not retain the three logical items and exact owner set. expectedNames=" +
                        string.Join(
                            "|",
                            strongPlantingGunSavedItemNames) +
                        "; actualNames=" +
                        string.Join("|", names) +
                        "; expectedCounts=" +
                        string.Join(
                            "|",
                            strongPlantingGunSavedItemCounts) +
                        "; actualCounts=" +
                        string.Join("|", counts) +
                        "; capacities=" +
                        inventoryCapacity +
                        "/" +
                        totalCapacity +
                        "/" +
                        lineCapacity +
                        "; patches=" +
                        owner.ExactOwnerPatchCount +
                        "; inventory={" +
                        owner.Details +
                        "}.");
                }

                strongPlantingGunReentryVerified = true;
                string details =
                    "nativeSave=True; titleReentry=True; jsonCtor=True; logicalItems=" +
                    string.Join("|", names) +
                    "; counts=" +
                    string.Join("|", counts) +
                    "; capacities=" +
                    inventoryCapacity +
                    "/" +
                    totalCapacity +
                    "/" +
                    lineCapacity +
                    "; patches=5";
                runtime.SetHookStatus(
                    "Smoke.StrongPlantingGunReentry",
                    "verified",
                    "DolocAPI.SaveGame(2) -> ReturnHome -> third-save reload -> ItemFarmingGun JSON constructor",
                    details);
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise StrongPlantingGunReentry OK " +
                    details +
                    ".");
            }
            catch (Exception ex)
            {
                strongPlantingGunReentryFailure =
                    ex.GetType().Name +
                    ": " +
                    ex.Message;
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "StrongPlantingGun native save/title re-entry observation failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.StrongPlantingGunReentry",
                    "failed",
                    "native save/title re-entry",
                    strongPlantingGunReentryFailure);
            }
        }

        private static object? CloneItemForFixture(
            object item,
            int count)
        {
            MethodInfo? clone = FindMethod(
                item?.GetType(),
                "Clone",
                1);
            return clone?.Invoke(
                item,
                new object[]
                {
                    count
                });
        }

        private static object
            ResolveStrongPlantingGunBackpackInventory(
                Type dolocApi)
        {
            object archive =
                ReadStaticMember(dolocApi, "archiveHandle") ??
                throw new InvalidOperationException(
                    "archiveHandle was unavailable.");
            object inventorySystem =
                ReadMember(archive, "InventorySystem") ??
                throw new InvalidOperationException(
                    "Archive InventorySystem was unavailable.");
            return ReadMember(inventorySystem, "inventory") ??
                throw new InvalidOperationException(
                    "Native backpack inventory was unavailable.");
        }

        private static int FindEmptyInventorySlotForFixture(
            object inventory,
            int excludedIndex)
        {
            int capacity =
                ReadIntMember(inventory, "capacity", 0);
            for (int index = 0; index < capacity; index++)
            {
                if (index != excludedIndex &&
                    ReadInventoryItemForFixture(
                        inventory,
                        index) == null)
                {
                    return index;
                }
            }
            return -1;
        }

        private static int FindInventoryItemSlotByNameForFixture(
            object inventory,
            string itemName)
        {
            int capacity =
                ReadIntMember(inventory, "capacity", 0);
            for (int index = 0; index < capacity; index++)
            {
                object? item =
                    ReadInventoryItemForFixture(
                        inventory,
                        index);
                if (item != null &&
                    string.Equals(
                        ReadStringMember(
                            item,
                            "name",
                            string.Empty),
                        itemName,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }
            return -1;
        }

        private static int CountInventoryItemByNameForFixture(
            object inventory,
            string itemName)
        {
            int count = 0;
            int capacity =
                ReadIntMember(inventory, "capacity", 0);
            for (int index = 0; index < capacity; index++)
            {
                object? item =
                    ReadInventoryItemForFixture(
                        inventory,
                        index);
                if (item != null &&
                    string.Equals(
                        ReadStringMember(
                            item,
                            "name",
                            string.Empty),
                        itemName,
                        StringComparison.Ordinal))
                {
                    count += ReadIntMember(
                        item,
                        "count",
                        0);
                }
            }
            return count;
        }

        private static object? ReadInventoryItemForFixture(
            object inventory,
            int index)
        {
            if (index < 0)
                return null;
            MethodInfo? read = FindMethod(
                inventory.GetType(),
                "Read",
                1);
            return read?.Invoke(
                inventory,
                new object[]
                {
                    index
                });
        }

        private static object? EnterStrongPlantingGunUiForFixture(
            object gun)
        {
            Type uiType =
                Type.GetType(
                    "DolocTown.FarmingGunUiState, Assembly-CSharp") ??
                throw new TypeLoadException(
                    "FarmingGunUiState was unavailable.");
            MethodInfo startup = uiType.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .FirstOrDefault(candidate =>
                    candidate.Name.Equals(
                        "HandleStartUpArgs",
                        StringComparison.Ordinal) &&
                    candidate.GetParameters().Length == 2 &&
                    candidate.ReturnType == typeof(bool)) ??
                throw new MissingMethodException(
                    uiType.FullName,
                    "HandleStartUpArgs(IContainer,Action)");
            ParameterInfo[] parameters =
                startup.GetParameters();
            ParameterExpression state =
                Expression.Parameter(
                    uiType,
                    "state");
            MethodCallExpression invoke =
                Expression.Call(
                    state,
                    startup,
                    Expression.Convert(
                        Expression.Constant(gun),
                        parameters[0].ParameterType),
                    Expression.Constant(
                        null,
                        parameters[1].ParameterType));
            Type delegateType =
                typeof(Func<,>).MakeGenericType(
                    uiType,
                    typeof(bool));
            Delegate callback =
                Expression.Lambda(
                    delegateType,
                    invoke,
                    state).Compile();
            return EnterNativeUiForFixture(
                uiType,
                callback);
        }

        private static void
            InvokeStrongPlantingGunUiTransferForFixture(
                object uiState,
                string panelMember,
                string methodName,
                int index)
        {
            if (index < 0)
            {
                throw new InvalidOperationException(
                    "StrongPlantingGun UI transfer received an invalid inventory index.");
            }
            object panel =
                ReadMember(uiState, panelMember) ??
                throw new InvalidOperationException(
                    "FarmingGunUiState did not expose " +
                    panelMember +
                    ".");
            MethodInfo select =
                FindMethod(
                    panel.GetType(),
                    "Select",
                    1) ??
                throw new MissingMethodException(
                    panel.GetType().FullName,
                    "Select(int)");
            select.Invoke(
                panel,
                new object[]
                {
                    index
                });

            MethodInfo transfer =
                FindMethod(
                    uiState.GetType(),
                    methodName,
                    1) ??
                throw new MissingMethodException(
                    uiState.GetType().FullName,
                    methodName + "(int)");
            transfer.Invoke(
                uiState,
                new object[]
                {
                    index
                });
        }

        private static void CloseStrongPlantingGunUiForFixture(
            Type dolocApi,
            object uiState)
        {
            if (!TryCloseStrongPlantingGunUiForFixture(
                dolocApi,
                uiState))
            {
                throw new InvalidOperationException(
                    "Could not close and release the exact FarmingGunUiState receipt.");
            }
        }

        private static bool TryCloseStrongPlantingGunUiForFixture(
            Type dolocApi,
            object uiState)
        {
            object? userInput =
                ReadStaticMember(dolocApi, "userInput");
            object? manager =
                ReadStaticMember(dolocApi, "gameUiStates");
            return TryInvokeExactPopStateForFixture(
                    userInput,
                    uiState) &&
                TryInvokeExactGenericRemoveUiForFixture(
                    manager,
                    uiState.GetType());
        }

        private static string[]
            ReadStrongPlantingGunSlotNamesForFixture(
                object inventory)
        {
            var names = new string[3];
            for (int index = 0; index < names.Length; index++)
            {
                object? item =
                    ReadInventoryItemForFixture(
                        inventory,
                        index);
                names[index] = item == null
                    ? string.Empty
                    : ReadStringMember(
                        item,
                        "name",
                        string.Empty);
            }
            return names;
        }

        private static int[]
            ReadStrongPlantingGunSlotCountsForFixture(
                object inventory)
        {
            var counts = new int[3];
            for (int index = 0; index < counts.Length; index++)
            {
                counts[index] =
                    ReadInventoryItemCount(
                        inventory,
                        index);
            }
            return counts;
        }

        private static void RequestStrongPlantingGunNativeSave(
            Type dolocApi)
        {
            MethodInfo? saveGame = dolocApi.GetMethod(
                "SaveGame",
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(int)
                },
                modifiers: null);
            if (saveGame == null)
            {
                throw new MissingMethodException(
                    dolocApi.FullName,
                    "SaveGame(int)");
            }
            object? result = saveGame.Invoke(
                null,
                new object[]
                {
                    2
                });
            if (result is bool saved && !saved)
            {
                throw new InvalidOperationException(
                    "DolocAPI.SaveGame(2) rejected the StrongPlantingGun protected native-save fixture.");
            }
        }

        private bool PrimeStrongPlantingGunCellTipForFixture(
            Type dolocApi,
            object gun,
            object equipment,
            out string summary)
        {
            summary = string.Empty;
            object? function = ReadMember(gun, "func");
            object? area = function == null
                ? null
                : ReadMember(function, "Area");
            object? anchor = ReadMember(equipment, "Anchor");
            object? agentCell =
                ReadStaticMember(dolocApi, "AgentRoomCellPosition");
            object? uiSystem = ReadStaticMember(dolocApi, "uiSystem");
            object? basicTip =
                uiSystem == null
                    ? null
                    : ReadMember(uiSystem, "basicTip");
            object? cellTip =
                basicTip == null
                    ? null
                    : ReadMember(basicTip, "AgentCellTip");
            MethodInfo? show =
                cellTip == null
                    ? null
                    : FindMethod(cellTip.GetType(), "Show", 5);
            if (area == null ||
                anchor == null ||
                agentCell == null ||
                cellTip == null ||
                show == null)
            {
                summary =
                    "func.Area/anchor/AgentRoomCellPosition/AgentCellTip.Show unavailable.";
                return false;
            }

            int dx =
                ReadIntMember(anchor, "x", 0) -
                ReadIntMember(agentCell, "x", 0);
            int dy =
                ReadIntMember(anchor, "y", 0) -
                ReadIntMember(agentCell, "y", 0);
            object? offset =
                CreateVector2IntForFixture(dx, dy);
            if (offset == null)
            {
                summary =
                    "Could not create the exact native cell-tip offset.";
                return false;
            }

            show.Invoke(
                cellTip,
                new[]
                {
                    offset,
                    area,
                    (object)false,
                    null,
                    (object)false
                });
            object? observedSize = ReadMember(cellTip, "CellSize");
            int width =
                observedSize == null
                    ? 0
                    : ReadIntMember(observedSize, "x", 0);
            int height =
                observedSize == null
                    ? 0
                    : ReadIntMember(observedSize, "y", 0);
            summary =
                "official AgentCellTip.Show offset=" +
                dx +
                "," +
                dy +
                " area=" +
                width +
                "x" +
                height;
            return width > 0 && height > 0;
        }

        private object? FindStrongPlantingGunSeedForFixture(Type dolocApi, object basin, out string summary)
        {
            string basinSeedType = ReadPlantBasinSeedTypeForFixture(basin);
            var notes = new List<string>();
            foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin", "seed_wheat" })
            {
                object? seed = GenerateItemForFixture(dolocApi, seedId, 1);
                if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                {
                    notes.Add(seedId + ":missing");
                    continue;
                }

                string seedType = ReadSeedTypeForFixture(seed);
                MethodInfo? checkSeason =
                    FindMethod(
                        seed.GetType(),
                        "CheckCurrentSeasonValid",
                        1);
                bool seasonValid =
                    checkSeason?.Invoke(
                        seed,
                        new object[]
                        {
                            false
                        }) is bool valid &&
                    valid;
                notes.Add(
                    seedId +
                    ":" +
                    seedType +
                    ":season=" +
                    seasonValid);
                if (seedType.Equals(
                        basinSeedType,
                        StringComparison.OrdinalIgnoreCase) &&
                    seasonValid)
                {
                    summary =
                        "selected=" +
                        seedId +
                        ", seedType=" +
                        seedType +
                        ", seasonValid=True, basinSeedType=" +
                        basinSeedType +
                        ", candidates=" +
                        string.Join("|", notes);
                    return seed;
                }
            }

            summary = "basinSeedType=" + basinSeedType + ", candidates=" + string.Join("|", notes);
            return null;
        }

        private static bool SwapInventoryItemAtForFixture(object inventory, int slot, object item, out string summary)
        {
            try
            {
                MethodInfo? swapItem = FindMethod(inventory.GetType(), "SwapItem", 2);
                if (swapItem == null)
                {
                    summary = "LinearInventory.SwapItem was not available.";
                    return false;
                }

                object? leftover = swapItem.Invoke(inventory, new object[] { slot, item });
                if (leftover != null)
                {
                    summary = "slot=" + slot + ", leftover=" + ReadStringMember(leftover, "name", leftover.GetType().Name) + ", count=" + ReadIntMember(leftover, "count", 0);
                    return false;
                }

                object? placed = FindMethod(inventory.GetType(), "Read", 1)?.Invoke(inventory, new object[] { slot });
                summary = "slot=" + slot + ", item=" + (placed == null ? "null" : ReadStringMember(placed, "name", placed.GetType().Name)) + ", count=" + (placed == null ? 0 : ReadIntMember(placed, "count", 0));
                return placed != null;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

    }
}
