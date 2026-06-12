using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void TryExerciseCropHarvestingApiForSmoke()
        {
            object? transientBasin = null;
            Type? dolocApiForCleanup = null;

            try
            {
                CropHarvestingService? cropHarvestingService = CropHarvestingService;
                if (cropHarvestingService == null)
                    throw new InvalidOperationException("CropHarvesting feature service is not available.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new InvalidOperationException("DolocAPI was not available.");
                dolocApiForCleanup = dolocApi;

                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                    throw new InvalidOperationException("CurrentRoom was not available.");

                transientBasin = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (transientBasin == null)
                    throw new InvalidOperationException("No PlantBasin target available. source=" + basinSource);

                if (!TryPlantAndMatureTransientCropForHarvestingSmoke(dolocApi, transientBasin, out string setupSummary, out string matureSummary))
                    throw new InvalidOperationException("Could not plant and mature the transient crop. " + setupSummary);

                string targetId = "equipment:" + RuntimeHelpers.GetHashCode(transientBasin).ToString(CultureInfo.InvariantCulture);
                ICropHarvestingApi api = cropHarvestingService;
                ManifestModel owner = CreateCropHarvestingSmokeManifest();
                var request = new CropHarvestRequest
                {
                    IncludeOrdinaryCrops = true,
                    IncludeVines = true,
                    IncludeMushroomBags = true,
                    IncludeBushes = true,
                    IncludeTreeBasinCrops = false,
                    MaxHarvests = 1,
                    SendNativeMessage = false,
                    VerboseLogging = true,
                    TargetIds = new[] { targetId }
                };

                var noTargetRequest = new CropHarvestRequest
                {
                    IncludeOrdinaryCrops = true,
                    IncludeVines = true,
                    IncludeMushroomBags = true,
                    IncludeBushes = true,
                    IncludeTreeBasinCrops = false,
                    MaxHarvests = 1,
                    SendNativeMessage = false,
                    VerboseLogging = true,
                    TargetIds = new[] { "equipment:missing-crop-harvesting-smoke" }
                };
                CropHarvestResult noTargetScan = api.ScanMatureCrops(owner, noTargetRequest);
                if (!noTargetScan.Success || noTargetScan.MatureTargetsFound != 0 || noTargetScan.FailedCount != 0)
                    throw new InvalidOperationException("Crop harvesting no-target scan should be a successful no-op. result={" + FormatCropHarvestResult(noTargetScan) + "}");

                CropHarvestResult scan = api.ScanMatureCrops(owner, request);
                if (!scan.Success || scan.MatureTargetsFound <= 0 || !scan.Targets.Any(target => target.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase) && target.Status == CropHarvestTargetStatus.Pending))
                    throw new InvalidOperationException("Crop harvesting scan did not find the transient mature target. targetId=" + targetId + ", result={" + FormatCropHarvestResult(scan) + "}");

                CropHarvestResult harvest = api.HarvestMatureCrops(owner, request);
                object? afterCrop = ReadMember(transientBasin, "crop") ?? ReadMember(transientBasin, "Crop");
                bool afterMature = afterCrop != null && ReadBoolMember(afterCrop, "isMature", false);
                bool afterCouldHarvest = ReadBoolMember(transientBasin, "CouldHarvest", false) || ReadBoolMember(transientBasin, "IsCropMature", false);
                if (!harvest.Success || harvest.HarvestedCount <= 0 || afterMature || afterCouldHarvest)
                    throw new InvalidOperationException("Crop harvesting API did not harvest the transient target. targetId=" + targetId + ", afterMature=" + afterMature + ", afterCouldHarvest=" + afterCouldHarvest + ", result={" + FormatCropHarvestResult(harvest) + "}");

                BridgeFeatureStatus status = api.GetStatus(owner.UniqueID);
                string summary = "targetId=" + targetId +
                    ", basin={" + DescribeEquipmentForSmoke(transientBasin) + " source=" + basinSource + "}" +
                    ", setup={" + setupSummary + "}" +
                    ", mature={" + matureSummary + "}" +
                    ", noTargetScan={" + FormatCropHarvestResult(noTargetScan) + "}" +
                    ", scan={" + FormatCropHarvestResult(scan) + "}" +
                    ", harvest={" + FormatCropHarvestResult(harvest) + "}" +
                    ", status=" + status.Status +
                    ", details=" + status.Details;
                runtime.RuntimeMonitor.Log("Smoke exercise CropHarvestingApi OK " + summary);
                runtime.SetHookStatus("Smoke.CropHarvestingApi", "verified", "ICropHarvestingApi -> PlantBasin.Harvest", summary);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke crop harvesting API exercise failed.", ex.InnerException.ToString());
                runtime.SetHookStatus("Smoke.CropHarvestingApi", "failed", "ICropHarvestingApi", ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke crop harvesting API exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.CropHarvestingApi", "failed", "ICropHarvestingApi", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (transientBasin != null && dolocApiForCleanup != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApiForCleanup, "CurrentRoom"), transientBasin);
            }
        }

        private bool TryPlantAndMatureTransientCropForHarvestingSmoke(Type dolocApi, object basin, out string setupSummary, out string matureSummary)
        {
            setupSummary = string.Empty;
            matureSummary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            const int quickSlot = 0;

            try
            {
                object? anchor = ReadMember(basin, "Anchor");
                string selectSummary = "equipment anchor unavailable.";
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, basin, anchor, out selectSummary))
                {
                    setupSummary = "Could not select plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + (anchor == null ? "equipment anchor unavailable." : selectSummary);
                    return false;
                }

                var attempts = new System.Collections.Generic.List<string>();
                foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin", "seed_wheat" })
                {
                    object? seed = GenerateItemForSmoke(dolocApi, seedId, 3);
                    if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                    {
                        attempts.Add(seedId + ":missing");
                        continue;
                    }

                    if (!TryPlaceSmokeItemInQuickSlot(dolocApi, seed, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                    {
                        setupSummary = placeSummary;
                        return false;
                    }

                    if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, basin, out string tipSummary))
                    {
                        setupSummary = "Could not point item cell tip at plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + tipSummary;
                        return false;
                    }

                    object? selectedSeed = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedSeed == null || !IsTypeOrBase(selectedSeed.GetType(), "DolocTown.ItemSeed"))
                    {
                        setupSummary = "SelectedItem was not an ItemSeed after quick-slot placement. place={" + placeSummary + "}, selected=" + (selectedSeed == null ? "null" : selectedSeed.GetType().FullName);
                        return false;
                    }

                    WriteBoolMember(selectedSeed, "enablePlantInInvalidSeason", true);
                    MethodInfo? plantSeed = selectedSeed.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "PlantSeed" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(basin));
                    if (plantSeed == null)
                    {
                        setupSummary = "ItemSeed.PlantSeed(PlantBasin) was not found.";
                        return false;
                    }

                    plantSeed.Invoke(selectedSeed, new[] { basin });
                    if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                    {
                        setupSummary = "Native planting setup did not reach AgentStateInteract.OnExit. " + interactSummary;
                        return false;
                    }

                    object? crop = ReadMember(basin, "crop") ?? ReadMember(basin, "Crop");
                    if (crop != null && TrySetCropMatureForSmoke(crop, out matureSummary))
                    {
                        setupSummary = "seed=" + seedId +
                            ", seedType=" + ReadSeedTypeForSmoke(selectedSeed) +
                            ", basinSeedType=" + ReadPlantBasinSeedTypeForSmoke(basin) +
                            ", place={" + placeSummary + "}" +
                            ", tip={" + tipSummary + "}" +
                            ", nativeInteract={" + interactSummary + "}";
                        return true;
                    }

                    attempts.Add(seedId + ":no mature crop (" + (crop == null ? "missing crop" : matureSummary) + ")");
                    RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                    inventory = null;
                    originalSlotItem = null;
                    TryEnterIdleStateForSmoke(dolocApi);
                }

                setupSummary = "No seed candidate produced a mature PlantBasin crop. target=" + DescribeEquipmentForSmoke(basin) +
                    ", basinSeedType=" + ReadPlantBasinSeedTypeForSmoke(basin) +
                    ", attempts=" + string.Join("|", attempts);
                return false;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                setupSummary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                return false;
            }
            catch (Exception ex)
            {
                setupSummary = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            finally
            {
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                TryEnterIdleStateForSmoke(dolocApi);
            }
        }

        private static ManifestModel CreateCropHarvestingSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Crop Harvesting Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.Smoke.CropHarvesting",
                Type = "Smoke"
            };
        }

        private static string FormatCropHarvestResult(CropHarvestResult result)
        {
            if (result == null)
                return "null";

            return "success=" + result.Success +
                ", dryRun=" + result.DryRun +
                ", rooms=" + result.RoomsVisited +
                ", basins=" + result.PlantBasinsVisited +
                ", mature=" + result.MatureTargetsFound +
                ", harvested=" + result.HarvestedCount +
                ", skipped=" + result.SkippedCount +
                ", failed=" + result.FailedCount +
                ", reason=" + result.FailureReason +
                ", message=" + result.Message;
        }
    }
}
