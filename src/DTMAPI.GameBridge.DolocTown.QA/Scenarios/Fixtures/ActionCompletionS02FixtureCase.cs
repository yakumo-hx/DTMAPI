using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private static object? oneActionDropTarget;
        private static int oneActionDropCalls;
        private static void ObserveOneActionNativeDrop(object __instance)
        {
            if (ReferenceEquals(__instance, oneActionDropTarget)) oneActionDropCalls++;
        }

        private void ExerciseOneActionAcceptedHitMatrixForFixture(Type api, Type toolType, Type rendererType)
        {
            object room = ReadStaticMember(api, "CurrentRoom") ?? throw new InvalidOperationException("No room.");
            object collider = FindToolColliderForFixture(api, toolType) ?? throw new InvalidOperationException("No tool collider.");
            MethodInfo handle = FindMethod(toolType, "HandleTools", 1)!;
            MethodInfo resetTool = FindMethod(toolType, "ResetTool", 1)!;
            MethodInfo resetCount = FindMethod(toolType, "ResetChopCounter", 1)!;
            MethodInfo resetEnergy = FindMethod(toolType, "ResetCostEnergyFlag", 0)!;
            object archive = ReadStaticMember(api, "archiveHandle")!;
            object agent = ReadMember(ReadMember(archive, "farmData")!, "agentData")!;
            int originalEnergy = ReadIntMember(agent, "energy", -1);
            int originalOverflow = ReadIntMember(agent, "overflowEnergy", -1);
            int cost = ReadIntMember(ReadStaticMember(api, "GlobalParameter")!, "ToolEnergyCost", 0);
            if (originalEnergy < 0 || originalOverflow < 0 || cost <= 0) throw new InvalidOperationException("Invalid energy fixture.");
            Type dataType = patcher!.ResolveType("DolocTown.ResourceFellData, Assembly-CSharp")!;
            object tables = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp")!.GetProperty("Tables")!.GetValue(null)!;
            var tools = new List<object>();
            foreach (object proto in (IEnumerable)ReadMember(ReadMember(tables, "TbItem")!, "DataList")!)
            {
                if (ReadMember(proto, "Function")?.GetType().Name != "ItemFunctionTool") continue;
                object? tool = GenerateItemForFixture(api, ReadStringMember(proto, "Id", ""));
                if (tool != null) tools.Add(tool);
            }
            object Data(object resource, object tool) => Activator.CreateInstance(dataType, new[] { resource, tool, ReadMember(resource, "PositionCenter")! })!;
            bool Flag(object data, string member) => ReadMember(data, member) is bool value && value;
            object Shape(object resource) => ReadMember(FindUnityObjects(rendererType).Single(r => ReferenceEquals(ReadMember(r, "DungeonResource"), resource)), "PolygonCollider")!;
            void Prepare(object tool, int count)
            {
                resetTool.Invoke(collider, new[] { tool }); resetCount.Invoke(collider, new object[] { count }); resetEnergy.Invoke(collider, null);
            }
            void Assert(bool ok, string message) { if (!ok) throw new InvalidOperationException("S02: " + message); }
            var dropProbe = new HarmonyReflectionPatcher(runtime, "dtmapi.qa.oneaction-drop." + access.RunId);
            var observations = new List<string>();
            var owned = new List<object>();
            try
            {
                Assert(dropProbe.TryPatchPrefix("DolocTown.DungeonResource, Assembly-CSharp", "GenerateDropItems",
                    typeof(QaScenarioController).GetMethod(nameof(ObserveOneActionNativeDrop), BindingFlags.NonPublic | BindingFlags.Static), 3), "drop observer install");
                WriteIntMember(agent, "energy", 3000); WriteIntMember(agent, "overflowEnergy", 0);
                foreach (string kind in new[] { "Tree", "Ore", "Garbage", "Weeds" })
                {
                    Assert(TryCreateTransientOneActionResourceForFixture(room, kind, null, out object? resource, out string why) && resource != null, "create " + kind + ": " + why);
                    owned.Add(resource!);
                    object shape = Shape(resource!);
                    object tool = tools.First(t => { object d = Data(resource!, t); return Flag(d, "Valid") && Flag(d, "levelMatch"); });
                    object wrong = tools.First(t => !Flag(Data(resource!, t), "Valid"));
                    int damage = ReadIntMember(Data(resource!, tool), "Damage", 0);
                    int seeded = Math.Max(20, damage * 5);
                    WriteIntMember(resource!, "currentHealth", seeded);
                    oneActionDropTarget = resource; oneActionDropCalls = 0;
                    Prepare(wrong, 1);
                    int energyBefore = ReadIntMember(agent, "energy", -1);
                    handle.Invoke(collider, new[] { shape });
                    Assert(ReadIntMember(resource!, "currentHealth", -1) == seeded && ReadIntMember(agent, "energy", -1) == energyBefore && oneActionDropCalls == 0,
                        kind + " wrong tool added damage, cost or drops");
                    Prepare(tool, 0);
                    handle.Invoke(collider, new[] { shape });
                    Assert(ReadIntMember(resource!, "currentHealth", -1) == seeded && ReadIntMember(agent, "energy", -1) == energyBefore,
                        kind + " exhausted swing added damage or cost");
                    Prepare(tool, 1);
                    handle.Invoke(collider, new[] { shape });
                    int energyAfter = ReadIntMember(agent, "energy", -1);
                    Assert(IsRemoved(resource!) && oneActionDropCalls == 1 && energyAfter < energyBefore - cost && (energyBefore - energyAfter) % cost == 0,
                        kind + " final legal hit did not complete with paid native hits and exactly one drop call");
                    Assert(TryCreateTransientOneActionResourceForFixture(room, kind, null, out object? second, out why) && second != null, "second " + kind);
                    owned.Add(second!);
                    int secondHealth = ReadIntMember(second!, "currentHealth", -1);
                    oneActionDropTarget = second; oneActionDropCalls = 0;
                    handle.Invoke(collider, new[] { Shape(second!) });
                    Assert(!IsRemoved(second!) && ReadIntMember(second!, "currentHealth", -1) == secondHealth && ReadIntMember(agent, "energy", -1) == energyAfter && oneActionDropCalls == 0,
                        kind + " second target inherited first hit");
                    observations.Add(kind + ":wrong/exhausted/last/second=pass; paidHits=" + ((energyBefore - energyAfter) / cost) + "; drops=1");
                    runtime.RuntimeMonitor.Log("Smoke OneAction S02 resource sample OK " + observations.Last());
                }

                // Select an actual low-level native tool/resource pair; no table
                // mutation or synthetic return value is used to create rejection.
                bool lowLevelCovered = false;
                Type hostType = patcher.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp")!;
                MethodInfo create = FindMethod(hostType, "CreateDungeonResource", 1)!;
                foreach (object proto in FindOneActionResourceProtosForFixture().Take(100))
                {
                    object? resource = create.Invoke(room, new[] { proto });
                    // Native room placement can refuse a table candidate.
                    if (resource == null) continue;
                    owned.Add(resource);
                    // A fresh sapling can require no tool level. Grow only our
                    // disposable resource using its native growth operation.
                    FindMethod(resource.GetType(), "SetMaxGrowthLevel", 1)?.Invoke(resource, new object[] { false });
                    object? low = tools.FirstOrDefault(t => { object d = Data(resource, t); return Flag(d, "Valid") && !Flag(d, "levelMatch"); });
                    if (low == null)
                    {
                        TryRemoveTransientDungeonResourceForFixture(room, resource);
                        continue;
                    }
                    int health = ReadIntMember(resource, "currentHealth", -1);
                    int energyBefore = ReadIntMember(agent, "energy", -1);
                    oneActionDropTarget = resource; oneActionDropCalls = 0;
                    Prepare(low, 1);
                    handle.Invoke(collider, new[] { Shape(resource) });
                    int spent = energyBefore - ReadIntMember(agent, "energy", -1);
                    Assert(ReadIntMember(resource, "currentHealth", -1) == health && !IsRemoved(resource) && oneActionDropCalls == 0 && spent >= 0 && spent <= cost,
                        "low level added damage, extra cost or drops");
                    observations.Add("lowLevel=pass; nativeFirstCost=" + spent + "; resource=" + ReadStringMember(resource, "ResourceName", ""));
                    lowLevelCovered = true;
                    break;
                }
                Assert(lowLevelCovered, "no actual low-level pair found");
                runtime.RuntimeMonitor.Log("Smoke OneAction S02 accepted-hit matrix OK " + string.Join(" | ", observations) + ".");
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.Log("Smoke OneAction S02 matrix failure after=" + string.Join(" | ", observations) + "; " + ex);
                throw;
            }
            finally
            {
                oneActionDropTarget = null;
                foreach (object resource in owned) TryRemoveTransientDungeonResourceForFixture(room, resource);
                WriteIntMember(agent, "energy", originalEnergy); WriteIntMember(agent, "overflowEnergy", originalOverflow); RefreshEnergyUiForFixture(api);
                if (!dropProbe.TryUnpatchAllOwnedPatches()) throw new InvalidOperationException("S02 native drop observer cleanup failed.");
            }
        }
    }
}
