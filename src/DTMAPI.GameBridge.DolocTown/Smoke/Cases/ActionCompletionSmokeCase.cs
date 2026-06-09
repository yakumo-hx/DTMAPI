using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private SmokeAttemptResult TryExerciseOneActionResourceHitForSmoke()
        {
            try
            {
                ActionCompletionService? actionCompletion = ActionCompletionService;
                if (actionCompletion == null)
                    throw new InvalidOperationException("ActionCompletion feature service was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                Type? resourceRendererType = patcher.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null || resourceRendererType == null)
                    throw new MissingMemberException("DolocAPI, ToolCollider, or DungeonResourceRenderer was not visible.");

                if (!TryPrepareOneActionResourceHitWorldForSmoke(dolocApi, resourceRendererType, out string pendingReason))
                {
                    if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds >= 5)
                    {
                        lastOneActionReadinessLog = DateTimeOffset.Now;
                        runtime.RuntimeMonitor.Log("Smoke one-action resource-hit waiting: " + pendingReason);
                        runtime.SetHookStatus("Smoke.OneActionResourceHit", "pending", "ToolCollider.HandleTools", pendingReason);
                    }
                    return SmokeAttemptResult.Pending;
                }

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

                int rendererCount = 0;
                int resourceCount = 0;
                int policyMatchCount = 0;
                int colliderCount = 0;
                int toolCount = 0;
                int invokedCount = 0;
                List<string> samples = new List<string>();

                foreach (object renderer in FindUnityObjects(resourceRendererType))
                {
                    rendererCount++;
                    object? resource = ReadMember(renderer, "DungeonResource");
                    if (resource == null || IsRemoved(resource))
                        continue;
                    resourceCount++;
                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (healthBefore <= 0)
                        continue;
                    bool policyMatch = actionCompletion.TryFindOneActionPolicyForSmoke(resource, out string ownerId);
                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    if (samples.Count < 8)
                        samples.Add((resource.GetType().FullName ?? resource.GetType().Name) + "/" + resourceName + "/class=" + resourceClass + "/health=" + healthBefore + "/policy=" + policyMatch);
                    if (!policyMatch)
                        continue;
                    policyMatchCount++;
                    if (!TryChooseToolIdForResource(resource, out string toolId))
                        continue;

                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        continue;
                    colliderCount++;
                    object? tool = GenerateItemForSmoke(dolocApi, toolId);
                    if (tool == null)
                        continue;
                    toolCount++;

                    int toolDamage = ReadIntMember(tool, "ChopNumber", 0);
                    int seededHealth = healthBefore;
                    if (toolDamage > 0 && healthBefore <= toolDamage)
                    {
                        seededHealth = toolDamage + Math.Max(1, healthBefore);
                        WriteIntMember(resource, "currentHealth", seededHealth);
                    }

                    int appliedBefore = actionCompletion.OneActionApplicationCount;
                    resetTool.Invoke(toolCollider, new object[] { tool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    if (actionCompletion.OneActionApplicationCount > appliedBefore)
                    {
                        string summary = "owner=" + ownerId + ", resource=" + resourceName + ", tool=" + toolId + ", healthBefore=" + healthBefore + ", seededHealth=" + seededHealth + ", toolDamage=" + toolDamage + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", bridge=" + actionCompletion.LastOneActionApplicationSummary;
                        runtime.RuntimeMonitor.Log("Smoke exercise OneActionResourceHit OK " + summary);
                        runtime.SetHookStatus("Smoke.OneActionResourceHit", "verified", "ToolCollider.HandleTools private path on real DungeonResourceRenderer", summary);
                        return SmokeAttemptResult.Succeeded;
                    }
                }

                throw new InvalidOperationException("No rendered resource matched an enabled OneAction policy and survived the normal ToolCollider hit long enough for the Postfix completion path. renderers=" + rendererCount + ", resources=" + resourceCount + ", policyMatches=" + policyMatchCount + ", colliders=" + colliderCount + ", tools=" + toolCount + ", invoked=" + invokedCount + ", samples=" + string.Join(" ; ", samples));
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action resource-hit exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionResourceHit", "failed", "ToolCollider.HandleTools", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseOneActionWrongToolForSmoke()
        {
            try
            {
                ActionCompletionService? actionCompletion = ActionCompletionService;
                if (actionCompletion == null)
                    throw new InvalidOperationException("ActionCompletion feature service was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                Type? resourceRendererType = patcher.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null || resourceRendererType == null)
                    throw new MissingMemberException("DolocAPI, ToolCollider, or DungeonResourceRenderer was not visible.");

                if (!TryPrepareOneActionResourceHitWorldForSmoke(dolocApi, resourceRendererType, out string pendingReason))
                {
                    if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds >= 5)
                    {
                        lastOneActionReadinessLog = DateTimeOffset.Now;
                        runtime.RuntimeMonitor.Log("Smoke one-action wrong-tool waiting: " + pendingReason);
                        runtime.SetHookStatus("Smoke.OneActionWrongTool", "pending", "ToolCollider.HandleTools", pendingReason);
                    }
                    return SmokeAttemptResult.Pending;
                }

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

                TryEnsureOneActionWrongToolMatrixResourcesForSmoke(dolocApi, resourceRendererType, out string matrixResourceSummary);

                int rendererCount = 0;
                int resourceCount = 0;
                int policyMatchCount = 0;
                int colliderCount = 0;
                int toolCount = 0;
                int invokedCount = 0;
                List<string> samples = new List<string>();
                Dictionary<string, string> covered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> failed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> attemptedKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (object renderer in FindUnityObjects(resourceRendererType))
                {
                    rendererCount++;
                    object? resource = ReadMember(renderer, "DungeonResource");
                    if (resource == null || IsRemoved(resource))
                        continue;
                    resourceCount++;

                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (healthBefore <= 0)
                        continue;

                    bool policyMatch = actionCompletion.TryFindOneActionPolicyForSmoke(resource, out string ownerId);
                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    if (samples.Count < 8)
                        samples.Add((resource.GetType().FullName ?? resource.GetType().Name) + "/" + resourceName + "/class=" + resourceClass + "/health=" + healthBefore + "/policy=" + policyMatch);
                    if (!policyMatch)
                        continue;
                    policyMatchCount++;

                    if (!TryChooseMismatchedToolIdForResource(resource, out string wrongToolId, out string expectedToolId))
                        continue;

                    string kind = GetOneActionResourceKind(resource);
                    if (string.IsNullOrWhiteSpace(kind) || covered.ContainsKey(kind) || attemptedKinds.Contains(kind))
                        continue;
                    attemptedKinds.Add(kind);

                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        continue;
                    colliderCount++;

                    object? wrongTool = GenerateItemForSmoke(dolocApi, wrongToolId);
                    if (wrongTool == null)
                        continue;
                    toolCount++;

                    int appliedBefore = actionCompletion.OneActionApplicationCount;
                    resetTool.Invoke(toolCollider, new object[] { wrongTool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int appliedAfter = actionCompletion.OneActionApplicationCount;
                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    if (appliedAfter == appliedBefore && healthAfter == healthBefore && !removedAfter)
                    {
                        string wrongToolName = ReadStringMember(wrongTool, "name", wrongToolId);
                        string wrongToolType = ReadMember(wrongTool, "ToolType")?.ToString() ?? "unknown";
                        string result = "kind=" + kind + ", owner=" + ownerId + ", resource=" + resourceName + ", class=" + resourceClass + ", wrongTool=" + wrongToolName + ", wrongToolType=" + wrongToolType + ", expectedTool=" + expectedToolId + ", healthBefore=" + healthBefore + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", oneActionDelta=0";
                        covered[kind] = result;
                        runtime.RuntimeMonitor.Log("Smoke exercise OneActionWrongTool sample OK " + result);
                        continue;
                    }

                    string failedResult = "resource=" + resourceName + "/kind=" + kind + "/wrongTool=" + wrongToolId + "/health=" + healthBefore + "->" + healthAfter + "/removed=" + removedAfter + "/oneActionDelta=" + (appliedAfter - appliedBefore);
                    failed[kind] = failedResult;
                    if (samples.Count < 8)
                        samples.Add("failedNegative/" + failedResult);
                }

                if (covered.Count > 0)
                {
                    string coveredKinds = string.Join(",", OneActionWrongToolTargetKinds.Where(k => covered.ContainsKey(k)));
                    string missingKinds = string.Join(",", OneActionWrongToolTargetKinds.Where(k => !covered.ContainsKey(k)));
                    string failedKinds = string.Join(",", failed.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
                    string summary = "coveredKinds=" + coveredKinds + ", missingKinds=" + (string.IsNullOrWhiteSpace(missingKinds) ? "none" : missingKinds) + ", failedKinds=" + (string.IsNullOrWhiteSpace(failedKinds) ? "none" : failedKinds) + ", " + string.Join(" | ", covered.Values) + (string.IsNullOrWhiteSpace(matrixResourceSummary) ? string.Empty : ", resourceSetup={" + matrixResourceSummary + "}");
                    string status = string.IsNullOrWhiteSpace(missingKinds) ? "verified" : "experimental";
                    runtime.RuntimeMonitor.Log("Smoke exercise OneActionWrongTool OK " + summary);
                    runtime.SetHookStatus("Smoke.OneActionWrongTool", status, "ToolCollider.HandleTools private path on real DungeonResourceRenderer", summary);
                    return SmokeAttemptResult.Succeeded;
                }

                throw new InvalidOperationException("No rendered resource produced a clean wrong-tool negative result. renderers=" + rendererCount + ", resources=" + resourceCount + ", policyMatches=" + policyMatchCount + ", colliders=" + colliderCount + ", tools=" + toolCount + ", invoked=" + invokedCount + ", samples=" + string.Join(" ; ", samples) + ", resourceSetup=" + matrixResourceSummary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action wrong-tool exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionWrongTool", "failed", "ToolCollider.HandleTools", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseOneActionFuelFeedForSmoke()
        {
            try
            {
                if (ActionCompletionService == null)
                    throw new InvalidOperationException("ActionCompletion feature service was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogOneActionFuelFeedPending("Waiting for NormalGameState before one-action fuel/feed smoke. context=" + runtime.UI.InputContext + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (!actionSpeedInteractExitPatched)
                {
                    LogOneActionFuelFeedPending("Waiting for AgentStateInteract.OnExit Postfix before one-action fuel/feed smoke.");
                    return SmokeAttemptResult.Pending;
                }

                if (!TryExerciseOneActionEquipmentFillKindForSmoke(
                    dolocApi,
                    "FuelMachine",
                    "DolocTown.PowerGeneratorFuel",
                    new[] { "wood_generator", "coal_generator", "modified_fuel_generator" },
                    new[] { "wood", "coal", "weeds" },
                    "FuelPercent",
                    8,
                    out string fuelSummary))
                {
                    throw new InvalidOperationException("Fuel-machine native fill path did not produce verified extra consumption. " + fuelSummary);
                }

                if (!TryExerciseOneActionEquipmentFillKindForSmoke(
                    dolocApi,
                    "Feeder",
                    "DolocTown.Feeder",
                    new[] { "feeder", "large_feeder" },
                    new[] { "roughage_feed", "green_feed", "weeds", "thunder_grass" },
                    "progress",
                    8,
                    out string feederSummary))
                {
                    throw new InvalidOperationException("Feeder native fill path did not produce verified extra consumption. " + feederSummary);
                }

                string summary = "fuel={" + fuelSummary + "}; feeder={" + feederSummary + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionFuelFeed OK " + summary);
                runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "Equipment.DecoratedInteract -> AgentStateInteract.OnExit Postfix -> native CostSelf/AddFuel/AddFeeds", summary);
                runtime.SetHookStatus("Actions.OneActionFuelFeed", "verified", "Harmony Postfix: AgentStateInteract.OnExit", "Fuel-machine and animal-feeder native CostSelf/AddFuel/AddFeeds paths verified by smoke. " + summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action fuel/feed exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionFuelFeed", "failed", "AgentStateInteract.OnExit", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseOneActionVegetationForSmoke()
        {
            object? vegetation = null;
            try
            {
                ActionCompletionService? actionCompletion = ActionCompletionService;
                if (actionCompletion == null)
                    throw new InvalidOperationException("ActionCompletion feature service was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null)
                    throw new MissingMemberException("DolocAPI or ToolCollider was not visible.");

                object? currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (currentRoom == null)
                    throw new InvalidOperationException("CurrentRoom was unavailable.");

                bool isMainFarm = IsMainFarmRoomForSmoke(dolocApi, currentRoom);
                if (!isMainFarm && !autoExerciseOneActionMainFarmRequested && TryEnterMainFarmForOneActionSmoke(dolocApi, currentRoom, out string transitionSummary))
                {
                    LogOneActionVegetationPending(transitionSummary);
                    return SmokeAttemptResult.Pending;
                }
                if (!isMainFarm && autoExerciseOneActionMainFarmRequested)
                {
                    LogOneActionVegetationPending("Waiting for main farm transition before vegetation smoke. room=" + DescribeRoomForSmoke(currentRoom));
                    return SmokeAttemptResult.Pending;
                }

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

                currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (currentRoom == null)
                    throw new InvalidOperationException("CurrentRoom was unavailable after transition.");

                if (!TryCreateTransientDandelionVegetationForSmoke(dolocApi, currentRoom, out vegetation, out object? renderer, out string sourceSummary) ||
                    vegetation == null ||
                    renderer == null)
                {
                    throw new InvalidOperationException("Could not create a transient dandelion vegetation sample. " + sourceSummary);
                }

                if (!TryChooseToolIdsForVegetation(vegetation, out string expectedToolId, out string expectedToolType, out int expectedMinLevel, out string wrongToolId, out string toolSummary))
                    throw new InvalidOperationException("Could not resolve vegetation tool constraints. " + toolSummary);

                object? collider = ReadMember(renderer, "Collider2d");
                if (collider == null)
                    throw new InvalidOperationException("VegetationRenderer.Collider2d was unavailable. target=" + DescribeVegetationForSmoke(vegetation));

                object? wrongTool = GenerateItemForSmoke(dolocApi, wrongToolId);
                object? expectedTool = GenerateItemForSmoke(dolocApi, expectedToolId);
                if (wrongTool == null || expectedTool == null)
                    throw new InvalidOperationException("Could not generate vegetation smoke tools. expected=" + expectedToolId + ", wrong=" + wrongToolId);

                int appBeforeWrong = actionCompletion.OneActionApplicationCount;
                resetTool.Invoke(toolCollider, new object[] { wrongTool });
                resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                handleTools.Invoke(toolCollider, new object[] { collider });
                int appAfterWrong = actionCompletion.OneActionApplicationCount;
                bool removedAfterWrong = ReadMember(renderer, "Vegetation") == null;
                if (removedAfterWrong || appAfterWrong != appBeforeWrong)
                    throw new InvalidOperationException("Wrong-tool vegetation hit was not a clean negative. removed=" + removedAfterWrong + ", oneActionDelta=" + (appAfterWrong - appBeforeWrong));

                int appBeforeCorrect = actionCompletion.OneActionApplicationCount;
                resetTool.Invoke(toolCollider, new object[] { expectedTool });
                resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                handleTools.Invoke(toolCollider, new object[] { collider });
                int appAfterCorrect = actionCompletion.OneActionApplicationCount;
                bool removedAfterCorrect = ReadMember(renderer, "Vegetation") == null;
                if (!removedAfterCorrect || appAfterCorrect != appBeforeCorrect)
                    throw new InvalidOperationException("Correct-tool vegetation hit did not stay on the native one-fell path. removed=" + removedAfterCorrect + ", oneActionDelta=" + (appAfterCorrect - appBeforeCorrect));

                string wrongToolName = ReadStringMember(wrongTool, "name", wrongToolId);
                string wrongToolType = ReadMember(wrongTool, "ToolType")?.ToString() ?? "unknown";
                string expectedToolName = ReadStringMember(expectedTool, "name", expectedToolId);
                string summary = "target=" + DescribeVegetationForSmoke(vegetation) +
                    ", source={" + sourceSummary + "}" +
                    ", path=ToolCollider.HandleTools->VegetationRenderer.OnFell->VegetationDandelion.OnFell->Vegetation.CheckToolConstraints" +
                    ", expectedTool=" + expectedToolName +
                    ", expectedToolType=" + expectedToolType +
                    ", expectedMinLevel=" + expectedMinLevel +
                    ", wrongTool=" + wrongToolName +
                    ", wrongToolType=" + wrongToolType +
                    ", wrongRemoved=" + removedAfterWrong +
                    ", correctRemoved=" + removedAfterCorrect +
                    ", oneActionDeltaWrong=" + (appAfterWrong - appBeforeWrong) +
                    ", oneActionDeltaCorrect=" + (appAfterCorrect - appBeforeCorrect) +
                    ", resourcePath=DungeonResourceRenderer:none";
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionVegetation OK " + summary);
                runtime.SetHookStatus("Smoke.OneActionVegetation", "verified", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", summary);
                runtime.SetHookStatus("Actions.OneActionVegetation", "verified", "native Vegetation.CheckToolConstraints", "Dandelion/vegetation is not a DungeonResource one-action path; wrong tools are rejected by the game and correct tools fell through native Vegetation.OnFell. " + summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action vegetation exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionVegetation", "failed", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
            finally
            {
                if (vegetation != null)
                    TryRemoveTransientVegetationForSmoke(ReadStaticMember(patcher?.ResolveType("DolocAPI, Assembly-CSharp"), "CurrentRoom"), vegetation);
            }
        }

        private bool TryExerciseOneActionEquipmentFillKindForSmoke(Type dolocApi, string kind, string targetTypeName, string[] equipmentIds, string[] itemIds, string ratioMember, int stackCount, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            object? transientEquipment = null;
            int quickSlot = 0;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? equipment = TryCreateTransientEquipmentForSmoke(dolocApi, room, targetTypeName, equipmentIds, out string targetSource);
                if (equipment == null)
                {
                    equipment = FindExistingEquipmentForSmoke(room, targetTypeName, ratioMember, out targetSource);
                    if (equipment == null)
                    {
                        summary = "No target equipment available. targetType=" + targetTypeName + ", source=" + targetSource;
                        return false;
                    }
                }
                else
                {
                    transientEquipment = equipment;
                }

                string? itemId = SelectFillItemIdForSmoke(dolocApi, equipment, kind, itemIds, out string itemSelectSummary);
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    summary = "No valid fill item. target=" + DescribeEquipmentForSmoke(equipment) + ", " + itemSelectSummary;
                    return false;
                }

                object? item = GenerateItemForSmoke(dolocApi, itemId!, stackCount);
                if (item == null)
                {
                    summary = "Could not generate smoke item " + itemId + ".";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                object? anchor = ReadMember(equipment, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable" : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, equipment, anchor, out selectSummary))
                {
                    summary = "Could not select target equipment. target=" + DescribeEquipmentForSmoke(equipment) + ", " + selectSummary;
                    return false;
                }

                ActionCompletionService? actionCompletion = ActionCompletionService;
                int beforeApplications = actionCompletion?.OneActionApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double beforeRatio = ReadDoubleMember(equipment, ratioMember, -1);
                MethodInfo? decoratedInteract = FindMethod(equipment.GetType(), "DecoratedInteract", 0);
                if (decoratedInteract == null)
                {
                    summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForSmoke(equipment);
                    return false;
                }

                decoratedInteract.Invoke(equipment, null);
                if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                {
                    summary = "Native interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForSmoke(equipment) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = actionCompletion?.OneActionApplicationCount ?? 0;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double afterRatio = ReadDoubleMember(equipment, ratioMember, -1);
                int totalConsumed = beforeCount - afterCount;
                bool applied = afterApplications > beforeApplications;
                bool changed = afterRatio > beforeRatio || (beforeRatio < 0 && totalConsumed > 0);
                if (!applied || totalConsumed < 2 || !changed)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(equipment) +
                        ", item=" + itemId +
                        ", source=" + targetSource +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                        ", oneActionDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (actionCompletion?.LastOneActionApplicationSummary ?? "none");
                    return false;
                }

                summary = "kind=" + kind +
                    ", target=" + DescribeEquipmentForSmoke(equipment) +
                    ", item=" + itemId +
                    ", source=" + targetSource +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", totalConsumed=" + totalConsumed +
                    ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                    ", oneActionDelta=" + (afterApplications - beforeApplications) +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + (actionCompletion?.LastOneActionApplicationSummary ?? "none");
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionFuelFeed sample OK " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action " + kind + " fill target failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action " + kind + " fill target failed.", ex.ToString());
                return false;
            }
            finally
            {
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }
    }
}
