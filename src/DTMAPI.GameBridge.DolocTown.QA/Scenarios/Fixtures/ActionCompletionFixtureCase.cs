using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private bool oneActionContinuationEvidenceCompleted;

        private FixtureAttemptResult TryExerciseOneActionResourceHitForFixture()
        {
            object? transientResource = null;
            object? transientRoom = null;
            try
            {
                ActionCompletionService? actionCompletion = ActionCompletionService;
                OneActionProductFixtureState product = CaptureOneActionProductFixtureState();
                EnsureOneActionFixtureOwnerReady(actionCompletion, product);

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                Type? resourceRendererType = patcher.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null || resourceRendererType == null)
                    throw new MissingMemberException("DolocAPI, ToolCollider, or DungeonResourceRenderer was not visible.");

                transientRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (transientRoom == null || !TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    runtime.SetHookStatus("Smoke.OneActionResourceHit", "pending", "ToolCollider.HandleTools", "Waiting for normal gameplay before creating the transient-only resource fixture.");
                    return FixtureAttemptResult.Pending;
                }
                if (!oneActionContinuationEvidenceCompleted)
                {
                    FixtureAttemptResult continuation = TryExerciseOneActionContinuationEvidenceForFixture(dolocApi, toolColliderType, resourceRendererType);
                    if (continuation != FixtureAttemptResult.Succeeded)
                        return continuation;
                    oneActionContinuationEvidenceCompleted = true;
                }
                if (!TryCreateTransientOneActionResourceForFixture(transientRoom, null, null, out transientResource, out string transientSummary) || transientResource == null)
                    throw new InvalidOperationException("No transient-only one-action resource could be created. " + transientSummary);

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForFixture(dolocApi, toolColliderType);
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
                    if (!ReferenceEquals(resource, transientResource))
                        continue;
                    if (resource == null || IsRemoved(resource))
                        continue;
                    resourceCount++;
                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (healthBefore <= 0)
                        continue;
                    string compatibilityOwnerId = string.Empty;
                    bool policyMatch = product.Present
                        ? product.MatchesResourceKind(GetOneActionResourceKind(resource))
                        : actionCompletion!.TryFindOneActionPolicyForFixture(resource, out compatibilityOwnerId);
                    string ownerId = product.Present ? product.OwnerId : compatibilityOwnerId;
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
                    object? tool = GenerateItemForFixture(dolocApi, toolId);
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

                    int appliedBefore = actionCompletion?.OneActionApplicationCount ?? 0;
                    resetTool.Invoke(toolCollider, new object[] { tool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    bool applied = product.Present
                        ? removedAfter || healthAfter <= 0
                        : actionCompletion!.OneActionApplicationCount > appliedBefore;
                    if (applied)
                    {
                        string summary = "owner=" + ownerId + ", product=" + product.Summary + ", resource=" + resourceName + ", tool=" + toolId + ", healthBefore=" + healthBefore + ", seededHealth=" + seededHealth + ", toolDamage=" + toolDamage + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", bridge=" + (actionCompletion?.LastOneActionApplicationSummary ?? "none");
                        runtime.RuntimeMonitor.Log("Smoke exercise OneActionResourceHit OK " + summary);
                        runtime.SetHookStatus("Smoke.OneActionResourceHit", "verified", "ToolCollider.HandleTools private path on transient DungeonResourceRenderer", summary + ", setup={" + transientSummary + "}");
                        return FixtureAttemptResult.Succeeded;
                    }
                }

                throw new InvalidOperationException("No rendered resource matched an enabled OneAction policy and survived the normal ToolCollider hit long enough for the Postfix completion path. renderers=" + rendererCount + ", resources=" + resourceCount + ", policyMatches=" + policyMatchCount + ", colliders=" + colliderCount + ", tools=" + toolCount + ", invoked=" + invokedCount + ", samples=" + string.Join(" ; ", samples));
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action resource-hit exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionResourceHit", "failed", "ToolCollider.HandleTools", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
            finally
            {
                if (transientResource != null)
                    TryRemoveTransientDungeonResourceForFixture(transientRoom, transientResource);
            }
        }

        private FixtureAttemptResult TryExerciseOneActionContinuationEvidenceForFixture(Type dolocApi, Type toolColliderType, Type resourceRendererType)
        {
            try
            {
                ExerciseOneActionConfigSaveReloadForFixture();
                ExerciseOneActionPartialEnergyForFixture(dolocApi, toolColliderType, resourceRendererType);
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "OneActionComplete continuation evidence failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionContinuationEvidence", "failed", "ConfigMenu persistence + native partial-energy path", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private void ExerciseOneActionConfigSaveReloadForFixture()
        {
            const string ownerId = "Yuuka.DTMAPI.OneActionComplete";
            FieldInfo? instancesField = runtime.GetType().GetField("modInstances", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(instancesField?.GetValue(runtime) is IDictionary instances) || !instances.Contains(ownerId) || instances[ownerId] == null)
                throw new InvalidOperationException("OneActionComplete owner instance is unavailable for config persistence evidence.");
            object entry = instances[ownerId]!;
            object helper = entry.GetType().GetField("helper", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(entry)
                ?? throw new MissingFieldException(entry.GetType().FullName, "helper");
            object config = entry.GetType().GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(entry)
                ?? throw new MissingFieldException(entry.GetType().FullName, "config");
            Type configType = config.GetType();
            bool original = Convert.ToBoolean(configType.GetProperty("VerboseLogging", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(config) ?? false);
            bool toggled = !original;
            bool restored = false;
            try
            {
                EditAndSaveConfigPage(ownerId, page =>
                    SetConfigPendingValue(page, "Bool", toggled ? "true" : "false", "详细日志", "Verbose logs"));
                object reloaded = ReadOwnerConfigForFixture(helper, configType);
                bool reloadedValue = Convert.ToBoolean(configType.GetProperty("VerboseLogging", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(reloaded) ?? original);
                object liveConfig = entry.GetType().GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(entry)
                    ?? throw new MissingFieldException(entry.GetType().FullName, "config");
                bool liveValue = Convert.ToBoolean(configType.GetProperty("VerboseLogging", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(liveConfig) ?? original);
                if (reloadedValue != toggled || liveValue != toggled)
                    throw new InvalidOperationException("OneActionComplete ConfigMenu save did not survive helper.ReadConfig reload. live=" + liveValue + "; reloaded=" + reloadedValue + "; expected=" + toggled + ".");
            }
            finally
            {
                EditAndSaveConfigPage(ownerId, page =>
                    SetConfigPendingValue(page, "Bool", original ? "true" : "false", "详细日志", "Verbose logs"));
                object restoredConfig = ReadOwnerConfigForFixture(helper, configType);
                restored = Convert.ToBoolean(configType.GetProperty("VerboseLogging", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(restoredConfig) ?? !original) == original;
            }
            if (!restored)
                throw new InvalidOperationException("OneActionComplete ConfigMenu persistence fixture did not restore its original value.");
            string details = "property=VerboseLogging; original=" + original + "; saved=" + toggled + "; helperReload=" + toggled + "; restored=" + original;
            runtime.SetHookStatus("Smoke.OneActionConfigReload", "verified", "DTMAPI ConfigMenu Save -> owner helper.ReadConfig<T> -> restore", details);
            runtime.RuntimeMonitor.Log("Smoke exercise OneActionConfigReload OK " + details + ".");
        }

        private static object ReadOwnerConfigForFixture(object helper, Type configType)
        {
            MethodInfo method = helper.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate => candidate.Name == "ReadConfig" && candidate.IsGenericMethodDefinition && candidate.GetParameters().Length == 0)
                ?? throw new MissingMethodException(helper.GetType().FullName, "ReadConfig<T>()");
            return method.MakeGenericMethod(configType).Invoke(helper, null)
                ?? throw new InvalidOperationException("Owner helper.ReadConfig<T>() returned null.");
        }

        private void ExerciseOneActionPartialEnergyForFixture(Type dolocApi, Type toolColliderType, Type resourceRendererType)
        {
            object? transientRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            object? transientResource = null;
            object? agentData = null;
            int originalEnergy = 0;
            int originalOverflowEnergy = 0;
            bool energyCaptured = false;
            try
            {
                string transientSummary = string.Empty;
                if (transientRoom == null ||
                    !TryCreateTransientOneActionResourceForFixture(transientRoom, null, null, out transientResource, out transientSummary) ||
                    transientResource == null)
                {
                    throw new InvalidOperationException("Could not create transient partial-energy resource. " + transientSummary);
                }
                object? archiveHandle = ReadStaticMember(dolocApi, "archiveHandle");
                object? farmData = archiveHandle == null ? null : ReadMember(archiveHandle, "farmData");
                agentData = farmData == null ? null : ReadMember(farmData, "agentData");
                object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
                int toolEnergyCost = globalParameter == null ? 0 : ReadIntMember(globalParameter, "ToolEnergyCost", 0);
                if (agentData == null || toolEnergyCost <= 0)
                    throw new MissingMemberException("AgentArchiveData or GlobalParameter.ToolEnergyCost was unavailable.");
                originalEnergy = ReadIntMember(agentData, "energy", -1);
                originalOverflowEnergy = ReadIntMember(agentData, "overflowEnergy", -1);
                if (originalEnergy < 0 || originalOverflowEnergy < 0)
                    throw new MissingMemberException("Agent energy fields were unavailable.");
                energyCaptured = true;

                MethodInfo handleTools = FindMethod(toolColliderType, "HandleTools", 1)
                    ?? throw new MissingMethodException(toolColliderType.FullName, "HandleTools");
                MethodInfo resetTool = FindMethod(toolColliderType, "ResetTool", 1)
                    ?? throw new MissingMethodException(toolColliderType.FullName, "ResetTool");
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                object toolCollider = FindToolColliderForFixture(dolocApi, toolColliderType)
                    ?? throw new InvalidOperationException("No ToolCollider instance was available for partial-energy evidence.");

                object? renderer = FindUnityObjects(resourceRendererType)
                    .FirstOrDefault(candidate => ReferenceEquals(ReadMember(candidate, "DungeonResource"), transientResource));
                object? collider = renderer == null ? null : ReadMember(renderer, "PolygonCollider");
                if (renderer == null || collider == null || !TryChooseToolIdForResource(transientResource, out string toolId))
                    throw new InvalidOperationException("Transient partial-energy resource did not expose a matching renderer/collider/tool.");
                object tool = GenerateItemForFixture(dolocApi, toolId)
                    ?? throw new InvalidOperationException("Could not generate the matching partial-energy tool " + toolId + ".");
                Type fellDataType = patcher?.ResolveType("DolocTown.ResourceFellData, Assembly-CSharp")
                    ?? throw new TypeLoadException("DolocTown.ResourceFellData was unavailable.");
                object hitPoint = ReadMember(transientResource, "PositionCenter")
                    ?? throw new MissingMemberException(transientResource.GetType().FullName, "PositionCenter");
                object nativeFellData = Activator.CreateInstance(fellDataType, new[] { transientResource, tool, hitPoint })
                    ?? throw new InvalidOperationException("Native ResourceFellData could not be created.");
                int nativeDamage = Math.Max(1, ReadIntMember(nativeFellData, "Damage", 0));
                int seededHealth = checked(nativeDamage * 4);
                if (!WriteIntMember(transientResource, "currentHealth", seededHealth) ||
                    !WriteIntMember(agentData, "energy", checked(toolEnergyCost * 2)) ||
                    !WriteIntMember(agentData, "overflowEnergy", 0))
                {
                    throw new InvalidOperationException("Could not seed the transient partial-energy state.");
                }
                RefreshEnergyUiForFixture(dolocApi);

                resetTool.Invoke(toolCollider, new[] { tool });
                resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                handleTools.Invoke(toolCollider, new[] { collider });

                int healthAfter = ReadIntMember(transientResource, "currentHealth", -1);
                int energyAfter = ReadIntMember(agentData, "energy", -1);
                bool removed = IsRemoved(transientResource);
                int damageApplied = seededHealth - healthAfter;
                if (removed || healthAfter <= 0 || damageApplied < nativeDamage * 2 || damageApplied >= seededHealth || energyAfter != 0)
                {
                    throw new InvalidOperationException(
                        "OneActionComplete partial-energy behavior was not bounded by native energy. health=" + seededHealth + "->" + healthAfter +
                        "; nativeDamage=" + nativeDamage +
                        "; damageApplied=" + damageApplied +
                        "; energy=" + (toolEnergyCost * 2) + "->" + energyAfter +
                        "; removed=" + removed + ".");
                }
                string details = "resource=" + ReadStringMember(transientResource, "ResourceName", transientResource.GetType().Name) +
                    "; nativeDamage=" + nativeDamage +
                    "; health=" + seededHealth + "->" + healthAfter +
                    "; energy=" + (toolEnergyCost * 2) + "->" + energyAfter +
                    "; paidExtraHits=1; complete=false";
                runtime.SetHookStatus("Smoke.OneActionPartialEnergy", "verified", "ToolCollider normal hit -> product extra-hit loop -> native energy rejection", details);
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionPartialEnergy OK " + details + ".");
            }
            finally
            {
                if (energyCaptured && agentData != null)
                {
                    WriteIntMember(agentData, "energy", originalEnergy);
                    WriteIntMember(agentData, "overflowEnergy", originalOverflowEnergy);
                    RefreshEnergyUiForFixture(dolocApi);
                }
                if (transientResource != null)
                    TryRemoveTransientDungeonResourceForFixture(transientRoom, transientResource);
            }
        }

        private static void RefreshEnergyUiForFixture(Type dolocApi)
        {
            try
            {
                FindMethod(dolocApi, "ChangeEnergy", 1)?.Invoke(null, new object[] { 0 });
            }
            catch
            {
                // The energy fields are the authoritative fixture state. UI refresh is
                // best-effort and the exact original values are restored in finally.
            }
        }

        private FixtureAttemptResult TryExerciseOneActionWrongToolForFixture()
        {
            object? transientResource = null;
            object? transientRoom = null;
            try
            {
                ActionCompletionService? actionCompletion = ActionCompletionService;
                OneActionProductFixtureState product = CaptureOneActionProductFixtureState();
                EnsureOneActionFixtureOwnerReady(actionCompletion, product);

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                Type? resourceRendererType = patcher.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null || resourceRendererType == null)
                    throw new MissingMemberException("DolocAPI, ToolCollider, or DungeonResourceRenderer was not visible.");

                transientRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (transientRoom == null || !TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    runtime.SetHookStatus("Smoke.OneActionWrongTool", "pending", "ToolCollider.HandleTools", "Waiting for normal gameplay before creating the transient-only wrong-tool fixture.");
                    return FixtureAttemptResult.Pending;
                }
                if (!TryCreateTransientOneActionResourceForFixture(transientRoom, null, null, out transientResource, out string matrixResourceSummary) || transientResource == null)
                    throw new InvalidOperationException("No transient-only wrong-tool resource could be created. " + matrixResourceSummary);

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForFixture(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

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
                    if (!ReferenceEquals(resource, transientResource))
                        continue;
                    if (resource == null || IsRemoved(resource))
                        continue;
                    resourceCount++;

                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (healthBefore <= 0)
                        continue;

                    string compatibilityOwnerId = string.Empty;
                    bool policyMatch = product.Present
                        ? product.MatchesResourceKind(GetOneActionResourceKind(resource))
                        : actionCompletion!.TryFindOneActionPolicyForFixture(resource, out compatibilityOwnerId);
                    string ownerId = product.Present ? product.OwnerId : compatibilityOwnerId;
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

                    object? wrongTool = GenerateItemForFixture(dolocApi, wrongToolId);
                    if (wrongTool == null)
                        continue;
                    toolCount++;

                    int appliedBefore = actionCompletion?.OneActionApplicationCount ?? 0;
                    resetTool.Invoke(toolCollider, new object[] { wrongTool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int appliedAfter = actionCompletion?.OneActionApplicationCount ?? 0;
                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    bool ownerStayedIdle = product.Present || appliedAfter == appliedBefore;
                    if (ownerStayedIdle && healthAfter == healthBefore && !removedAfter)
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
                    runtime.SetHookStatus("Smoke.OneActionWrongTool", status, "ToolCollider.HandleTools private path on transient DungeonResourceRenderer", summary);
                    return FixtureAttemptResult.Succeeded;
                }

                throw new InvalidOperationException("No rendered resource produced a clean wrong-tool negative result. renderers=" + rendererCount + ", resources=" + resourceCount + ", policyMatches=" + policyMatchCount + ", colliders=" + colliderCount + ", tools=" + toolCount + ", invoked=" + invokedCount + ", samples=" + string.Join(" ; ", samples) + ", resourceSetup=" + matrixResourceSummary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action wrong-tool exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionWrongTool", "failed", "ToolCollider.HandleTools", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
            finally
            {
                if (transientResource != null)
                    TryRemoveTransientDungeonResourceForFixture(transientRoom, transientResource);
            }
        }

        private FixtureAttemptResult TryExerciseOneActionFuelFeedForFixture()
        {
            try
            {
                ActionCompletionService? actionCompletion = ActionCompletionService;
                OneActionProductFixtureState product = CaptureOneActionProductFixtureState();
                EnsureOneActionFixtureOwnerReady(actionCompletion, product);

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogOneActionFuelFeedPending("Waiting for NormalGameState before one-action fuel/feed smoke. context=" + runtime.UI.InputContext + ".");
                    return FixtureAttemptResult.Pending;
                }

                if (!actionSpeedInteractExitPatched)
                {
                    LogOneActionFuelFeedPending("Waiting for AgentStateInteract.OnExit Postfix before one-action fuel/feed smoke.");
                    return FixtureAttemptResult.Pending;
                }

                if (!TryExerciseOneActionEquipmentFillKindForFixture(
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

                if (!TryExerciseOneActionEquipmentFillKindForFixture(
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

                string summary = "owner=" + (product.Present ? product.OwnerId : "frozen-compatibility-demand") + ", product=" + product.Summary + ", fuel={" + fuelSummary + "}; feeder={" + feederSummary + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionFuelFeed OK " + summary);
                runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "Equipment.DecoratedInteract -> AgentStateInteract.OnExit Postfix -> native CostSelf/AddFuel/AddFeeds", summary);
                runtime.SetHookStatus("Actions.OneActionFuelFeed", "verified", "Harmony Postfix: AgentStateInteract.OnExit", "Fuel-machine and animal-feeder native CostSelf/AddFuel/AddFeeds paths verified by smoke. " + summary);
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action fuel/feed exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionFuelFeed", "failed", "AgentStateInteract.OnExit", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult TryExerciseOneActionVegetationForFixture()
        {
            object? vegetation = null;
            try
            {
                ActionCompletionService? actionCompletion = ActionCompletionService;
                OneActionProductFixtureState product = CaptureOneActionProductFixtureState();
                EnsureOneActionFixtureOwnerReady(actionCompletion, product);

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null)
                    throw new MissingMemberException("DolocAPI or ToolCollider was not visible.");

                object? currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (currentRoom == null)
                    throw new InvalidOperationException("CurrentRoom was unavailable.");

                bool isMainFarm = IsMainFarmRoomForFixture(dolocApi, currentRoom);
                if (!isMainFarm && !autoExerciseOneActionMainFarmRequested && TryEnterMainFarmForOneActionSmoke(dolocApi, currentRoom, out string transitionSummary))
                {
                    LogOneActionVegetationPending(transitionSummary);
                    return FixtureAttemptResult.Pending;
                }
                if (!isMainFarm && autoExerciseOneActionMainFarmRequested)
                {
                    LogOneActionVegetationPending("Waiting for main farm transition before vegetation smoke. room=" + DescribeRoomForFixture(currentRoom));
                    return FixtureAttemptResult.Pending;
                }

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForFixture(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

                currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (currentRoom == null)
                    throw new InvalidOperationException("CurrentRoom was unavailable after transition.");

                if (!TryCreateTransientDandelionVegetationForFixture(dolocApi, currentRoom, out vegetation, out object? renderer, out string sourceSummary) ||
                    vegetation == null ||
                    renderer == null)
                {
                    throw new InvalidOperationException("Could not create a transient dandelion vegetation sample. " + sourceSummary);
                }

                if (!TryChooseToolIdsForVegetation(vegetation, out string expectedToolId, out string expectedToolType, out int expectedMinLevel, out string wrongToolId, out string toolSummary))
                    throw new InvalidOperationException("Could not resolve vegetation tool constraints. " + toolSummary);

                object? collider = ReadMember(renderer, "Collider2d");
                if (collider == null)
                    throw new InvalidOperationException("VegetationRenderer.Collider2d was unavailable. target=" + DescribeVegetationForFixture(vegetation));

                object? wrongTool = GenerateItemForFixture(dolocApi, wrongToolId);
                object? expectedTool = GenerateItemForFixture(dolocApi, expectedToolId);
                if (wrongTool == null || expectedTool == null)
                    throw new InvalidOperationException("Could not generate vegetation smoke tools. expected=" + expectedToolId + ", wrong=" + wrongToolId);

                int appBeforeWrong = actionCompletion?.OneActionApplicationCount ?? 0;
                resetTool.Invoke(toolCollider, new object[] { wrongTool });
                resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                handleTools.Invoke(toolCollider, new object[] { collider });
                int appAfterWrong = actionCompletion?.OneActionApplicationCount ?? 0;
                bool removedAfterWrong = ReadMember(renderer, "Vegetation") == null;
                if (removedAfterWrong || appAfterWrong != appBeforeWrong)
                    throw new InvalidOperationException("Wrong-tool vegetation hit was not a clean negative. removed=" + removedAfterWrong + ", oneActionDelta=" + (appAfterWrong - appBeforeWrong));

                int appBeforeCorrect = actionCompletion?.OneActionApplicationCount ?? 0;
                resetTool.Invoke(toolCollider, new object[] { expectedTool });
                resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                handleTools.Invoke(toolCollider, new object[] { collider });
                int appAfterCorrect = actionCompletion?.OneActionApplicationCount ?? 0;
                bool removedAfterCorrect = ReadMember(renderer, "Vegetation") == null;
                if (!removedAfterCorrect || appAfterCorrect != appBeforeCorrect)
                    throw new InvalidOperationException("Correct-tool vegetation hit did not stay on the native one-fell path. removed=" + removedAfterCorrect + ", oneActionDelta=" + (appAfterCorrect - appBeforeCorrect));

                string wrongToolName = ReadStringMember(wrongTool, "name", wrongToolId);
                string wrongToolType = ReadMember(wrongTool, "ToolType")?.ToString() ?? "unknown";
                string expectedToolName = ReadStringMember(expectedTool, "name", expectedToolId);
                string summary = "target=" + DescribeVegetationForFixture(vegetation) +
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
                    ", resourcePath=DungeonResourceRenderer:none" +
                    ", product=" + product.Summary;
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionVegetation OK " + summary);
                runtime.SetHookStatus("Smoke.OneActionVegetation", "verified", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", summary);
                runtime.SetHookStatus("Actions.OneActionVegetation", "verified", "native Vegetation.CheckToolConstraints", "Dandelion/vegetation is not a DungeonResource one-action path; wrong tools are rejected by the game and correct tools fell through native Vegetation.OnFell. " + summary);
                return FixtureAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action vegetation exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionVegetation", "failed", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
            finally
            {
                if (vegetation != null)
                    TryRemoveTransientVegetationForFixture(ReadStaticMember(patcher?.ResolveType("DolocAPI, Assembly-CSharp"), "CurrentRoom"), vegetation);
            }
        }

        private bool TryExerciseOneActionEquipmentFillKindForFixture(Type dolocApi, string kind, string targetTypeName, string[] equipmentIds, string[] itemIds, string ratioMember, int stackCount, out string summary)
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

                object? item = GenerateItemForFixture(dolocApi, itemId!, stackCount);
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
                if (anchor == null || !TrySelectEquipmentForFixture(dolocApi, equipment, anchor, out selectSummary))
                {
                    summary = "Could not select target equipment. target=" + DescribeEquipmentForFixture(equipment) + ", " + selectSummary;
                    return false;
                }

                ActionCompletionService? actionCompletion = ActionCompletionService;
                int beforeApplications = actionCompletion?.OneActionApplicationCount ?? 0;
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

                int afterApplications = actionCompletion?.OneActionApplicationCount ?? 0;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double afterRatio = ReadDoubleMember(equipment, ratioMember, -1);
                int totalConsumed = beforeCount - afterCount;
                OneActionProductFixtureState product = CaptureOneActionProductFixtureState();
                bool applied = product.Present ? product.Ready && totalConsumed >= 2 : afterApplications > beforeApplications;
                bool changed = afterRatio > beforeRatio || (beforeRatio < 0 && totalConsumed > 0);
                if (!applied || totalConsumed < 2 || !changed)
                {
                    summary = "target=" + DescribeEquipmentForFixture(equipment) +
                        ", item=" + itemId +
                        ", source=" + targetSource +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                        ", oneActionDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", product=" + product.Summary +
                        ", bridge=" + (actionCompletion?.LastOneActionApplicationSummary ?? "none");
                    return false;
                }

                summary = "kind=" + kind +
                    ", target=" + DescribeEquipmentForFixture(equipment) +
                    ", item=" + itemId +
                    ", source=" + targetSource +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", totalConsumed=" + totalConsumed +
                    ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                    ", oneActionDelta=" + (afterApplications - beforeApplications) +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", product=" + product.Summary +
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
                    TryRemoveTransientEquipmentForFixture(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private void EnsureOneActionFixtureOwnerReady(ActionCompletionService? compatibilityOwner, OneActionProductFixtureState product)
        {
            if (product.Present)
            {
                if (!product.Ready)
                    throw new InvalidOperationException("OneActionComplete managed product was present but its two-hook atomic owner was not ready. " + product.Summary);
                return;
            }

            if (compatibilityOwner == null)
                throw new InvalidOperationException("Neither the OneActionComplete managed product nor the frozen ActionCompletion compatibility service was available.");
        }

        private OneActionProductFixtureState CaptureOneActionProductFixtureState()
        {
            const string ownerId = "Yuuka.DTMAPI.OneActionComplete";
            FieldInfo? instancesField = runtime.GetType().GetField("modInstances", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(instancesField?.GetValue(runtime) is IDictionary instances) || !instances.Contains(ownerId))
                return OneActionProductFixtureState.Absent(ownerId);

            object? entry = instances[ownerId];
            if (entry == null)
                return OneActionProductFixtureState.Failed(ownerId, "published owner instance was null");

            try
            {
                FieldInfo nativeField = entry.GetType().GetField("nativeRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(entry.GetType().FullName, "nativeRuntime");
                FieldInfo configField = entry.GetType().GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(entry.GetType().FullName, "config");
                object native = nativeField.GetValue(entry) ?? throw new InvalidOperationException("nativeRuntime was null");
                object config = configField.GetValue(entry) ?? throw new InvalidOperationException("config was null");
                int patchCount = Convert.ToInt32(native.GetType().GetProperty("InstalledPatchCount", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(native) ?? -1);
                Type callbacks = entry.GetType().Assembly.GetType("Yuuka.DTMAPI.OneActionComplete.OneActionCallbacks", throwOnError: true)!;
                object? callbackRuntime = callbacks.GetField("runtime", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                bool callbackAttached = ReferenceEquals(callbackRuntime, native);
                return new OneActionProductFixtureState(
                    ownerId,
                    present: true,
                    patchCount,
                    callbackAttached,
                    ReadRequiredBoolProperty(config, "Enabled"),
                    ReadRequiredBoolProperty(config, "CompleteTrees"),
                    ReadRequiredBoolProperty(config, "CompleteOres"),
                    ReadRequiredBoolProperty(config, "CompleteGarbage"),
                    ReadRequiredBoolProperty(config, "CompleteWeeds"),
                    ReadRequiredBoolProperty(config, "CompleteMachineFuel"),
                    ReadRequiredBoolProperty(config, "CompleteFeeder"),
                    error: string.Empty);
            }
            catch (Exception ex)
            {
                return OneActionProductFixtureState.Failed(ownerId, ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool ReadRequiredBoolProperty(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new MissingMemberException(target.GetType().FullName, name);
            return Convert.ToBoolean(property.GetValue(target));
        }

        private sealed class OneActionProductFixtureState
        {
            internal OneActionProductFixtureState(string ownerId, bool present, int patchCount, bool callbackAttached, bool enabled, bool trees, bool ores, bool garbage, bool weeds, bool fuel, bool feeder, string error)
            {
                OwnerId = ownerId;
                Present = present;
                PatchCount = patchCount;
                CallbackAttached = callbackAttached;
                Enabled = enabled;
                Trees = trees;
                Ores = ores;
                Garbage = garbage;
                Weeds = weeds;
                Fuel = fuel;
                Feeder = feeder;
                Error = error ?? string.Empty;
            }

            internal string OwnerId { get; }
            internal bool Present { get; }
            internal int PatchCount { get; }
            internal bool CallbackAttached { get; }
            internal bool Enabled { get; }
            internal bool Trees { get; }
            internal bool Ores { get; }
            internal bool Garbage { get; }
            internal bool Weeds { get; }
            internal bool Fuel { get; }
            internal bool Feeder { get; }
            internal string Error { get; }
            internal bool Ready => Present && PatchCount == 2 && CallbackAttached && Enabled && string.IsNullOrEmpty(Error);
            internal string Summary => "present=" + Present + ", ready=" + Ready + ", patches=" + PatchCount + ", callbackAttached=" + CallbackAttached + ", enabled=" + Enabled + (string.IsNullOrEmpty(Error) ? string.Empty : ", error=" + Error);

            internal bool MatchesResourceKind(string kind)
            {
                if (!Ready)
                    return false;
                if (kind.Equals("Tree", StringComparison.OrdinalIgnoreCase)) return Trees;
                if (kind.Equals("Ore", StringComparison.OrdinalIgnoreCase)) return Ores;
                if (kind.Equals("Garbage", StringComparison.OrdinalIgnoreCase)) return Garbage;
                if (kind.Equals("Weeds", StringComparison.OrdinalIgnoreCase)) return Weeds;
                return false;
            }

            internal static OneActionProductFixtureState Absent(string ownerId) => new OneActionProductFixtureState(ownerId, false, 0, false, false, false, false, false, false, false, false, string.Empty);
            internal static OneActionProductFixtureState Failed(string ownerId, string error) => new OneActionProductFixtureState(ownerId, true, -1, false, false, false, false, false, false, false, false, error);
        }
    }
}
