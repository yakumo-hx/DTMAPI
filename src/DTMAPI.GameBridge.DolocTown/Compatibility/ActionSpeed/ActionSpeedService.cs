#pragma warning disable CS0618 // This file implements the frozen IActionSpeedApi compatibility island.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen 0.5.5 executor for already-built IActionSpeedApi consumers only.</summary>
    internal sealed class ActionSpeedService : IActionSpeedApi
    {
        private const string ManagedProductHarmonyOwner = "dtmapi.mod.yuuka.dtmapi.actionspeed";
        private readonly DtmApiRuntime runtime;
        private readonly Func<bool> managedProductOwnerPresent;
        private readonly Dictionary<string, ActionSpeedOptions> actionSpeedOptions = new Dictionary<string, ActionSpeedOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedActionSpeedApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, double> originalAnimatorSpeeds = new Dictionary<object, double>();
        private bool actionSpeedToolHooksInstalled;
        private bool actionSpeedInteractionHooksInstalled;
        private DateTimeOffset lastActionSpeedAutoFillAt = DateTimeOffset.MinValue;
        private DateTimeOffset pendingNativeAnimalInteractAt = DateTimeOffset.MinValue;
        private object? pendingNativeAnimalInteract;
        private int actionSpeedAutoFillApplications;

        public ActionSpeedService(DtmApiRuntime runtime)
            : this(runtime, IsManagedProductOwnerPresent)
        {
        }

        internal ActionSpeedService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.managedProductOwnerPresent = managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent));
        }

        internal int ActionSpeedApplicationCount { get; private set; }

        internal string LastActionSpeedApplicationSummary { get; private set; } = string.Empty;

        internal int ActionSpeedContinuousUseApplicationCount { get; private set; }

        internal string LastActionSpeedContinuousUseSummary { get; private set; } = string.Empty;

        internal string LastActionSpeedAutoFillSummary { get; private set; } = string.Empty;

        internal int ActionSpeedAutoFillApplicationCount => actionSpeedAutoFillApplications;

        internal string GetActionSpeedLifecycleSummary()
        {
            return "actionAnimators=" + originalAnimatorSpeeds.Count +
                ", actionAutoFillApplications=" + actionSpeedAutoFillApplications +
                ", actionPendingAnimalInteract=" + (pendingNativeAnimalInteract == null ? "false" : "true");
        }

        internal void SetActionSpeedToolHooksInstalled(bool installed)
        {
            actionSpeedToolHooksInstalled = installed;
        }

        internal void SetActionSpeedInteractionHooksInstalled(bool installed)
        {
            actionSpeedInteractionHooksInstalled = installed;
        }

        internal void Update()
        {
            // AutoFill-only legacy policies do not request a Hook-install pass. Reconcile
            // at the updater boundary too, before the frozen executor can process the
            // first frame after the managed product has acquired its exact Harmony owner.
            if (ReconcileManagedProductOwner("compatibility updater") > 0)
                return;
            UpdateActionSpeedAutoFill();
        }

        public void Configure(IManifest owner, ActionSpeedOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            ActionSpeedOptions normalized = NormalizeActionSpeedOptions(options);
            bool routeRequested = normalized.Enabled &&
                ((normalized.ToolSpeedEnabled && normalized.ToolMultiplier > 1) ||
                 (normalized.BottleFillSpeedEnabled && normalized.BottleFillMultiplier > 1) ||
                 (normalized.EatDrinkSpeedEnabled && normalized.EatDrinkMultiplier > 1) ||
                 (normalized.MachineAddSpeedEnabled && normalized.MachineAddMultiplier > 1) ||
                 (normalized.HarvestSpeedEnabled && normalized.HarvestMultiplier > 1) ||
                 (normalized.PlantSpeedEnabled && normalized.PlantMultiplier > 1) ||
                 (normalized.AutoFillBottle) ||
                 (normalized.ContinuousDrinkWithRightClick && normalized.EatDrinkMultiplier > 1));
            if (routeRequested)
                ThrowIfManagedProductOwnsActionSpeedRoute();
            actionSpeedOptions[owner.UniqueID] = normalized;
            bool toolDemand = normalized.Enabled && normalized.ToolSpeedEnabled && normalized.ToolMultiplier > 1;
            bool interactionDemand = normalized.Enabled &&
                ((normalized.BottleFillSpeedEnabled && normalized.BottleFillMultiplier > 1) ||
                 (normalized.EatDrinkSpeedEnabled && normalized.EatDrinkMultiplier > 1) ||
                 (normalized.MachineAddSpeedEnabled && normalized.MachineAddMultiplier > 1) ||
                 (normalized.HarvestSpeedEnabled && normalized.HarvestMultiplier > 1) ||
                 (normalized.PlantSpeedEnabled && normalized.PlantMultiplier > 1) ||
                 (normalized.ContinuousDrinkWithRightClick && normalized.EatDrinkMultiplier > 1));
            bool interactExitDemand = normalized.Enabled &&
                ((normalized.BottleFillSpeedEnabled && normalized.BottleFillMultiplier > 1) ||
                 (normalized.MachineAddSpeedEnabled && normalized.MachineAddMultiplier > 1) ||
                 (normalized.HarvestSpeedEnabled && normalized.HarvestMultiplier > 1) ||
                 (normalized.PlantSpeedEnabled && normalized.PlantMultiplier > 1));
            bool baseExitDemand = normalized.Enabled &&
                ((normalized.EatDrinkSpeedEnabled && normalized.EatDrinkMultiplier > 1) ||
                 (normalized.MachineAddSpeedEnabled && normalized.MachineAddMultiplier > 1));
            bool autoFillDemand = normalized.Enabled && normalized.AutoFillBottle;
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeed, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "policy", toolDemand || interactionDemand, "action speed native-stage policy configured");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeedToolStages, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "tool-stages", toolDemand, "action speed tool-stage hook group");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeedInteractionStages, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "interaction-stages", interactionDemand, "action speed interaction-stage hook group");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AgentStateToolExitShared, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "action-speed-tool-exit", toolDemand, "action speed AgentStateTool.OnExit dependency");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AgentStateInteractExitShared, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "action-speed-interact-exit", interactExitDemand, "action speed AgentStateInteract.OnExit dependency");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AgentStateBaseExitShared, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "action-speed-base-exit", baseExitDemand, "action speed AgentStateBase.OnExit fallback for EatDrink/animal-interact animator restoration");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeedAutoFill, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "auto-fill", autoFillDemand, "action speed auto-fill policy");
            runtime.RuntimeMonitor.Log("Action speed bridge configured by " + owner.UniqueID + ".");
        }

        internal int ReconcileManagedProductOwnerBeforeHookInstall()
        {
            return ReconcileManagedProductOwner("Hook installation");
        }

        private int ReconcileManagedProductOwner(string boundary)
        {
            // Keep the no-consumer path reflection-free. The managed-owner probe walks
            // Harmony target metadata and is useful only while a frozen policy exists.
            if (actionSpeedOptions.Count == 0 || !managedProductOwnerPresent())
                return 0;

            var owners = new List<string>(actionSpeedOptions.Keys);
            int removed = 0;
            foreach (string ownerId in owners)
                removed += RemoveOwner(ownerId, "managed ActionSpeed product acquired native ownership before " + (boundary ?? string.Empty));

            actionSpeedToolHooksInstalled = false;
            actionSpeedInteractionHooksInstalled = false;
            runtime.RuntimeMonitor.Log("Frozen IActionSpeedApi compatibility removed " + removed + " pending owner(s) at " + (boundary ?? string.Empty) + " because the managed ActionSpeed product owns the action-speed route.");
            return removed;
        }

        private void ThrowIfManagedProductOwnsActionSpeedRoute()
        {
            if (managedProductOwnerPresent())
                throw new InvalidOperationException("Frozen IActionSpeedApi compatibility refused action-speed ownership because the managed ActionSpeed product is active.");
        }

        private static bool IsManagedProductOwnerPresent()
        {
            return TargetHasManagedProductOwner("DolocTown.AgentStateTool, Assembly-CSharp", "OnEnter", 0) ||
                TargetHasManagedProductOwner("DolocTown.AgentStateInteract, Assembly-CSharp", "OnEnter", 0) ||
                TargetHasManagedProductOwner("DolocTown.AgentControllerState, Assembly-CSharp", "UseItemContinues", 1) ||
                TargetHasManagedProductOwner("DolocTown.AnimalRenderer, Assembly-CSharp", "OnInteract", 0);
        }

        private static bool TargetHasManagedProductOwner(string typeName, string methodName, int parameterCount)
        {
            Type? type = ResolveType(typeName);
            MethodInfo? target = null;
            for (Type? current = type; current != null && target == null; current = current.BaseType)
            {
                target = current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(method => method.Name == methodName && method.GetParameters().Length == parameterCount);
            }
            Type? harmonyType = ResolveType("HarmonyLib.Harmony, 0Harmony");
            MethodInfo? getPatchInfo = harmonyType?.GetMethod("GetPatchInfo", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(MethodBase) }, null);
            object? info = target == null || getPatchInfo == null ? null : getPatchInfo.Invoke(null, new object[] { target });
            object? ownerList = info?.GetType().GetProperty("Owners", BindingFlags.Public | BindingFlags.Instance)?.GetValue(info);
            if (!(ownerList is IEnumerable owners))
                return false;
            foreach (object? ownerValue in owners)
            {
                if ((ownerValue as string ?? string.Empty).Equals(ManagedProductHarmonyOwner, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeed, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "policy", false, "action speed owner cleanup " + (reason ?? string.Empty));
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeedToolStages, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "tool-stages", false, "action speed owner cleanup " + (reason ?? string.Empty));
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeedInteractionStages, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "interaction-stages", false, "action speed owner cleanup " + (reason ?? string.Empty));
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AgentStateToolExitShared, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "action-speed-tool-exit", false, "action speed owner cleanup " + (reason ?? string.Empty));
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AgentStateInteractExitShared, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "action-speed-interact-exit", false, "action speed owner cleanup " + (reason ?? string.Empty));
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AgentStateBaseExitShared, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "action-speed-base-exit", false, "action speed owner cleanup " + (reason ?? string.Empty));
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ActionSpeedAutoFill, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "auto-fill", false, "action speed owner cleanup " + (reason ?? string.Empty));
            if (!actionSpeedOptions.Remove(ownerId))
                return 0;
            loggedActionSpeedApplications.RemoveWhere(key => key.IndexOf(ownerId, StringComparison.OrdinalIgnoreCase) >= 0);
            RestoreActionSpeed("owner cleanup " + (reason ?? string.Empty));
            if (actionSpeedOptions.Count == 0)
            {
                actionSpeedAutoFillApplications = 0;
                lastActionSpeedAutoFillAt = DateTimeOffset.MinValue;
            }
            return 1;
        }

        internal int CountOwnerResources(string ownerId) => actionSpeedOptions.ContainsKey(ownerId ?? string.Empty) ? 1 : 0;

        internal bool RequiresToolHooks => actionSpeedOptions.Values.Any(HasToolStageDemand);

        internal bool RequiresInteractionHooks => actionSpeedOptions.Values.Any(HasInteractionStageDemand);

        internal bool RequiresInteractExit => actionSpeedOptions.Values.Any(HasInteractExitDemand);

        internal bool RequiresBaseExit => actionSpeedOptions.Values.Any(HasBaseExitDemand);

        BridgeFeatureStatus IActionSpeedApi.GetStatus(string uniqueId)
        {
            return actionSpeedOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(actionSpeedToolHooksInstalled ? "configured-experimental-runtime-hooks" : "configured-pending-tool-hook", actionSpeedToolHooksInstalled ? "Tool animation speed has historical smoke evidence. Native-stage interaction hooks are experimental in this run; water-container InteractContinues and planting/fertilizer/film paths require fresh third-save smoke/manual QA" + (actionSpeedInteractionHooksInstalled ? " and the interaction hooks are installed." : "; waiting for interaction hooks in this run.") : "Policy accepted; waiting for AgentStateTool hooks in this run.")
                : new BridgeFeatureStatus("not-configured", "No action-speed policy was registered for this mod.");
        }

        internal bool TryGetConfiguredActionSpeedOwner(out string ownerId)
        {
            return TryFindActionSpeedToolPolicy(out ownerId, out _);
        }

        internal bool TryGetConfiguredActionSpeedInteractionOwner(out string ownerId)
        {
            return TrySelectActionSpeedPolicy(
                candidate => (candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1) ||
                    (candidate.EatDrinkSpeedEnabled && candidate.EatDrinkMultiplier > 1) ||
                    (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1) ||
                    (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1) ||
                    (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1) ||
                    (candidate.AutoFillBottle && candidate.BottleFillMultiplier > 1) ||
                    (candidate.ContinuousDrinkWithRightClick && candidate.EatDrinkMultiplier > 1),
                candidate => Math.Max(
                    Math.Max(candidate.BottleFillMultiplier, candidate.EatDrinkMultiplier),
                    Math.Max(Math.Max(candidate.MachineAddMultiplier, candidate.HarvestMultiplier), candidate.PlantMultiplier)),
                out ownerId,
                out _,
                out _);
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

            return ScaleActionSpeedContinuousDelta(ref dt, ownerId, options, kind, multiplier, target, "UseItemContinues", "AgentControllerState.UseItemContinues Prefix");
        }

        internal bool AdjustActionSpeedInteractContinuesDelta(ref float dt)
        {
            if (dt <= 0 || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target))
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
            runtime.RuntimeMonitor.Log("ActionSpeed animator speeds restored reason=" + reason + " restored=" + restored + ".");
        }

        private void UpdateActionSpeedAutoFill()
        {
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
            return TrySelectActionSpeedPolicy(
                candidate => candidate.AutoFillBottle,
                candidate => candidate.AutoFillStrong ? 2 : 1,
                out ownerId,
                out options,
                out _,
                allowUnitMultiplier: true);
        }

        private bool TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            return TrySelectActionSpeedPolicy(
                candidate => candidate.ToolSpeedEnabled,
                candidate => candidate.ToolMultiplier,
                out ownerId,
                out options,
                out _);
        }

        private bool TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            return TrySelectActionSpeedPolicy(
                candidate => candidate.EatDrinkSpeedEnabled,
                candidate => candidate.EatDrinkMultiplier,
                out ownerId,
                out options,
                out _);
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

        private static bool HasToolStageDemand(ActionSpeedOptions candidate) => candidate != null &&
            candidate.Enabled &&
            candidate.ToolSpeedEnabled &&
            candidate.ToolMultiplier > 1;

        private static bool HasInteractionStageDemand(ActionSpeedOptions candidate) => candidate != null &&
            candidate.Enabled &&
            ((candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1) ||
             (candidate.EatDrinkSpeedEnabled && candidate.EatDrinkMultiplier > 1) ||
             (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1) ||
             (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1) ||
             (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1) ||
             (candidate.ContinuousDrinkWithRightClick && candidate.EatDrinkMultiplier > 1));

        private static bool HasInteractExitDemand(ActionSpeedOptions candidate) => candidate != null &&
            candidate.Enabled &&
            ((candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1) ||
             (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1) ||
             (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1) ||
             (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1));

        private static bool HasBaseExitDemand(ActionSpeedOptions candidate) => candidate != null &&
            candidate.Enabled &&
            ((candidate.EatDrinkSpeedEnabled && candidate.EatDrinkMultiplier > 1) ||
             (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1));

        private static double GetActionSpeedAutoFillCooldownSeconds(ActionSpeedOptions options)
        {
            return options.AutoFillStrong
                ? Math.Min(options.AutoFillCooldownSeconds, options.AutoFillStrongCooldownSeconds)
                : options.AutoFillCooldownSeconds;
        }

        internal static ActionSpeedOptions NormalizeActionSpeedOptionsForTest(ActionSpeedOptions? options)
        {
            return NormalizeActionSpeedOptions(options);
        }

        internal static bool TrySelectPolicyForTest(
            IReadOnlyDictionary<string, ActionSpeedOptions> policies,
            Func<ActionSpeedOptions, bool> isCandidate,
            Func<ActionSpeedOptions, double> getMultiplier,
            out string ownerId,
            out double multiplier)
        {
            return TrySelectActionSpeedPolicy(policies, isCandidate, getMultiplier, out ownerId, out _, out multiplier);
        }

        private bool TrySelectActionSpeedPolicy(Func<ActionSpeedOptions, bool> isCandidate, Func<ActionSpeedOptions, double> getMultiplier, out string ownerId, out ActionSpeedOptions options, out double multiplier, bool allowUnitMultiplier = false)
        {
            return TrySelectActionSpeedPolicy(actionSpeedOptions, isCandidate, getMultiplier, out ownerId, out options, out multiplier, allowUnitMultiplier);
        }

        private static bool TrySelectActionSpeedPolicy(IReadOnlyDictionary<string, ActionSpeedOptions> policies, Func<ActionSpeedOptions, bool> isCandidate, Func<ActionSpeedOptions, double> getMultiplier, out string ownerId, out ActionSpeedOptions options, out double multiplier, bool allowUnitMultiplier = false)
        {
            ownerId = string.Empty;
            options = null!;
            multiplier = 1;
            bool found = false;

            foreach (KeyValuePair<string, ActionSpeedOptions> entry in policies)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
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

        private bool ScaleActionSpeedContinuousDelta(ref float dt, string ownerId, ActionSpeedOptions options, string kind, double multiplier, string target, string nativeStage, string hookSource)
        {
            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            float original = dt;
            dt = (float)Math.Min(1, original * multiplier);
            string logKey = ownerId + ":" + nativeStage + ":" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                runtime.RuntimeMonitor.Log("ActionSpeed continuous native timer scaled by " + ownerId + " stage=" + nativeStage + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###") + ".");
            loggedActionSpeedApplications.Add(logKey);
            ActionSpeedApplicationCount++;
            ActionSpeedContinuousUseApplicationCount++;
            LastActionSpeedContinuousUseSummary = "owner=" + ownerId + ", stage=" + nativeStage + ", kind=" + kind + ", target=" + target + ", multiplier=" + multiplier.ToString("0.###") + ", dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###");
            LastActionSpeedApplicationSummary = LastActionSpeedContinuousUseSummary;
            runtime.SetHookStatus("Smoke.ActionSpeedContinuousUse", "experimental", hookSource, LastActionSpeedApplicationSummary);
            return true;
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
                !string.Equals(reason, "AgentStateTool.OnExit", StringComparison.Ordinal);
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

            object? toolType = ReadMember(tool, "ToolType");
            string name = toolType?.ToString() ?? string.Empty;
            return name.Equals("AXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("SICKLE", StringComparison.OrdinalIgnoreCase);
        }

        private int ApplyAnimatorSpeed(object? animator, double multiplier, string label, List<string> samples)
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

            if (samples.Count < 6)
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
}
