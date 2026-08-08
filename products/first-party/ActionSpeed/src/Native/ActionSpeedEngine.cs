using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.ActionSpeed
{
    internal sealed class ActionSpeedEngine
    {
        private const string ProductOwnerId = "Yuuka.DTMAPI.ActionSpeed";
        private readonly Dictionary<string, ActionSpeedPolicy> actionSpeedOptions = new Dictionary<string, ActionSpeedPolicy>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedActionSpeedApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, double> originalAnimatorSpeeds = new Dictionary<object, double>();
        private DateTimeOffset lastActionSpeedAutoFillAt = DateTimeOffset.MinValue;
        private DateTimeOffset pendingNativeAnimalInteractAt = DateTimeOffset.MinValue;
        private object? pendingNativeAnimalInteract;
        private ActionSpeedQaDiagnostics? qaDiagnostics;
        private bool hasLoggedAutoFillApplication;

        internal ActionSpeedEngine(IDtmHelper helper)
        {
            Monitor = (helper ?? throw new ArgumentNullException(nameof(helper))).Monitor;
        }

        internal IMonitor Monitor { get; }

        internal ActionSpeedQaDiagnostics EnableQaObservation() => qaDiagnostics ??= new ActionSpeedQaDiagnostics();

        internal void DisableQaObservation() => qaDiagnostics = null;

        internal string GetActionSpeedLifecycleSummary()
        {
            return "actionAnimators=" + originalAnimatorSpeeds.Count +
                ", actionAutoFillApplications=" + (qaDiagnostics?.ActionSpeedAutoFillApplicationCount ?? 0) +
                ", actionPendingAnimalInteract=" + (pendingNativeAnimalInteract == null ? "false" : "true");
        }

        internal void Update()
        {
            UpdateActionSpeedAutoFill();
        }

        internal void Configure(ActionSpeedConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            RestoreActionSpeed("configuration change");
            actionSpeedOptions.Clear();
            actionSpeedOptions[ProductOwnerId] = NormalizeActionSpeedPolicy(new ActionSpeedPolicy(config));
            loggedActionSpeedApplications.Clear();
            lastActionSpeedAutoFillAt = DateTimeOffset.MinValue;
            if (!config.Enabled)
                DisableQaObservation();
        }

        internal void ResetBoundary(string reason)
        {
            RestoreActionSpeed(reason ?? string.Empty);
            loggedActionSpeedApplications.Clear();
            lastActionSpeedAutoFillAt = DateTimeOffset.MinValue;
        }

        internal bool ApplyActionSpeedToolEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedPolicy options))
                return false;

            object? tool = ReadMember(state, "tool") ?? ReadMember(state, "waterCan");
            if (!IsAcceleratedTool(tool))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            double multiplier = ClampMultiplier(options.ToolMultiplier);
            int changed = 0;
            List<string>? samples = qaDiagnostics == null ? null : new List<string>();
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

            qaDiagnostics?.RecordToolApplication(ownerId, toolName, multiplier, changed, samples);
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(ownerId + ":" + toolName))
                Monitor.Log("ActionSpeed tool animation speed applied by " + ownerId + " tool=" + toolName + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(ownerId + ":" + toolName);
            return true;
        }

        internal bool ApplyActionSpeedInteractEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedPolicy options, out string kind, out double multiplier, out string target))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, kind, multiplier, target, "AgentStateInteract.OnEnter Postfix");
        }

        internal void MarkNativeAnimalInteract(object animalRenderer)
        {
            if (!IsAnimalFondleInteraction(animalRenderer))
                return;

            pendingNativeAnimalInteract = animalRenderer;
            pendingNativeAnimalInteractAt = DateTimeOffset.Now;
        }

        internal bool ApplyActionSpeedEatEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedPolicy options))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, "EatDrink", options.EatDrinkMultiplier, "AgentStateEat", "AgentStateEat.OnEnter Postfix");
        }

        internal bool AdjustActionSpeedUseItemContinuesDelta(ref float dt)
        {
            if (dt <= 0 || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedPolicy options, out string kind, out double multiplier, out string target))
                return false;

            return ScaleActionSpeedContinuousDelta(ref dt, ownerId, options, kind, multiplier, target, "UseItemContinues", "AgentControllerState.UseItemContinues Prefix");
        }

        internal bool AdjustActionSpeedInteractContinuesDelta(ref float dt)
        {
            if (dt <= 0 || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedPolicy options, out string kind, out double multiplier, out string target))
                return false;

            return ScaleActionSpeedContinuousDelta(ref dt, ownerId, options, kind, multiplier, target, "InteractContinues", "AgentControllerState.InteractContinues Prefix");
        }

        internal void RestoreActionSpeed(string reason)
        {
            if (ShouldClearPendingNativeAnimalInteract(reason))
                ClearPendingNativeAnimalInteract();
            if (originalAnimatorSpeeds.Count == 0)
                return;

            int restored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalAnimatorSpeeds))
            {
                if (TryWriteAnimatorSpeed(entry.Key, entry.Value))
                    restored++;
            }
            originalAnimatorSpeeds.Clear();
            Monitor.Log("ActionSpeed animator speeds restored reason=" + reason + " restored=" + restored + ".");
        }

        private void UpdateActionSpeedAutoFill()
        {
            if (!TryFindActionSpeedAutoFillPolicy(out string ownerId, out ActionSpeedPolicy options))
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
                double normalCooldownSeconds = options.AutoFillCooldownSeconds;
                double strongCooldownSeconds = Math.Min(options.AutoFillCooldownSeconds, options.AutoFillStrongCooldownSeconds);
                qaDiagnostics?.RecordAutoFillApplication(
                    ownerId,
                    ReadStringMember(selectedItem!, "name"),
                    options.AutoFillStrong,
                    cooldownSeconds,
                    normalCooldownSeconds,
                    strongCooldownSeconds);
                if (options.VerboseLogging || !hasLoggedAutoFillApplication)
                    Monitor.Log("ActionSpeed auto-fill invoked native ItemBottle.UseAsItem by " + ownerId +
                        " item=" + ReadStringMember(selectedItem!, "name") +
                        " strong=" + options.AutoFillStrong +
                        " cooldownSeconds=" + cooldownSeconds.ToString("0.###") + ".");
                hasLoggedAutoFillApplication = true;
            }
            catch (Exception ex)
            {
                Monitor.Log("ActionSpeed auto-fill bottle failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private bool TryFindActionSpeedAutoFillPolicy(out string ownerId, out ActionSpeedPolicy options)
        {
            return TrySelectActionSpeedPolicy(
                candidate => candidate.AutoFillBottle,
                candidate => candidate.AutoFillStrong ? 2 : 1,
                out ownerId,
                out options,
                out _,
                allowUnitMultiplier: true);
        }

        private bool TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedPolicy options)
        {
            return TrySelectActionSpeedPolicy(
                candidate => candidate.ToolSpeedEnabled,
                candidate => candidate.ToolMultiplier,
                out ownerId,
                out options,
                out _);
        }

        private bool TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedPolicy options)
        {
            return TrySelectActionSpeedPolicy(
                candidate => candidate.EatDrinkSpeedEnabled,
                candidate => candidate.EatDrinkMultiplier,
                out ownerId,
                out options,
                out _);
        }

        private bool TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedPolicy options, out string kind, out double multiplier, out string target)
        {
            ownerId = string.Empty;
            options = null!;
            kind = string.Empty;
            multiplier = 1;
            target = string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            object? selectedEquipment = ReadSelectedItemEquipment(selectedItem, allowSourceFallback: false) ?? ReadStaticMember(dolocApi, "SelectedEquipment");
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);
            object? nativeAnimalInteract = ReadPendingNativeAnimalInteract();

            if (IsBottleFillInteraction(selectedItem, selectedEquipment, currentInteractable, isInWater) &&
                TrySelectActionSpeedPolicy(candidate => candidate.BottleFillSpeedEnabled, candidate => candidate.BottleFillMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "BottleFill";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable) + ",nativeOwner=" + DescribeBottleFillNativeOwner(selectedItem, selectedEquipment, currentInteractable, isInWater);
                return true;
            }

            if (IsPlantInteraction(selectedItem, selectedEquipment, currentInteractable, out string plantNativeOwner) &&
                TrySelectActionSpeedPolicy(candidate => candidate.PlantSpeedEnabled, candidate => candidate.PlantMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "Plant";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable) + ",nativeOwner=" + plantNativeOwner;
                return true;
            }

            if (IsPlantActionItem(selectedItem))
                return false;

            object? animalInteractTarget = SelectAnimalFondleInteractionTarget(nativeAnimalInteract, currentInteractable);
            if (animalInteractTarget != null &&
                TrySelectActionSpeedPolicy(candidate => candidate.MachineAddSpeedEnabled, candidate => candidate.MachineAddMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "AnimalInteract";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, animalInteractTarget);
                if (nativeAnimalInteract != null && !ReferenceEquals(nativeAnimalInteract, currentInteractable))
                    target += ",scanner=" + DescribeActionSpeedObject(currentInteractable);
                target += ",nativeOwner=AnimalRenderer.OnInteract->_HandleInteract->BodyController._Interact->Animal.Fondle";
                return true;
            }

            if (IsMachineInteraction(selectedEquipment, currentInteractable, out string machineNativeOwner) &&
                TrySelectActionSpeedPolicy(candidate => candidate.MachineAddSpeedEnabled, candidate => candidate.MachineAddMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "MachineInteract";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable) + ",nativeOwner=" + machineNativeOwner;
                return true;
            }

            if (IsHarvestInteraction(selectedEquipment, currentInteractable, out string harvestNativeOwner) &&
                TrySelectActionSpeedPolicy(candidate => candidate.HarvestSpeedEnabled, candidate => candidate.HarvestMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "Harvest";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable) + ",nativeOwner=" + harvestNativeOwner;
                return true;
            }

            return false;
        }

        private bool TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedPolicy options, out string kind, out double multiplier, out string target)
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
            selectedEquipment = ReadSelectedItemEquipment(selectedItem, allowSourceFallback: false) ?? selectedEquipment;
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);

            if (IsBottleFillInteraction(selectedItem, selectedEquipment, currentInteractable, isInWater) &&
                TrySelectActionSpeedPolicy(candidate => candidate.BottleFillSpeedEnabled || candidate.AutoFillBottle, candidate => candidate.BottleFillMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "BottleFill";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable) + ",nativeOwner=" + DescribeBottleFillNativeOwner(selectedItem, selectedEquipment, currentInteractable, isInWater);
                return true;
            }

            if (IsPlantInteraction(selectedItem, selectedEquipment, currentInteractable, out string plantNativeOwner) &&
                TrySelectActionSpeedPolicy(candidate => candidate.PlantSpeedEnabled, candidate => candidate.PlantMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "Plant";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable) + ",nativeOwner=" + plantNativeOwner;
                return true;
            }

            if (IsPlantActionItem(selectedItem))
                return false;

            if (IsBottledWaterItem(selectedItem, dolocApi) &&
                TrySelectActionSpeedPolicy(candidate => candidate.ContinuousDrinkWithRightClick, candidate => candidate.EatDrinkMultiplier, out ownerId, out options, out multiplier))
            {
                kind = "BottledWaterDrink";
                target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable) + ",nativeOwner=AgentControllerState.UseItemContinues->ItemBottle.OnUseAsItem";
                return true;
            }

            return false;
        }

        private static ActionSpeedPolicy NormalizeActionSpeedPolicy(ActionSpeedPolicy? options)
        {
            options ??= new ActionSpeedPolicy();
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

        private static double GetActionSpeedAutoFillCooldownSeconds(ActionSpeedPolicy options)
        {
            return options.AutoFillStrong
                ? Math.Min(options.AutoFillCooldownSeconds, options.AutoFillStrongCooldownSeconds)
                : options.AutoFillCooldownSeconds;
        }

        internal static ActionSpeedPolicy NormalizeActionSpeedPolicyForTest(ActionSpeedPolicy? options)
        {
            return NormalizeActionSpeedPolicy(options);
        }

        internal static bool TrySelectPolicyForTest(
            IReadOnlyDictionary<string, ActionSpeedPolicy> policies,
            Func<ActionSpeedPolicy, bool> isCandidate,
            Func<ActionSpeedPolicy, double> getMultiplier,
            out string ownerId,
            out double multiplier)
        {
            return TrySelectActionSpeedPolicy(policies, isCandidate, getMultiplier, out ownerId, out _, out multiplier);
        }

        private bool TrySelectActionSpeedPolicy(Func<ActionSpeedPolicy, bool> isCandidate, Func<ActionSpeedPolicy, double> getMultiplier, out string ownerId, out ActionSpeedPolicy options, out double multiplier, bool allowUnitMultiplier = false)
        {
            return TrySelectActionSpeedPolicy(actionSpeedOptions, isCandidate, getMultiplier, out ownerId, out options, out multiplier, allowUnitMultiplier);
        }

        private static bool TrySelectActionSpeedPolicy(IReadOnlyDictionary<string, ActionSpeedPolicy> policies, Func<ActionSpeedPolicy, bool> isCandidate, Func<ActionSpeedPolicy, double> getMultiplier, out string ownerId, out ActionSpeedPolicy options, out double multiplier, bool allowUnitMultiplier = false)
        {
            ownerId = string.Empty;
            options = null!;
            multiplier = 1;
            bool found = false;

            foreach (KeyValuePair<string, ActionSpeedPolicy> entry in policies)
            {
                ActionSpeedPolicy candidate = entry.Value ?? new ActionSpeedPolicy();
                double candidateMultiplier = ClampMultiplier(getMultiplier(candidate));
                if (!candidate.Enabled || !isCandidate(candidate) || (!allowUnitMultiplier && candidateMultiplier <= 1))
                    continue;

                if (!found ||
                    candidateMultiplier > multiplier ||
                    (Math.Abs(candidateMultiplier - multiplier) < 0.0001 && string.Compare(entry.Key, ownerId, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    found = true;
                    ownerId = entry.Key;
                    options = candidate;
                    multiplier = candidateMultiplier;
                }
            }

            return found;
        }

        private bool ScaleActionSpeedContinuousDelta(ref float dt, string ownerId, ActionSpeedPolicy options, string kind, double multiplier, string target, string nativeStage, string hookSource)
        {
            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            float original = dt;
            dt = (float)Math.Min(1, original * multiplier);
            string logKey = ownerId + ":" + nativeStage + ":" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                Monitor.Log("ActionSpeed continuous native timer scaled by " + ownerId + " stage=" + nativeStage + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###") + ".");
            loggedActionSpeedApplications.Add(logKey);
            qaDiagnostics?.RecordContinuousUseApplication(ownerId, nativeStage, kind, target, multiplier, original, dt);
            return true;
        }

        private bool ApplyActionSpeedToBody(object body, string ownerId, ActionSpeedPolicy options, string kind, double multiplier, string target, string source)
        {
            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            int changed = 0;
            List<string>? samples = qaDiagnostics == null ? null : new List<string>();
            changed += ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);

            if (changed <= 0)
                return false;

            qaDiagnostics?.RecordInteractionApplication(ownerId, kind, target, multiplier, changed, samples);
            string logKey = ownerId + ":" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                Monitor.Log("ActionSpeed interaction animation speed applied by " + ownerId + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(logKey);
            return true;
        }

        private static bool IsBottleFillInteraction(object? selectedItem, object? selectedEquipment, object? currentInteractable, bool isInWater)
        {
            bool itemBottle = selectedItem != null && IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemBottle");
            bool selectedWaterContainer = IsWaterContainer(selectedEquipment);
            bool interactableWaterContainer = IsWaterContainer(currentInteractable);
            return (itemBottle && (isInWater || selectedWaterContainer || interactableWaterContainer)) || selectedWaterContainer || interactableWaterContainer;
        }

        private static string DescribeBottleFillNativeOwner(object? selectedItem, object? selectedEquipment, object? currentInteractable, bool isInWater)
        {
            if (IsWaterContainer(selectedEquipment) || IsWaterContainer(currentInteractable))
                return "AgentControllerState.UseItemContinues->ItemBottle.DrawWaterInContainer->IWaterContainer";
            if (selectedItem != null && IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemBottle") && isInWater)
                return "AgentControllerState.UseItemContinues->ItemBottle.DrawWaterInWater";
            return "ItemBottle.DrawWater";
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

        internal static bool IsPlantInteractionForTest(object? selectedItem, object? selectedEquipment, object? currentInteractable, out string nativeOwner)
        {
            return IsPlantInteraction(selectedItem, selectedEquipment, currentInteractable, out nativeOwner);
        }

        internal static bool IsMachineInteractionForTest(object? selectedEquipment, object? currentInteractable, out string nativeOwner)
        {
            return IsMachineInteraction(selectedEquipment, currentInteractable, out nativeOwner);
        }

        internal static bool IsAnimalFondleInteractionForTest(object? currentInteractable)
        {
            return IsAnimalFondleInteraction(currentInteractable);
        }

        internal static bool IsAnimalFondleInteractionForTest(object? currentInteractable, object? nativeOwnerCandidate)
        {
            return SelectAnimalFondleInteractionTarget(nativeOwnerCandidate, currentInteractable) != null;
        }

        internal static bool IsHarvestInteractionForTest(object? selectedEquipment, object? currentInteractable, out string nativeOwner)
        {
            return IsHarvestInteraction(selectedEquipment, currentInteractable, out nativeOwner);
        }

        internal static bool ShouldClearPendingNativeAnimalInteractForTest(string reason)
        {
            return ShouldClearPendingNativeAnimalInteract(reason);
        }

        private static bool IsPlantInteraction(object? selectedItem, object? selectedEquipment, object? currentInteractable, out string nativeOwner)
        {
            nativeOwner = string.Empty;
            if (selectedItem == null)
                return false;

            Type itemType = selectedItem.GetType();
            if (IsTypeOrBase(itemType, "DolocTown.ItemSeedTree"))
            {
                object? treeTarget = FindActionSpeedTarget(candidate => IsTypeOrBase(candidate.GetType(), "DolocTown.PlantBasinTree"), selectedEquipment, currentInteractable);
                if (treeTarget != null)
                {
                    if (ReadMember(treeTarget, "Crop") != null)
                        return false;
                    nativeOwner = "AgentStateInteract/AgentControllerState.UseItemContinues->PlantBasinTree.TryPlantCrop";
                    return true;
                }
                return false;
            }

            if (IsSeedItem(itemType))
            {
                object? target = FindActionSpeedTarget(candidate => IsPlantTarget(candidate.GetType()), selectedEquipment, currentInteractable);
                if (target == null)
                    return false;

                Type targetType = target.GetType();
                if (IsTypeOrBase(targetType, "DolocTown.PlantBasin") && (ReadBoolMember(target, "IsPlanted", false) || ReadBoolMember(target, "CouldHarvest", false)))
                    return false;
                nativeOwner = IsTypeOrBase(targetType, "DolocTown.FlowerPot")
                    ? "AgentStateInteract/AgentControllerState.UseItemContinues->FlowerPot.Plant"
                    : "AgentStateInteract/AgentControllerState.UseItemContinues->PlantBasin.Plant";
                return true;
            }

            if (IsTypeOrBase(itemType, "DolocTown.ItemFertilizer"))
            {
                bool treeFertilizer = IsTreeFertilizerItem(selectedItem);
                object? target = treeFertilizer
                    ? FindActionSpeedTarget(candidate => IsTypeOrBase(candidate.GetType(), "DolocTown.PlantBasinTree"), selectedEquipment, currentInteractable)
                    : FindActionSpeedTarget(candidate => IsTypeOrBase(candidate.GetType(), "DolocTown.PlantBasin"), selectedEquipment, currentInteractable);
                if (target == null)
                    return false;

                if (treeFertilizer)
                {
                    object? crop = ReadMember(target, "Crop");
                    string treeState = crop == null
                        ? "no-crop"
                        : ReadBoolMember(crop, "IsFertilizered", false) ? "already-fertilized" : "apply";
                    nativeOwner = "AgentStateInteract/AgentControllerState.UseItemContinues->PlantBasinTree.Fertilizer(state=" + treeState + ")";
                    return true;
                }

                string basinState = ReadBoolMember(target, "IsFertilizerd", false) ? "already-fertilized" : "apply";
                nativeOwner = "AgentStateInteract/AgentControllerState.UseItemContinues->PlantBasin.Fertilizer(state=" + basinState + ")";
                return true;
            }

            if (IsTypeOrBase(itemType, "DolocTown.ItemFilm"))
            {
                object? target = FindActionSpeedTarget(candidate => IsTypeOrBase(candidate.GetType(), "DolocTown.PlantBasin"), selectedEquipment, currentInteractable);
                if (target == null)
                    return false;
                string state = IsPlantBasinProtectedFull(target)
                    ? "already-full"
                    : ReadBoolMember(target, "IsProtected", false) ? "extend" : "apply";
                nativeOwner = "AgentStateInteract/AgentControllerState.UseItemContinues->PlantBasin.Protect(state=" + state + ")";
                return true;
            }

            return false;
        }

        private static bool IsWaterContainer(object? candidate)
        {
            return ImplementsInterface(candidate?.GetType(), "DolocTown.IWaterContainer");
        }

        private static bool IsSeedItem(Type type)
        {
            string name = type.FullName ?? type.Name;
            return IsTypeOrBase(type, "DolocTown.ItemSeed") ||
                IsTypeOrBase(type, "DolocTown.ItemSeedTree") ||
                name.IndexOf("ItemSeed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPlantActionItem(object? selectedItem)
        {
            if (selectedItem == null)
                return false;
            Type type = selectedItem.GetType();
            return IsSeedItem(type) ||
                IsTypeOrBase(type, "DolocTown.ItemFertilizer") ||
                IsTypeOrBase(type, "DolocTown.ItemFilm");
        }

        private static bool IsPlantTarget(Type type)
        {
            return IsTypeOrBase(type, "DolocTown.PlantBasin") || IsTypeOrBase(type, "DolocTown.FlowerPot");
        }

        private static bool IsTreeFertilizerItem(object selectedItem)
        {
            object? func = ReadMember(selectedItem, "func");
            return func != null && ReadBoolMember(func, "IsTree", false);
        }

        private static bool IsPlantBasinProtectedFull(object target)
        {
            object? supply = ReadMember(target, "supply");
            return supply != null && ReadBoolMember(supply, "IsProtectedFull", false);
        }

        private static bool IsMachineInteraction(object? selectedEquipment, object? currentInteractable, out string nativeOwner)
        {
            nativeOwner = string.Empty;
            object? target = FindActionSpeedTarget(candidate =>
            {
                Type type = candidate.GetType();
                return IsTypeOrBase(type, "DolocTown.PowerGeneratorFuel") ||
                    IsTypeOrBase(type, "DolocTown.Feeder") ||
                    IsTypeOrBase(type, "DolocTown.Sprinkler") ||
                    IsTypeOrBase(type, "DolocTown.FarmLight");
            }, selectedEquipment, currentInteractable);
            if (target == null)
                return false;

            Type targetType = target.GetType();
            if (IsTypeOrBase(targetType, "DolocTown.PowerGeneratorFuel") || IsTypeOrBase(targetType, "DolocTown.Feeder"))
                nativeOwner = "AgentStateInteract.OnEnter->native fuel/feed add";
            else
                nativeOwner = "AffectorElectric.OnInteract->BodyController._Interact switch";
            return true;
        }

        private static bool IsAnimalFondleInteraction(object? currentInteractable)
        {
            return currentInteractable != null && IsTypeOrBase(currentInteractable.GetType(), "DolocTown.AnimalRenderer");
        }

        private static object? SelectAnimalFondleInteractionTarget(object? nativeOwnerCandidate, object? currentInteractable)
        {
            if (IsAnimalFondleInteraction(nativeOwnerCandidate))
                return nativeOwnerCandidate;
            return IsAnimalFondleInteraction(currentInteractable) ? currentInteractable : null;
        }

        private object? ReadPendingNativeAnimalInteract()
        {
            object? target = pendingNativeAnimalInteract;
            DateTimeOffset markedAt = pendingNativeAnimalInteractAt;
            if (target == null)
                return null;
            if ((DateTimeOffset.Now - markedAt).TotalSeconds > 2)
            {
                ClearPendingNativeAnimalInteract();
                return null;
            }
            return IsAnimalFondleInteraction(target) ? target : null;
        }

        private static bool ShouldClearPendingNativeAnimalInteract(string reason)
        {
            return !string.Equals(reason, "AgentStateBase.OnExit", StringComparison.Ordinal) &&
                !string.Equals(reason, "AgentStateTool.OnExit", StringComparison.Ordinal) &&
                !string.Equals(reason, "AgentStateWater.OnExit", StringComparison.Ordinal);
        }

        private void ClearPendingNativeAnimalInteract()
        {
            pendingNativeAnimalInteract = null;
            pendingNativeAnimalInteractAt = DateTimeOffset.MinValue;
        }

        private static object? FindActionSpeedTarget(Func<object, bool> predicate, params object?[] candidates)
        {
            foreach (object? candidate in candidates)
            {
                if (candidate != null && predicate(candidate))
                    return candidate;
            }
            return null;
        }

        private static bool IsHarvestInteraction(object? selectedEquipment, object? currentInteractable, out string nativeOwner)
        {
            nativeOwner = string.Empty;

            object? target = FindActionSpeedTarget(candidate => IsResinCollectorReady(candidate), selectedEquipment, currentInteractable);
            if (target != null)
            {
                nativeOwner = "ResinCollector.OnInteract->BodyController._Interact->Collect";
                return true;
            }

            target = FindActionSpeedTarget(candidate =>
            {
                Type type = candidate.GetType();
                return IsTypeOrBase(type, "DolocTown.PlantBasin") && ReadBoolMember(candidate, "CouldHarvest", false);
            }, selectedEquipment, currentInteractable);
            if (target != null)
            {
                nativeOwner = "PlantBasin.OnInteract->BodyController._Interact->Harvest";
                return true;
            }

            target = FindActionSpeedTarget(candidate =>
            {
                Type type = candidate.GetType();
                return !IsTypeOrBase(type, "DolocTown.ResinCollector") &&
                    ImplementsInterface(type, "DolocTown.IGatherableEquipment");
            }, selectedEquipment, currentInteractable);
            if (target != null)
            {
                nativeOwner = "IGatherableEquipment.OnInteract->BodyController._Interact->Gather";
                return true;
            }

            target = FindActionSpeedTarget(candidate =>
            {
                string name = candidate.GetType().FullName ?? candidate.GetType().Name;
                return name.IndexOf("Vegetation", StringComparison.OrdinalIgnoreCase) >= 0;
            }, currentInteractable);
            if (target != null)
            {
                nativeOwner = "VegetationRenderer.OnInteract->Vegetation.OnInteract";
                return true;
            }

            return false;
        }

        private static bool IsResinCollectorReady(object? target)
        {
            if (target == null || !IsTypeOrBase(target.GetType(), "DolocTown.ResinCollector"))
                return false;
            return ReadIntMember(target, "currentValue", 0) > 0 || ReadBoolMember(target, "IsGatherable", false);
        }

        private static object? ReadCurrentActionSpeedInteractable(Type? dolocApi)
        {
            object? currentInteractable = ReadStaticMember(dolocApi, "CurrentInteractableObject");
            if (currentInteractable != null)
                return UnwrapActionSpeedInteractable(currentInteractable);

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

        private static object? ReadSelectedItemEquipment(object? selectedItem, bool allowSourceFallback)
        {
            if (selectedItem == null)
                return null;
            object? selectedEquipment = ReadMember(selectedItem, "SelectedEquipment");
            if (selectedEquipment != null)
                return selectedEquipment;
            if (!allowSourceFallback)
                return null;
            return ReadMember(selectedItem, "SelectedEquipmentSource");
        }

        private static string DescribeActionSpeedTarget(object? selectedItem, object? selectedEquipment, object? currentInteractable)
        {
            string item = selectedItem == null ? "none" : FirstText(ReadStringMember(selectedItem, "name"), selectedItem.GetType().Name);
            string equipment = selectedEquipment == null ? "none" : FirstText(ReadStringMember(selectedEquipment, "equipmentName"), selectedEquipment.GetType().Name);
            string interactable = DescribeActionSpeedObject(currentInteractable);
            return "item=" + item + ",equipment=" + equipment + ",interactable=" + interactable;
        }

        private static string DescribeActionSpeedObject(object? value)
        {
            if (value == null)
                return "none";
            return FirstText(
                ReadStringMember(value, "VegetationName"),
                ReadStringMember(value, "equipmentName"),
                value.GetType().Name);
        }

        private static bool IsAcceleratedTool(object? tool)
        {
            if (tool == null)
                return false;

            Type type = tool.GetType();
            if (IsTypeOrBase(type, "DolocTown.ItemWaterCan"))
                return true;

            object? toolType = ReadMember(tool, "ToolType");
            string name = toolType?.ToString() ?? string.Empty;
            return name.Equals("AXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("SICKLE", StringComparison.OrdinalIgnoreCase);
        }

        private int ApplyAnimatorSpeed(object? animator, double multiplier, string label, List<string>? samples)
        {
            if (animator == null)
                return 0;

            double original = ReadAnimatorSpeed(animator, 1);
            if (!originalAnimatorSpeeds.ContainsKey(animator))
                originalAnimatorSpeeds[animator] = original;
            else
                original = originalAnimatorSpeeds[animator];

            double target = original * multiplier;
            if (!TryWriteAnimatorSpeed(animator, target))
                return 0;

            if (samples != null && samples.Count < 6)
                samples.Add(label + ":" + original.ToString("0.###") + "->" + target.ToString("0.###"));
            return 1;
        }

        private static double ReadAnimatorSpeed(object animator, double fallback)
        {
            object? value = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animator);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        private static bool TryWriteAnimatorSpeed(object animator, double value)
        {
            try
            {
                PropertyInfo? speed = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance);
                if (speed == null || !speed.CanWrite)
                    return false;
                speed.SetValue(animator, Convert.ChangeType(value, speed.PropertyType));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static double ClampMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            return Math.Min(4, Math.Max(1, value));
        }

        private static double ClampSeconds(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return min;
            return Math.Min(max, Math.Max(min, value));
        }

        private static int ReadIntMember(object instance, string name, int fallback)
        {
            object? value = ReadMember(instance, name);
            if (value == null)
                return fallback;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static Type? ResolveType(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string typeName = comma >= 0 ? assemblyQualifiedName.Substring(0, comma).Trim() : assemblyQualifiedName;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static bool IsTypeOrBase(Type type, string fullName)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool ImplementsInterface(Type? type, string fullName)
        {
            if (type == null)
                return false;
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (string.Equals(interfaceType.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                try
                {
                    object? value = field.GetValue(null);
                    if (value != null)
                        return value;
                }
                catch
                {
                }
            }

            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null)
            {
                try
                {
                    return property.GetValue(null);
                }
                catch
                {
                }
            }
            return null;
        }

        private static bool ReadStaticBoolMember(Type? type, string name, bool fallback)
        {
            object? value = ReadStaticMember(type, name);
            return value is bool result ? result : fallback;
        }

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private static string ReadStringMember(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            return value as string ?? string.Empty;
        }

        private static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        private static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        object? value = field.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    try
                    {
                        object? value = property.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }
    }

    internal sealed class ActionSpeedQaDiagnostics
    {
        internal int ActionSpeedApplicationCount { get; private set; }
        internal int ActionSpeedContinuousUseApplicationCount { get; private set; }
        internal int ActionSpeedAutoFillApplicationCount { get; private set; }
        internal string LastActionSpeedApplicationSummary { get; private set; } = string.Empty;
        internal string LastActionSpeedContinuousUseSummary { get; private set; } = string.Empty;
        internal string LastActionSpeedAutoFillSummary { get; private set; } = string.Empty;

        internal void RecordToolApplication(
            string ownerId,
            string toolName,
            double multiplier,
            int changed,
            List<string>? samples)
        {
            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId +
                ", tool=" + toolName +
                ", multiplier=" + multiplier.ToString("0.###") +
                ", animators=" + changed +
                ", samples=" + FormatSamples(samples);
        }

        internal void RecordInteractionApplication(
            string ownerId,
            string kind,
            string target,
            double multiplier,
            int changed,
            List<string>? samples)
        {
            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId +
                ", kind=" + kind +
                ", target=" + target +
                ", multiplier=" + multiplier.ToString("0.###") +
                ", animators=" + changed +
                ", samples=" + FormatSamples(samples);
        }

        internal void RecordContinuousUseApplication(
            string ownerId,
            string nativeStage,
            string kind,
            string target,
            double multiplier,
            float original,
            float adjusted)
        {
            ActionSpeedApplicationCount++;
            ActionSpeedContinuousUseApplicationCount++;
            LastActionSpeedContinuousUseSummary = "owner=" + ownerId +
                ", stage=" + nativeStage +
                ", kind=" + kind +
                ", target=" + target +
                ", multiplier=" + multiplier.ToString("0.###") +
                ", dt=" + original.ToString("0.###") + "->" + adjusted.ToString("0.###");
            LastActionSpeedApplicationSummary = LastActionSpeedContinuousUseSummary;
        }

        internal void RecordAutoFillApplication(
            string ownerId,
            string itemName,
            bool strong,
            double cooldownSeconds,
            double normalCooldownSeconds,
            double strongCooldownSeconds)
        {
            ActionSpeedApplicationCount++;
            ActionSpeedAutoFillApplicationCount++;
            LastActionSpeedAutoFillSummary = "owner=" + ownerId +
                ", behavior=AutoFillBottle" +
                ", item=" + itemName +
                ", inWater=true" +
                ", strong=" + strong +
                ", cooldownSeconds=" + cooldownSeconds.ToString("0.###") +
                ", normalCooldownSeconds=" + normalCooldownSeconds.ToString("0.###") +
                ", strongCooldownSeconds=" + strongCooldownSeconds.ToString("0.###") +
                ", applications=" + ActionSpeedAutoFillApplicationCount;
            LastActionSpeedApplicationSummary = LastActionSpeedAutoFillSummary;
        }

        private static string FormatSamples(List<string>? samples) =>
            samples == null || samples.Count == 0 ? string.Empty : string.Join(";", samples.ToArray());
    }
}
