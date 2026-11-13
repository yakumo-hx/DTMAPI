using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void CropHarvestingUsesNativeMaturityAndCallerLimit()
        {
            string? previousRoot = UseTempPersistentRoot();
            object? previousArchive = DolocAPI.archiveHandle;
            object? previousRoom = DolocAPI.CurrentRoom;
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var service = new CropHarvestingService(runtime);
                var owner = new ManifestModel { UniqueID = "DTMAPI.Tests.CropHarvest", Name = "Crop harvest test", Version = "1.0.0" };
                var farm = new HarvestTestRoom();
                var child = new HarvestTestRoom();
                farm.DM_building.Buildings.Add(new HarvestTestBuilding { room = child });
                // An ordinary room can have a misleading name; only the native farm root authorizes traversal.
                DolocAPI.CurrentRoom = farm;
                DolocAPI.archiveHandle = new HarvestTestArchive();
                AddHarvestBasin(farm, mature: true);
                Assert(service.HarvestMatureCrops(owner, new CropHarvestRequest()).FailureReason == "no-farm-scope",
                    "A missing native farm root must not be replaced by a room-name guess.");
                farm.DM_equipment.AllEquipments.Clear();
                DolocAPI.archiveHandle = new HarvestTestArchive { MainFarm = farm };

                var mature = AddHarvestBasin(farm, mature: true);
                mature.Name = "藤 mushroom 灌木";
                var immature = AddHarvestBasin(farm, mature: false);
                var dead = AddHarvestBasin(farm, mature: true, dead: true);
                var empty = AddHarvestBasin(farm, mature: false);
                empty.Crop = null;
                var indoor = AddHarvestBasin(child, mature: true);
                CropHarvestResult preview = service.ScanMatureCrops(owner, new CropHarvestRequest());
                Assert(preview.Success && preview.DryRun && preview.MatureTargetsFound == 2 && preview.RoomsVisited == 2 &&
                    mature.HarvestCalls == 0 && indoor.HarvestCalls == 0,
                    "Preview recognizes only live mature crops across the native farm and building rooms, without harvesting.");
                CropHarvestResult harvested = service.HarvestMatureCrops(owner, new CropHarvestRequest { SendNativeMessage = false });
                Assert(harvested.Success && harvested.HarvestedCount == 2 && mature.HarvestCalls == 1 && indoor.HarvestCalls == 1 &&
                    immature.HarvestCalls == 0 && dead.HarvestCalls == 0 && empty.HarvestCalls == 0 && !mature.LastSendMessage,
                    "Native Harvest receives mature live crops and the requested message flag; CouldHarvest cannot admit a dead or immature crop.");
                Assert(harvested.Targets.Single(t => t.EquipmentName == mature.Name).Kind == CropHarvestTargetKind.OrdinaryCrop,
                    "Crop names and language keywords do not create guessed families.");

                farm.DM_equipment.AllEquipments.Clear();
                child.DM_equipment.AllEquipments.Clear();
                for (int i = 0; i < 210; i++) AddHarvestBasin(farm, mature: true);
                CropHarvestResult limited = service.HarvestMatureCrops(owner, new CropHarvestRequest { MaxHarvests = 205 });
                Assert(limited.Success && limited.HarvestedCount == 205,
                    "A positive caller limit above 200 must not be silently clamped by the platform.");
                Assert(service.HarvestMatureCrops(owner, new CropHarvestRequest()).HarvestedCount == 5,
                    "An omitted limit harvests every remaining eligible crop.");
                farm.DM_equipment.AllEquipments.Clear();
                for (int i = 0; i < 210; i++) AddHarvestBasin(farm, mature: true);
                Assert(service.HarvestMatureCrops(owner, new CropHarvestRequest()).HarvestedCount == 210,
                    "The default request has neither a 24-target default nor a 200-target cap.");

                farm.DM_equipment.AllEquipments.Clear();
                var first = AddHarvestBasin(farm, mature: true);
                var removed = AddHarvestBasin(farm, mature: true);
                first.AfterHarvest = () => farm.DM_equipment.AllEquipments.Remove(removed);
                Assert(service.HarvestMatureCrops(owner, new CropHarvestRequest()).HarvestedCount == 1 && removed.HarvestCalls == 0,
                    "A target removed by an earlier native operation is skipped before Harvest.");
                farm.DM_equipment.AllEquipments.Clear();
                first = AddHarvestBasin(farm, mature: true);
                var next = AddHarvestBasin(farm, mature: true);
                first.AfterHarvest = () => DolocAPI.CurrentRoom = child;
                CropHarvestResult changedRoom = service.HarvestMatureCrops(owner, new CropHarvestRequest());
                Assert(!changedRoom.Success && changedRoom.FailureReason == "save-or-room-changed" &&
                    changedRoom.HarvestedCount == 1 && next.HarvestCalls == 0,
                    "A native callback that changes the active room ends the remaining batch and preserves completed results.");
            }
            finally
            {
                DolocAPI.archiveHandle = previousArchive;
                DolocAPI.CurrentRoom = previousRoom;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static DolocTown.PlantBasin AddHarvestBasin(HarvestTestRoom room, bool mature, bool dead = false)
        {
            var basin = new DolocTown.PlantBasin
            {
                CurrentRoom = room,
                CouldHarvest = true,
                Crop = new HarvestTestCrop { isMature = mature, isDead = dead }
            };
            room.DM_equipment.AllEquipments.Add(basin);
            return basin;
        }
    }
}

public static partial class DolocAPI
{
    public static object? CurrentRoom;
}

public sealed class HarvestTestArchive
{
    public HarvestTestRoom? MainFarm { get; set; }
}

public sealed class HarvestTestRoom
{
    public string Name => "farm";
    public HarvestTestEquipmentManager DM_equipment { get; } = new HarvestTestEquipmentManager();
    public HarvestTestBuildingManager DM_building { get; } = new HarvestTestBuildingManager();
}

public sealed class HarvestTestEquipmentManager
{
    public List<object> AllEquipments { get; } = new List<object>();
}

public sealed class HarvestTestBuildingManager
{
    public List<HarvestTestBuilding> Buildings { get; } = new List<HarvestTestBuilding>();
}

public sealed class HarvestTestBuilding
{
    public HarvestTestRoom room { get; set; } = null!;
}

public sealed class HarvestTestCrop
{
    public bool isMature { get; set; }
    public bool isDead { get; set; }
    public bool hasHarvested { get; set; }
    public string SeedId => "seed";
}

namespace DolocTown
{
    public sealed partial class PlantBasin
    {
        public HarvestTestCrop? Crop { get; set; }
        public object? CurrentRoom { get; set; }
        public bool IsRemoved { get; set; }
        public string Name { get; set; } = "basin";
        public int HarvestCalls { get; private set; }
        public bool LastSendMessage { get; private set; }
        public Action? AfterHarvest { get; set; }
        public void Harvest(bool showDrop, bool sendMessage)
        {
            HarvestCalls++;
            LastSendMessage = sendMessage;
            Crop = null;
            CouldHarvest = false;
            AfterHarvest?.Invoke();
        }
    }
}
