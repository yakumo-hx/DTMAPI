using System;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void TryExerciseChestLocatorEnhancerForSmoke()
        {
            object? transientCase = null;
            object? targetRoom = null;
            try
            {
                ChestLocatorEnhancerService? chestLocatorService = ChestLocatorEnhancerService;
                if (chestLocatorService == null)
                    throw new InvalidOperationException("ChestLocatorEnhancer feature service is not available.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new InvalidOperationException("DolocAPI was not available.");

                IChestLocatorEnhancerApi api = chestLocatorService;
                ManifestModel owner = CreateChestLocatorSmokeManifest();
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
                if (!register.HookInstalled)
                    throw new InvalidOperationException("ArchiveDataHandle.GetAvailableInventories hook was not installed. message=" + register.Message);

                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                if (archive == null)
                    throw new InvalidOperationException("archiveHandle was not available.");
                targetRoom = FindChestLocatorSmokeBuildingRoom(dolocApi, archive, out string roomSummary);
                if (targetRoom == null)
                    throw new InvalidOperationException("No building room was available for cross-room chest locator smoke. " + roomSummary);

                string itemId = SelectZeroBaselineSmokeItemId(dolocApi, new[] { "dtmapi_mine", "crude_oil", "case_locator", "recipe_case_locator", "sunmao_showcase", "mountain_showcase" }, out int baseline, out string itemSummary);
                if (string.IsNullOrWhiteSpace(itemId))
                    throw new InvalidOperationException("No zero-baseline generated item was available for safe consume smoke. " + itemSummary);

                transientCase = TryCreateTransientEquipmentNoRenderForSmoke(dolocApi, targetRoom, "DolocTown.Case", new[] { "wooden_case", "large_wooden_case" }, out string caseSummary);
                if (transientCase == null)
                    throw new InvalidOperationException("Failed to create transient shared Case. " + caseSummary);
                if (!SetMemberValue(transientCase, "IsShared", true))
                    throw new InvalidOperationException("Failed to set transient Case IsShared=true. " + DescribeEquipmentForSmoke(transientCase));

                object? inventory = ReadMember(transientCase, "inventory");
                MethodInfo? placeItem = inventory == null ? null : FindMethod(inventory.GetType(), "PlaceItem", 1);
                object? item = GenerateItemForSmoke(dolocApi, itemId, 3);
                if (inventory == null || placeItem == null || item == null)
                    throw new InvalidOperationException("Failed to prepare transient inventory item. inventory=" + (inventory != null) + ", placeItem=" + (placeItem != null) + ", item=" + (item != null));
                object? leftover = placeItem.Invoke(inventory, new[] { item });
                if (leftover != null)
                    throw new InvalidOperationException("Transient Case inventory did not accept " + itemId + "; leftover=" + leftover.GetType().FullName);

                int afterPlace = CountNativeItemForSmoke(dolocApi, itemId, checkBox: true);
                if (afterPlace < baseline + 3)
                    throw new InvalidOperationException("CountItem did not include transient shared Case. item=" + itemId + ", baseline=" + baseline + ", afterPlace=" + afterPlace + ", bridge={" + chestLocatorService.LastChestLocatorEnhancerSummary + "}");

                bool cost = CostNativeItemForSmoke(dolocApi, itemId, 2, checkBox: true);
                int afterCost = CountNativeItemForSmoke(dolocApi, itemId, checkBox: true);
                if (!cost || afterCost < baseline + 1 || afterCost > baseline + 1)
                    throw new InvalidOperationException("CostItem did not consume through shared inventory array. item=" + itemId + ", cost=" + cost + ", baseline=" + baseline + ", afterPlace=" + afterPlace + ", afterCost=" + afterCost + ", bridge={" + chestLocatorService.LastChestLocatorEnhancerSummary + "}");

                ChestLocatorEnhancerState state = api.GetState(owner.UniqueID);
                if (state.LastAppendedInventoryCount <= 0 || state.LastSharedCaseCount <= 0)
                    throw new InvalidOperationException("Bridge state did not record appended shared Case inventory. state=" + FormatChestLocatorState(state));

                string summary = "item=" + itemId +
                    ", baseline=" + baseline +
                    ", afterPlace=" + afterPlace +
                    ", afterCost=" + afterCost +
                    ", room={" + roomSummary + "}" +
                    ", case={" + caseSummary + "}" +
                    ", bridge={" + chestLocatorService.LastChestLocatorEnhancerSummary + "}" +
                    ", state={" + FormatChestLocatorState(state) + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise ChestLocatorEnhancer OK " + summary);
                runtime.SetHookStatus("Smoke.ChestLocatorEnhancer", "verified", "IChestLocatorEnhancerApi -> ArchiveDataHandle.GetAvailableInventories -> CountItem/CostItem", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke chest locator enhancer exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ChestLocatorEnhancer", "failed", "IChestLocatorEnhancerApi", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (transientCase != null)
                    TryRemoveTransientEquipmentForSmoke(targetRoom, transientCase);
            }
        }

        private static ManifestModel CreateChestLocatorSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Chest Locator Enhancer Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.ChestLocatorEnhancerMod",
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
