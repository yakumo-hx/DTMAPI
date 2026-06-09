using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ActionCompletionService : IActionCompletionApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly Func<string, bool, string, string> rollOilDropFromCoal;
        private readonly Dictionary<string, ActionCompletionOptions> actionOptions = new Dictionary<string, ActionCompletionOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedActionApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool actionHooksInstalled;

        public ActionCompletionService(DtmApiRuntime runtime, Func<string, bool, string, string> rollOilDropFromCoal)
        {
            this.runtime = runtime;
            this.rollOilDropFromCoal = rollOilDropFromCoal;
        }

        internal int OneActionApplicationCount { get; private set; }

        internal string LastOneActionApplicationSummary { get; private set; } = string.Empty;

        internal void SetActionHooksInstalled(bool installed)
        {
            actionHooksInstalled = installed;
        }

        public void Configure(IManifest owner, ActionCompletionOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            actionOptions[owner.UniqueID] = options ?? new ActionCompletionOptions();
            runtime.RuntimeMonitor.Log("Action completion bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IActionCompletionApi.GetStatus(string uniqueId)
        {
            return actionOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(actionHooksInstalled ? "configured-verified-resource-hook" : "configured-pending-hook", actionHooksInstalled ? "Policy accepted; resource/tool-hit, wrong-tool guard, and fuel/feeder native consume/fill paths have third-save smoke evidence. Vegetation/dandelion is a native VegetationRenderer.OnFell exception path, not a DungeonResource one-action path." : "Policy accepted; action-completion evidence exists, but this run has not installed the gameplay hook yet.")
                : new BridgeFeatureStatus("not-configured", "No action completion policy was registered for this mod.");
        }

        internal bool ApplyOneActionToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null || actionOptions.Count == 0)
                return false;

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null || IsResourceRemoved(resource))
                return false;

            int currentHealth = ReadIntMember(resource, "currentHealth", 0);
            if (currentHealth <= 0)
                return false;

            if (!TryFindActionPolicy(resource, out string ownerId, out ActionCompletionOptions options))
                return false;

            object? currentTool = ReadMember(toolCollider, "currentTool");
            if (currentTool == null)
                return false;

            object? hitPoint = resource.GetType().GetProperty("PositionCenter", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            if (hitPoint == null)
                return false;

            Type? resourceFellDataType = ResolveType("DolocTown.ResourceFellData, Assembly-CSharp");
            if (resourceFellDataType == null)
                return false;

            if (!TryBuildValidatedResourceFellData(resourceFellDataType, resource, currentTool, hitPoint, out object? nativeFellData, out string rejectReason))
            {
                string skippedResource = GetResourceName(resource);
                string skippedTool = ReadStringMember(currentTool, "name");
                string skippedToolType = ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown";
                LogOnce(loggedActionApplications, "skip:" + ownerId + ":" + skippedResource + ":" + skippedTool + ":" + rejectReason, "One-action tool hook skipped resource " + skippedResource + " for " + ownerId + " tool=" + skippedTool + " toolType=" + skippedToolType + " reason=" + rejectReason + ".");
                return false;
            }

            try
            {
                int toolLevel = ReadIntMember(nativeFellData!, "toolLevel", 0);
                int nativeDamage = Math.Max(1, ReadIntMember(nativeFellData!, "Damage", currentHealth));
                bool levelMatch = ReadBoolMember(nativeFellData!, "levelMatch", false);
                bool shouldCounterBack = ReadBoolMember(nativeFellData!, "shouldCounterBack", true);
                bool shouldRaiseToolTip = ReadBoolMember(nativeFellData!, "shouldRaiseToolTip", true);
                string overrideSpawnLut = ReadMember(nativeFellData!, "overrideSpawnLut") as string ?? string.Empty;
                object? extraItems = ReadMember(nativeFellData!, "extraItems");
                int requiredExtraHits = Math.Max(1, (int)Math.Ceiling(currentHealth / (double)nativeDamage));
                int paidExtraHits = CostOneActionExtraToolHits(requiredExtraHits, out string energyReason);
                if (paidExtraHits <= 0)
                {
                    string skippedResource = GetResourceName(resource);
                    LogOnce(loggedActionApplications, "energy-skip:" + ownerId + ":" + skippedResource, "One-action tool hook skipped resource " + skippedResource + " for " + ownerId + " reason=" + energyReason + ".");
                    runtime.SetHookStatus("Actions.OneActionComplete", "experimental", "DolocAPI.HasEnoughEnergyForUsingTool/CostToolEnergy", "Skipped extra one-action hits because " + energyReason + ".");
                    return false;
                }

                int damageToApply = Math.Min(currentHealth, paidExtraHits * nativeDamage);
                object? fellData = Activator.CreateInstance(resourceFellDataType, new object?[] { levelMatch, toolLevel, damageToApply, hitPoint, shouldCounterBack, shouldRaiseToolTip, overrideSpawnLut, extraItems });
                if (fellData == null)
                    return false;
                MethodInfo? fell = resource.GetType().GetMethod("_Fell", BindingFlags.Public | BindingFlags.Instance);
                if (fell == null)
                    return false;
                fell.Invoke(resource, new object?[] { fellData });
                string resourceName = GetResourceName(resource);
                string resourceClass = GetResourceClass(resource);
                string toolName = ReadStringMember(currentTool, "name");
                string toolType = ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown";
                int afterHealth = ReadIntMember(resource, "currentHealth", 0);
                bool removed = IsResourceRemoved(resource);
                string oilDropSummary = rollOilDropFromCoal(resourceName, removed, "one-action-tool-hit");
                LastOneActionApplicationSummary = "owner=" + ownerId + ", resource=" + resourceName + ", class=" + resourceClass + ", tool=" + toolName + ", toolType=" + toolType + ", toolLevel=" + toolLevel + ", nativeDamage=" + nativeDamage + ", paidExtraHits=" + paidExtraHits + "/" + requiredExtraHits + ", damage=" + damageToApply + ", healthAfter=" + afterHealth + ", removed=" + removed;
                if (!string.IsNullOrWhiteSpace(oilDropSummary))
                    LastOneActionApplicationSummary += ", " + oilDropSummary;
                OneActionApplicationCount++;
                string key = ownerId + ":" + resourceName;
                if (options.VerboseLogging || !loggedActionApplications.Contains(key))
                    runtime.RuntimeMonitor.Log("One-action tool hook completed resource " + resourceName + " for " + ownerId + " tool=" + toolName + " toolType=" + toolType + " nativeDamage=" + nativeDamage + " paidExtraHits=" + paidExtraHits + "/" + requiredExtraHits + " damage=" + damageToApply + ".");
                loggedActionApplications.Add(key);
                runtime.SetHookStatus("Smoke.OneActionResourceHit", "verified", "ToolCollider.HandleTools Postfix -> DungeonResource._Fell", LastOneActionApplicationSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action tool hook failed.", ex.ToString());
                return false;
            }
        }

        internal bool ApplyOneActionEquipmentFillAfterInteract()
        {
            if (actionOptions.Count == 0)
                return false;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedEquipment == null || selectedItem == null)
                return false;

            Type equipmentType = selectedEquipment.GetType();
            if (IsTypeOrBase(equipmentType, "DolocTown.PowerGeneratorFuel"))
                return TryApplyOneActionFuelFill(selectedEquipment, selectedItem, dolocApi);
            if (IsTypeOrBase(equipmentType, "DolocTown.Feeder"))
                return TryApplyOneActionFeederFill(selectedEquipment, selectedItem, dolocApi);
            return false;
        }

        private bool TryApplyOneActionFuelFill(object generator, object firstSelectedItem, Type? dolocApi)
        {
            if (!TryFindOneActionFuelPolicy(out string ownerId, out ActionCompletionOptions options))
                return false;

            MethodInfo? isSuitableFuel = FindMethodInHierarchy(generator.GetType(), "IsSuitableFuel", 1);
            MethodInfo? addFuel = FindMethodInHierarchy(generator.GetType(), "AddFuel", 2);
            if (isSuitableFuel == null || addFuel == null)
                return false;
            if (!InvokeBool(isSuitableFuel, generator, firstSelectedItem))
                return false;

            double before = ReadDoubleMember(generator, "FuelPercent", -1);
            int consumed = 0;
            int guard = 0;
            try
            {
                while (guard++ < 99 && !IsFuelGeneratorFull(generator))
                {
                    object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedItem == null || !InvokeBool(isSuitableFuel, generator, selectedItem))
                        break;

                    object? proto = ReadMember(selectedItem, "proto");
                    if (proto == null)
                        break;

                    if (!TryCostSelf(selectedItem))
                        break;

                    addFuel.Invoke(generator, new object?[] { proto, true });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action fuel fill failed.", ex.ToString());
                return consumed > 0;
            }

            if (consumed <= 0)
                return false;

            double after = ReadDoubleMember(generator, "FuelPercent", -1);
            string itemName = FirstText(ReadStringMember(firstSelectedItem, "name"), firstSelectedItem.GetType().Name);
            LastOneActionApplicationSummary = "owner=" + ownerId + ", kind=FuelMachine, item=" + itemName + ", extraConsumed=" + consumed + ", fuelBefore=" + FormatRatio(before) + ", fuelAfter=" + FormatRatio(after);
            OneActionApplicationCount++;
            runtime.RuntimeMonitor.Log("One-action fuel fill completed by " + ownerId + " item=" + itemName + " extraConsumed=" + consumed + " fuel=" + FormatRatio(before) + "->" + FormatRatio(after) + ".");
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "AgentStateInteract.OnExit Postfix -> CostSelf/AddFuel", LastOneActionApplicationSummary);
            return true;
        }

        private bool TryApplyOneActionFeederFill(object feeder, object firstSelectedItem, Type? dolocApi)
        {
            if (!TryFindOneActionFeederPolicy(out string ownerId, out ActionCompletionOptions options))
                return false;

            MethodInfo? isAnimalFeeds = FindMethodInHierarchy(feeder.GetType(), "IsAnimalFeeds", 2);
            MethodInfo? addFeeds = FindMethodInHierarchy(feeder.GetType(), "AddFeeds", 1);
            if (isAnimalFeeds == null || addFeeds == null)
                return false;

            object? firstProto = ReadMember(firstSelectedItem, "proto");
            if (firstProto == null || !InvokeIsAnimalFeeds(isAnimalFeeds, feeder, firstProto, out _))
                return false;

            double before = ReadDoubleMember(feeder, "progress", -1);
            int consumed = 0;
            int guard = 0;
            try
            {
                while (guard++ < 99 && ReadDoubleMember(feeder, "progress", 0) < 0.999)
                {
                    object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
                    object? proto = selectedItem == null ? null : ReadMember(selectedItem, "proto");
                    if (selectedItem == null || proto == null || !InvokeIsAnimalFeeds(isAnimalFeeds, feeder, proto, out _))
                        break;

                    if (!TryCostSelf(selectedItem))
                        break;

                    addFeeds.Invoke(feeder, new object?[] { proto });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action feeder fill failed.", ex.ToString());
                return consumed > 0;
            }

            if (consumed <= 0)
                return false;

            double after = ReadDoubleMember(feeder, "progress", -1);
            string itemName = FirstText(ReadStringMember(firstSelectedItem, "name"), firstSelectedItem.GetType().Name);
            LastOneActionApplicationSummary = "owner=" + ownerId + ", kind=Feeder, item=" + itemName + ", extraConsumed=" + consumed + ", progressBefore=" + FormatRatio(before) + ", progressAfter=" + FormatRatio(after);
            OneActionApplicationCount++;
            runtime.RuntimeMonitor.Log("One-action feeder fill completed by " + ownerId + " item=" + itemName + " extraConsumed=" + consumed + " progress=" + FormatRatio(before) + "->" + FormatRatio(after) + ".");
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "AgentStateInteract.OnExit Postfix -> CostSelf/AddFeeds", LastOneActionApplicationSummary);
            return true;
        }

        private bool TryBuildValidatedResourceFellData(Type resourceFellDataType, object resource, object tool, object hitPoint, out object? fellData, out string rejectReason)
        {
            fellData = null;
            rejectReason = string.Empty;
            try
            {
                fellData = Activator.CreateInstance(resourceFellDataType, new object?[] { resource, tool, hitPoint });
                if (fellData == null)
                {
                    rejectReason = "native-fell-data-null";
                    return false;
                }
                if (!ReadBoolMember(fellData, "Valid", false))
                {
                    rejectReason = "tool-type-mismatch";
                    return false;
                }
                if (!ReadBoolMember(fellData, "levelMatch", false))
                {
                    int toolLevel = ReadIntMember(fellData, "toolLevel", -1);
                    rejectReason = "tool-level-mismatch level=" + toolLevel;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                rejectReason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static int CostOneActionExtraToolHits(int requiredHits, out string reason)
        {
            reason = string.Empty;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? hasEnergy = dolocApi?.GetMethod("HasEnoughEnergyForUsingTool", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            MethodInfo? costEnergy = dolocApi?.GetMethod("CostToolEnergy", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (hasEnergy == null || costEnergy == null)
            {
                reason = "native energy methods were not found";
                return 0;
            }

            int paid = 0;
            for (int i = 0; i < requiredHits; i++)
            {
                object? enough = hasEnergy.Invoke(null, null);
                if (!(enough is bool ok && ok))
                {
                    reason = paid == 0 ? "not enough energy for the first extra hit" : "not enough energy after " + paid + " extra hit(s)";
                    break;
                }

                object? cost = costEnergy.Invoke(null, null);
                if (!(cost is bool charged && charged))
                {
                    reason = paid == 0 ? "native CostToolEnergy rejected the first extra hit" : "native CostToolEnergy rejected after " + paid + " extra hit(s)";
                    break;
                }
                paid++;
            }

            if (string.IsNullOrWhiteSpace(reason))
                reason = "charged " + paid + " extra hit(s)";
            return paid;
        }

        internal bool TryFindOneActionPolicyForSmoke(object resource, out string ownerId)
        {
            return TryFindActionPolicy(resource, out ownerId, out _);
        }

        private bool TryFindActionPolicy(object resource, out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            Type resourceType = resource.GetType();
            string typeName = resourceType.FullName ?? resourceType.Name;
            string resourceClass = GetResourceClass(resource);

            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled)
                    continue;

                bool matches =
                    (candidate.CompleteTrees && IsTreeResource(resourceType)) ||
                    (candidate.CompleteOres && (IsTypeOrBase(resourceType, "DolocTown.DungeonResourceOre") || resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase))) ||
                    (candidate.CompleteGarbage && (typeName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 || resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase))) ||
                    (candidate.CompleteWeeds && IsTypeOrBase(resourceType, "DolocTown.DungeonResourceWeeds"));

                if (!matches)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }

            return false;
        }

        private bool TryFindOneActionFuelPolicy(out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled || !candidate.CompleteMachineFuel)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryFindOneActionFeederPolicy(out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled || !candidate.CompleteFeeder)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private static bool IsTreeResource(Type type)
        {
            return IsTypeOrBase(type, "DolocTown.DungeonResourceTree") ||
                IsTypeOrBase(type, "DolocTown.DungeonResourceTreeTrunk");
        }

        private static bool IsFuelGeneratorFull(object generator)
        {
            object? generatorFuel = ReadMember(generator, "generatorFuel");
            if (generatorFuel != null && ReadBoolMember(generatorFuel, "IsFull", false))
                return true;

            double percent = ReadDoubleMember(generator, "FuelPercent", 0);
            return percent >= 0.999;
        }

        private static bool InvokeBool(MethodInfo method, object target, object arg)
        {
            try
            {
                object? result = method.Invoke(target, new object?[] { arg });
                return result is bool value && value;
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeIsAnimalFeeds(MethodInfo method, object target, object? itemInfo, out int energy)
        {
            energy = 0;
            try
            {
                object?[] args = new object?[] { itemInfo, energy };
                object? result = method.Invoke(target, args);
                if (!(result is bool ok) || !ok)
                    return false;
                if (args.Length > 1 && args[1] != null)
                    energy = Convert.ToInt32(args[1]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCostSelf(object item)
        {
            MethodInfo? costWithOut = FindMethodInHierarchy(item.GetType(), "CostSelf", 2);
            if (costWithOut != null)
            {
                ParameterInfo[] parameters = costWithOut.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType.IsByRef)
                {
                    object?[] args = new object?[] { null, false };
                    object? result = costWithOut.Invoke(item, args);
                    return result is bool ok && ok;
                }
            }

            MethodInfo? costSingle = FindMethodInHierarchy(item.GetType(), "CostSelf", 1);
            if (costSingle == null)
                return false;
            object? consumed = costSingle.Invoke(item, new object?[] { false });
            return consumed != null;
        }

        private void LogOnce(HashSet<string> keys, string key, string message)
        {
            if (keys.Contains(key))
                return;
            keys.Add(key);
            runtime.RuntimeMonitor.Log(message);
        }
    }
}
