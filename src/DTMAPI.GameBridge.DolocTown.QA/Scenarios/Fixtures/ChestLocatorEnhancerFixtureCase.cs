#pragma warning disable CS0618 // The fallback route deliberately exercises the frozen IChestLocatorEnhancerApi ABI.
using System;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const string ChestLocatorProductAssemblyName = "DTMAPI.ChestLocatorEnhancer";
        private const string ChestLocatorProductHarmonyOwner = "dtmapi.mod.dtmapi.chestlocatorenhancermod";
        private static readonly Batch6HarmonyPatchTarget[] ChestLocatorProductHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget(
                "DolocTown.GameData.ArchiveDataHandle",
                "GetAvailableInventories",
                3)
        };

        private void TryExerciseChestLocatorEnhancerForFixture()
        {
            object? transientCase = null;
            object? targetRoom = null;
            bool legacyCompatibilityRegistered = false;
            const string qaOwnerId = "DTMAPI.QA.G5.ChestLocatorEnhancer";
            try
            {
                ChestLocatorEnhancerService? chestLocatorService = ChestLocatorEnhancerService;
                if (chestLocatorService == null)
                    throw new InvalidOperationException("ChestLocatorEnhancer feature service is not available.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new InvalidOperationException("DolocAPI was not available.");

                Batch6HarmonyOwnerInventory productBefore = Batch6AdvancedHarmonyOwnerObserver.Observe(
                    ChestLocatorProductAssemblyName,
                    ChestLocatorProductHarmonyOwner,
                    ChestLocatorProductHarmonyTargets);
                bool productNativeRoute = productBefore.ProductAssemblyLoaded;
                IChestLocatorEnhancerApi? api = null;
                ManifestModel? owner = null;
                if (productNativeRoute)
                {
                    if (!productBefore.IsComplete ||
                        productBefore.ExactOwnerPatchCount != 1)
                    {
                        throw new InvalidOperationException(
                            "Loaded ChestLocatorEnhancer ProductNative owner was incomplete; refusing frozen ABI fallback. patches=" +
                            productBefore.ExactOwnerPatchCount +
                            ", targets=" +
                            productBefore.ExactOwnerTargetCount +
                            "/" +
                            productBefore.ResolvedTargetCount +
                            ", inventory={" +
                            productBefore.Details +
                            "}.");
                    }
                }
                else
                {
                    api = chestLocatorService;
                    owner = CreateChestLocatorSmokeManifest();
                    ChestLocatorEnhancerRegisterResult register = api.Register(owner, new ChestLocatorEnhancerOptions
                    {
                        Enabled = true,
                        IncludeSharedCases = true,
                        IncludeSharedStorageShelfBoxes = true,
                        RespectNativeAutoUseBoxSetting = true,
                        VerboseLogging = true
                    });
                    if (!register.Success)
                        throw new InvalidOperationException("Register failed: " + register.FailureReason + ": " + register.Message);
                    legacyCompatibilityRegistered = true;
                    if (!register.HookInstalled)
                        throw new InvalidOperationException("ArchiveDataHandle.GetAvailableInventories hook was not installed. message=" + register.Message);
                }

                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                if (archive == null)
                    throw new InvalidOperationException("archiveHandle was not available.");
                targetRoom = FindChestLocatorSmokeBuildingRoom(dolocApi, archive, out string roomSummary);
                if (targetRoom == null)
                    throw new InvalidOperationException("No building room was available for cross-room chest locator smoke. " + roomSummary);

                string itemId = SelectZeroBaselineSmokeItemId(dolocApi, new[] { "dtmapi_mine", "crude_oil", "case_locator", "recipe_case_locator", "sunmao_showcase", "mountain_showcase" }, out int baseline, out string itemSummary);
                if (string.IsNullOrWhiteSpace(itemId))
                    throw new InvalidOperationException("No zero-baseline generated item was available for safe consume smoke. " + itemSummary);

                transientCase = TryCreateTransientEquipmentNoRenderForFixture(dolocApi, targetRoom, "DolocTown.Case", new[] { "wooden_case", "large_wooden_case" }, out string caseSummary);
                if (transientCase == null)
                    throw new InvalidOperationException("Failed to create transient shared Case. " + caseSummary);
                if (!SetMemberValue(transientCase, "IsShared", true))
                    throw new InvalidOperationException("Failed to set transient Case IsShared=true. " + DescribeEquipmentForFixture(transientCase));

                object? inventory = ReadMember(transientCase, "inventory");
                MethodInfo? placeItem = inventory == null ? null : FindMethod(inventory.GetType(), "PlaceItem", 1);
                object? item = GenerateItemForFixture(dolocApi, itemId, 3);
                if (inventory == null || placeItem == null || item == null)
                    throw new InvalidOperationException("Failed to prepare transient inventory item. inventory=" + (inventory != null) + ", placeItem=" + (placeItem != null) + ", item=" + (item != null));
                object? leftover = placeItem.Invoke(inventory, new[] { item });
                if (leftover != null)
                    throw new InvalidOperationException("Transient Case inventory did not accept " + itemId + "; leftover=" + leftover.GetType().FullName);

                int afterPlace = CountNativeItemForFixture(dolocApi, itemId, checkBox: true);
                if (afterPlace < baseline + 3)
                    throw new InvalidOperationException("CountItem did not include transient shared Case. item=" + itemId + ", baseline=" + baseline + ", afterPlace=" + afterPlace + ", route=" + (productNativeRoute ? "ProductNative" : "LegacyCompatibility") + ", bridge={" + chestLocatorService.LastChestLocatorEnhancerSummary + "}");

                bool cost = CostNativeItemForFixture(dolocApi, itemId, 2, checkBox: true);
                int afterCost = CountNativeItemForFixture(dolocApi, itemId, checkBox: true);
                if (!cost || afterCost < baseline + 1 || afterCost > baseline + 1)
                    throw new InvalidOperationException("CostItem did not consume through shared inventory array. item=" + itemId + ", cost=" + cost + ", baseline=" + baseline + ", afterPlace=" + afterPlace + ", afterCost=" + afterCost + ", route=" + (productNativeRoute ? "ProductNative" : "LegacyCompatibility") + ", bridge={" + chestLocatorService.LastChestLocatorEnhancerSummary + "}");

                string routeEvidence;
                if (productNativeRoute)
                {
                    Batch6HarmonyOwnerInventory productAfter = Batch6AdvancedHarmonyOwnerObserver.Observe(
                        ChestLocatorProductAssemblyName,
                        ChestLocatorProductHarmonyOwner,
                        ChestLocatorProductHarmonyTargets);
                    if (!productAfter.IsComplete ||
                        productAfter.ExactOwnerPatchCount != 1)
                    {
                        throw new InvalidOperationException(
                            "ChestLocatorEnhancer ProductNative behavior completed without exactly one retained product owner patch. patches=" +
                            productAfter.ExactOwnerPatchCount +
                            ", targets=" +
                            productAfter.ExactOwnerTargetCount +
                            "/" +
                            productAfter.ResolvedTargetCount +
                            ", inventory={" +
                            productAfter.Details +
                            "}.");
                    }
                    routeEvidence =
                        "route=ProductNative, exactOwnerPatches=1, exactOwnerTargets=1/1, inventory={" +
                        productAfter.Details +
                        "}";
                }
                else
                {
                    ChestLocatorEnhancerState state = api!.GetState(owner!.UniqueID);
                    if (state.LastAppendedInventoryCount <= 0 || state.LastSharedCaseCount <= 0)
                        throw new InvalidOperationException("Bridge state did not record appended shared Case inventory. state=" + FormatChestLocatorState(state));
                    routeEvidence =
                        "route=LegacyCompatibility, bridge={" +
                        chestLocatorService.LastChestLocatorEnhancerSummary +
                        "}, state={" +
                        FormatChestLocatorState(state) +
                        "}";
                }

                string summary = "item=" + itemId +
                    ", baseline=" + baseline +
                    ", afterPlace=" + afterPlace +
                    ", afterCost=" + afterCost +
                    ", room={" + roomSummary + "}" +
                    ", case={" + caseSummary + "}" +
                    ", " + routeEvidence;
                runtime.RuntimeMonitor.Log("Smoke exercise ChestLocatorEnhancer OK " + summary);
                runtime.SetHookStatus(
                    "Smoke.ChestLocatorEnhancer",
                    "verified",
                    productNativeRoute
                        ? "ChestLocatorEnhancer ProductNative exact owner -> ArchiveDataHandle.GetAvailableInventories -> CountItem/CostItem"
                        : "Frozen IChestLocatorEnhancerApi -> ArchiveDataHandle.GetAvailableInventories -> CountItem/CostItem",
                    summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke chest locator enhancer exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ChestLocatorEnhancer", "failed", "ChestLocatorEnhancer ProductNative or frozen compatibility route", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (transientCase != null)
                    TryRemoveTransientEquipmentForFixture(targetRoom, transientCase);
                if (legacyCompatibilityRegistered)
                    ChestLocatorEnhancerService?.RemoveOwner(qaOwnerId, "G5 fixture terminal cleanup");
            }
        }

        private static ManifestModel CreateChestLocatorSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Chest Locator Enhancer Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.QA.G5.ChestLocatorEnhancer",
                Type = "Smoke"
            };
        }

        private static string FormatChestLocatorState(ChestLocatorEnhancerState state)
        {
            if (state == null)
                return "unknown";
            return "owner=" + state.OwnerId +
                ", status=" + state.Status +
                ", enabled=" + state.Enabled +
                ", hook=" + state.HookInstalled +
                ", applications=" + state.ExtensionApplications +
                ", base=" + state.LastBaseInventoryCount +
                ", appended=" + state.LastAppendedInventoryCount +
                ", roots=" + state.LastScannedRootCount +
                ", equipment=" + state.LastScannedEquipmentCount +
                ", cases=" + state.LastSharedCaseCount +
                ", storageBoxes=" + state.LastSharedStorageBoxCount +
                ", message=" + state.LastMessage;
        }
    }
}
