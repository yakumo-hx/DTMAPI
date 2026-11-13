using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;
using DTMAPI.ModConfigMenu;
using UnityEngine;

public static partial class DolocAPI
{
    public static Vector2Int? CropMaturityPlayerCell;
}

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void DebugConsoleCropCircleAndFamilies()
        {
            Assert(new DebugConsoleConfig().CropMaturityRadius == 25 &&
                DebugConsoleConfig.NormalizeCropRadius(4) == 5 && DebugConsoleConfig.NormalizeCropRadius(101) == 100 &&
                DebugConsoleConfig.NormalizeCropRadius(double.NaN) == 25, "Radius defaults/reset and integer bounds.");
            WithCropActions((actions, owner, context, room) =>
            {
                var normal = CropBasin(room, "crop", 1, 3, 4, new CropTestCrop());
                var corner = CropBasin(room, "crop", 2, 4, 4, new CropTestCrop());
                var multi = CropBasin(room, "tree", 3, 99, 99, new CropTestTree());
                multi.CoveredPositions = new[] { new Vector2Int(5, 0), new Vector2Int(6, 0) };
                var anchorOnly = CropBasin(room, "crop", 4, 0, 5, new CropTestCrop());
                anchorOnly.CoveredPositions = null;
                var noFallback = CropBasin(room, "crop", 5, 0, 0, new CropTestCrop());
                noFallback.CoveredPositions = new[] { new Vector2Int(6, 0) };
                var dead = CropBasin(room, "crop", 6, 1, 0, new CropTestCrop { isDead = true });
                var already = CropBasin(room, "crop", 7, 2, 0, new CropTestCrop { CurrentLevel = 4, isMature = true });
                CropBasin(room, "crop", 8, 0, 1, null);
                var grass = CropBasin(room, "grass", 9, 0, 2, null);
                grass.crops = new CropTestGrass?[] { new CropTestGrass(), new CropTestGrass { currentLevel = 3, IsMature = true }, null, new CropTestGrass() };
                var timber = CropBasin(room, "tree", 10, 1, 1, new CropTestTree { ShouldRepeat = false, HasTreeCrop = false });
                var deadTree = CropBasin(room, "tree", 11, 1, 2, new CropTestTree { currentHealth = 0 });
                var child = new CropTestRoom();
                room.DM_building.Buildings.Add(new { room = child });
                var foreign = CropBasin(child, "crop", 1, 0, 0, new CropTestCrop());
                room.DM_equipment.AllEquipments.Add(foreign); // Even polluted membership cannot mix coordinate domains.
                room.DM_equipment.AllEquipments.Add(normal); // Reference de-duplication.
                CropBasin(room, "crop", 12, int.MinValue, int.MaxValue, new CropTestCrop());
                RoomCropMaturityResult result = actions.MatureAllCrops(owner, 5);
                Assert(result.Success && result.CandidateBasins == 9 && result.GrassBasins == 1 && result.Targets.Count == 12,
                    "One-room circle must include a 3-4-5 boundary, multi-cell intersection, and absent-coverage anchor without square/diamond/recursive semantics.");
                Assert(result.CropsMatured == 4 && result.GrassMatured == 2 && result.AlreadyMature == 2 && result.EmptyOrDead == 4,
                    "Count real crop/tree transitions and every grass slot separately; empty/dead and already mature are not new.");
                Assert(((CropTestCrop)normal.Crop!).Calls == 1 && ((CropTestCrop)normal.Crop!).Rendered &&
                    ((CropTestCrop)corner.Crop!).Calls == 0 && ((CropTestCrop)noFallback.Crop!).Calls == 0 &&
                    ((CropTestCrop)foreign.Crop!).Calls == 0 && ((CropTestCrop)dead.Crop!).Calls == 0 &&
                    ((CropTestTree)deadTree.Crop!).Calls == 0, "Only correct live references may receive exact native methods.");
                Assert(result.Targets.Single(t => t.EquipmentId == 3).FruitReady == true &&
                    result.Targets.Single(t => t.EquipmentId == 10).FruitReady == false,
                    "A fully grown timber tree is not a fruit-harvestable tree.");
                Assert(actions.MatureAllCrops(owner, 5).CropsMatured == 0 && actions.MatureAllCrops(owner, 5).GrassMatured == 0,
                    "Repeated clicks must not count already mature plants again.");
                room.DM_equipment.AllEquipments.Clear();
                var at25 = CropBasin(room, "crop", 21, 15, 20, new CropTestCrop());
                var outside25 = CropBasin(room, "crop", 22, 16, 20, new CropTestCrop());
                var at100 = CropBasin(room, "crop", 23, 60, 80, new CropTestCrop());
                var outside100 = CropBasin(room, "crop", 24, 61, 80, new CropTestCrop());
                Assert(actions.MatureAllCrops(owner, 25).CropsMatured == 1, "Default circle includes exact radius25 only.");
                Assert(actions.MatureAllCrops(owner, 100).CropsMatured == 2 && ((CropTestCrop)outside100.Crop!).Calls == 0,
                    "Radius100 still uses the circular boundary.");
            });
        }

        private static void DebugConsoleCropPartialAndIdentity()
        {
            WithCropActions((actions, owner, context, room) =>
            {
                var first = CropBasin(room, "crop", 1, 0, 0, new CropTestCrop());
                var replaced = CropBasin(room, "crop", 2, 1, 0, new CropTestCrop());
                var changedCrop = CropBasin(room, "crop", 3, 2, 0, new CropTestCrop());
                var grass = CropBasin(room, "grass", 4, 3, 0, null);
                var grass0 = new CropTestGrass(); var grass1 = new CropTestGrass();
                grass.crops = new CropTestGrass?[] { grass0, grass1 };
                ((CropTestCrop)first.Crop!).AfterMutation = () =>
                {
                    room.DM_equipment.AllEquipments.Remove(replaced);
                    CropBasin(room, "crop", 2, 1, 0, new CropTestCrop());
                    changedCrop.Crop = new CropTestCrop();
                    grass.crops[1] = new CropTestGrass();
                    CropBasin(room, "crop", 5, 0, 0, new CropTestCrop());
                    DolocAPI.CropMaturityPlayerCell = new Vector2Int(100, 100);
                    throw new InvalidOperationException("after-growth-notification-failed");
                };
                RoomCropMaturityResult result = actions.MatureAllCrops(owner, 5);
                Assert(!result.Success && result.CropsMatured == 1 && result.GrassMatured == 1 && result.Failed == 1 && result.Invalidated == 3,
                    "Native post-write failure retains growth; reused ID, replaced crop and replaced grass member are invalid.");
                Assert(((CropTestCrop)replaced.Crop!).Calls == 0 && ((CropTestCrop)changedCrop.Crop!).Calls == 0 && grass1.Calls == 0 &&
                    ((CropTestCrop)room.DM_equipment.AllEquipments.Single(e => e.id == 5).Crop!).Calls == 0,
                    "The fixed batch must not rescan additions or follow replaced references or the new player position.");
            });
            foreach (string end in new[] { "room", "save", "owner" })
                WithCropActions((actions, owner, context, room) =>
                {
                    var first = CropBasin(room, "tree", 1, 0, 0, new CropTestTree());
                    var next = CropBasin(room, "crop", 2, 1, 0, new CropTestCrop());
                    ((CropTestTree)first.Crop!).AfterMutation = () =>
                    {
                        if (end == "room") DolocAPI.CurrentRoom = new CropTestRoom();
                        else if (end == "save") context.Epoch++;
                        else actions.EndSaveSession("owner-deactivation");
                        throw new InvalidOperationException("native-notification-ended-" + end);
                    };
                    RoomCropMaturityResult result = actions.MatureAllCrops(owner, 25);
                    Assert(result.CropsMatured == 1 && result.Failed == 1 && result.Unexecuted == 1 && !result.Success &&
                        ((CropTestCrop)next.Crop!).Calls == 0, "Save/room/Owner changes end remaining work without losing prior mutation.");
                });
            WithCropActions((actions, owner, context, room) =>
            {
                var grass = CropBasin(room, "grass", 1, 0, 0, null);
                grass.crops = new CropTestGrass?[] { new CropTestGrass { AfterMutation = () => throw new Exception("render") }, new CropTestGrass() };
                RoomCropMaturityResult result = actions.MatureAllCrops(owner, 5);
                Assert(result.GrassMatured == 2 && result.Failed == 1 && !result.Success, "Per-grass mutation retains partial result and processes other still-valid members once.");
                var broken = CropBasin(room, "crop", 2, 1, 0, new CropTestCrop { SuppressMutation = true });
                Assert(actions.MatureAllCrops(owner, 5).Failed == 1, "A native void return without maturity is a failure.");
            });
        }

        private static void WithCropActions(Action<DebugConsoleNativeActions, ManifestModel, CropTestContext, CropTestRoom> test)
        {
            object? oldArchive = DolocAPI.archiveHandle, oldRoom = DolocAPI.CurrentRoom;
            Vector2Int? oldPlayerCell = DolocAPI.CropMaturityPlayerCell;
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var actions = new DebugConsoleNativeActions(new DebugConsoleTestRuntime(runtime));
            var context = new CropTestContext(); var room = new CropTestRoom();
            try
            {
                DolocAPI.archiveHandle = new ModeTestArchive(); DolocAPI.CurrentRoom = room;
                DolocAPI.CropMaturityPlayerCell = new Vector2Int(0, 0);
                actions.SetCropMaturityContext(context); actions.BeginSaveSession();
                test(actions, new ManifestModel { UniqueID = "DTMAPI.DebugConsoleMod" }, context, room);
            }
            finally { actions.EndSaveSession("test"); DolocAPI.archiveHandle = oldArchive; DolocAPI.CurrentRoom = oldRoom; DolocAPI.CropMaturityPlayerCell = oldPlayerCell; }
        }

        private static readonly Dictionary<string, Type> CropFixtureTypes = CreateCropFixtureTypes();
        private static Dictionary<string, Type> CreateCropFixtureTypes()
        {
            // Reuse the shared DolocAPI/Vector2Int fixture. Local derived shapes avoid changing other suites' minimal basin types.
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Y21CropShapes"), AssemblyBuilderAccess.Run).DefineDynamicModule("Y21");
            return new Dictionary<string, Type>
            {
                ["crop"] = module.DefineType("DolocTown.PlantBasin", TypeAttributes.Public, typeof(CropTestEquipment)).CreateType()!,
                ["tree"] = module.DefineType("DolocTown.PlantBasinTree", TypeAttributes.Public, typeof(CropTestEquipment)).CreateType()!,
                ["grass"] = module.DefineType("DolocTown.PlantBasinGrass", TypeAttributes.Public, typeof(CropTestEquipment)).CreateType()!
            };
        }
        private static CropTestEquipment CropBasin(CropTestRoom room, string family, int id, int x, int y, object? crop)
        {
            var basin = (CropTestEquipment)Activator.CreateInstance(CropFixtureTypes[family])!;
            basin.id = id; basin.CurrentRoom = room; basin.Anchor = new Vector2Int(x, y);
            basin.CoveredPositions = new[] { basin.Anchor }; basin.Crop = crop;
            room.DM_equipment.AllEquipments.Add(basin); return basin;
        }
    }

    public class CropTestEquipment
    {
        public int id;
        public CropTestRoom CurrentRoom = null!;
        public bool IsRemoved, IsRender = true;
        public Vector2Int Anchor;
        public Vector2Int[]? CoveredPositions;
        public object? Crop;
        public CropTestGrass?[]? crops;
    }
    public sealed class CropTestRoom
    {
        public Vector2Int AgentPositionCell = new Vector2Int(777, 777); // A stale native room cache must not select the request center.
        public CropTestManager DM_equipment = new CropTestManager();
        public CropTestBuildings DM_building = new CropTestBuildings();
    }
    public sealed class CropTestBuildings { public List<object> Buildings = new List<object>(); }
    public sealed class CropTestManager
    {
        public List<CropTestEquipment> AllEquipments = new List<CropTestEquipment>();
        // Authored positional fixture: the real native getter is a list index, not an id lookup.
        public CropTestEquipment GetEquipment(int position) => AllEquipments[position];
    }
    public sealed class CropTestSeed { public int MatureLevel = 4; }
    public sealed class CropTestTreeProto { public int MaxLevel = 7; }
    // Authored stubs expose observable actions/faults only, not the game's growth or renderer implementation.
    public sealed class CropTestCrop
    {
        public CropTestSeed seedProto = new CropTestSeed();
        public int CurrentLevel, Calls;
        public bool isDead, isMature, Rendered, SuppressMutation;
        public Action? AfterMutation;
        public void DEBUG_SetLevel(bool shouldRender, int level)
        { Calls++; Rendered = shouldRender; if (!SuppressMutation) { CurrentLevel = level; isMature = true; } AfterMutation?.Invoke(); }
    }
    public sealed class CropTestTree
    {
        public CropTestTreeProto protoTree = new CropTestTreeProto();
        public int CurrentLevel, Calls, currentHealth = 10;
        public bool isMature, HasTreeCrop = true, ShouldRepeat = true;
        public Action? AfterMutation;
        public void DEBUG_SetLevel(int level) { Calls++; CurrentLevel = level; isMature = true; AfterMutation?.Invoke(); }
    }
    public sealed class CropTestGrass
    {
        public CropTestSeed seedProto = new CropTestSeed { MatureLevel = 3 };
        public int currentLevel, Calls;
        public bool IsMature;
        public Action? AfterMutation;
        public void DEBUG_SetLevel(int level) { Calls++; currentLevel = level; IsMature = true; AfterMutation?.Invoke(); }
    }
    public sealed class CropTestContext : IDtmRuntimeContext
    {
        public long Epoch = 1;
        public DtmRuntimeContextSnapshot Snapshot => new DtmRuntimeContextSnapshot(Guid.Empty, Epoch, DtmRuntimePhase.WorldReady, Epoch, Epoch);
        public bool IsMainThread => true;
        public IDisposable Subscribe(Action<DtmRuntimeContextSnapshot> changed) => throw new NotSupportedException();
    }
}
