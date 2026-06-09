using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private SmokeAttemptResult TryExerciseActionSpeedToolForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? agentStateToolType = patcher.ResolveType("DolocTown.AgentStateTool, Assembly-CSharp");
                if (dolocApi == null || agentStateToolType == null)
                    throw new MissingMemberException("DolocAPI or AgentStateTool was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogActionSpeedPending("Waiting for NormalGameState before action-speed tool smoke. context=" + runtime.UI.InputContext + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (!experimentalApi.TryGetConfiguredActionSpeedOwner(out string ownerId))
                    throw new InvalidOperationException("ActionSpeed policy was not configured/enabled for tool animation. Is the official ActionSpeed package enabled and smoke config written?");

                object? agent = dolocApi.GetProperty("agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
                object? tool = GenerateActionSpeedToolForSmoke(dolocApi);
                if (agent == null || stateManager == null || tool == null)
                    throw new MissingMemberException("DolocAPI.agent, AgentStateManager, or generated tool was not available.");

                MethodInfo? getState = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "GetState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
                MethodInfo? onEnter = FindMethod(agentStateToolType, "OnEnter", 0);
                MethodInfo? onExit = FindMethod(agentStateToolType, "OnExit", 0);
                if (getState == null || onEnter == null || onExit == null)
                    throw new MissingMethodException("AgentStateManager.GetState<T>() or AgentStateTool.OnEnter/OnExit was not found.");

                object? state = getState.MakeGenericMethod(agentStateToolType).Invoke(stateManager, null);
                if (state == null || !WriteObjectMember(state, "tool", tool))
                    throw new MissingMemberException("Could not seed AgentStateTool.tool for smoke.");

                int before = experimentalApi.ActionSpeedApplicationCount;
                onEnter.Invoke(state, null);
                int after = experimentalApi.ActionSpeedApplicationCount;
                string summary = experimentalApi.LastActionSpeedApplicationSummary;
                onExit.Invoke(state, null);
                experimentalApi.RestoreActionSpeed("smoke action-speed tool cleanup");

                if (after <= before)
                    throw new InvalidOperationException("AgentStateTool.OnEnter ran but ActionSpeed did not apply. owner=" + ownerId + ", tool=" + (ReadStringMember(tool, "name", tool.GetType().Name)));

                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedTool OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedTool", "verified", "AgentStateTool.OnEnter/OnExit private smoke path", summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed tool exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedTool", "failed", "AgentStateTool.OnEnter", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseActionSpeedConfigApplyForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                SmokeAttemptResult beforeResult = TryExerciseActionSpeedToolForSmoke();
                if (beforeResult == SmokeAttemptResult.Pending)
                    return SmokeAttemptResult.Pending;
                if (beforeResult == SmokeAttemptResult.Failed)
                    throw new InvalidOperationException("Initial ActionSpeed tool exercise failed before config save.");

                string beforeSummary = experimentalApi.LastActionSpeedApplicationSummary;
                if (!SummaryContainsMultiplier(beforeSummary, 2))
                    throw new InvalidOperationException("Expected initial ActionSpeed multiplier=2 before config save; actual summary=" + beforeSummary);

                IConfigMenuPage? page = runtime.CreateSnapshot().ConfigPages.FirstOrDefault(p => p.Manifest.UniqueID.Equals("Yuuka.DTMAPI.ActionSpeed", StringComparison.OrdinalIgnoreCase));
                if (page == null)
                    throw new InvalidOperationException("ActionSpeed config page was not registered.");
                if (page.IsLocked)
                    throw new InvalidOperationException("ActionSpeed config page was locked: " + page.LockReason);

                IConfigMenuRuntime? configMenuRuntime = runtime.ConfigMenuRuntime;
                if (configMenuRuntime == null)
                    throw new InvalidOperationException("Config menu runtime was not available.");

                configMenuRuntime.BeginEditing(page.Manifest.UniqueID);
                SetConfigPendingValue(page, "Bool", "true", "启用", "Enabled");
                SetConfigPendingValue(page, "InlineBoolNumber", "true|4", "工具动画加速", "Tool animation speed");
                configMenuRuntime.Save(page.Manifest.UniqueID);
                runtime.RuntimeMonitor.Log("Smoke ActionSpeed config saved through DTMAPI ConfigMenu page multiplier=4.");

                SmokeAttemptResult afterResult = TryExerciseActionSpeedToolForSmoke();
                if (afterResult == SmokeAttemptResult.Pending)
                    return SmokeAttemptResult.Pending;
                if (afterResult == SmokeAttemptResult.Failed)
                    throw new InvalidOperationException("ActionSpeed tool exercise failed after config save.");

                string afterSummary = experimentalApi.LastActionSpeedApplicationSummary;
                if (!SummaryContainsMultiplier(afterSummary, 4))
                    throw new InvalidOperationException("Expected ActionSpeed multiplier=4 after config save; actual summary=" + afterSummary);

                string summary = "before=" + beforeSummary + "; after=" + afterSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedConfigApply OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedConfigApply", "verified", "DTMAPI ConfigMenu Save -> AgentStateTool.OnEnter", summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed config-apply exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedConfigApply", "failed", "DTMAPI ConfigMenu Save", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseActionSpeedInteractionForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogActionSpeedPending("Waiting for NormalGameState before action-speed interaction smoke. context=" + runtime.UI.InputContext + ".", "Smoke.ActionSpeedInteraction");
                    return SmokeAttemptResult.Pending;
                }

                if (!actionSpeedInteractEnterPatched || !actionSpeedInteractExitPatched || !actionSpeedEatEnterPatched || !actionSpeedUseItemContinuesPatched)
                {
                    LogActionSpeedPending("Waiting for AgentStateInteract/AgentStateEat/UseItemContinues patches before action-speed interaction smoke.", "Smoke.ActionSpeedInteraction");
                    return SmokeAttemptResult.Pending;
                }

                if (!experimentalApi.TryGetConfiguredActionSpeedInteractionOwner(out string ownerId))
                    throw new InvalidOperationException("ActionSpeed policy was not configured/enabled for interaction paths. Is the official ActionSpeed package enabled and smoke config written?");

                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (currentRoom != null && ReadBoolMember(currentRoom, "IsInHouse", false))
                {
                    if (!autoExerciseActionSpeedInteractionMainFarmRequested && TryEnterMainFarmForActionSpeedInteractionSmoke(dolocApi, currentRoom, out string transitionSummary))
                    {
                        LogActionSpeedPending(transitionSummary, "Smoke.ActionSpeedInteraction");
                        return SmokeAttemptResult.Pending;
                    }

                    if (autoExerciseActionSpeedInteractionMainFarmRequested)
                    {
                        LogActionSpeedPending("Waiting for main farm transition before action-speed interaction smoke. room=" + DescribeRoomForSmoke(currentRoom), "Smoke.ActionSpeedInteraction");
                        return SmokeAttemptResult.Pending;
                    }
                }

                var samples = new List<string>();
                if (!TryExerciseActionSpeedMachineAddKindForSmoke(
                    dolocApi,
                    "FuelMachine",
                    "DolocTown.PowerGeneratorFuel",
                    new[] { "wood_generator", "coal_generator", "modified_fuel_generator" },
                    new[] { "wood", "coal", "weeds" },
                    "FuelPercent",
                    out string fuelSummary))
                {
                    throw new InvalidOperationException("Fuel-machine ActionSpeed interaction path failed. " + fuelSummary);
                }
                samples.Add("fuel={" + fuelSummary + "}");

                if (!TryExerciseActionSpeedMachineAddKindForSmoke(
                    dolocApi,
                    "Feeder",
                    "DolocTown.Feeder",
                    new[] { "feeder", "large_feeder" },
                    new[] { "roughage_feed", "green_feed", "weeds", "thunder_grass" },
                    "progress",
                    out string feederSummary))
                {
                    throw new InvalidOperationException("Feeder ActionSpeed interaction path failed. " + feederSummary);
                }
                samples.Add("feeder={" + feederSummary + "}");

                if (!TryExerciseActionSpeedEatDrinkForSmoke(dolocApi, out string eatSummary))
                    throw new InvalidOperationException("Eat/drink ActionSpeed animation path failed. " + eatSummary);
                samples.Add("eatDrink={" + eatSummary + "}");

                if (!TryExerciseActionSpeedBottledWaterContinuousForSmoke(dolocApi, out string bottledWaterSummary))
                    throw new InvalidOperationException("Bottled-water right-click continuous-use path failed. " + bottledWaterSummary);
                samples.Add("bottledWaterRightClick={" + bottledWaterSummary + "}");

                if (!TryExerciseActionSpeedBottleFillForSmoke(dolocApi, out string bottleSummary))
                    throw new InvalidOperationException("Bottle-fill ActionSpeed continuous-use path failed. " + bottleSummary);
                samples.Add("bottleFill={" + bottleSummary + "}");

                if (!TryExerciseActionSpeedBottleFillInWaterForSmoke(dolocApi, out string bottleWaterSummary))
                    throw new InvalidOperationException("In-water bottle-fill ActionSpeed continuous-use path failed. " + bottleWaterSummary);
                samples.Add("bottleFillInWater={" + bottleWaterSummary + "}");

                if (!TryExerciseActionSpeedAutoFillBottleForSmoke(dolocApi, out string autoFillBottleSummary))
                    throw new InvalidOperationException("No-key in-water bottle auto-fill path failed. " + autoFillBottleSummary);
                samples.Add("autoFillBottle={" + autoFillBottleSummary + "}");

                if (!TryExerciseActionSpeedPlantForSmoke(dolocApi, out string plantSummary))
                    throw new InvalidOperationException("Planting ActionSpeed interaction path failed. " + plantSummary);
                samples.Add("plant={" + plantSummary + "}");

                if (!TryExerciseActionSpeedCropHarvestForSmoke(dolocApi, out string cropHarvestSummary))
                    throw new InvalidOperationException("Plant-basin crop harvest ActionSpeed interaction path failed. " + cropHarvestSummary);
                samples.Add("cropHarvest={" + cropHarvestSummary + "}");

                if (!TryExerciseActionSpeedResinHarvestForSmoke(dolocApi, out string resinSummary))
                    throw new InvalidOperationException("Resin harvest ActionSpeed interaction path failed. " + resinSummary);
                samples.Add("resin={" + resinSummary + "}");

                if (!TryExerciseActionSpeedVegetationHarvestForSmoke(dolocApi, out string vegetationSummary))
                    throw new InvalidOperationException("Wild vegetation harvest ActionSpeed interaction path failed. " + vegetationSummary);
                samples.Add("vegetationHarvest={" + vegetationSummary + "}");

                string summary = "owner=" + ownerId + "; " + string.Join("; ", samples.ToArray()) + "; pending=none";
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedInteraction", "verified", "Native item/equipment paths -> AgentStateInteract/AgentStateEat/UseItemContinues", summary);
                runtime.SetHookStatus("ActionSpeed.InteractionAnimation", "experimental", "Harmony Postfix/Prefix: AgentStateInteract.OnEnter, AgentStateEat.OnEnter, AgentControllerState.UseItemContinues", "Verified fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, bottle fill from IWaterContainer and in-water branch, no-key ItemBottle.UseAsItem auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest in third-save smoke.");
                if (!TryVerifyDiagnosticsSnapshotForSmoke("ActionSpeed", "ActionSpeed"))
                    return SmokeAttemptResult.Failed;
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed interaction exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedInteraction", "failed", "AgentStateInteract/AgentStateEat/UseItemContinues", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private bool TryExerciseActionSpeedMachineAddKindForSmoke(Type dolocApi, string kind, string targetTypeName, string[] equipmentIds, string[] itemIds, string ratioMember, out string summary)
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

                object? item = GenerateItemForSmoke(dolocApi, itemId!, 3);
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
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, equipment, anchor, out selectSummary))
                {
                    summary = "Could not select target equipment. target=" + DescribeEquipmentForSmoke(equipment) + ", " + selectSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
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

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double afterRatio = ReadDoubleMember(equipment, ratioMember, -1);
                int totalConsumed = beforeCount - afterCount;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "MachineAdd");
                bool changed = afterRatio > beforeRatio || (beforeRatio < 0 && totalConsumed > 0);
                if (!speedApplied || totalConsumed <= 0 || !changed)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(equipment) +
                        ", item=" + itemId +
                        ", source=" + targetSource +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "kind=" + kind +
                    ", target=" + DescribeEquipmentForSmoke(equipment) +
                    ", item=" + itemId +
                    ", source=" + targetSource +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed " + kind + " target failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed " + kind + " target failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedEatDrinkForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;

            try
            {
                object? item = null;
                string itemId = string.Empty;
                foreach (string candidate in new[] { "can", "bread", "berry", "milk" })
                {
                    item = GenerateItemForSmoke(dolocApi, candidate, 3);
                    if (item != null && IsTypeOrBase(item.GetType(), "DolocTown.ItemFood"))
                    {
                        itemId = candidate;
                        break;
                    }
                }
                if (item == null)
                {
                    summary = "No ItemFood candidate could be generated.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    if (afterCount < beforeCount)
                        break;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "EatDrink");
                if (!speedApplied || afterCount >= beforeCount)
                {
                    summary = "item=" + itemId +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", invoke={" + invokeSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "item=" + itemId +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", invoke={" + invokeSummary + "}" +
                    ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK EatDrinkAnimation " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed eat/drink failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed eat/drink failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
            }
        }

        private bool TryExerciseActionSpeedBottledWaterContinuousForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;

            try
            {
                object? item = GenerateItemForSmoke(dolocApi, "bottle_of_water", 3);
                if (item == null)
                {
                    summary = "Could not generate bottle_of_water.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    if (afterCount < beforeCount)
                        break;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "EatDrink");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottledWaterDrink");
                if (!speedApplied || !continuousApplied || afterCount >= beforeCount)
                {
                    summary = "item=bottle_of_water" +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", place={" + placeSummary + "}" +
                        ", invoke={" + invokeSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "item=bottle_of_water" +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", place={" + placeSummary + "}" +
                    ", invoke={" + invokeSummary + "}" +
                    ", continuous=" + continuousSummary +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK BottledWaterRightClick " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottled-water right-click failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottled-water right-click failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
            }
        }

        private bool TryExerciseActionSpeedBottleFillForSmoke(Type dolocApi, out string summary)
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

                object? well = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.SimpleWell", new[] { "well" }, out string wellSource);
                if (well == null)
                {
                    summary = "No SimpleWell target available. source=" + wellSource;
                    return false;
                }
                transientEquipment = well;
                MethodInfo? drawMax = FindMethod(well.GetType(), "DrawMax", 0);
                drawMax?.Invoke(well, null);

                object? item = GenerateItemForSmoke(dolocApi, "waste_plastic_bottle", 3);
                if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBottle"))
                {
                    summary = "Could not generate ItemBottle waste_plastic_bottle.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                object? anchor = ReadMember(well, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, well, anchor, out selectSummary))
                {
                    summary = "Could not select well. target=" + DescribeEquipmentForSmoke(well) + ", " + selectSummary;
                    return false;
                }
                if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, well, out string tipSummary))
                {
                    summary = "Could not point item cell tip at well. target=" + DescribeEquipmentForSmoke(well) + ", " + tipSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int beforeWater = ReadIntMember(well, "Water", -1);
                int afterCount = beforeCount;
                int afterWater = beforeWater;
                string invokeSummary = string.Empty;
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    if (TryRunCurrentAgentInteractExitForSmoke(dolocApi, out interactSummary))
                    {
                        afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                        afterWater = ReadIntMember(well, "Water", -1);
                        if (afterCount < beforeCount)
                            break;
                    }
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "BottleFill");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottleFill");
                bool waterChanged = beforeWater < 0 || afterWater < beforeWater;
                if (!speedApplied || !continuousApplied || afterCount >= beforeCount || !waterChanged)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(well) +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", water=" + beforeWater + "->" + afterWater +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", tip={" + tipSummary + "}" +
                        ", invoke={" + invokeSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeEquipmentForSmoke(well) +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", water=" + beforeWater + "->" + afterWater +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", tip={" + tipSummary + "}" +
                    ", invoke={" + invokeSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", continuous=" + continuousSummary +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK BottleFill " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottle fill failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottle fill failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedBottleFillInWaterForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            bool originalIsInWater = false;
            Type? interactiveWaterType = null;

            try
            {
                if (experimentalApi != null)
                    experimentalApi.SuppressActionSpeedAutoFillForSmoke = true;

                interactiveWaterType = patcher?.ResolveType("DolocTown.InteractiveWater, Assembly-CSharp");
                if (interactiveWaterType == null)
                {
                    summary = "InteractiveWater type unavailable.";
                    return false;
                }

                object? original = ReadStaticMember(interactiveWaterType, "IsInWater");
                originalIsInWater = original is bool value && value;
                if (!WriteStaticBoolMember(interactiveWaterType, "IsInWater", true))
                {
                    summary = "Could not force InteractiveWater.IsInWater for water-pit branch.";
                    return false;
                }
                TryClearRoomScannerSelectionForSmoke(dolocApi);

                object? item = GenerateItemForSmoke(dolocApi, "waste_plastic_bottle", 3);
                if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBottle"))
                {
                    summary = "Could not generate ItemBottle waste_plastic_bottle.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    if (TryRunCurrentAgentInteractExitForSmoke(dolocApi, out interactSummary))
                    {
                        afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                        if (afterCount < beforeCount)
                            break;
                    }
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "BottleFill");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottleFill");
                if (!speedApplied || !continuousApplied || afterCount >= beforeCount)
                {
                    summary = "branch=InteractiveWater.IsInWater" +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", place={" + placeSummary + "}" +
                        ", invoke={" + invokeSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "branch=InteractiveWater.IsInWater" +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", place={" + placeSummary + "}" +
                    ", invoke={" + invokeSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", continuous=" + continuousSummary +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK BottleFillInWater " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed in-water bottle fill failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed in-water bottle fill failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (experimentalApi != null)
                    experimentalApi.SuppressActionSpeedAutoFillForSmoke = false;
                if (interactiveWaterType != null)
                    WriteStaticBoolMember(interactiveWaterType, "IsInWater", originalIsInWater);
            }
        }

        private bool TryExerciseActionSpeedAutoFillBottleForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            bool originalIsInWater = false;
            Type? interactiveWaterType = null;

            try
            {
                if (experimentalApi == null)
                {
                    summary = "Experimental API unavailable.";
                    return false;
                }

                interactiveWaterType = patcher?.ResolveType("DolocTown.InteractiveWater, Assembly-CSharp");
                if (interactiveWaterType == null)
                {
                    summary = "InteractiveWater type unavailable.";
                    return false;
                }

                object? original = ReadStaticMember(interactiveWaterType, "IsInWater");
                originalIsInWater = original is bool value && value;
                if (!WriteStaticBoolMember(interactiveWaterType, "IsInWater", true))
                {
                    summary = "Could not force InteractiveWater.IsInWater for no-key auto-fill branch.";
                    return false;
                }
                TryClearRoomScannerSelectionForSmoke(dolocApi);

                object? item = GenerateItemForSmoke(dolocApi, "waste_plastic_bottle", 3);
                if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBottle"))
                {
                    summary = "Could not generate ItemBottle waste_plastic_bottle.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeAutoFill = experimentalApi.ActionSpeedAutoFillApplicationCount;
                int beforeApplications = experimentalApi.ActionSpeedApplicationCount;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                string beforeItem = ReadQuickSlotItemName(inventory, quickSlot);
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 8; attempt++)
                {
                    UpdateRuntimeAutomation();
                    if (experimentalApi.ActionSpeedAutoFillApplicationCount > beforeAutoFill)
                    {
                        WriteStaticBoolMember(interactiveWaterType, "IsInWater", false);
                        break;
                    }
                    Thread.Sleep(80);
                }
                TryRunCurrentAgentInteractExitForSmoke(dolocApi, out interactSummary);

                int afterAutoFill = experimentalApi.ActionSpeedAutoFillApplicationCount;
                int afterApplications = experimentalApi.ActionSpeedApplicationCount;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                string afterItem = ReadQuickSlotItemName(inventory, quickSlot);
                string bridgeSummary = experimentalApi.LastActionSpeedAutoFillSummary;
                bool invoked = afterAutoFill > beforeAutoFill && bridgeSummary.IndexOf("AutoFillBottle", StringComparison.OrdinalIgnoreCase) >= 0;
                bool inventoryChanged = afterCount != beforeCount || !afterItem.Equals(beforeItem, StringComparison.OrdinalIgnoreCase);
                if (!invoked)
                {
                    summary = "branch=InteractiveWater.IsInWater" +
                        ", item=" + beforeItem + "->" + afterItem +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", autoFillDelta=" + (afterAutoFill - beforeAutoFill) +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", place={" + placeSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "branch=InteractiveWater.IsInWater" +
                    ", item=" + beforeItem + "->" + afterItem +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", inventoryChanged=" + inventoryChanged +
                    ", autoFillDelta=" + (afterAutoFill - beforeAutoFill) +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", place={" + placeSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK AutoFillBottle " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedAutoFillBottle", "verified", "ItemBottle.UseAsItem native path", summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed no-key auto-fill bottle failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed no-key auto-fill bottle failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (interactiveWaterType != null)
                    WriteStaticBoolMember(interactiveWaterType, "IsInWater", originalIsInWater);
            }
        }

        private bool TryExerciseActionSpeedPlantForSmoke(Type dolocApi, out string summary)
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

                object? basin = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (basin == null)
                {
                    summary = "No PlantBasin target available. source=" + basinSource;
                    return false;
                }
                transientEquipment = basin;

                object? anchor = ReadMember(basin, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, basin, anchor, out selectSummary))
                {
                    summary = "Could not select plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + selectSummary;
                    return false;
                }

                foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin" })
                {
                    object? seed = GenerateItemForSmoke(dolocApi, seedId, 3);
                    if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                        continue;

                    if (!TryPlaceSmokeItemInQuickSlot(dolocApi, seed, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                    {
                        summary = placeSummary;
                        return false;
                    }
                    if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, basin, out string tipSummary))
                    {
                        summary = "Could not point item cell tip at plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + tipSummary;
                        return false;
                    }

                    object? selectedSeed = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedSeed == null || !IsTypeOrBase(selectedSeed.GetType(), "DolocTown.ItemSeed"))
                    {
                        summary = "SelectedItem was not an ItemSeed after quick-slot placement. place={" + placeSummary + "}, selected=" + (selectedSeed == null ? "null" : selectedSeed.GetType().FullName);
                        return false;
                    }

                    string seedType = ReadSeedTypeForSmoke(selectedSeed);
                    string basinSeedType = ReadPlantBasinSeedTypeForSmoke(basin);
                    WriteBoolMember(selectedSeed, "enablePlantInInvalidSeason", true);
                    MethodInfo? plantSeed = selectedSeed.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "PlantSeed" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(basin));
                    if (plantSeed == null)
                    {
                        summary = "ItemSeed.PlantSeed(PlantBasin) was not found.";
                        return false;
                    }

                    int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    plantSeed.Invoke(selectedSeed, new[] { basin });
                    if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                    {
                        summary = "Native planting interaction did not reach AgentStateInteract.OnExit. " + interactSummary;
                        return false;
                    }

                    int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    bool isPlanted = ReadBoolMember(basin, "IsPlanted", false);
                    string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                    bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Plant");
                    if (speedApplied && isPlanted && afterCount < beforeCount)
                    {
                        summary = "seed=" + seedId +
                            ", seedType=" + seedType +
                            ", basinSeedType=" + basinSeedType +
                            ", target=" + DescribeEquipmentForSmoke(basin) +
                            ", count=" + beforeCount + "->" + afterCount +
                            ", isPlanted=" + isPlanted +
                            ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                            ", place={" + placeSummary + "}" +
                            ", tip={" + tipSummary + "}" +
                            ", nativeInteract={" + interactSummary + "}" +
                            ", bridge=" + bridgeSummary;
                        runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK Plant " + summary);
                        return true;
                    }

                    RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                    inventory = null;
                    originalSlotItem = null;
                    TryEnterIdleStateForSmoke(dolocApi);
                }

                summary = "No seed candidate produced a planted PlantBasin through the native ItemSeed.PlantSeed path. target=" + DescribeEquipmentForSmoke(basin) +
                    ", basinSeedType=" + ReadPlantBasinSeedTypeForSmoke(basin) +
                    ", isRemoved=" + ReadBoolMember(basin, "IsRemoved", false);
                return false;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed planting failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed planting failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedCropHarvestForSmoke(Type dolocApi, out string summary)
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

                object? basin = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (basin == null)
                {
                    summary = "No PlantBasin target available. source=" + basinSource;
                    return false;
                }
                transientEquipment = basin;

                object? anchor = ReadMember(basin, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, basin, anchor, out selectSummary))
                {
                    summary = "Could not select plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + selectSummary;
                    return false;
                }

                foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin" })
                {
                    object? seed = GenerateItemForSmoke(dolocApi, seedId, 3);
                    if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                        continue;

                    if (!TryPlaceSmokeItemInQuickSlot(dolocApi, seed, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                    {
                        summary = placeSummary;
                        return false;
                    }
                    if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, basin, out string tipSummary))
                    {
                        summary = "Could not point item cell tip at plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + tipSummary;
                        return false;
                    }

                    object? selectedSeed = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedSeed == null || !IsTypeOrBase(selectedSeed.GetType(), "DolocTown.ItemSeed"))
                    {
                        summary = "SelectedItem was not an ItemSeed after quick-slot placement. place={" + placeSummary + "}, selected=" + (selectedSeed == null ? "null" : selectedSeed.GetType().FullName);
                        return false;
                    }

                    WriteBoolMember(selectedSeed, "enablePlantInInvalidSeason", true);
                    MethodInfo? plantSeed = selectedSeed.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "PlantSeed" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(basin));
                    if (plantSeed == null)
                    {
                        summary = "ItemSeed.PlantSeed(PlantBasin) was not found.";
                        return false;
                    }

                    plantSeed.Invoke(selectedSeed, new[] { basin });
                    if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string plantInteractSummary))
                    {
                        summary = "Native planting setup did not reach AgentStateInteract.OnExit. " + plantInteractSummary;
                        return false;
                    }

                    object? crop = ReadMember(basin, "crop");
                    if (crop == null || !TrySetCropMatureForSmoke(crop, out string matureSummary))
                    {
                        RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                        inventory = null;
                        originalSlotItem = null;
                        TryEnterIdleStateForSmoke(dolocApi);
                        continue;
                    }

                    RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                    inventory = null;
                    originalSlotItem = null;
                    TryEnterIdleStateForSmoke(dolocApi);

                    if (!TrySelectEquipmentForSmoke(dolocApi, basin, anchor, out string harvestSelectSummary))
                    {
                        summary = "Could not re-select mature plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + harvestSelectSummary;
                        return false;
                    }

                    bool beforeCouldHarvest = ReadBoolMember(basin, "CouldHarvest", false);
                    int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    MethodInfo? decoratedInteract = FindMethod(basin.GetType(), "DecoratedInteract", 0);
                    if (decoratedInteract == null)
                    {
                        summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForSmoke(basin);
                        return false;
                    }

                    decoratedInteract.Invoke(basin, null);
                    if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string harvestInteractSummary))
                    {
                        summary = "Native crop harvest interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForSmoke(basin) + ", " + harvestInteractSummary;
                        return false;
                    }

                    int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    object? afterCrop = ReadMember(basin, "crop");
                    bool afterCouldHarvest = ReadBoolMember(basin, "CouldHarvest", false);
                    bool afterMature = afterCrop != null && ReadBoolMember(afterCrop, "isMature", false);
                    string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                    bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                    bool changed = beforeCouldHarvest && (!afterCouldHarvest || afterCrop == null || !afterMature);
                    if (speedApplied && changed)
                    {
                        summary = "seed=" + seedId +
                            ", target=" + DescribeEquipmentForSmoke(basin) +
                            ", source=" + basinSource +
                            ", couldHarvest=" + beforeCouldHarvest + "->" + afterCouldHarvest +
                            ", afterCrop=" + (afterCrop == null ? "null" : afterCrop.GetType().Name) +
                            ", afterMature=" + afterMature +
                            ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                            ", setup={" + plantInteractSummary + "; " + matureSummary + "}" +
                            ", place={" + placeSummary + "}" +
                            ", tip={" + tipSummary + "}" +
                            ", nativeInteract={" + harvestInteractSummary + "}" +
                            ", bridge=" + bridgeSummary;
                        runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK CropHarvest " + summary);
                        return true;
                    }

                    summary = "seed=" + seedId +
                        ", target=" + DescribeEquipmentForSmoke(basin) +
                        ", couldHarvest=" + beforeCouldHarvest + "->" + afterCouldHarvest +
                        ", afterCrop=" + (afterCrop == null ? "null" : afterCrop.GetType().Name) +
                        ", afterMature=" + afterMature +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + harvestInteractSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "No seed candidate could be planted and matured for crop-harvest smoke. target=" + DescribeEquipmentForSmoke(basin);
                return false;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed crop harvest failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed crop harvest failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedResinHarvestForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? transientEquipment = null;
            object? transientResource = null;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? collector = FindExistingResinCollectorForSmoke(room, out string collectorSource);
                if (collector == null)
                {
                    collector = TryCreateTransientResinCollectorForSmoke(dolocApi, room, out transientResource, out collectorSource);
                    if (collector == null)
                    {
                        summary = "No ResinCollector target available. source=" + collectorSource;
                        return false;
                    }
                    transientEquipment = collector;
                }

                MethodInfo? updateCurrentValue = FindMethod(collector.GetType(), "UpdateCurrentValue", 1);
                if (updateCurrentValue != null)
                    updateCurrentValue.Invoke(collector, new object[] { 3 });
                else
                    WriteIntMember(collector, "currentValue", 3);

                object? anchor = ReadMember(collector, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, collector, anchor, out selectSummary))
                {
                    summary = "Could not select resin collector. target=" + DescribeEquipmentForSmoke(collector) + ", " + selectSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeValue = ReadIntMember(collector, "currentValue", -1);
                MethodInfo? decoratedInteract = FindMethod(collector.GetType(), "DecoratedInteract", 0);
                if (decoratedInteract == null)
                {
                    summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForSmoke(collector);
                    return false;
                }

                decoratedInteract.Invoke(collector, null);
                if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                {
                    summary = "Native resin interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForSmoke(collector) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterValue = ReadIntMember(collector, "currentValue", -1);
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                if (!speedApplied || beforeValue <= 0 || afterValue != 0)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(collector) +
                        ", source=" + collectorSource +
                        ", currentValue=" + beforeValue + "->" + afterValue +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeEquipmentForSmoke(collector) +
                    ", source=" + collectorSource +
                    ", currentValue=" + beforeValue + "->" + afterValue +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK ResinHarvest " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed resin harvest failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed resin harvest failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
                if (transientResource != null)
                    TryRemoveTransientDungeonResourceForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientResource);
            }
        }

        private bool TryExerciseActionSpeedVegetationHarvestForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? transientVegetation = null;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                if (!TryCreateTransientMatureVegetationForSmoke(dolocApi, room, out object? vegetation, out object? renderer, out string vegetationSource) ||
                    vegetation == null ||
                    renderer == null)
                {
                    summary = vegetationSource;
                    return false;
                }
                transientVegetation = vegetation;

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeLevel = ReadIntMember(vegetation, "currentLevel", -1);
                int beforeMaxLevel = ReadIntMember(vegetation, "maxLevel", -1);
                TryClearRoomScannerSelectionForSmoke(dolocApi);
                if (!TryInteractWithCurrentInteractableForSmoke(dolocApi, renderer, out string interactStartSummary))
                {
                    summary = "Could not start vegetation interact. target=" + DescribeVegetationForSmoke(vegetation) + ", " + interactStartSummary;
                    return false;
                }

                if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                {
                    summary = "Native vegetation harvest interaction did not reach AgentStateInteract.OnExit. target=" + DescribeVegetationForSmoke(vegetation) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterLevel = ReadIntMember(vegetation, "currentLevel", -1);
                object? afterRenderer = ReadMember(vegetation, "Renderer");
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                bool changed = afterLevel >= 0 && beforeLevel >= 0
                    ? afterLevel < beforeLevel || afterRenderer == null
                    : afterRenderer == null;
                if (!speedApplied || !changed)
                {
                    summary = "target=" + DescribeVegetationForSmoke(vegetation) +
                        ", source=" + vegetationSource +
                        ", level=" + beforeLevel + "/" + beforeMaxLevel + "->" + afterLevel +
                        ", rendererAfter=" + (afterRenderer == null ? "null" : afterRenderer.GetType().Name) +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", start={" + interactStartSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeVegetationForSmoke(vegetation) +
                    ", source=" + vegetationSource +
                    ", level=" + beforeLevel + "/" + beforeMaxLevel + "->" + afterLevel +
                    ", rendererAfter=" + (afterRenderer == null ? "null" : afterRenderer.GetType().Name) +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", start={" + interactStartSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK VegetationHarvest " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed vegetation harvest failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed vegetation harvest failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                if (transientVegetation != null)
                    TryRemoveTransientVegetationForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientVegetation);
            }
        }

        private void LogActionSpeedPending(string message, string statusKey = "Smoke.ActionSpeedTool")
        {
            if ((DateTimeOffset.Now - lastActionSpeedReadinessLog).TotalSeconds < 5)
                return;
            lastActionSpeedReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke action-speed waiting: " + message);
            runtime.SetHookStatus(statusKey, "pending", "ActionSpeed smoke readiness", message);
        }

        private object? GenerateActionSpeedToolForSmoke(Type dolocApi)
        {
            foreach (string itemId in new[] { "old_pickaxe", "old_axe", "old_sickle" })
            {
                object? item = GenerateItemForSmoke(dolocApi, itemId);
                if (item != null && IsTypeOrBase(item.GetType(), "DolocTown.ItemTool"))
                    return item;
            }
            return null;
        }

        private bool TryEnterMainFarmForActionSpeedInteractionSmoke(Type dolocApi, object currentRoom, out string summary)
        {
            summary = string.Empty;
            try
            {
                object? archiveHandle = dolocApi.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? mainFarm = archiveHandle == null ? null : ReadMember(archiveHandle, "MainFarm");
                object? geometry = mainFarm == null ? null : ReadMember(mainFarm, "Geometry");
                object? entryPosition = geometry == null ? null : ReadMember(geometry, "DefaultEntryPosition");
                if (mainFarm == null || entryPosition == null)
                    return false;

                MethodInfo? enterFarm = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        return m.Name == "EnterFarm" &&
                            parameters.Length == 3 &&
                            parameters[0].ParameterType == typeof(string);
                    });
                if (enterFarm == null)
                    return false;

                object? result = enterFarm.Invoke(null, new object?[] { string.Empty, entryPosition, null });
                if (result is bool entered && !entered)
                    return false;

                autoExerciseActionSpeedInteractionMainFarmRequested = true;
                summary = "Current room is indoor; requested official main farm transition for action-speed interaction smoke. from=" + DescribeRoomForSmoke(currentRoom) + ", to=" + DescribeRoomForSmoke(mainFarm);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed interaction main farm transition failed.", ex.ToString());
                return false;
            }
        }
    }
}
