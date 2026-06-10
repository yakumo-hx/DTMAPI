using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void TryExerciseStrongPlantingGunForSmoke()
        {
            object? transientBasin = null;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            Type? dolocApiForCleanup = null;

            try
            {
                StrongPlantingGunService? strongPlantingGunService = StrongPlantingGunService;
                if (strongPlantingGunService == null)
                    throw new InvalidOperationException("StrongPlantingGun feature service is not available.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new InvalidOperationException("DolocAPI was not available.");
                dolocApiForCleanup = dolocApi;

                IStrongPlantingGunApi api = strongPlantingGunService;
                ManifestModel owner = CreateStrongPlantingGunSmokeManifest();
                StrongPlantingGunRegisterResult register = api.Register(owner, new StrongPlantingGunOptions
                {
                    Enabled = true,
                    SlotCount = 3,
                    IncludeSeeds = true,
                    IncludeFilms = true,
                    IncludeFertilizers = true,
                    IncludeWater = false,
                    VerboseLogging = true
                });
                if (!register.Success)
                    throw new InvalidOperationException("Register failed: " + register.FailureReason + ": " + register.Message);
                if (!register.ToolHookInstalled || !register.UiHookInstalled)
                    throw new InvalidOperationException("StrongPlantingGun hooks were not installed. toolHook=" + register.ToolHookInstalled + ", uiHook=" + register.UiHookInstalled + ", message=" + register.Message);

                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                    throw new InvalidOperationException("CurrentRoom was not available.");

                transientBasin = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (transientBasin == null)
                    throw new InvalidOperationException("No PlantBasin target available. source=" + basinSource);

                object? gun = GenerateItemForSmoke(dolocApi, "farming_gun", 1);
                if (gun == null || !IsTypeOrBase(gun.GetType(), "DolocTown.ItemFarmingGun"))
                    throw new InvalidOperationException("Could not generate official farming_gun item.");

                strongPlantingGunService.ExpandFarmingGunInventoryIfNeeded(gun, "StrongPlantingGun smoke");
                object? gunInventory = ReadMember(gun, "inventory");
                if (gunInventory == null)
                    throw new InvalidOperationException("Generated farming gun did not expose inventory.");

                int inventoryCapacity = ReadIntMember(gunInventory, "capacity", 0);
                int totalCapacity = ReadIntMember(gun, "totalCapacity", 0);
                int lineCapacity = ReadIntMember(gun, "lineCapacity", 0);
                if (inventoryCapacity < 3 || totalCapacity < 3 || lineCapacity < 3)
                    throw new InvalidOperationException("Farming gun capacity was not expanded to three visible slots. inventory=" + inventoryCapacity + ", total=" + totalCapacity + ", line=" + lineCapacity + ".");

                object? seed = FindStrongPlantingGunSeedForSmoke(dolocApi, transientBasin, out string seedSummary);
                object? film = GenerateItemForSmoke(dolocApi, "plastic_film", 1);
                object? fertilizer = GenerateItemForSmoke(dolocApi, "fertilizer", 1);
                if (seed == null || film == null || fertilizer == null)
                    throw new InvalidOperationException("Could not generate seed/film/fertilizer. seed={" + seedSummary + "}, film=" + (film != null) + ", fertilizer=" + (fertilizer != null));

                bool seedPlaced = SwapInventoryItemAtForSmoke(gunInventory, 0, seed, out string seedPlace);
                bool filmPlaced = SwapInventoryItemAtForSmoke(gunInventory, 1, film, out string filmPlace);
                bool fertilizerPlaced = SwapInventoryItemAtForSmoke(gunInventory, 2, fertilizer, out string fertilizerPlace);
                if (!seedPlaced || !filmPlaced || !fertilizerPlaced)
                    throw new InvalidOperationException("Could not place strong planting gun contents. seed={" + seedPlace + "}, film={" + filmPlace + "}, fertilizer={" + fertilizerPlace + "}");

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, gun, quickSlot, out inventory, out originalSlotItem, out string quickSlotSummary))
                    throw new InvalidOperationException("Could not quick-slot generated farming gun. " + quickSlotSummary);

                object? selectedGun = ReadStaticMember(dolocApi, "SelectedItem");
                if (selectedGun == null || !IsTypeOrBase(selectedGun.GetType(), "DolocTown.ItemFarmingGun"))
                    throw new InvalidOperationException("SelectedItem was not the generated farming gun after quick-slot placement. selected=" + (selectedGun == null ? "null" : selectedGun.GetType().FullName));

                if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, transientBasin, out string tipSummary))
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
                StrongPlantingGunState state = api.GetState(owner.UniqueID);

                if (!planted || !protectedByFilm || !fertilized || state.LastSeedActions <= 0 || state.LastFilmActions <= 0 || state.LastFertilizerActions <= 0)
                    throw new InvalidOperationException("Strong planting gun did not apply all three visible actions. planted=" + planted + ", protected=" + protectedByFilm + ", fertilized=" + fertilized + ", state={" + FormatStrongPlantingGunState(state) + "}");

                string summary = "seed={" + seedSummary + "}" +
                    ", basin={" + DescribeEquipmentForSmoke(transientBasin) + " source=" + basinSource + "}" +
                    ", capacities=inventory:" + inventoryCapacity + "/total:" + totalCapacity + "/line:" + lineCapacity +
                    ", counts=seed:" + beforeSeedCount + "->" + afterSeedCount +
                    ", film:" + beforeFilmCount + "->" + afterFilmCount +
                    ", fertilizer:" + beforeFertilizerCount + "->" + afterFertilizerCount +
                    ", basinState=planted:" + planted + ",protected:" + protectedByFilm + ",fertilized:" + fertilized +
                    ", quickSlot={" + quickSlotSummary + "}" +
                    ", tip={" + tipSummary + "}" +
                    ", state={" + FormatStrongPlantingGunState(state) + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise StrongPlantingGun OK " + summary);
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "verified", "IStrongPlantingGunApi -> ItemFarmingGun.OnUseAsTool", summary);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke strong planting gun exercise failed.", ex.InnerException.ToString());
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "failed", "IStrongPlantingGunApi", ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke strong planting gun exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "failed", "IStrongPlantingGunApi", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (dolocApiForCleanup != null)
                    RestoreSmokeQuickSlot(dolocApiForCleanup, inventory, quickSlot, originalSlotItem);
                if (transientBasin != null && dolocApiForCleanup != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApiForCleanup, "CurrentRoom"), transientBasin);
            }
        }

        private static ManifestModel CreateStrongPlantingGunSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Strong Planting Gun Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.StrongPlantingGunMod",
                Type = "Smoke"
            };
        }

        private object? FindStrongPlantingGunSeedForSmoke(Type dolocApi, object basin, out string summary)
        {
            string basinSeedType = ReadPlantBasinSeedTypeForSmoke(basin);
            var notes = new List<string>();
            foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin", "seed_wheat" })
            {
                object? seed = GenerateItemForSmoke(dolocApi, seedId, 1);
                if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                {
                    notes.Add(seedId + ":missing");
                    continue;
                }

                WriteBoolMember(seed, "enablePlantInInvalidSeason", true);
                string seedType = ReadSeedTypeForSmoke(seed);
                notes.Add(seedId + ":" + seedType);
                if (seedType.Equals(basinSeedType, StringComparison.OrdinalIgnoreCase))
                {
                    summary = "selected=" + seedId + ", seedType=" + seedType + ", basinSeedType=" + basinSeedType + ", candidates=" + string.Join("|", notes);
                    return seed;
                }
            }

            summary = "basinSeedType=" + basinSeedType + ", candidates=" + string.Join("|", notes);
            return null;
        }

        private static bool SwapInventoryItemAtForSmoke(object inventory, int slot, object item, out string summary)
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

        private static string FormatStrongPlantingGunState(StrongPlantingGunState state)
        {
            if (state == null)
                return "unknown";
            return "owner=" + state.OwnerId +
                ", status=" + state.Status +
                ", enabled=" + state.Enabled +
                ", slots=" + state.SlotCount +
                ", toolHook=" + state.ToolHookInstalled +
                ", uiHook=" + state.UiHookInstalled +
                ", expanded=" + state.ExpandedGunCount +
                ", visited=" + state.LastVisitedEquipmentCount +
                ", seed=" + state.LastSeedActions +
                ", film=" + state.LastFilmActions +
                ", fertilizer=" + state.LastFertilizerActions +
                ", water=" + state.LastWaterActions +
                ", consumed=" + state.LastConsumedItemCount +
                ", message=" + state.LastMessage;
        }
    }
}
