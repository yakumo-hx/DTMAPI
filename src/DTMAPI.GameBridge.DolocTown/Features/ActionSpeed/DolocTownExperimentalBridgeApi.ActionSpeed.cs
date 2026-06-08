using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
        internal void SetActionSpeedToolHooksInstalled(bool installed)
        {
            actionSpeedToolHooksInstalled = installed;
        }

        internal void SetActionSpeedInteractionHooksInstalled(bool installed)
        {
            actionSpeedInteractionHooksInstalled = installed;
        }

        public void Configure(IManifest owner, ActionSpeedOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            actionSpeedOptions[owner.UniqueID] = NormalizeActionSpeedOptions(options);
            runtime.RuntimeMonitor.Log("Action speed bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IActionSpeedApi.GetStatus(string uniqueId)
        {
            return actionSpeedOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(actionSpeedToolHooksInstalled ? "configured-verified-runtime-hooks" : "configured-pending-tool-hook", actionSpeedToolHooksInstalled ? "Tool animation speed is verified. Fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, IWaterContainer and in-water bottle fill, no-key auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest have third-save smoke evidence" + (actionSpeedInteractionHooksInstalled ? " and interaction hooks are installed in this run." : "; waiting for interaction hooks in this run.") : "Policy accepted; waiting for AgentStateTool hooks in this run.")
                : new BridgeFeatureStatus("not-configured", "No action-speed policy was registered for this mod.");
        }

        internal bool TryGetConfiguredActionSpeedOwner(out string ownerId)
        {
            return TryFindActionSpeedToolPolicy(out ownerId, out _);
        }

        internal bool TryGetConfiguredActionSpeedInteractionOwner(out string ownerId)
        {
            ownerId = string.Empty;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;
                if ((candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1) ||
                    (candidate.EatDrinkSpeedEnabled && candidate.EatDrinkMultiplier > 1) ||
                    (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1) ||
                    (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1) ||
                    (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1) ||
                    (candidate.AutoFillBottle && candidate.BottleFillMultiplier > 1) ||
                    (candidate.ContinuousDrinkWithRightClick && candidate.EatDrinkMultiplier > 1))
                {
                    ownerId = entry.Key;
                    return true;
                }
            }
            return false;
        }

        internal bool ApplyActionSpeedToolEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedOptions options))
                return false;

            object? tool = ReadMember(state, "tool");
            if (!IsAcceleratedTool(tool))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            double multiplier = ClampMultiplier(options.ToolMultiplier);
            int changed = 0;
            var samples = new List<string>();
            changed += ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);

            object? toolRenderer = ReadMember(body, "ToolRenderer");
            if (toolRenderer != null)
            {
                changed += ApplyAnimatorSpeed(ReadMember(toolRenderer, "animator"), multiplier, "tool-renderer", samples);
                object? collider = ReadMember(toolRenderer, "_collider");
                if (collider != null)
                    changed += ApplyAnimatorSpeed(ReadMember(collider, "_animator"), multiplier, "tool-collider", samples);
            }

            if (changed <= 0)
                return false;

            string toolName = ReadStringMember(tool!, "name");
            if (string.IsNullOrWhiteSpace(toolName))
                toolName = tool!.GetType().Name;

            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId + ", tool=" + toolName + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray());
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(ownerId + ":" + toolName))
                runtime.RuntimeMonitor.Log("ActionSpeed tool animation speed applied by " + ownerId + " tool=" + toolName + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(ownerId + ":" + toolName);
            runtime.SetHookStatus("Smoke.ActionSpeedTool", "verified", "AgentStateTool.OnEnter Postfix", LastActionSpeedApplicationSummary);
            return true;
        }

        internal bool ApplyActionSpeedInteractEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, kind, multiplier, target, "AgentStateInteract.OnEnter Postfix", "Smoke.ActionSpeedInteraction");
        }

        internal bool ApplyActionSpeedEatEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedOptions options))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, "EatDrink", options.EatDrinkMultiplier, "AgentStateEat", "AgentStateEat.OnEnter Postfix", "Smoke.ActionSpeedEatDrink");
        }

        internal bool AdjustActionSpeedUseItemContinuesDelta(ref float dt)
        {
            if (dt <= 0 || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target))
                return false;

            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            float original = dt;
            dt = (float)Math.Min(1, original * multiplier);
            string logKey = ownerId + ":UseItemContinues:" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                runtime.RuntimeMonitor.Log("ActionSpeed right-click continuous use scaled by " + ownerId + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###") + ".");
            loggedActionSpeedApplications.Add(logKey);
            ActionSpeedApplicationCount++;
            ActionSpeedContinuousUseApplicationCount++;
            LastActionSpeedContinuousUseSummary = "owner=" + ownerId + ", kind=" + kind + ", target=" + target + ", multiplier=" + multiplier.ToString("0.###") + ", dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###");
            LastActionSpeedApplicationSummary = LastActionSpeedContinuousUseSummary;
            runtime.SetHookStatus("Smoke.ActionSpeedContinuousUse", "experimental", "AgentControllerState.UseItemContinues Prefix", LastActionSpeedApplicationSummary);
            return true;
        }

        internal void RestoreActionSpeed(string reason)
        {
            if (originalAnimatorSpeeds.Count == 0)
                return;

            int restored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalAnimatorSpeeds))
            {
                if (TryWriteAnimatorSpeed(entry.Key, entry.Value))
                    restored++;
            }
            originalAnimatorSpeeds.Clear();
            runtime.RuntimeMonitor.Log("ActionSpeed animator speeds restored reason=" + reason + " restored=" + restored + ".");
        }

        private void UpdateActionSpeedAutoFill()
        {
            if (SuppressActionSpeedAutoFillForSmoke)
                return;

            if (!TryFindActionSpeedAutoFillPolicy(out string ownerId, out ActionSpeedOptions options))
                return;

            double cooldownSeconds = GetActionSpeedAutoFillCooldownSeconds(options);
            if ((DateTimeOffset.Now - lastActionSpeedAutoFillAt).TotalSeconds < cooldownSeconds)
                return;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (!ReadStaticBoolMember(dolocApi, "IsNormalState", false) || !ReadStaticBoolMember(dolocApi, "IsAgentInWater", false))
                return;

            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (!IsEmptyBottleItem(selectedItem, dolocApi))
                return;

            object? agent = ReadStaticMember(dolocApi, "agent");
            if (agent != null && !ReadBoolMember(agent, "IsCurrentStateSupportInteract", true))
                return;

            MethodInfo? useAsItem = selectedItem?.GetType().GetMethod("UseAsItem", BindingFlags.Public | BindingFlags.Instance);
            if (useAsItem == null)
                return;

            lastActionSpeedAutoFillAt = DateTimeOffset.Now;
            try
            {
                useAsItem.Invoke(selectedItem, null);
                actionSpeedAutoFillApplications++;
                double normalCooldownSeconds = options.AutoFillCooldownSeconds;
                double strongCooldownSeconds = Math.Min(options.AutoFillCooldownSeconds, options.AutoFillStrongCooldownSeconds);
                LastActionSpeedAutoFillSummary = "owner=" + ownerId + ", behavior=AutoFillBottle, item=" + ReadStringMember(selectedItem!, "name") + ", inWater=true, strong=" + options.AutoFillStrong + ", cooldownSeconds=" + cooldownSeconds.ToString("0.###") + ", normalCooldownSeconds=" + normalCooldownSeconds.ToString("0.###") + ", strongCooldownSeconds=" + strongCooldownSeconds.ToString("0.###") + ", applications=" + actionSpeedAutoFillApplications;
                LastActionSpeedApplicationSummary = LastActionSpeedAutoFillSummary;
                if (options.VerboseLogging || actionSpeedAutoFillApplications == 1)
                    runtime.RuntimeMonitor.Log("ActionSpeed auto-fill invoked native ItemBottle.UseAsItem by " + ownerId + " summary=" + LastActionSpeedAutoFillSummary + ".");
                runtime.SetHookStatus("Smoke.ActionSpeedAutoFillBottle", "experimental", "ItemBottle.UseAsItem native path", LastActionSpeedAutoFillSummary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "ActionSpeed auto-fill bottle failed.", ex.ToString());
            }
        }

        private bool TryFindActionSpeedAutoFillPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (candidate.Enabled && candidate.AutoFillBottle)
                {
                    ownerId = entry.Key;
                    options = candidate;
                    return true;
                }
            }
            return false;
        }

        private bool TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled || !candidate.ToolSpeedEnabled || candidate.ToolMultiplier <= 1)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled || !candidate.EatDrinkSpeedEnabled || candidate.EatDrinkMultiplier <= 1)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target)
        {
            ownerId = string.Empty;
            options = null!;
            kind = string.Empty;
            multiplier = 1;
            target = string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);

            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;

                if (candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1 && IsBottleFillInteraction(selectedItem, selectedEquipment, isInWater))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottleFill";
                    multiplier = candidate.BottleFillMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1 && IsPlantInteraction(selectedItem, selectedEquipment))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "Plant";
                    multiplier = candidate.PlantMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1 && IsMachineAddInteraction(selectedEquipment))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "MachineAdd";
                    multiplier = candidate.MachineAddMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1 && IsHarvestInteraction(selectedEquipment, currentInteractable))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "Harvest";
                    multiplier = candidate.HarvestMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }
            }

            return false;
        }

        private bool TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target)
        {
            ownerId = string.Empty;
            options = null!;
            kind = string.Empty;
            multiplier = 1;
            target = string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedItem == null)
                return false;

            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);

            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;

                if ((candidate.BottleFillSpeedEnabled || candidate.AutoFillBottle) && candidate.BottleFillMultiplier > 1 && IsBottleFillInteraction(selectedItem, selectedEquipment, isInWater))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottleFill";
                    multiplier = candidate.BottleFillMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.ContinuousDrinkWithRightClick && candidate.EatDrinkMultiplier > 1 && IsBottledWaterItem(selectedItem, dolocApi))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottledWaterDrink";
                    multiplier = candidate.EatDrinkMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }
            }

            return false;
        }

        private static ActionSpeedOptions NormalizeActionSpeedOptions(ActionSpeedOptions? options)
        {
            options ??= new ActionSpeedOptions();
            options.ToolMultiplier = ClampMultiplier(options.ToolMultiplier);
            options.BottleFillMultiplier = ClampMultiplier(options.BottleFillMultiplier);
            options.EatDrinkMultiplier = ClampMultiplier(options.EatDrinkMultiplier);
            options.MachineAddMultiplier = ClampMultiplier(options.MachineAddMultiplier);
            options.HarvestMultiplier = ClampMultiplier(options.HarvestMultiplier);
            options.PlantMultiplier = ClampMultiplier(options.PlantMultiplier);
            options.AutoFillCooldownSeconds = ClampSeconds(options.AutoFillCooldownSeconds, 0.05, 5);
            options.AutoFillStrongCooldownSeconds = ClampSeconds(options.AutoFillStrongCooldownSeconds, 0.03, 5);
            if (!options.AutoFillBottle)
                options.AutoFillStrong = false;
            return options;
        }

        private static double GetActionSpeedAutoFillCooldownSeconds(ActionSpeedOptions options)
        {
            return options.AutoFillStrong
                ? Math.Min(options.AutoFillCooldownSeconds, options.AutoFillStrongCooldownSeconds)
                : options.AutoFillCooldownSeconds;
        }

        private bool ApplyActionSpeedToBody(object body, string ownerId, ActionSpeedOptions options, string kind, double multiplier, string target, string source, string hookId)
        {
            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            int changed = 0;
            var samples = new List<string>();
            changed += ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);

            if (changed <= 0)
                return false;

            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId + ", kind=" + kind + ", target=" + target + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray());
            string logKey = ownerId + ":" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                runtime.RuntimeMonitor.Log("ActionSpeed interaction animation speed applied by " + ownerId + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(logKey);
            runtime.SetHookStatus(hookId, "experimental", source, LastActionSpeedApplicationSummary);
            return true;
        }

        private static bool IsBottleFillInteraction(object? selectedItem, object? selectedEquipment, bool isInWater)
        {
            if (selectedItem == null || !IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemBottle"))
                return false;
            return isInWater || ImplementsInterface(selectedEquipment?.GetType(), "DolocTown.IWaterContainer");
        }

        private static bool IsEmptyBottleItem(object? selectedItem, Type? dolocApi)
        {
            if (selectedItem == null || !IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemBottle"))
                return false;
            string itemName = ReadStringMember(selectedItem, "name");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            string wasteBottle = globalParameter == null ? string.Empty : ReadStringMember(globalParameter, "ItemRefWastePlasticBottle");
            return string.IsNullOrWhiteSpace(wasteBottle) || itemName.Equals(wasteBottle, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEatDrinkItem(object? selectedItem)
        {
            if (selectedItem == null)
                return false;

            Type itemType = selectedItem.GetType();
            if (ImplementsInterface(itemType, "DolocTown.IEatable"))
                return true;

            string typeName = itemType.FullName ?? itemType.Name;
            return typeName.IndexOf("ItemFood", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("ItemDrink", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBottledWaterItem(object? selectedItem, Type? dolocApi)
        {
            if (selectedItem == null)
                return false;
            string itemName = ReadStringMember(selectedItem, "name");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            string bottleOfWater = globalParameter == null ? string.Empty : ReadStringMember(globalParameter, "ItemRefBottleOfWater");
            return !string.IsNullOrWhiteSpace(itemName) &&
                !string.IsNullOrWhiteSpace(bottleOfWater) &&
                itemName.Equals(bottleOfWater, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPlantInteraction(object? selectedItem, object? selectedEquipment)
        {
            if (selectedItem == null || selectedEquipment == null)
                return false;
            if (!IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemSeed"))
                return false;
            Type equipmentType = selectedEquipment.GetType();
            if (ReadBoolMember(selectedEquipment, "IsPlanted", false) || ReadBoolMember(selectedEquipment, "CouldHarvest", false))
                return false;
            return IsTypeOrBase(equipmentType, "DolocTown.PlantBasin") || IsTypeOrBase(equipmentType, "DolocTown.FlowerPot");
        }

        private static bool IsMachineAddInteraction(object? selectedEquipment)
        {
            if (selectedEquipment == null)
                return false;
            Type type = selectedEquipment.GetType();
            return IsTypeOrBase(type, "DolocTown.PowerGeneratorFuel") || IsTypeOrBase(type, "DolocTown.Feeder");
        }

        private static bool IsHarvestInteraction(object? selectedEquipment, object? currentInteractable)
        {
            if (selectedEquipment != null)
            {
                Type equipmentType = selectedEquipment.GetType();
                if (IsTypeOrBase(equipmentType, "DolocTown.ResinCollector") && ReadIntMember(selectedEquipment, "currentValue", 0) > 0)
                    return true;
                if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasin") && ReadBoolMember(selectedEquipment, "CouldHarvest", false))
                    return true;
                if (ImplementsInterface(equipmentType, "DolocTown.IGatherableEquipment"))
                    return true;
            }

            if (currentInteractable == null)
                return false;
            Type interactableType = currentInteractable.GetType();
            string name = interactableType.FullName ?? interactableType.Name;
            return name.IndexOf("Vegetation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ImplementsInterface(interactableType, "DolocTown.IGatherableEquipment");
        }

        private static object? ReadCurrentActionSpeedInteractable(Type? dolocApi)
        {
            object? currentInteractable = ReadStaticMember(dolocApi, "CurrentInteractableObject");
            if (currentInteractable != null)
                return currentInteractable;

            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            object? interactableManager = agentController == null ? null : ReadMember(agentController, "interactableManager");
            if (interactableManager == null)
                return null;

            object? baseManager = ReadMember(interactableManager, "baseManager");
            object? current = baseManager == null ? null : ReadMember(baseManager, "Current");
            if (current != null)
                return UnwrapActionSpeedInteractable(current);

            object? subManagers = ReadMember(interactableManager, "subManagers");
            if (subManagers is IDictionary dictionary)
            {
                foreach (object? manager in dictionary.Values)
                {
                    current = manager == null ? null : ReadMember(manager, "Current");
                    if (current != null)
                        return UnwrapActionSpeedInteractable(current);
                }
            }

            return null;
        }

        private static object? UnwrapActionSpeedInteractable(object? interactable)
        {
            if (interactable == null)
                return null;

            object? vegetation = ReadMember(interactable, "Vegetation");
            if (vegetation != null)
                return vegetation;

            object? equipment = ReadMember(interactable, "equipment");
            return equipment ?? interactable;
        }

        private static string DescribeActionSpeedTarget(object? selectedItem, object? selectedEquipment, object? currentInteractable)
        {
            string item = selectedItem == null ? "none" : FirstText(ReadStringMember(selectedItem, "name"), selectedItem.GetType().Name);
            string equipment = selectedEquipment == null ? "none" : FirstText(ReadStringMember(selectedEquipment, "equipmentName"), selectedEquipment.GetType().Name);
            string interactable = currentInteractable == null ? "none" : FirstText(ReadStringMember(currentInteractable, "VegetationName"), currentInteractable.GetType().Name);
            return "item=" + item + ",equipment=" + equipment + ",interactable=" + interactable;
        }
    }
}
