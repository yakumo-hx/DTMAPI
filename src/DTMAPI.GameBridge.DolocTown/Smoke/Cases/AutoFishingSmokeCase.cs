using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private bool TryExerciseAutoFishingAutoCastForSmoke(Type dolocApi, Type fishingPoolType, out string summary)
        {
            summary = string.Empty;
            FishingAutomationService? fishingService = FishingAutomationService;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;

            try
            {
                if (fishingService == null)
                {
                    summary = "FishingAutomation service unavailable.";
                    return false;
                }

                fishingService.SuppressFishingAutoCastForSmoke = false;

                fishingService.ResetFishingFeedbackCooldownForSmoke();
                fishingService.ForceFishingNoWaterForSmoke = true;
                UpdateRuntimeAutomation();

                object? fishingPool = FindOrCreateFishingPoolForSmoke(fishingPoolType, dolocApi, out string poolSource);
                if (fishingPool == null)
                {
                    summary = "No fishing pool was available for auto-cast smoke. " + poolSource;
                    return false;
                }
                fishingService.FishingPoolOverrideForSmoke = fishingPool;

                fishingService.ResetFishingFeedbackCooldownForSmoke();
                fishingService.ForceFishingNoRodForSmoke = true;
                UpdateRuntimeAutomation();
                fishingService.ForceFishingNoWaterForSmoke = false;
                fishingService.ForceFishingNoRodForSmoke = false;

                object? fishingRod = GenerateFishingRodForSmoke(dolocApi);
                if (fishingRod == null)
                {
                    summary = "Generated fishing rod was not available.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, fishingRod, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                TryEnterIdleStateForSmoke(dolocApi);
                int beforeAutoCast = fishingService.FishingAutoCastApplicationCount;
                fishingService.ResetFishingFeedbackCooldownForSmoke();
                int afterAutoCast = beforeAutoCast;
                for (int attempt = 1; attempt <= 24; attempt++)
                {
                    UpdateRuntimeAutomation();
                    afterAutoCast = fishingService.FishingAutoCastApplicationCount;
                    if (afterAutoCast > beforeAutoCast)
                        break;
                    Thread.Sleep(125);
                }
                string bridgeSummary = fishingService.LastFishingAutomationApplicationSummary;
                string attemptSummary = fishingService.LastFishingAutoCastAttemptSummary;
                bool invoked = afterAutoCast > beforeAutoCast && bridgeSummary.IndexOf("AutoCast", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!invoked)
                {
                    summary = "pool={" + poolSource + "}, rod=" + ReadStringMember(fishingRod, "name", fishingRod.GetType().Name) +
                        ", autoCastDelta=" + (afterAutoCast - beforeAutoCast) +
                        ", place={" + placeSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary) +
                        ", lastAttempt=" + (string.IsNullOrWhiteSpace(attemptSummary) ? "none" : attemptSummary);
                    return false;
                }

                summary = "pool={" + poolSource + "}, rod=" + ReadStringMember(fishingRod, "name", fishingRod.GetType().Name) +
                    ", autoCastDelta=" + (afterAutoCast - beforeAutoCast) +
                    ", place={" + placeSummary + "}" +
                    ", toastPolicy=0.2.3-suppressed-no-water-no-rod-cast" +
                    ", bridge=" + bridgeSummary;
                fishingService.SuppressFishingAutoCastForSmoke = true;
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing auto-cast exercise failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing auto-cast exercise failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (fishingService != null)
                {
                    fishingService.ForceFishingNoWaterForSmoke = false;
                    fishingService.ForceFishingNoRodForSmoke = false;
                    fishingService.FishingPoolOverrideForSmoke = null;
                }
            }
        }

        private SmokeAttemptResult TryExerciseAutoFishingPhaseForSmoke()
        {
            FishingAutomationService? fishingService = FishingAutomationService;
            try
            {
                if (fishingService == null)
                    throw new InvalidOperationException("FishingAutomation service was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? fishingWaitType = patcher.ResolveType("DolocTown.AgentStateFishingWait, Assembly-CSharp");
                Type? fishingPoolType = patcher.ResolveType("DolocTown.FishingPool, Assembly-CSharp");
                if (dolocApi == null || fishingWaitType == null || fishingPoolType == null)
                    throw new MissingMemberException("DolocAPI, AgentStateFishingWait, or FishingPool was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogAutoFishingPending("Waiting for NormalGameState before auto-fishing smoke. context=" + runtime.UI.InputContext + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (!autoFishingHotkeyInjected)
                {
                    if (fishingService.TryGetEnabledFishingAutomationOwner(out string externallyEnabledOwner))
                    {
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation observed AutoFishing already enabled before synthetic input; treating F6 input chain as external/real path. owner=" + externallyEnabledOwner + ".");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "Unity input polling -> DTMAPI input event", "AutoFishing enabled by registered hotkey before bridge fallback; owner=" + externallyEnabledOwner + ".");
                    }
                    else if (smokeSettings?.AutoFishingExternalHotkeyRequired == true)
                    {
                        LogAutoFishingPending("Waiting for external F6 keypress to enable AutoFishing. context=" + runtime.UI.InputContext + ", menuOpen=" + runtime.UI.IsOpen + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    else if (runtime.UI.BlocksGameplayHotkeys)
                    {
                        LogAutoFishingPending("Waiting for gameplay hotkeys before synthetic F6 toggle. context=" + runtime.UI.InputContext + ", menuOpen=" + runtime.UI.IsOpen + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    else
                    {
                        runtime.RecordInputPressed("F6");
                        runtime.RecordInputReleased("F6");
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation dispatched AutoFishing toggle key F6 through DTMAPI input service.");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "DtmApiRuntime.RecordInputPressed", "Dispatched F6 through the same registered input event path used by gameplay hotkeys.");
                    }
                }

                if (!fishingService.TryGetEnabledFishingAutomationOwner(out string ownerId))
                    throw new InvalidOperationException("AutoFishing policy was not enabled after the F6 input dispatch. Is the official AutoFishing package enabled?");

                if (!autoExerciseAutoFishingMovementCancelVerified)
                {
                    runtime.RecordInputPressed("W");
                    runtime.RecordInputReleased("W");
                    if (fishingService.TryGetEnabledFishingAutomationOwner(out string stillEnabledOwner))
                        throw new InvalidOperationException("AutoFishing movement cancel did not disable automation. owner=" + stillEnabledOwner);

                    autoExerciseAutoFishingMovementCancelVerified = true;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingMovementCancel OK key=W owner=" + ownerId + ".");
                    runtime.SetHookStatus("Smoke.AutoFishingMovementCancel", "verified", "DTMAPI input W -> AutoFishingMod manual cancel", "Movement key W disabled automation after F6 enable; owner=" + ownerId + ".");

                    runtime.RecordInputPressed("F6");
                    runtime.RecordInputReleased("F6");
                    if (!fishingService.TryGetEnabledFishingAutomationOwner(out ownerId))
                        throw new InvalidOperationException("AutoFishing policy did not re-enable after movement-cancel re-toggle.");
                    runtime.RuntimeMonitor.Log("Smoke automation re-enabled AutoFishing after movement-cancel proof through F6 input. owner=" + ownerId + ".");
                    runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "DtmApiRuntime.RecordInputPressed", "F6 enabled automation and re-enabled it after movement-cancel proof.");
                }

                if (!autoExerciseAutoFishingAutoCastVerified)
                {
                    if (!TryExerciseAutoFishingAutoCastForSmoke(dolocApi, fishingPoolType, out string autoCastSummary))
                        throw new InvalidOperationException("AutoFishing auto-cast path failed. " + autoCastSummary);
                    autoExerciseAutoFishingAutoCastVerified = true;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingAutoCast OK " + autoCastSummary);
                    runtime.SetHookStatus("Smoke.AutoFishingAutoCast", "verified", "BodyController.UseFishRod", autoCastSummary);
                }

                if (!autoExerciseAutoFishingPhaseStarted)
                {
                    object? agent = dolocApi.GetProperty("agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    object? fishingRod = GenerateFishingRodForSmoke(dolocApi);
                    if (agent == null || fishingRod == null)
                        throw new MissingMemberException("DolocAPI.agent or a generated fishing rod was not available.");

                    object? fishingPool = FindOrCreateFishingPoolForSmoke(fishingPoolType, dolocApi, out string poolSource);
                    if (fishingPool == null)
                        throw new InvalidOperationException("No fishing pool was available for auto-fishing smoke. " + poolSource);

                    object? cache = ReadMember(agent, "FishingCache");
                    object? stateManager = ReadMember(agent, "StateManager");
                    if (cache == null || stateManager == null)
                        throw new MissingMemberException("Agent FishingCache or StateManager was not available.");

                    if (!WriteObjectMember(cache, "FishingRod", fishingRod) || !WriteObjectMember(cache, "FishingPool", fishingPool))
                        throw new MissingMemberException("Could not seed FishingCache with a rod and pool for smoke.");

                    MethodInfo? overwrite = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(m =>
                        {
                            if (m.Name != "Overwrite" || !m.IsGenericMethodDefinition)
                                return false;
                            ParameterInfo[] parameters = m.GetParameters();
                            return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
                        });
                    if (overwrite == null)
                        throw new MissingMethodException("AgentStateManager.Overwrite<T>(bool) was not found.");

                    autoFishingApplicationBaseline = fishingService.FishingAutomationApplicationCount;
                    autoFishingMiniGameCompleteBaseline = fishingService.FishingMiniGameCompleteApplicationCount;
                    autoFishingPhaseStartedAt = DateTimeOffset.Now;
                    autoExerciseAutoFishingPhaseStarted = true;
                    fishingService.ForceFishingFishForSmoke = smokeSettings?.AutoExerciseAutoFishingMiniGameComplete == true;
                    runtime.RuntimeMonitor.Log("Smoke automation entering AgentStateFishingWait for AutoFishing phase evidence. owner=" + ownerId + ", rod=" + fishingRod.GetType().Name + ", poolSource=" + poolSource + ".");
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "pending", "AgentStateManager.Overwrite<AgentStateFishingWait>", "Entered real AgentStateFishingWait; waiting for OnPlay instant-bite automation.");
                    overwrite.MakeGenericMethod(fishingWaitType).Invoke(stateManager, new object[] { true });
                    return SmokeAttemptResult.Pending;
                }

                if (fishingService.FishingAutomationApplicationCount > autoFishingApplicationBaseline)
                {
                    string summary = fishingService.LastFishingAutomationApplicationSummary;
                    if (!autoExerciseAutoFishingPhaseVerified)
                    {
                        autoExerciseAutoFishingPhaseVerified = true;
                        runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingPhase OK " + summary);
                        runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "AgentStateFishingWait.OnPlay Postfix", summary);
                    }

                    if (smokeSettings?.AutoExerciseAutoFishingMiniGameComplete == true)
                    {
                        if (fishingService.FishingMiniGameCompleteApplicationCount > autoFishingMiniGameCompleteBaseline)
                        {
                            fishingService.ForceFishingFishForSmoke = false;
                            string completeSummary = string.IsNullOrWhiteSpace(fishingService.LastFishingMiniGameCompleteSummary)
                                ? fishingService.LastFishingAutomationApplicationSummary
                                : fishingService.LastFishingMiniGameCompleteSummary;
                            runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingMiniGameComplete OK " + completeSummary);
                            runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "verified", "FishingGameScrollBar.UpdateGame Postfix", completeSummary);
                            VerifyAutoFishingReportExportForSmoke("AutoFishingMiniGameComplete");
                            return SmokeAttemptResult.Succeeded;
                        }

                        if ((DateTimeOffset.Now - autoFishingPhaseStartedAt).TotalSeconds > 30)
                            throw new TimeoutException("AutoFishing skip=false minigame completion did not apply within 30 seconds after entering AgentStateFishingWait.");

                        LogAutoFishingPending("Waiting for FishingGameScrollBar.UpdateGame auto-complete skip=false. miniGameApplications=" + fishingService.FishingMiniGameCompleteApplicationCount + ".");
                        return SmokeAttemptResult.Pending;
                    }

                    fishingService.ForceFishingFishForSmoke = false;
                    VerifyAutoFishingReportExportForSmoke("AutoFishingPhase");
                    return SmokeAttemptResult.Succeeded;
                }

                if ((DateTimeOffset.Now - autoFishingPhaseStartedAt).TotalSeconds > 20)
                    throw new TimeoutException("AutoFishing wait-phase automation did not apply within 20 seconds after entering AgentStateFishingWait.");

                LogAutoFishingPending("Waiting for AgentStateFishingWait.OnPlay instant-bite application. applications=" + fishingService.FishingAutomationApplicationCount + ".");
                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                if (fishingService != null)
                    fishingService.ForceFishingFishForSmoke = false;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing phase exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private void LogAutoFishingPending(string message)
        {
            if ((DateTimeOffset.Now - lastAutoFishingReadinessLog).TotalSeconds < 5)
                return;
            lastAutoFishingReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke auto-fishing waiting: " + message);
            runtime.SetHookStatus("Smoke.AutoFishingPhase", "pending", "AgentStateFishingWait.OnPlay", message);
        }

        private void VerifyAutoFishingReportExportForSmoke(string scenario)
        {
            if (autoFishingReportExported)
                return;

            if (!TryVerifyDiagnosticsSnapshotForSmoke("AutoFishing " + scenario + " report export", "FishingAutomation"))
                throw new InvalidOperationException("AutoFishing smoke report export verification failed for " + scenario + ".");

            autoFishingReportExported = true;
        }

        private object? GenerateFishingRodForSmoke(Type dolocApi)
        {
            foreach (string itemId in new[] { "carbon_fishrod", "bamboo_fishrod", "simple_fishrod", "old_fishrod" })
            {
                object? item = GenerateItemForSmoke(dolocApi, itemId);
                if (item != null && IsTypeOrBase(item.GetType(), "DolocTown.ItemFishingRod"))
                    return item;
            }
            return null;
        }

        private object? FindOrCreateFishingPoolForSmoke(Type fishingPoolType, Type dolocApi, out string source)
        {
            List<string> details = new List<string>();
            object[] activePools = FindUnityObjects(fishingPoolType);
            details.Add("activeScenePools=" + activePools.Length);
            foreach (object existing in activePools)
            {
                string existingName = ReadStringMember(existing, "PoolName", string.Empty);
                if (string.IsNullOrWhiteSpace(existingName))
                    continue;

                if (TryRollFishForSmoke(dolocApi, existingName, out string sceneFishId, out string sceneRollDetails))
                {
                    source = "scene:" + existingName + ", fish=" + sceneFishId + ", " + string.Join(", ", details);
                    return existing;
                }

                details.Add("scenePoolNoRoll=" + existingName + "(" + sceneRollDetails + ")");
            }

            object[] allPools = FindUnityObjects(fishingPoolType, includeInactive: true);
            if (allPools.Length != activePools.Length)
                details.Add("inactiveOrHiddenScenePools=" + Math.Max(0, allPools.Length - activePools.Length));
            foreach (object existing in allPools)
            {
                string existingName = ReadStringMember(existing, "PoolName", string.Empty);
                if (string.IsNullOrWhiteSpace(existingName) || !TryRollFishForSmoke(dolocApi, existingName, out string hiddenFishId, out string _))
                    continue;

                if (activePools.Contains(existing))
                    continue;

                if (TrySetUnityComponentActiveForSmoke(existing, true, out string activationDetails))
                {
                    details.Add("activatedHiddenScenePool=" + existingName + "(" + hiddenFishId + ", " + activationDetails + ")");
                    source = "activated-scene:" + existingName + ", fish=" + hiddenFishId + ", " + string.Join(", ", details);
                    return existing;
                }

                details.Add("hiddenScenePool=" + existingName + "(" + hiddenFishId + ", activation=" + activationDetails + ")");
                source = "hidden-scene:" + existingName + ", fish=" + hiddenFishId + ", " + string.Join(", ", details);
                return existing;
            }

            IReadOnlyList<string> poolNames = FindFishingPoolNamesForSmoke(out string configDetails);
            details.Add(configDetails);
            foreach (string candidatePoolName in poolNames)
            {
                if (TryRollFishForSmoke(dolocApi, candidatePoolName, out string fishId, out string rollDetails))
                {
                    object? pool = CreateTransientFishingPoolForSmoke(fishingPoolType, candidatePoolName, out string createDetails);
                    details.Add("selected=" + candidatePoolName + ", fish=" + fishId + ", " + createDetails);
                    if (pool != null)
                    {
                        source = "transient:" + candidatePoolName + ", fish=" + fishId + ", " + string.Join(", ", details);
                        return pool;
                    }
                }
                else
                {
                    details.Add("candidateNoRoll=" + candidatePoolName + "(" + rollDetails + ")");
                }
            }

            source = string.Join(", ", details);
            return null;
        }

        private static bool TrySetUnityComponentActiveForSmoke(object component, bool active, out string details)
        {
            details = string.Empty;
            try
            {
                object? gameObject = ReadMember(component, "gameObject");
                MethodInfo? setActive = gameObject?.GetType().GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
                if (gameObject == null || setActive == null)
                {
                    details = "missing-gameObject";
                    return false;
                }

                int activatedParents = 0;
                object? transform = ReadMember(gameObject, "transform");
                for (object? parent = transform == null ? null : ReadMember(transform, "parent"); parent != null; parent = ReadMember(parent, "parent"))
                {
                    object? parentGameObject = ReadMember(parent, "gameObject");
                    MethodInfo? parentSetActive = parentGameObject?.GetType().GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
                    if (parentGameObject == null || parentSetActive == null)
                        break;
                    parentSetActive.Invoke(parentGameObject, new object[] { active });
                    activatedParents++;
                    if (activatedParents >= 16)
                        break;
                }

                setActive.Invoke(gameObject, new object[] { active });
                bool activeSelf = ReadBoolMember(gameObject, "activeSelf", false);
                bool activeInHierarchy = ReadBoolMember(gameObject, "activeInHierarchy", false);
                details = "activeSelf=" + activeSelf + ", activeInHierarchy=" + activeInHierarchy + ", activatedParents=" + activatedParents;
                return activeInHierarchy;
            }
            catch (Exception ex)
            {
                details = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private object? CreateTransientFishingPoolForSmoke(Type fishingPoolType, string poolName, out string details)
        {
            Type? gameObjectType = patcher?.ResolveType("UnityEngine.GameObject, UnityEngine.CoreModule") ?? patcher?.ResolveType("UnityEngine.GameObject, UnityEngine");
            MethodInfo? addComponent = gameObjectType?.GetMethod("AddComponent", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null);
            if (gameObjectType == null || addComponent == null)
            {
                details = "create=missing-gameobject";
                return null;
            }

            object? gameObject = Activator.CreateInstance(gameObjectType, new object[] { "DTMAPI.SmokeFishingPool" });
            object? pool = gameObject == null ? null : addComponent.Invoke(gameObject, new object[] { fishingPoolType });
            if (pool == null || !WriteObjectMember(pool, "poolName", poolName))
            {
                details = "create=failed";
                return null;
            }

            details = "create=ok";
            return pool;
        }

        private IReadOnlyList<string> FindFishingPoolNamesForSmoke(out string details)
        {
            List<string> names = new List<string>();
            List<string> notes = new List<string>();
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            if (dolocConfig == null)
            {
                details = "config=missing-dolocconfig";
                return names;
            }

            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (tables == null)
            {
                details = "config=missing-tables";
                return names;
            }

            object? tbFishingPool = tables == null ? null : ReadMember(tables, "TbFishingPool");
            if (tbFishingPool == null)
            {
                details = "config=missing-tbfishingpool";
                return names;
            }

            object? dataList = tbFishingPool == null ? null : ReadMember(tbFishingPool, "DataList");
            if (dataList is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object info in enumerable)
                {
                    count++;
                    string id = ReadStringMember(info, "Id", string.Empty);
                    object? fishes = ReadMember(info, "Fishes_Ref") ?? ReadMember(info, "Fishes");
                    if (!string.IsNullOrWhiteSpace(id) && HasAnyEnumerableItem(fishes))
                    {
                        AddUnique(names, id);
                        if (notes.Count < 5)
                            notes.Add(id);
                    }
                }
                details = "config=dataList:" + count + ", candidates=" + names.Count + (notes.Count == 0 ? string.Empty : ", sample=" + string.Join("|", notes));
                return names;
            }

            object? dataMap = ReadMember(tbFishingPool!, "DataMap");
            if (dataMap is IDictionary dictionary)
            {
                foreach (object key in dictionary.Keys)
                {
                    if (key is string id && !string.IsNullOrWhiteSpace(id))
                        AddUnique(names, id);
                }
                details = "config=dataMap:" + dictionary.Count + ", candidates=" + names.Count;
                return names;
            }

            if (dataMap is IEnumerable mapEnumerable)
            {
                int count = 0;
                foreach (object entry in mapEnumerable)
                {
                    count++;
                    if (entry == null)
                        continue;
                    string id = ReadMember(entry, "Key") as string ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(id))
                        AddUnique(names, id);
                }
                details = "config=dataMapEnumerable:" + count + ", candidates=" + names.Count;
                return names;
            }

            details = "config=missing-datalist-and-datamap";
            return names;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (values.Any(existing => existing.Equals(value, StringComparison.Ordinal)))
                return;
            values.Add(value);
        }

        private static bool TryRollFishForSmoke(Type dolocApi, string poolName, out string fishId, out string details)
        {
            fishId = string.Empty;
            MethodInfo? rollFish = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "RollFish")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(int);
                });
            if (rollFish == null)
            {
                details = "missing-rollfish";
                return false;
            }

            int attempts = 0;
            foreach (int toolLevel in new[] { 5, 4, 3, 2, 1, 0 })
            {
                attempts++;
                object? fish = rollFish.Invoke(null, new object[] { poolName, toolLevel });
                if (fish == null)
                    continue;

                fishId = ReadStringMember(fish, "Id", fish.GetType().Name);
                details = "toolLevel=" + toolLevel + ", attempts=" + attempts;
                return true;
            }
            details = "attempts=" + attempts + ", no-fish";
            return false;
        }

        private static bool HasAnyEnumerableItem(object? value)
        {
            if (!(value is IEnumerable enumerable))
                return false;
            foreach (object _ in enumerable)
                return true;
            return false;
        }
    }
}
