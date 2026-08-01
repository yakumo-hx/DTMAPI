using System;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;
using static Yuuka.DTMAPI.OneActionComplete.NativeAccess;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal sealed class OneActionEngine
    {
        private readonly HashSet<string> logged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ActionPolicy policy;

        internal OneActionEngine(IDtmHelper helper)
        {
            Monitor = (helper ?? throw new ArgumentNullException(nameof(helper))).Monitor;
        }

        internal IMonitor Monitor { get; }

        internal void Configure(OneActionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            policy = new ActionPolicy(config);
            logged.Clear();
        }

        internal void ResetBoundary(string reason)
        {
            logged.Clear();
            if (policy.VerboseLogging)
                Monitor.Log("OneActionComplete boundary reset reason=" + (reason ?? string.Empty) + ".");
        }

        internal bool ApplyToolHit(object toolCollider, object collider)
        {
            if (!policy.ToolRoute || toolCollider == null || collider == null)
                return false;

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null || IsRemoved(resource) || !MatchesResource(resource))
                return false;
            int currentHealth = ReadInt(resource, "currentHealth", 0);
            if (currentHealth <= 0)
                return false;

            object? currentTool = ReadMember(toolCollider, "currentTool");
            object? hitPoint = ReadMember(resource, "PositionCenter");
            Type? fellDataType = ResolveType("DolocTown.ResourceFellData, Assembly-CSharp");
            if (currentTool == null || hitPoint == null || fellDataType == null)
                return false;
            if (!TryBuildValidatedFellData(fellDataType, resource, currentTool, hitPoint, out object? nativeFellData, out string rejectReason))
            {
                LogOnce("skip:" + ResourceName(resource) + ":" + ReadString(currentTool, "name") + ":" + rejectReason,
                    "One-action tool hook skipped resource " + ResourceName(resource) + " tool=" + ReadString(currentTool, "name") + " reason=" + rejectReason + ".");
                return false;
            }

            try
            {
                int toolLevel = ReadInt(nativeFellData!, "toolLevel", 0);
                int nativeDamage = Math.Max(1, ReadInt(nativeFellData!, "Damage", currentHealth));
                int requiredExtraHits = Math.Max(1, (int)Math.Ceiling(currentHealth / (double)nativeDamage));
                int paidExtraHits = CostExtraToolHits(requiredExtraHits, out string energyReason);
                if (paidExtraHits <= 0)
                {
                    LogOnce("energy:" + ResourceName(resource), "One-action tool hook skipped resource " + ResourceName(resource) + " reason=" + energyReason + ".");
                    return false;
                }

                int damage = Math.Min(currentHealth, paidExtraHits * nativeDamage);
                object? fellData = Activator.CreateInstance(fellDataType, new object?[]
                {
                    ReadBool(nativeFellData!, "levelMatch", false),
                    toolLevel,
                    damage,
                    hitPoint,
                    ReadBool(nativeFellData!, "shouldCounterBack", true),
                    ReadBool(nativeFellData!, "shouldRaiseToolTip", true),
                    ReadMember(nativeFellData!, "overrideSpawnLut") as string ?? string.Empty,
                    ReadMember(nativeFellData!, "extraItems")
                });
                MethodInfo? fell = FindMethod(resource.GetType(), "_Fell", 1);
                if (fellData == null || fell == null)
                    return false;
                fell.Invoke(resource, new[] { fellData });
                string summary = "resource=" + ResourceName(resource) + ", class=" + ResourceClass(resource) + ", tool=" + ReadString(currentTool, "name") + ", nativeDamage=" + nativeDamage + ", paidExtraHits=" + paidExtraHits + "/" + requiredExtraHits + ", damage=" + damage;
                if (policy.VerboseLogging || logged.Add("complete:" + ResourceName(resource)))
                    Monitor.Log("One-action tool hook completed " + summary + ".");
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log("One-action tool completion failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return false;
            }
        }

        internal bool ApplyEquipmentFillAfterInteract()
        {
            if (!policy.FuelRoute && !policy.FeederRoute)
                return false;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? equipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? item = ReadStaticMember(dolocApi, "SelectedItem");
            if (equipment == null || item == null)
                return false;
            Type type = equipment.GetType();
            if (policy.FuelRoute && IsTypeOrBase(type, "DolocTown.PowerGeneratorFuel"))
                return FillFuel(equipment, item, dolocApi);
            if (policy.FeederRoute && IsTypeOrBase(type, "DolocTown.Feeder"))
                return FillFeeder(equipment, item, dolocApi);
            return false;
        }

        private bool FillFuel(object generator, object firstItem, Type? dolocApi)
        {
            MethodInfo? suitable = FindMethod(generator.GetType(), "IsSuitableFuel", 1);
            MethodInfo? addFuel = FindMethod(generator.GetType(), "AddFuel", 2);
            if (suitable == null || addFuel == null || !InvokeBool(suitable, generator, firstItem))
                return false;
            double before = ReadDouble(generator, "FuelPercent", -1);
            int consumed = 0;
            try
            {
                for (int guard = 0; guard < 99 && !IsFuelFull(generator); guard++)
                {
                    object? selected = ReadStaticMember(dolocApi, "SelectedItem");
                    object? proto = selected == null ? null : ReadMember(selected, "proto");
                    if (selected == null || proto == null || !InvokeBool(suitable, generator, selected) || !TryCostSelf(selected))
                        break;
                    addFuel.Invoke(generator, new[] { proto, (object)true });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log("One-action fuel fill failed closed after extraConsumed=" + consumed + ": " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
            if (consumed <= 0)
                return false;
            Monitor.Log("One-action fuel fill completed item=" + ItemName(firstItem) + " extraConsumed=" + consumed + " fuel=" + FormatRatio(before) + "->" + FormatRatio(ReadDouble(generator, "FuelPercent", -1)) + ".");
            return true;
        }

        private bool FillFeeder(object feeder, object firstItem, Type? dolocApi)
        {
            MethodInfo? suitable = FindMethod(feeder.GetType(), "IsAnimalFeeds", 2);
            MethodInfo? addFeeds = FindMethod(feeder.GetType(), "AddFeeds", 1);
            object? firstProto = ReadMember(firstItem, "proto");
            if (suitable == null || addFeeds == null || firstProto == null || !InvokeAnimalFeed(suitable, feeder, firstProto))
                return false;
            double before = ReadDouble(feeder, "progress", -1);
            int consumed = 0;
            try
            {
                for (int guard = 0; guard < 99 && ReadDouble(feeder, "progress", 0) < 0.999; guard++)
                {
                    object? selected = ReadStaticMember(dolocApi, "SelectedItem");
                    object? proto = selected == null ? null : ReadMember(selected, "proto");
                    if (selected == null || proto == null || !InvokeAnimalFeed(suitable, feeder, proto) || !TryCostSelf(selected))
                        break;
                    addFeeds.Invoke(feeder, new[] { proto });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log("One-action feeder fill failed closed after extraConsumed=" + consumed + ": " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
            if (consumed <= 0)
                return false;
            Monitor.Log("One-action feeder fill completed item=" + ItemName(firstItem) + " extraConsumed=" + consumed + " progress=" + FormatRatio(before) + "->" + FormatRatio(ReadDouble(feeder, "progress", -1)) + ".");
            return true;
        }

        private bool MatchesResource(object resource)
        {
            Type type = resource.GetType();
            string fullName = type.FullName ?? type.Name;
            string resourceClass = ResourceClass(resource);
            return (policy.CompleteTrees && (IsTypeOrBase(type, "DolocTown.DungeonResourceTree") || IsTypeOrBase(type, "DolocTown.DungeonResourceTreeTrunk")))
                || (policy.CompleteOres && (IsTypeOrBase(type, "DolocTown.DungeonResourceOre") || resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase)))
                || (policy.CompleteGarbage && (fullName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 || resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase)))
                || (policy.CompleteWeeds && IsTypeOrBase(type, "DolocTown.DungeonResourceWeeds"));
        }

        private static bool TryBuildValidatedFellData(Type type, object resource, object tool, object hitPoint, out object? fellData, out string reason)
        {
            fellData = null;
            reason = string.Empty;
            try
            {
                fellData = Activator.CreateInstance(type, new[] { resource, tool, hitPoint });
                if (fellData == null) { reason = "native-fell-data-null"; return false; }
                if (!ReadBool(fellData, "Valid", false)) { reason = "tool-type-mismatch"; return false; }
                if (!ReadBool(fellData, "levelMatch", false)) { reason = "tool-level-mismatch level=" + ReadInt(fellData, "toolLevel", -1); return false; }
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static int CostExtraToolHits(int required, out string reason)
        {
            Type? api = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? hasEnergy = api?.GetMethod("HasEnoughEnergyForUsingTool", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            MethodInfo? costEnergy = api?.GetMethod("CostToolEnergy", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (hasEnergy == null || costEnergy == null)
            {
                reason = "native energy methods were not found";
                return 0;
            }
            int paid = 0;
            for (; paid < required; paid++)
            {
                if (!(hasEnergy.Invoke(null, null) is bool enough) || !enough)
                    break;
                if (!(costEnergy.Invoke(null, null) is bool charged) || !charged)
                    break;
            }
            reason = paid == required ? "charged " + paid + " extra hit(s)" : "native energy rejected after " + paid + " extra hit(s)";
            return paid;
        }

        private static bool InvokeBool(MethodInfo method, object target, object arg)
        {
            try { return method.Invoke(target, new[] { arg }) is bool result && result; }
            catch { return false; }
        }

        private static bool InvokeAnimalFeed(MethodInfo method, object target, object proto)
        {
            try
            {
                object?[] args = { proto, 0 };
                return method.Invoke(target, args) is bool result && result;
            }
            catch { return false; }
        }

        private static bool TryCostSelf(object item)
        {
            try
            {
                MethodInfo? withOut = FindMethod(item.GetType(), "CostSelf", 2);
                if (withOut != null && withOut.GetParameters()[0].ParameterType.IsByRef)
                {
                    object?[] args = { null, false };
                    return withOut.Invoke(item, args) is bool ok && ok;
                }
                MethodInfo? single = FindMethod(item.GetType(), "CostSelf", 1);
                return single != null && single.Invoke(item, new object?[] { false }) != null;
            }
            catch { return false; }
        }

        private static bool IsFuelFull(object generator)
        {
            object? state = ReadMember(generator, "generatorFuel");
            return (state != null && ReadBool(state, "IsFull", false)) || ReadDouble(generator, "FuelPercent", 0) >= 0.999;
        }

        private static string ItemName(object item)
        {
            string value = ReadString(item, "name");
            return value.Length == 0 ? item.GetType().Name : value;
        }

        private void LogOnce(string key, string message)
        {
            if (logged.Add(key))
                Monitor.Log(message);
        }

        private readonly struct ActionPolicy
        {
            internal ActionPolicy(OneActionConfig config)
            {
                Enabled = config.Enabled;
                CompleteTrees = config.CompleteTrees;
                CompleteOres = config.CompleteOres;
                CompleteGarbage = config.CompleteGarbage;
                CompleteWeeds = config.CompleteWeeds;
                CompleteMachineFuel = config.CompleteMachineFuel;
                CompleteFeeder = config.CompleteFeeder;
                VerboseLogging = config.VerboseLogging;
            }
            internal bool Enabled { get; }
            internal bool CompleteTrees { get; }
            internal bool CompleteOres { get; }
            internal bool CompleteGarbage { get; }
            internal bool CompleteWeeds { get; }
            internal bool CompleteMachineFuel { get; }
            internal bool CompleteFeeder { get; }
            internal bool VerboseLogging { get; }
            internal bool ToolRoute => Enabled && (CompleteTrees || CompleteOres || CompleteGarbage || CompleteWeeds);
            internal bool FuelRoute => Enabled && CompleteMachineFuel;
            internal bool FeederRoute => Enabled && CompleteFeeder;
        }
    }
}
