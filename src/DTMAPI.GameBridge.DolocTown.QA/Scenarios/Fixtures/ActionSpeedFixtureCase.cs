using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private bool actionSpeedAutoFillFixtureActive;
        private Type? actionSpeedAutoFillDolocApi;
        private Type? actionSpeedAutoFillWaterType;
        private object? actionSpeedAutoFillInventory;
        private object? actionSpeedAutoFillOriginalSlotItem;
        private bool actionSpeedAutoFillQuickSlotReceiptPublished;
        private bool actionSpeedAutoFillOriginalIsInWater;
        private int actionSpeedAutoFillQuickSlot;
        private int actionSpeedAutoFillBeforeApplications;
        private int actionSpeedAutoFillBeforeAutoFill;
        private int actionSpeedAutoFillBeforeCount;
        private string actionSpeedAutoFillBeforeItem = string.Empty;
        private string actionSpeedAutoFillPlaceSummary = string.Empty;
        private DateTimeOffset actionSpeedAutoFillDeadline = DateTimeOffset.MinValue;
        private Batch5GcActiveWindow? actionSpeedGcLadderActiveWindow;
        private bool actionSpeedGcLadderDisableApplied;
        private int actionSpeedGcLadderRecoveryUnits;
        private bool actionSpeedGcLadderFixtureCompleted;

        private FixtureAttemptResult TryExerciseActionSpeedToolForFixture() =>
            TryExerciseActionSpeedToolCore(
                expectedAcceleration: true,
                expectedMultiplier: null,
                hookId: "Smoke.ActionSpeedTool",
                logLabel: "ActionSpeedTool");

        private FixtureAttemptResult TryExerciseActionSpeedToolCore(
            bool expectedAcceleration,
            double? expectedMultiplier,
            string hookId,
            string logLabel)
        {
            try
            {
                Batch6ActionSpeedObservation product = RequireActionSpeedOwnerReady();

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? agentStateToolType = patcher.ResolveType("DolocTown.AgentStateTool, Assembly-CSharp");
                if (dolocApi == null || agentStateToolType == null)
                    throw new MissingMemberException("DolocAPI or AgentStateTool was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogActionSpeedPending("Waiting for NormalGameState before action-speed tool smoke. context=" + runtime.UI.InputContext + ".");
                    return FixtureAttemptResult.Pending;
                }

                bool configured = product.ToolConfigured;
                string ownerId = configured ? "Yuuka.DTMAPI.ActionSpeed" : string.Empty;
                if (expectedAcceleration && !configured)
                    throw new InvalidOperationException("ActionSpeed policy was not configured/enabled for tool animation. Is the official ActionSpeed package enabled and smoke config written?");
                if (!expectedAcceleration && configured)
                    throw new InvalidOperationException("ActionSpeed native-control workload still had a configured owner=" + ownerId + ".");

                object? agent = dolocApi.GetProperty("agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
                object? tool = GenerateActionSpeedToolForFixture(dolocApi);
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

                int before = ObserveActionSpeedProduct().ApplicationCount;
                onEnter.Invoke(state, null);
                int after = ObserveActionSpeedProduct().ApplicationCount;
                string summary = ObserveActionSpeedProduct().LastApplicationSummary;
                onExit.Invoke(state, null);
                actionSpeedProductObserver.ResetBoundary(runtime, "smoke action-speed tool cleanup");

                if (expectedAcceleration && after <= before)
                    throw new InvalidOperationException("AgentStateTool.OnEnter ran but ActionSpeed did not apply. owner=" + ownerId + ", tool=" + (ReadStringMember(tool, "name", tool.GetType().Name)));
                if (!expectedAcceleration && after != before)
                    throw new InvalidOperationException("AgentStateTool.OnEnter unexpectedly applied ActionSpeed during native-control workload. before=" + before + "; after=" + after + "; summary=" + summary);
                if (expectedAcceleration && expectedMultiplier.HasValue && !SummaryContainsMultiplier(summary, expectedMultiplier.Value))
                    throw new InvalidOperationException("ActionSpeed applied with the wrong ladder multiplier. expected=" + expectedMultiplier.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "; summary=" + summary);

                string mode = expectedAcceleration ? "accelerated" : "native-behavior";
                string details = "mode=" + mode + "; before=" + before + "; after=" + after + "; owner=" + (configured ? ownerId : "none") + "; application={" + summary + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise " + logLabel + " OK " + details);
                runtime.SetHookStatus(hookId, "verified", "AgentStateTool.OnEnter/OnExit private smoke path", details);
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed tool exercise failed.", ex.ToString());
                runtime.SetHookStatus(hookId, "failed", "AgentStateTool.OnEnter", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        internal ActionSpeedGcLadderProgress CaptureActionSpeedGcLadderProgress(DateTimeOffset observedAtUtc)
        {
            Batch5GcActiveWindow? activeWindow = actionSpeedGcLadderActiveWindow;
            return new ActionSpeedGcLadderProgress
            {
                Level = batch5GcLadder.Level,
                Workload = batch5GcLadder.Workload,
                ObservedAtUtc = observedAtUtc,
                CompletedUnits = activeWindow?.CompletedUnits ?? 0,
                FirstUnitAtUtc = activeWindow?.FirstUnitAtUtc,
                LastUnitAtUtc = activeWindow?.LastUnitAtUtc,
                ActiveDurationSeconds = activeWindow?.ActiveDurationSeconds ?? 0d,
                ActiveWindowSatisfied = activeWindow?.IsSatisfied == true,
                RecoveryVerified = actionSpeedGcLadderRecoveryUnits > 0,
                RecoveryUnits = actionSpeedGcLadderRecoveryUnits,
                BehaviorReceiptKind = actionSpeedGcLadderFixtureCompleted
                    ? Batch5GcLadderBehavior.ExpectedActionSpeedReceiptKind(batch5GcLadder.Level)
                    : string.Empty,
                FixtureCompleted = actionSpeedGcLadderFixtureCompleted
            };
        }

        private FixtureAttemptResult TryExerciseActionSpeedGcLadderForFixture()
        {
            if (!batch5GcLadder.Enabled || !batch5GcLadder.Domain.Equals("ActionSpeed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("ActionSpeed GC ladder fixture was invoked without its exact Batch 5 activation receipt.");

            bool nativeControl = batch5GcLadder.Level.Equals("L0", StringComparison.Ordinal);
            bool disableRecovery = batch5GcLadder.Level.Equals("L4", StringComparison.Ordinal);
            bool expectedAcceleration = !nativeControl && batch5GcLadder.Multiplier > 1d;
            int targetUnits = Math.Max(1, batch5GcLadder.TargetUnits);
            Batch5GcActiveWindow activeWindow = actionSpeedGcLadderActiveWindow ??=
                new Batch5GcActiveWindow(TimeSpan.FromSeconds(Math.Max(1, batch5GcLadder.MeasureSeconds)), targetUnits);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!activeWindow.IsSatisfied)
            {
                if (!activeWindow.IsUnitDue(now))
                {
                    LogActionSpeedPending(
                        "Driving the ActionSpeed GC workload across the complete measurement window. level=" + batch5GcLadder.Level +
                        "; workload=" + batch5GcLadder.Workload +
                        "; units=" + activeWindow.CompletedUnits + "/" + targetUnits +
                        "; activeSeconds=" + activeWindow.ActiveDurationSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        "; nextUnitAt=" + activeWindow.NextUnitAtUtc?.ToString("O"),
                        "Smoke.Batch5GcLadder.ActionSpeed");
                    return FixtureAttemptResult.Pending;
                }

                FixtureAttemptResult sample = TryExerciseActionSpeedGcLadderSampleForFixture(
                    expectedAcceleration,
                    expectedAcceleration ? batch5GcLadder.Multiplier : (double?)null,
                    "Sample");
                if (sample != FixtureAttemptResult.Succeeded)
                    return sample;
                activeWindow.RecordUnit(DateTimeOffset.UtcNow);
                if (!activeWindow.IsSatisfied)
                    return FixtureAttemptResult.Pending;
            }

            if (disableRecovery && !actionSpeedGcLadderDisableApplied)
            {
                EditAndSaveConfigPage("Yuuka.DTMAPI.ActionSpeed", page =>
                    SetConfigPendingValue(page, "Bool", "false", "启用", "Enabled"));
                actionSpeedGcLadderDisableApplied = true;
                runtime.RuntimeMonitor.Log("Batch 5 ActionSpeed GC ladder disabled the product through its ConfigMenu save boundary after active workload units=" + activeWindow.CompletedUnits + "; activeSeconds=" + activeWindow.ActiveDurationSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ".");
                return FixtureAttemptResult.Pending;
            }

            if (disableRecovery && actionSpeedGcLadderRecoveryUnits == 0)
            {
                FixtureAttemptResult recovery = TryExerciseActionSpeedGcLadderSampleForFixture(
                    expectedAcceleration: false,
                    expectedMultiplier: null,
                    receiptKind: "Recovery");
                if (recovery != FixtureAttemptResult.Succeeded)
                    return recovery;
                actionSpeedGcLadderRecoveryUnits++;
            }

            actionSpeedGcLadderFixtureCompleted = true;
            string behavior = Batch5GcLadderBehavior.ExpectedActionSpeedReceiptKind(batch5GcLadder.Level);
            string summary = "domain=ActionSpeed; level=" + batch5GcLadder.Level +
                "; workload=" + batch5GcLadder.Workload +
                "; multiplier=" + batch5GcLadder.Multiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                "; units=" + activeWindow.CompletedUnits +
                "; activeSeconds=" + activeWindow.ActiveDurationSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                "; nativeControl=" + nativeControl.ToString().ToLowerInvariant() +
                "; disableRecovery=" + actionSpeedGcLadderDisableApplied.ToString().ToLowerInvariant() +
                "; recoveryUnits=" + actionSpeedGcLadderRecoveryUnits +
                "; behavior=" + behavior;
            runtime.SetHookStatus("Smoke.Batch5GcLadder.ActionSpeed", "verified", "native ActionSpeed workload family ladder", summary);
            runtime.RuntimeMonitor.Log("Smoke exercise Batch5GcLadder ActionSpeed OK " + summary);
            return FixtureAttemptResult.Succeeded;
        }

        private FixtureAttemptResult TryExerciseActionSpeedGcLadderSampleForFixture(
            bool expectedAcceleration,
            double? expectedMultiplier,
            string receiptKind)
        {
            string workload = batch5GcLadder.Workload;
            string hookId = "Smoke.Batch5GcLadder.ActionSpeed" + receiptKind + "." + workload;
            string logLabel = "Batch5GcLadder.ActionSpeed" + receiptKind + "." + workload;
            if (workload.Equals("Tool", StringComparison.Ordinal))
            {
                return TryExerciseActionSpeedToolCore(
                    expectedAcceleration,
                    expectedMultiplier,
                    hookId,
                    logLabel);
            }

            try
            {
                RequireActionSpeedOwnerReady();

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");
                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogActionSpeedPending("Waiting for NormalGameState before ActionSpeed GC ladder workload=" + workload + ".", hookId);
                    return FixtureAttemptResult.Pending;
                }

                if (expectedAcceleration && !ObserveActionSpeedProduct().InteractionConfigured)
                {
                    LogActionSpeedPending("Waiting for the configured ActionSpeed interaction owner before GC ladder workload=" + workload + ".", hookId);
                    return FixtureAttemptResult.Pending;
                }

                bool patchesReady = workload.Equals("Interact", StringComparison.Ordinal)
                    ? actionSpeedInteractEnterPatched && actionSpeedInteractExitPatched
                    : actionSpeedEatEnterPatched && actionSpeedUseItemContinuesPatched;
                if (expectedAcceleration && !patchesReady)
                {
                    LogActionSpeedPending("Waiting for the ActionSpeed native state patches before GC ladder workload=" + workload + ".", hookId);
                    return FixtureAttemptResult.Pending;
                }

                bool succeeded;
                string details;
                if (workload.Equals("Interact", StringComparison.Ordinal))
                {
                    succeeded = TryExerciseActionSpeedMachineAddKindForFixture(
                        dolocApi,
                        "FuelMachine",
                        "DolocTown.PowerGeneratorFuel",
                        new[] { "wood_generator", "coal_generator", "modified_fuel_generator" },
                        new[] { "wood", "coal", "weeds" },
                        "FuelPercent",
                        expectedAcceleration,
                        expectedMultiplier,
                        out details);
                }
                else if (workload.Equals("Eat", StringComparison.Ordinal))
                {
                    succeeded = TryExerciseActionSpeedEatDrinkForFixture(dolocApi, expectedAcceleration, expectedMultiplier, out details);
                }
                else if (workload.Equals("ContinuousUse", StringComparison.Ordinal))
                {
                    succeeded = TryExerciseActionSpeedBottledWaterContinuousForFixture(dolocApi, expectedAcceleration, expectedMultiplier, out details);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported ActionSpeed GC ladder workload " + workload + ".");
                }

                if (!succeeded)
                    throw new InvalidOperationException("ActionSpeed GC ladder workload did not satisfy its native/effective-speed contract. workload=" + workload + "; details={" + details + "}.");

                string mode = expectedAcceleration ? "accelerated" : "native-behavior";
                string summary = "workload=" + workload + "; mode=" + mode + "; details={" + details + "}";
                runtime.SetHookStatus(hookId, "verified", "native ActionSpeed workload family", summary);
                runtime.RuntimeMonitor.Log("Smoke exercise " + logLabel + " OK " + summary);
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke ActionSpeed GC ladder workload failed.", ex.ToString());
                runtime.SetHookStatus(hookId, "failed", "native ActionSpeed workload family", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult TryExerciseActionSpeedConfigApplyForFixture()
        {
            try
            {
                RequireActionSpeedOwnerReady();

                FixtureAttemptResult beforeResult = TryExerciseActionSpeedToolForFixture();
                if (beforeResult == FixtureAttemptResult.Pending)
                    return FixtureAttemptResult.Pending;
                if (beforeResult == FixtureAttemptResult.Failed)
                    throw new InvalidOperationException("Initial ActionSpeed tool exercise failed before config save.");

                string beforeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                if (!SummaryContainsMultiplier(beforeSummary, 2))
                    throw new InvalidOperationException("Expected initial ActionSpeed multiplier=2 before config save; actual summary=" + beforeSummary);

                EditAndSaveConfigPage("Yuuka.DTMAPI.ActionSpeed", page =>
                {
                    SetConfigPendingValue(page, "Bool", "true", "启用", "Enabled");
                    SetConfigPendingValue(page, "InlineBoolNumber", "true|4", "工具动画加速", "Tool animation speed");
                });
                runtime.RuntimeMonitor.Log("Smoke ActionSpeed config saved through DTMAPI ConfigMenu page multiplier=4.");

                FixtureAttemptResult afterResult = TryExerciseActionSpeedToolForFixture();
                if (afterResult == FixtureAttemptResult.Pending)
                    return FixtureAttemptResult.Pending;
                if (afterResult == FixtureAttemptResult.Failed)
                    throw new InvalidOperationException("ActionSpeed tool exercise failed after config save.");

                string afterSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                if (!SummaryContainsMultiplier(afterSummary, 4))
                    throw new InvalidOperationException("Expected ActionSpeed multiplier=4 after config save; actual summary=" + afterSummary);

                string summary = "before=" + beforeSummary + "; after=" + afterSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedConfigApply OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedConfigApply", "verified", "DTMAPI ConfigMenu Save -> AgentStateTool.OnEnter", summary);
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed config-apply exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedConfigApply", "failed", "DTMAPI ConfigMenu Save", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult TryExerciseActionSpeedInteractionForFixture()
        {
            try
            {
                RequireActionSpeedOwnerReady();

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogActionSpeedPending("Waiting for NormalGameState before action-speed interaction smoke. context=" + runtime.UI.InputContext + ".", "Smoke.ActionSpeedInteraction");
                    return FixtureAttemptResult.Pending;
                }

                if (!actionSpeedInteractEnterPatched || !actionSpeedInteractExitPatched || !actionSpeedEatEnterPatched || !actionSpeedUseItemContinuesPatched || !actionSpeedInteractContinuesPatched)
                {
                    LogActionSpeedPending("Waiting for AgentStateInteract/AgentStateEat/UseItemContinues/InteractContinues patches before action-speed interaction smoke.", "Smoke.ActionSpeedInteraction");
                    return FixtureAttemptResult.Pending;
                }

                if (!ObserveActionSpeedProduct().InteractionConfigured)
                    throw new InvalidOperationException("ActionSpeed policy was not configured/enabled for interaction paths. Is the official ActionSpeed package enabled and smoke config written?");
                string ownerId = "Yuuka.DTMAPI.ActionSpeed";

                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (currentRoom != null && ReadBoolMember(currentRoom, "IsInHouse", false))
                {
                    if (!autoExerciseActionSpeedInteractionMainFarmRequested && TryEnterMainFarmForActionSpeedInteractionSmoke(dolocApi, currentRoom, out string transitionSummary))
                    {
                        LogActionSpeedPending(transitionSummary, "Smoke.ActionSpeedInteraction");
                        return FixtureAttemptResult.Pending;
                    }

                    if (autoExerciseActionSpeedInteractionMainFarmRequested)
                    {
                        LogActionSpeedPending("Waiting for main farm transition before action-speed interaction smoke. room=" + DescribeRoomForFixture(currentRoom), "Smoke.ActionSpeedInteraction");
                        return FixtureAttemptResult.Pending;
                    }
                }

                var samples = new List<string>();
                FixtureAttemptResult autoFillBottleResult = TryExerciseActionSpeedAutoFillBottleForFixture(dolocApi, out string autoFillBottleSummary);
                if (autoFillBottleResult == FixtureAttemptResult.Pending)
                    return FixtureAttemptResult.Pending;
                if (autoFillBottleResult == FixtureAttemptResult.Failed)
                    throw new InvalidOperationException("No-key in-water bottle auto-fill path failed. " + autoFillBottleSummary);
                samples.Add("autoFillBottle={" + autoFillBottleSummary + "}");

                if (!TryExerciseActionSpeedMachineAddKindForFixture(
                    dolocApi,
                    "FuelMachine",
                    "DolocTown.PowerGeneratorFuel",
                    new[] { "wood_generator", "coal_generator", "modified_fuel_generator" },
                    new[] { "wood", "coal", "weeds" },
                    "FuelPercent",
                    expectedAcceleration: true,
                    expectedMultiplier: null,
                    out string fuelSummary))
                {
                    throw new InvalidOperationException("Fuel-machine ActionSpeed interaction path failed. " + fuelSummary);
                }
                samples.Add("fuel={" + fuelSummary + "}");

                if (!TryExerciseActionSpeedMachineAddKindForFixture(
                    dolocApi,
                    "Feeder",
                    "DolocTown.Feeder",
                    new[] { "feeder", "large_feeder" },
                    new[] { "roughage_feed", "green_feed", "weeds", "thunder_grass" },
                    "progress",
                    expectedAcceleration: true,
                    expectedMultiplier: null,
                    out string feederSummary))
                {
                    throw new InvalidOperationException("Feeder ActionSpeed interaction path failed. " + feederSummary);
                }
                samples.Add("feeder={" + feederSummary + "}");

                if (!TryExerciseActionSpeedEatDrinkForFixture(dolocApi, expectedAcceleration: true, expectedMultiplier: null, out string eatSummary))
                    throw new InvalidOperationException("Eat/drink ActionSpeed animation path failed. " + eatSummary);
                samples.Add("eatDrink={" + eatSummary + "}");

                if (!TryExerciseActionSpeedBottledWaterContinuousForFixture(dolocApi, expectedAcceleration: true, expectedMultiplier: null, out string bottledWaterSummary))
                    throw new InvalidOperationException("Bottled-water right-click continuous-use path failed. " + bottledWaterSummary);
                samples.Add("bottledWaterRightClick={" + bottledWaterSummary + "}");

                if (!TryExerciseActionSpeedBottleFillForFixture(dolocApi, out string bottleSummary))
                    throw new InvalidOperationException("Bottle-fill ActionSpeed continuous-use path failed. " + bottleSummary);
                samples.Add("bottleFill={" + bottleSummary + "}");

                if (!TryExerciseActionSpeedBottleFillInWaterForFixture(dolocApi, out string bottleWaterSummary))
                    throw new InvalidOperationException("In-water bottle-fill ActionSpeed continuous-use path failed. " + bottleWaterSummary);
                samples.Add("bottleFillInWater={" + bottleWaterSummary + "}");

                if (!TryExerciseActionSpeedPlantForFixture(dolocApi, out string plantSummary))
                    throw new InvalidOperationException("Planting ActionSpeed interaction path failed. " + plantSummary);
                samples.Add("plant={" + plantSummary + "}");

                if (!TryExerciseActionSpeedCropHarvestForFixture(dolocApi, out string cropHarvestSummary))
                    throw new InvalidOperationException("Plant-basin crop harvest ActionSpeed interaction path failed. " + cropHarvestSummary);
                samples.Add("cropHarvest={" + cropHarvestSummary + "}");

                if (!TryExerciseActionSpeedResinHarvestForFixture(dolocApi, out string resinSummary))
                    throw new InvalidOperationException("Resin harvest ActionSpeed interaction path failed. " + resinSummary);
                samples.Add("resin={" + resinSummary + "}");

                if (!TryExerciseActionSpeedVegetationHarvestForFixture(dolocApi, out string vegetationSummary))
                    throw new InvalidOperationException("Wild vegetation harvest ActionSpeed interaction path failed. " + vegetationSummary);
                samples.Add("vegetationHarvest={" + vegetationSummary + "}");

                string summary = "owner=" + ownerId + "; " + string.Join("; ", samples.ToArray()) + "; pending=none";
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedInteraction", "verified", "Native item/equipment paths -> AgentStateInteract/AgentStateEat/UseItemContinues/InteractContinues", summary);
                runtime.SetHookStatus("ActionSpeed.InteractionAnimation", "experimental", "Harmony Postfix/Prefix: AgentStateInteract.OnEnter, AgentStateEat.OnEnter, AgentControllerState.UseItemContinues/InteractContinues", "Verified fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, bottle fill from IWaterContainer and in-water branch, no-key ItemBottle.UseAsItem auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest in third-save smoke.");
                if (!TryVerifyDiagnosticsSnapshotForFixture("ActionSpeed", "ActionSpeed"))
                    return FixtureAttemptResult.Failed;

                EditAndSaveConfigPage("Yuuka.DTMAPI.ActionSpeed", page =>
                    SetConfigPendingValue(page, "Bool", "false", "启用", "Enabled"));
                Batch6ActionSpeedObservation disabled = ObserveActionSpeedProduct();
                if (disabled.ToolConfigured || disabled.InteractionConfigured || disabled.UpdateSubscribed ||
                    disabled.NativeTransientCount != 0 || disabled.InstalledPatchCount != 11 ||
                    !disabled.ActualHarmonyOwnerReady || !disabled.ProductCallbackRuntimePresent)
                {
                    throw new InvalidOperationException(
                        "ActionSpeed disable recovery was incomplete. tool=" + disabled.ToolConfigured +
                        "; interaction=" + disabled.InteractionConfigured +
                        "; update=" + disabled.UpdateSubscribed +
                        "; transients=" + disabled.NativeTransientCount +
                        "; internalPatches=" + disabled.InstalledPatchCount +
                        "; actualPatches=" + disabled.CanonicalHarmonyPatchCount +
                        "; actualTargets=" + disabled.CanonicalHarmonyTargetCount + "/" + disabled.ResolvedHarmonyTargetCount +
                        "; callback=" + disabled.ProductCallbackRuntimePresent + ".");
                }
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedDisableRecovery OK tool=false; interaction=false; update=false; nativeTransients=0; internalPatches=9; actualOwnerPatches=9; actualOwnerTargets=9; callback=true.");
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed interaction exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedInteraction", "failed", "AgentStateInteract/AgentStateEat/UseItemContinues/InteractContinues", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private bool TryExerciseActionSpeedMachineAddKindForFixture(
            Type dolocApi,
            string kind,
            string targetTypeName,
            string[] equipmentIds,
            string[] itemIds,
            string ratioMember,
            bool expectedAcceleration,
            double? expectedMultiplier,
            out string summary)
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

                object? equipment = TryCreateTransientEquipmentForFixture(dolocApi, room, targetTypeName, equipmentIds, out string targetSource);
                if (equipment == null)
                {
                    summary = "No transient target equipment could be created; real player equipment fallback is forbidden. targetType=" + targetTypeName + ", source=" + targetSource;
                    return false;
                }
                transientEquipment = equipment;

                string? itemId = SelectFillItemIdForFixture(dolocApi, equipment, kind, itemIds, out string itemSelectSummary);
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    summary = "No valid fill item. target=" + DescribeEquipmentForFixture(equipment) + ", " + itemSelectSummary;
                    return false;
                }

                object? item = GenerateItemForFixture(dolocApi, itemId!, 3);
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
                if (anchor == null || !TrySelectEquipmentForFixture(dolocApi, equipment, anchor, out selectSummary))
                {
                    summary = "Could not select target equipment. target=" + DescribeEquipmentForFixture(equipment) + ", " + selectSummary;
                    return false;
                }

                int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double beforeRatio = ReadDoubleMember(equipment, ratioMember, -1);
                MethodInfo? decoratedInteract = FindMethod(equipment.GetType(), "DecoratedInteract", 0);
                if (decoratedInteract == null)
                {
                    summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForFixture(equipment);
                    return false;
                }

                decoratedInteract.Invoke(equipment, null);
                if (!TryRunCurrentAgentInteractExitForFixture(dolocApi, out string interactSummary))
                {
                    summary = "Native interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForFixture(equipment) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double afterRatio = ReadDoubleMember(equipment, ratioMember, -1);
                int totalConsumed = beforeCount - afterCount;
                string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "MachineInteract");
                bool changed = afterRatio > beforeRatio || (beforeRatio < 0 && totalConsumed > 0);
                bool accelerationMatches = expectedAcceleration ? speedApplied : !speedApplied;
                bool multiplierMatches = !expectedAcceleration || !expectedMultiplier.HasValue || SummaryContainsMultiplier(bridgeSummary, expectedMultiplier.Value);
                if (!accelerationMatches || !multiplierMatches || totalConsumed <= 0 || !changed)
                {
                    summary = "target=" + DescribeEquipmentForFixture(equipment) +
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
                    ", target=" + DescribeEquipmentForFixture(equipment) +
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
                TryEnterIdleStateForFixture(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedEatDrinkForFixture(Type dolocApi, bool expectedAcceleration, double? expectedMultiplier, out string summary)
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
                    item = GenerateItemForFixture(dolocApi, candidate, 3);
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

                int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                int beforeContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForFixture(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    if (afterCount < beforeCount)
                        break;
                }

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                string continuousSummary = ObserveActionSpeedProduct().LastContinuousUseSummary;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "EatDrink");
                bool accelerationMatches = expectedAcceleration ? speedApplied : !speedApplied;
                bool multiplierMatches = !expectedAcceleration || !expectedMultiplier.HasValue || SummaryContainsMultiplier(bridgeSummary, expectedMultiplier.Value);
                if (!accelerationMatches || !multiplierMatches || afterCount >= beforeCount)
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
                TryEnterIdleStateForFixture(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
            }
        }

        private bool TryExerciseActionSpeedBottledWaterContinuousForFixture(Type dolocApi, bool expectedAcceleration, double? expectedMultiplier, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;

            try
            {
                object? item = GenerateItemForFixture(dolocApi, "bottle_of_water", 3);
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

                int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                int beforeContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForFixture(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    if (afterCount < beforeCount)
                        break;
                }

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                string continuousSummary = ObserveActionSpeedProduct().LastContinuousUseSummary;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "EatDrink");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottledWaterDrink");
                bool accelerationMatches = expectedAcceleration ? speedApplied && continuousApplied : !speedApplied && !continuousApplied;
                bool multiplierMatches = !expectedAcceleration || !expectedMultiplier.HasValue ||
                    (SummaryContainsMultiplier(bridgeSummary, expectedMultiplier.Value) && SummaryContainsMultiplier(continuousSummary, expectedMultiplier.Value));
                if (!accelerationMatches || !multiplierMatches || afterCount >= beforeCount)
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
                TryEnterIdleStateForFixture(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
            }
        }

        private bool TryExerciseActionSpeedBottleFillForFixture(Type dolocApi, out string summary)
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

                object? well = TryCreateTransientEquipmentForFixture(dolocApi, room, "DolocTown.SimpleWell", new[] { "well" }, out string wellSource);
                if (well == null)
                {
                    summary = "No SimpleWell target available. source=" + wellSource;
                    return false;
                }
                transientEquipment = well;
                MethodInfo? drawMax = FindMethod(well.GetType(), "DrawMax", 0);
                drawMax?.Invoke(well, null);

                object? item = GenerateItemForFixture(dolocApi, "waste_plastic_bottle", 3);
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
                if (anchor == null || !TrySelectEquipmentForFixture(dolocApi, well, anchor, out selectSummary))
                {
                    summary = "Could not select well. target=" + DescribeEquipmentForFixture(well) + ", " + selectSummary;
                    return false;
                }
                if (!TryPointAgentCellTipAtEquipmentForFixture(dolocApi, well, out string tipSummary))
                {
                    summary = "Could not point item cell tip at well. target=" + DescribeEquipmentForFixture(well) + ", " + tipSummary;
                    return false;
                }

                int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                int beforeContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int beforeWater = ReadIntMember(well, "Water", -1);
                int afterCount = beforeCount;
                int afterWater = beforeWater;
                string invokeSummary = string.Empty;
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForFixture(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    if (TryRunCurrentAgentInteractExitForFixture(dolocApi, out interactSummary))
                    {
                        afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                        afterWater = ReadIntMember(well, "Water", -1);
                        if (afterCount < beforeCount)
                            break;
                    }
                }

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                string continuousSummary = ObserveActionSpeedProduct().LastContinuousUseSummary;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "BottleFill");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottleFill");
                bool waterChanged = beforeWater < 0 || afterWater < beforeWater;
                if (!speedApplied || !continuousApplied || afterCount >= beforeCount || !waterChanged)
                {
                    summary = "target=" + DescribeEquipmentForFixture(well) +
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

                summary = "target=" + DescribeEquipmentForFixture(well) +
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
                TryEnterIdleStateForFixture(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedBottleFillInWaterForFixture(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            bool originalIsInWater = false;
            Type? interactiveWaterType = null;

            try
            {
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
                TryClearRoomScannerSelectionForFixture(dolocApi);

                object? item = GenerateItemForFixture(dolocApi, "waste_plastic_bottle", 3);
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

                int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                int beforeContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForFixture(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    if (TryRunCurrentAgentInteractExitForFixture(dolocApi, out interactSummary))
                    {
                        afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                        if (afterCount < beforeCount)
                            break;
                    }
                }

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterContinuous = ObserveActionSpeedProduct().ContinuousUseApplicationCount;
                string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                string continuousSummary = ObserveActionSpeedProduct().LastContinuousUseSummary;
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
                TryEnterIdleStateForFixture(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (interactiveWaterType != null)
                    WriteStaticBoolMember(interactiveWaterType, "IsInWater", originalIsInWater);
            }
        }

        private FixtureAttemptResult TryExerciseActionSpeedAutoFillBottleForFixture(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            try
            {
                if (!ObserveActionSpeedProduct().ProductPresent)
                {
                    summary = "Managed ActionSpeed product unavailable.";
                    return FixtureAttemptResult.Failed;
                }

                if (!actionSpeedAutoFillFixtureActive)
                {
                    Type? interactiveWaterType = patcher?.ResolveType("DolocTown.InteractiveWater, Assembly-CSharp");
                    if (interactiveWaterType == null)
                    {
                        summary = "InteractiveWater type unavailable.";
                        return FixtureAttemptResult.Failed;
                    }

                    object? original = ReadStaticMember(interactiveWaterType, "IsInWater");
                    bool originalIsInWater = original is bool value && value;
                    if (!WriteStaticBoolMember(interactiveWaterType, "IsInWater", true))
                    {
                        summary = "Could not force InteractiveWater.IsInWater for no-key auto-fill branch.";
                        return FixtureAttemptResult.Failed;
                    }

                    actionSpeedAutoFillDolocApi = dolocApi;
                    actionSpeedAutoFillWaterType = interactiveWaterType;
                    actionSpeedAutoFillOriginalIsInWater = originalIsInWater;
                    actionSpeedAutoFillQuickSlot = 0;
                    actionSpeedAutoFillFixtureActive = true;
                    TryClearRoomScannerSelectionForFixture(dolocApi);

                    object? item = GenerateItemForFixture(dolocApi, "waste_plastic_bottle", 3);
                    if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBottle"))
                        throw new InvalidOperationException("Could not generate ItemBottle waste_plastic_bottle.");

                    bool placed = TryPlaceSmokeItemInQuickSlot(
                        dolocApi,
                        item,
                        actionSpeedAutoFillQuickSlot,
                        out object? inventory,
                        out object? originalSlotItem,
                        out string placeSummary,
                        (receiptInventory, receiptOriginalSlotItem) =>
                        {
                            actionSpeedAutoFillInventory = receiptInventory;
                            actionSpeedAutoFillOriginalSlotItem = receiptOriginalSlotItem;
                            actionSpeedAutoFillQuickSlotReceiptPublished = true;
                        });
                    actionSpeedAutoFillPlaceSummary = placeSummary;
                    if (!placed)
                    {
                        throw new InvalidOperationException(placeSummary);
                    }

                    // The callback above must publish the receipt before mutation; these
                    // assignments retain the successful out values as an explicit readback.
                    actionSpeedAutoFillInventory = inventory;
                    actionSpeedAutoFillOriginalSlotItem = originalSlotItem;
                    actionSpeedAutoFillBeforeAutoFill = ObserveActionSpeedProduct().AutoFillApplicationCount;
                    actionSpeedAutoFillBeforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                    actionSpeedAutoFillBeforeCount = ReadQuickSlotItemCount(inventory, actionSpeedAutoFillQuickSlot);
                    actionSpeedAutoFillBeforeItem = ReadQuickSlotItemName(inventory, actionSpeedAutoFillQuickSlot);
                    actionSpeedAutoFillDeadline = DateTimeOffset.UtcNow.AddSeconds(5);
                }

                UpdateRuntimeAutomation();
                int afterAutoFill = ObserveActionSpeedProduct().AutoFillApplicationCount;
                if (afterAutoFill <= actionSpeedAutoFillBeforeAutoFill)
                {
                    summary = "Waiting across real Unity frames for ActionSpeed auto-fill; before=" + actionSpeedAutoFillBeforeAutoFill +
                        ", current=" + afterAutoFill + ", deadline=" + actionSpeedAutoFillDeadline.ToString("o", System.Globalization.CultureInfo.InvariantCulture) + ".";
                    if (DateTimeOffset.UtcNow < actionSpeedAutoFillDeadline)
                    {
                        runtime.SetHookStatus("Smoke.ActionSpeedAutoFillBottle", "pending", "normal ActionSpeed frame update", summary);
                        return FixtureAttemptResult.Pending;
                    }

                    CleanupActionSpeedAutoFillFixtureState("timeout");
                    return FixtureAttemptResult.Failed;
                }
                string interactSummary = string.Empty;
                TryRunCurrentAgentInteractExitForFixture(dolocApi, out interactSummary);

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterCount = ReadQuickSlotItemCount(actionSpeedAutoFillInventory, actionSpeedAutoFillQuickSlot);
                string afterItem = ReadQuickSlotItemName(actionSpeedAutoFillInventory, actionSpeedAutoFillQuickSlot);
                string bridgeSummary = ObserveActionSpeedProduct().LastAutoFillSummary;
                bool invoked = afterAutoFill > actionSpeedAutoFillBeforeAutoFill && bridgeSummary.IndexOf("AutoFillBottle", StringComparison.OrdinalIgnoreCase) >= 0;
                bool inventoryChanged = afterCount != actionSpeedAutoFillBeforeCount || !afterItem.Equals(actionSpeedAutoFillBeforeItem, StringComparison.OrdinalIgnoreCase);
                if (!invoked)
                {
                    summary = "branch=InteractiveWater.IsInWater" +
                        ", item=" + actionSpeedAutoFillBeforeItem + "->" + afterItem +
                        ", count=" + actionSpeedAutoFillBeforeCount + "->" + afterCount +
                        ", autoFillDelta=" + (afterAutoFill - actionSpeedAutoFillBeforeAutoFill) +
                        ", actionSpeedDelta=" + (afterApplications - actionSpeedAutoFillBeforeApplications) +
                        ", place={" + actionSpeedAutoFillPlaceSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    CleanupActionSpeedAutoFillFixtureState("not-invoked");
                    return FixtureAttemptResult.Failed;
                }

                summary = "branch=InteractiveWater.IsInWater" +
                    ", item=" + actionSpeedAutoFillBeforeItem + "->" + afterItem +
                    ", count=" + actionSpeedAutoFillBeforeCount + "->" + afterCount +
                    ", inventoryChanged=" + inventoryChanged +
                    ", autoFillDelta=" + (afterAutoFill - actionSpeedAutoFillBeforeAutoFill) +
                    ", actionSpeedDelta=" + (afterApplications - actionSpeedAutoFillBeforeApplications) +
                    ", place={" + actionSpeedAutoFillPlaceSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                CleanupActionSpeedAutoFillFixtureState("completed");
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK AutoFillBottle " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedAutoFillBottle", "verified", "ItemBottle.UseAsItem native path", summary);
                return FixtureAttemptResult.Succeeded;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed no-key auto-fill bottle failed.", ex.InnerException.ToString());
                CleanupActionSpeedAutoFillFixtureState("target-invocation-failure");
                return FixtureAttemptResult.Failed;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed no-key auto-fill bottle failed.", ex.ToString());
                CleanupActionSpeedAutoFillFixtureState("fixture-failure");
                return FixtureAttemptResult.Failed;
            }
        }

        private string CleanupActionSpeedAutoFillFixtureState(string reason)
        {
            if (!actionSpeedAutoFillFixtureActive)
                return "reason=" + (reason ?? string.Empty) + ", active=false";

            Type? dolocApi = actionSpeedAutoFillDolocApi;
            Type? waterType = actionSpeedAutoFillWaterType;
            if (dolocApi != null)
            {
                TryEnterIdleStateForFixture(dolocApi);
                if (actionSpeedAutoFillQuickSlotReceiptPublished &&
                    !RestoreSmokeQuickSlot(dolocApi, actionSpeedAutoFillInventory, actionSpeedAutoFillQuickSlot, actionSpeedAutoFillOriginalSlotItem))
                {
                    throw new InvalidOperationException("Could not restore the QA ActionSpeed quick-slot transaction during " + (reason ?? string.Empty) + ".");
                }
            }
            if (waterType != null && !WriteStaticBoolMember(waterType, "IsInWater", actionSpeedAutoFillOriginalIsInWater))
                throw new InvalidOperationException("Could not restore InteractiveWater.IsInWater during " + (reason ?? string.Empty) + ".");

            actionSpeedAutoFillFixtureActive = false;
            actionSpeedAutoFillDolocApi = null;
            actionSpeedAutoFillWaterType = null;
            actionSpeedAutoFillInventory = null;
            actionSpeedAutoFillOriginalSlotItem = null;
            actionSpeedAutoFillQuickSlotReceiptPublished = false;
            actionSpeedAutoFillOriginalIsInWater = false;
            actionSpeedAutoFillQuickSlot = 0;
            actionSpeedAutoFillBeforeApplications = 0;
            actionSpeedAutoFillBeforeAutoFill = 0;
            actionSpeedAutoFillBeforeCount = 0;
            actionSpeedAutoFillBeforeItem = string.Empty;
            actionSpeedAutoFillPlaceSummary = string.Empty;
            actionSpeedAutoFillDeadline = DateTimeOffset.MinValue;
            return "reason=" + (reason ?? string.Empty) + ", active=true, restored=true";
        }

        private bool TryExerciseActionSpeedPlantForFixture(Type dolocApi, out string summary)
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

                object? basin = TryCreateTransientEquipmentForFixture(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (basin == null)
                {
                    summary = "No PlantBasin target available. source=" + basinSource;
                    return false;
                }
                transientEquipment = basin;

                object? anchor = ReadMember(basin, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForFixture(dolocApi, basin, anchor, out selectSummary))
                {
                    summary = "Could not select plant basin. target=" + DescribeEquipmentForFixture(basin) + ", " + selectSummary;
                    return false;
                }

                foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin" })
                {
                    object? seed = GenerateItemForFixture(dolocApi, seedId, 3);
                    if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                        continue;

                    if (!TryPlaceSmokeItemInQuickSlot(dolocApi, seed, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                    {
                        summary = placeSummary;
                        return false;
                    }
                    if (!TryPointAgentCellTipAtEquipmentForFixture(dolocApi, basin, out string tipSummary))
                    {
                        summary = "Could not point item cell tip at plant basin. target=" + DescribeEquipmentForFixture(basin) + ", " + tipSummary;
                        return false;
                    }

                    object? selectedSeed = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedSeed == null || !IsTypeOrBase(selectedSeed.GetType(), "DolocTown.ItemSeed"))
                    {
                        summary = "SelectedItem was not an ItemSeed after quick-slot placement. place={" + placeSummary + "}, selected=" + (selectedSeed == null ? "null" : selectedSeed.GetType().FullName);
                        return false;
                    }

                    string seedType = ReadSeedTypeForFixture(selectedSeed);
                    string basinSeedType = ReadPlantBasinSeedTypeForFixture(basin);
                    WriteBoolMember(selectedSeed, "enablePlantInInvalidSeason", true);
                    MethodInfo? plantSeed = selectedSeed.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "PlantSeed" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(basin));
                    if (plantSeed == null)
                    {
                        summary = "ItemSeed.PlantSeed(PlantBasin) was not found.";
                        return false;
                    }

                    int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                    int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    plantSeed.Invoke(selectedSeed, new[] { basin });
                    if (!TryRunCurrentAgentInteractExitForFixture(dolocApi, out string interactSummary))
                    {
                        summary = "Native planting interaction did not reach AgentStateInteract.OnExit. " + interactSummary;
                        return false;
                    }

                    int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                    int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    bool isPlanted = ReadBoolMember(basin, "IsPlanted", false);
                    string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                    bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Plant");
                    if (speedApplied && isPlanted && afterCount < beforeCount)
                    {
                        summary = "seed=" + seedId +
                            ", seedType=" + seedType +
                            ", basinSeedType=" + basinSeedType +
                            ", target=" + DescribeEquipmentForFixture(basin) +
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
                    TryEnterIdleStateForFixture(dolocApi);
                }

                summary = "No seed candidate produced a planted PlantBasin through the native ItemSeed.PlantSeed path. target=" + DescribeEquipmentForFixture(basin) +
                    ", basinSeedType=" + ReadPlantBasinSeedTypeForFixture(basin) +
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
                TryEnterIdleStateForFixture(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedCropHarvestForFixture(Type dolocApi, out string summary)
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

                object? basin = TryCreateTransientEquipmentForFixture(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (basin == null)
                {
                    summary = "No PlantBasin target available. source=" + basinSource;
                    return false;
                }
                transientEquipment = basin;

                object? anchor = ReadMember(basin, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForFixture(dolocApi, basin, anchor, out selectSummary))
                {
                    summary = "Could not select plant basin. target=" + DescribeEquipmentForFixture(basin) + ", " + selectSummary;
                    return false;
                }

                foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin" })
                {
                    object? seed = GenerateItemForFixture(dolocApi, seedId, 3);
                    if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                        continue;

                    if (!TryPlaceSmokeItemInQuickSlot(dolocApi, seed, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                    {
                        summary = placeSummary;
                        return false;
                    }
                    if (!TryPointAgentCellTipAtEquipmentForFixture(dolocApi, basin, out string tipSummary))
                    {
                        summary = "Could not point item cell tip at plant basin. target=" + DescribeEquipmentForFixture(basin) + ", " + tipSummary;
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
                    if (!TryRunCurrentAgentInteractExitForFixture(dolocApi, out string plantInteractSummary))
                    {
                        summary = "Native planting setup did not reach AgentStateInteract.OnExit. " + plantInteractSummary;
                        return false;
                    }

                    object? crop = ReadMember(basin, "crop");
                    if (crop == null || !TrySetCropMatureForFixture(crop, out string matureSummary))
                    {
                        RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                        inventory = null;
                        originalSlotItem = null;
                        TryEnterIdleStateForFixture(dolocApi);
                        continue;
                    }

                    RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                    inventory = null;
                    originalSlotItem = null;
                    TryEnterIdleStateForFixture(dolocApi);

                    if (!TrySelectEquipmentForFixture(dolocApi, basin, anchor, out string harvestSelectSummary))
                    {
                        summary = "Could not re-select mature plant basin. target=" + DescribeEquipmentForFixture(basin) + ", " + harvestSelectSummary;
                        return false;
                    }

                    bool beforeCouldHarvest = ReadBoolMember(basin, "CouldHarvest", false);
                    int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                    MethodInfo? decoratedInteract = FindMethod(basin.GetType(), "DecoratedInteract", 0);
                    if (decoratedInteract == null)
                    {
                        summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForFixture(basin);
                        return false;
                    }

                    decoratedInteract.Invoke(basin, null);
                    if (!TryRunCurrentAgentInteractExitForFixture(dolocApi, out string harvestInteractSummary))
                    {
                        summary = "Native crop harvest interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForFixture(basin) + ", " + harvestInteractSummary;
                        return false;
                    }

                    int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                    object? afterCrop = ReadMember(basin, "crop");
                    bool afterCouldHarvest = ReadBoolMember(basin, "CouldHarvest", false);
                    bool afterMature = afterCrop != null && ReadBoolMember(afterCrop, "isMature", false);
                    string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                    bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                    bool changed = beforeCouldHarvest && (!afterCouldHarvest || afterCrop == null || !afterMature);
                    if (speedApplied && changed)
                    {
                        summary = "seed=" + seedId +
                            ", target=" + DescribeEquipmentForFixture(basin) +
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
                        ", target=" + DescribeEquipmentForFixture(basin) +
                        ", couldHarvest=" + beforeCouldHarvest + "->" + afterCouldHarvest +
                        ", afterCrop=" + (afterCrop == null ? "null" : afterCrop.GetType().Name) +
                        ", afterMature=" + afterMature +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + harvestInteractSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "No seed candidate could be planted and matured for crop-harvest smoke. target=" + DescribeEquipmentForFixture(basin);
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
                TryEnterIdleStateForFixture(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedResinHarvestForFixture(Type dolocApi, out string summary)
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

                object? collector = TryCreateTransientResinCollectorForFixture(dolocApi, room, out transientResource, out string collectorSource);
                if (collector == null)
                {
                    summary = "No transient ResinCollector target available; real player equipment fallback is forbidden. source=" + collectorSource;
                    return false;
                }
                transientEquipment = collector;

                MethodInfo? updateCurrentValue = FindMethod(collector.GetType(), "UpdateCurrentValue", 1);
                if (updateCurrentValue != null)
                    updateCurrentValue.Invoke(collector, new object[] { 3 });
                else
                    WriteIntMember(collector, "currentValue", 3);

                object? anchor = ReadMember(collector, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForFixture(dolocApi, collector, anchor, out selectSummary))
                {
                    summary = "Could not select resin collector. target=" + DescribeEquipmentForFixture(collector) + ", " + selectSummary;
                    return false;
                }

                int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                int beforeValue = ReadIntMember(collector, "currentValue", -1);
                MethodInfo? decoratedInteract = FindMethod(collector.GetType(), "DecoratedInteract", 0);
                if (decoratedInteract == null)
                {
                    summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForFixture(collector);
                    return false;
                }

                decoratedInteract.Invoke(collector, null);
                if (!TryRunCurrentAgentInteractExitForFixture(dolocApi, out string interactSummary))
                {
                    summary = "Native resin interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForFixture(collector) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterValue = ReadIntMember(collector, "currentValue", -1);
                string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                if (!speedApplied || beforeValue <= 0 || afterValue != 0)
                {
                    summary = "target=" + DescribeEquipmentForFixture(collector) +
                        ", source=" + collectorSource +
                        ", currentValue=" + beforeValue + "->" + afterValue +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeEquipmentForFixture(collector) +
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
                TryEnterIdleStateForFixture(dolocApi);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
                if (transientResource != null)
                    TryRemoveTransientDungeonResourceForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientResource);
            }
        }

        private bool TryExerciseActionSpeedVegetationHarvestForFixture(Type dolocApi, out string summary)
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

                if (!TryCreateTransientMatureVegetationForFixture(dolocApi, room, out object? vegetation, out object? renderer, out string vegetationSource) ||
                    vegetation == null ||
                    renderer == null)
                {
                    summary = vegetationSource;
                    return false;
                }
                transientVegetation = vegetation;

                int beforeApplications = ObserveActionSpeedProduct().ApplicationCount;
                int beforeLevel = ReadIntMember(vegetation, "currentLevel", -1);
                int beforeMaxLevel = ReadIntMember(vegetation, "maxLevel", -1);
                TryClearRoomScannerSelectionForFixture(dolocApi);
                if (!TryInteractWithCurrentInteractableForFixture(dolocApi, renderer, out string interactStartSummary))
                {
                    summary = "Could not start vegetation interact. target=" + DescribeVegetationForFixture(vegetation) + ", " + interactStartSummary;
                    return false;
                }

                if (!TryRunCurrentAgentInteractExitForFixture(dolocApi, out string interactSummary))
                {
                    summary = "Native vegetation harvest interaction did not reach AgentStateInteract.OnExit. target=" + DescribeVegetationForFixture(vegetation) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = ObserveActionSpeedProduct().ApplicationCount;
                int afterLevel = ReadIntMember(vegetation, "currentLevel", -1);
                object? afterRenderer = ReadMember(vegetation, "Renderer");
                string bridgeSummary = ObserveActionSpeedProduct().LastApplicationSummary;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                bool changed = afterLevel >= 0 && beforeLevel >= 0
                    ? afterLevel < beforeLevel || afterRenderer == null
                    : afterRenderer == null;
                if (!speedApplied || !changed)
                {
                    summary = "target=" + DescribeVegetationForFixture(vegetation) +
                        ", source=" + vegetationSource +
                        ", level=" + beforeLevel + "/" + beforeMaxLevel + "->" + afterLevel +
                        ", rendererAfter=" + (afterRenderer == null ? "null" : afterRenderer.GetType().Name) +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", start={" + interactStartSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeVegetationForFixture(vegetation) +
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
                TryEnterIdleStateForFixture(dolocApi);
                if (transientVegetation != null)
                    TryRemoveTransientVegetationForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientVegetation);
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

        private Batch6ActionSpeedObservation RequireActionSpeedOwnerReady()
        {
            Batch6ActionSpeedObservation observation = ObserveActionSpeedProduct();
            if (!observation.ProductPresent)
                throw new InvalidOperationException("Managed ActionSpeed product was not loaded.");
            if (!observation.ActualHarmonyOwnerReady || observation.InstalledPatchCount != 11 || !observation.ProductCallbackRuntimePresent)
            {
                throw new InvalidOperationException(
                    "Managed ActionSpeed owner is not physically ready. internal=" + observation.InstalledPatchCount +
                    "; actualPatches=" + observation.CanonicalHarmonyPatchCount +
                    "; actualTargets=" + observation.CanonicalHarmonyTargetCount + "/" + observation.ResolvedHarmonyTargetCount +
                    "; callback=" + observation.ProductCallbackRuntimePresent +
                    "; inventory=" + observation.HarmonyInventoryDetails + ".");
            }
            return observation;
        }

        private object? GenerateActionSpeedToolForFixture(Type dolocApi)
        {
            foreach (string itemId in new[] { "old_pickaxe", "old_axe", "old_sickle" })
            {
                object? item = GenerateItemForFixture(dolocApi, itemId);
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
                summary = "Current room is indoor; requested official main farm transition for action-speed interaction smoke. from=" + DescribeRoomForFixture(currentRoom) + ", to=" + DescribeRoomForFixture(mainFarm);
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
