using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void RegisterGameBridgeFeatureApis(IManifest manifest)
        {
            DispatchGameBridgeFeatures("RegisterApis", feature => feature.RegisterApis(manifest));
        }

        private void PublishGameBridgeFeatureHookStatuses()
        {
            foreach (IGameBridgeFeature feature in features)
            {
                if (!HasGameBridgeDemand(feature.Id))
                    continue;
                DispatchGameBridgeFeature(feature, "PublishHookStatuses", item => item.PublishHookStatuses());
            }
        }

        private void InstallGameBridgeFeatureHooks(HarmonyReflectionPatcher patcher)
        {
            // Fishing needs the patcher reference for synchronous first-use activation,
            // but BindPatcher performs no reflection search or patch installation.
            fishingCompatibilityFeature?.BindPatcher(patcher);
            // Demand can be registered before the managed product loads. Reconcile at the
            // physical install boundary so both load orders fail closed before either
            // ActionCompletion dependency Hook is installed.
            actionCompletionFeature?.Service.ReconcileManagedProductOwnerBeforeHookInstall();
            // ActionSpeed has the same bidirectional exclusion requirement across its
            // parent route and five child demand routes. Reconcile before shared exits
            // or unique native stages can be installed by the frozen compatibility owner.
            actionSpeedFeature?.Service.ReconcileManagedProductOwnerBeforeHookInstall();
            // FishRoe compatibility can be requested before the managed product loads.
            // Remove that pending old-ABI demand before its three item-display Hooks install.
            fishRoeTooltipFeature?.Service.ReconcileManagedProductOwnerBeforeHookInstall();
            // Animal compatibility has the same two-order gap: old demand can predate
            // the managed product, so remove it before the next deferred Hook pass.
            animalViewerFeature?.Service.ReconcileManagedProductOwnerBeforeHookInstall();
            cameraFeature?.ReconcileManagedProductOwnerBeforeHookInstall();
            if (HasGameBridgeDemand(GameBridgeDemandRoutes.AgentStateToolExitShared))
                agentStateLifecycleHooks?.InstallToolExitHook(patcher);
            if (HasGameBridgeDemand(GameBridgeDemandRoutes.AgentStateInteractExitShared))
                agentStateLifecycleHooks?.InstallInteractExitHook(patcher);
            if (HasGameBridgeDemand(GameBridgeDemandRoutes.AgentStateBaseExitShared))
                agentStateLifecycleHooks?.InstallBaseExitHook(patcher);
            if (HasGameBridgeDemand(GameBridgeDemandRoutes.ToolColliderShared))
                toolColliderHitHooks?.InstallHooks(patcher);

            InstallDemandedEnvironmentResetHook(patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.Camera, cameraFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.FishingCompatibility, fishingCompatibilityFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.FishRoeTooltip, fishRoeTooltipFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.ChestLocatorEnhancer, chestLocatorEnhancerFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.SaveSlots, saveSlotsFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, nativeUiLayoutDiagnosticsFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.AnimalViewer, animalViewerFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.CustomAnimalAnimatorBridge, customAnimalAnimatorBridgeFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.AudioReplacement, audioReplacementFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.ActionSpeed, actionSpeedFeature, patcher);
            InstallDemandedFeatureHooks(GameBridgeDemandRoutes.ActionCompletion, actionCompletionFeature, patcher);
        }

        private void InstallDemandedEnvironmentResetHook(HarmonyReflectionPatcher patcher)
        {
            if (environmentResetHookBridge == null ||
                !HasGameBridgeDemand(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset))
            {
                return;
            }

            environmentResetHookBridge.InstallHooks(patcher);
        }

        private void InstallDemandedFeatureHooks(
            string capabilityId,
            IGameBridgeFeature? feature,
            HarmonyReflectionPatcher patcher)
        {
            if (feature == null || !HasGameBridgeDemand(capabilityId))
                return;
            DispatchGameBridgeFeature(feature, "InstallHooks", item => item.InstallHooks(patcher));
        }

        private void DispatchGameBridgeFeatures(string operation, Action<IGameBridgeFeature> action)
        {
            int count = features.Count;
            for (int i = 0; i < count; i++)
                DispatchGameBridgeFeature(features[i], operation, action);
        }

        private bool DispatchGameBridgeFeature(IGameBridgeFeature feature, string operation, Action<IGameBridgeFeature> action)
        {
            string id = "<unknown>";
            try
            {
                id = GetGameBridgeFeatureId(feature);
                action(feature);
                GetGameBridgeFeatureRuntimeState(id).RecordDispatch(operation);
                GameBridgeFeatureStatus status = RecordGameBridgeFeatureSuccess(id, operation);
                string details = FormatGameBridgeFeatureStatus(status);
                PublishGameBridgeFeatureStatusIfNeeded(
                    status,
                    "ready",
                    operation,
                    "Safe feature host dispatch completed " + operation + " for this GameBridge feature. " + details);
                return true;
            }
            catch (Exception ex)
            {
                GetGameBridgeFeatureRuntimeState(id).RecordFailure(operation, DateTimeOffset.UtcNow);
                GameBridgeFeatureStatus status = RecordGameBridgeFeatureDispatchFailure(id, operation, ex, out GameBridgeFeatureFailurePublication publication);
                string details = FormatGameBridgeFeatureStatus(status);
                PublishGameBridgeFeatureStatusIfNeeded(
                    status,
                    "failed",
                    operation,
                    operation + " failed: " + FormatGameBridgeExceptionSummary(ex) + ". " + details,
                    publication.ShouldPublishHookStatus);
                return false;
            }
        }

        private void PublishGameBridgeFeatureContractDiagnostics(string operation)
        {
            if (!runtime.RefactorOptions.GameBridgeFeatureContracts)
                return;

            EnsureGameBridgeFeatures();
            List<string> diagnostics = new List<string>();
            foreach (IGameBridgeFeature feature in features)
            {
                string id = GetGameBridgeFeatureId(feature);
                string warning = feature.Contract.Validate(id);
                if (!string.IsNullOrWhiteSpace(warning))
                    diagnostics.Add(id + ":" + warning);
            }

            string summary = FormatGameBridgeFeatureContractSummary();
            bool success = diagnostics.Count == 0;
            string details = summary + (success ? string.Empty : "; warnings=" + string.Join("|", diagnostics.Take(8).ToArray()));
            runtime.Diagnostics.SetFeatureStatus(
                "Refactor.GameBridgeFeatureContracts",
                success ? "ok" : "warning",
                operation ?? string.Empty,
                success,
                diagnostics.Count,
                success ? string.Empty : diagnostics[0],
                details);
            runtime.SetHookStatus(
                "Refactor.GameBridgeFeatureContracts",
                success ? "ok" : "warning",
                "DTMAPI.GameBridge.DolocTown feature contracts",
                details);

            foreach (string diagnostic in diagnostics.Take(8))
                runtime.Diagnostics.RecordWarning("DTMAPI.GameBridge.FeatureContract", "GameBridge feature contract diagnostic.", diagnostic);
        }

        private void PublishGameBridgeFinalHealthSnapshot(string reason)
        {
            if (!runtime.RefactorOptions.GameBridgeFinalHealthSnapshot)
                return;

            lastGameBridgeFinalHealthSnapshotAtUtc = DateTimeOffset.UtcNow;
            latestGameBridgeFinalHealthSummary = BuildGameBridgeFinalHealthSummary(reason ?? string.Empty);
            bool success = latestGameBridgeFinalHealthSummary.IndexOf("needsRestart=0", StringComparison.OrdinalIgnoreCase) >= 0 &&
                latestGameBridgeFinalHealthSummary.IndexOf("cleanupFailures=0", StringComparison.OrdinalIgnoreCase) >= 0;
            runtime.Diagnostics.SetFeatureStatus(
                "Refactor.GameBridgeFinalHealthSnapshot",
                success ? "ok" : "warning",
                reason ?? string.Empty,
                success,
                success ? 0 : 1,
                success ? string.Empty : "Final health snapshot reported cleanup or restart warnings.",
                latestGameBridgeFinalHealthSummary);
            runtime.SetHookStatus(
                "Refactor.GameBridgeFinalHealthSnapshot",
                success ? "ok" : "warning",
                "DTMAPI.GameBridge.DolocTown final health snapshot",
                latestGameBridgeFinalHealthSummary);
            runtime.RuntimeMonitor.Log("GameBridge final health snapshot reason=" + (reason ?? string.Empty) + " " + latestGameBridgeFinalHealthSummary + ".");
        }

        private string BuildGameBridgeRuntimeReportContext()
        {
            var builder = new StringBuilder();
            builder.AppendLine("GameBridgeFeatureContracts: " + (runtime.RefactorOptions.GameBridgeFeatureContracts ? FormatGameBridgeFeatureContractSummary() : "disabled"));
            builder.AppendLine("GameBridgeDemandRouting: " + FormatGameBridgeDemandRoutingSummary());
            builder.AppendLine("GameBridgeFeatureFanout: " + FormatGameBridgeFeatureFanoutSummary());
            builder.AppendLine("GameBridgeFinalHealthSnapshot: " + (runtime.RefactorOptions.GameBridgeFinalHealthSnapshot ? latestGameBridgeFinalHealthSummary : "disabled"));
            builder.AppendLine("GameBridgeFinalHealthSnapshotAt: " + (lastGameBridgeFinalHealthSnapshotAtUtc == DateTimeOffset.MinValue ? "not-run" : lastGameBridgeFinalHealthSnapshotAtUtc.ToString("O", CultureInfo.InvariantCulture)));
            return builder.ToString();
        }

        private TitleReturnObjectGraphSection BuildGameBridgeTitleReturnObjectGraphSection()
        {
            return new TitleReturnObjectGraphSection(
                "GameBridge",
                "bridgeLifecycle={features=" + features.Count.ToString(CultureInfo.InvariantCulture) +
                "; featureStatuses=" + featureStatuses.Count.ToString(CultureInfo.InvariantCulture) +
                "; featureFailures=" + featureFailures.Count.ToString(CultureInfo.InvariantCulture) +
                "; featureRuntimeStates=" + featureRuntimeStates.Count.ToString(CultureInfo.InvariantCulture) +
                "; hookRetryTimerAlive=" + (hookRetryTimer == null ? "false" : "true") +
                "; assemblyLoadSubscribed=" + assemblyLoadSubscribed.ToString(CultureInfo.InvariantCulture) +
                "; saveLoadedEventSubscribed=" + saveLoadedEventSubscribed.ToString(CultureInfo.InvariantCulture) +
                "; saveLoadedDelegateAlive=" + (saveLoadedUnityEventDelegate == null ? "false" : "true") +
                "; patcherAlive=" + (patcher == null ? "false" : "true") +
                "; pendingHookInstalls=" + hookInstallScheduler.GetSnapshot(false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, legacyAllReady: false, assemblyLoadSubscribed: assemblyLoadSubscribed, retryTimerAlive: hookRetryTimer != null).Pending.ToString(CultureInfo.InvariantCulture) +
                "; pendingWorkshopUploadPlanResolutions=" + pendingWorkshopUploadPlanResolutions.Count.ToString(CultureInfo.InvariantCulture) +
                "; shutdownCleanupRan=" + shutdownCleanupRan.ToString(CultureInfo.InvariantCulture) + "}" +
                "; featureById={" + FormatGameBridgeFeatureObjectGraphSummary() + "}" +
                "; saveSlots={" + (saveSlotsFeature?.Service.GetOfficialSaveUiLifecycleSummary() ?? "saveUiStates=0, saveUiPagers=0, saveUiBinders=0") + "}" +
                "; equipmentSlots={" + (equipmentSlotsFeature?.Service.GetLifecycleSummary() ?? "equipmentClones=0, equipmentBinders=0, equipmentRendered=false, equipmentStorageOwners=0, equipmentDirtyOwners=0, equipmentEntries=0") + "}" +
                "; animalViewer={" + (animalViewerFeature?.Service.GetAnimalViewerLifecycleSummary() ?? "nativeData=0, overlayObjects=0, overlayRows=0") + "}" +
                "; customAnimals={" + (customAnimalAnimatorBridgeFeature?.Service.GetCustomAnimalAnimatorLifecycleSummary() ?? "registrations=0, controllerCache=0, bundleCache=0") + "}" +
                "; audioReplacement={" + (audioReplacementFeature?.Service.GetAudioReplacementLifecycleSummary() ?? "entries=0, audioClips=0, pendingRequests=0") + "}" +
                "; fishingCompatibility={" + (fishingCompatibilityFeature?.GetCompatibilityLifecycleSummary() ?? "compatibilityStatus=inactive/no-consumer, fishingStates=0, fishingOptions=0") + "}");
        }

        private TitleReturnObjectGraphSection BuildGameBridgeUiOwnerObjectGraphSection()
        {
            return new TitleReturnObjectGraphSection(
                "UI",
                "byOwner={" +
                (saveSlotsFeature?.Service.GetOfficialSaveUiOwnerObjectGraphSummary() ?? "DTMAPI.SaveSlots={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}") +
                "; " +
                (equipmentSlotsFeature?.Service.GetUiOwnerObjectGraphSummary() ?? "DTMAPI.EquipmentSlots={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}") +
                "; " +
                (debugConsoleApi is DebugConsoleCompatibilityProxy debugConsole
                    ? debugConsole.GetOwnerObjectGraphSummary()
                    : "DTMAPI.DebugConsole={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}") +
                "}");
        }

        internal string BuildGameBridgeFinalHealthSummaryForTests(string reason)
        {
            return BuildGameBridgeFinalHealthSummary(reason ?? string.Empty);
        }

        private string BuildGameBridgeFinalHealthSummary(string reason)
        {
            int failed = featureRuntimeStates.Values.Count(s => s.FailureCount > 0) +
                featureStatuses.Values.Count(s => !s.LastSucceeded && s.FailureCount > 0);
            int active = Math.Max(0, features.Count - failed);
            string nativeUiSummary =
                "saveSlots={" + (saveSlotsFeature?.Service.GetOfficialSaveUiLifecycleSummary() ?? "saveUiStates=0, saveUiPagers=0, saveUiBinders=0") + "}" +
                "; runtimeAutomation={" + (experimentalApi?.GetRuntimeAutomationLifecycleSummary() ?? "actionAnimators=0, actionAutoFillApplications=0, actionPendingAnimalInteract=false") + "}" +
                "; animalViewer={" + (animalViewerFeature?.Service.GetAnimalViewerLifecycleSummary() ?? "nativeData=0, overlayObjects=0, overlayRows=0") + "}" +
                "; customAnimals={" + (customAnimalAnimatorBridgeFeature?.Service.GetCustomAnimalAnimatorLifecycleSummary() ?? "registrations=0, controllerCache=0, bundleCache=0") + "}" +
                "; audioReplacement={" + (audioReplacementFeature?.Service.GetAudioReplacementLifecycleSummary() ?? "entries=0, audioClips=0, pendingRequests=0") + "}" +
                "; fishingCompatibility={" + (fishingCompatibilityFeature?.GetCompatibilityLifecycleSummary() ?? "compatibilityStatus=inactive/no-consumer, fishingStates=0, fishingOptions=0") + "}" +
                "; actionSpeed={" + (actionSpeedFeature?.Service.GetActionSpeedLifecycleSummary() ?? "actionAnimators=0, actionAutoFillApplications=0, actionPendingAnimalInteract=false") + "}";
            string ownerSummary =
                "input={" + runtime.Input.GetOwnerSnapshot().FormatSummary() + "}" +
                "; events={" + runtime.Events.GetHandlerCleanupSnapshot().FormatSummary() + "}" +
                "; owners={" + runtime.ModOwnerLedgerSnapshot.FormatSummary() + "}";
            string disposeGraph = BuildGameBridgeDisposeGraphSummary();

            return "reason=" + SingleLine(reason) +
                "; featureCount=" + features.Count.ToString(CultureInfo.InvariantCulture) +
                "; activeFeatures=" + active.ToString(CultureInfo.InvariantCulture) +
                "; failedFeatures=" + failed.ToString(CultureInfo.InvariantCulture) +
                "; autoDisabledFeatures=0" +
                "; demandRouting={" + FormatGameBridgeDemandRoutingSummary() + "}" +
                "; featureFanout={" + FormatGameBridgeFeatureFanoutSummary() + "}" +
                "; environmentResetCount=" + environmentResetCount.ToString(CultureInfo.InvariantCulture) +
                "; nativeUi={" + nativeUiSummary + "}" +
                "; ownerCounters={" + ownerSummary + "}" +
                "; resourceLedger={" + runtime.ResourceLifecycleSnapshot.FormatSummary() + "}" +
                "; saveLoad={" + runtime.SaveLoadRequestSnapshot.FormatSummary() + "}" +
                "; disposeGraph={" + disposeGraph + "}";
        }

        private string BuildGameBridgeDisposeGraphSummary()
        {
            int cleanupFailures = runtime.ModOwnerLedgerSnapshot.CleanupFailures;
            int needsRestart = runtime.ModOwnerLedgerSnapshot.NeedsRestart;
            int resourceErrors = runtime.ResourceLifecycleSnapshot.ErrorCount;
            return "cleanupFailures=" + cleanupFailures.ToString(CultureInfo.InvariantCulture) +
                "; needsRestart=" + needsRestart.ToString(CultureInfo.InvariantCulture) +
                "; resourceErrors=" + resourceErrors.ToString(CultureInfo.InvariantCulture) +
                "; nativeOwnedRelease=not-attempted" +
                "; externalOwnerRelease=needs-restart-if-present";
        }

        private string FormatGameBridgeFeatureContractSummary()
        {
            EnsureGameBridgeFeatures();
            return "contracts=" + features.Count.ToString(CultureInfo.InvariantCulture) +
                "; " + string.Join("; ", features.Select(feature => feature.Contract.Format()).ToArray());
        }

        private string FormatGameBridgeFeatureFanoutSummary()
        {
            EnsureGameBridgeFeatures();
            foreach (IGameBridgeFeature feature in features)
                GetGameBridgeFeatureRuntimeState(feature);
            return string.Join("; ", featureRuntimeStates.Values
                .OrderBy(state => state.Id, StringComparer.OrdinalIgnoreCase)
                .Select(state => state.FormatDispatchSummary())
                .ToArray());
        }

        private string FormatGameBridgeFeatureObjectGraphSummary()
        {
            EnsureGameBridgeFeatures();
            return string.Join("; ", features
                .OrderBy(feature => GetGameBridgeFeatureId(feature), StringComparer.OrdinalIgnoreCase)
                .Take(32)
                .Select(feature =>
                {
                    string id = GetGameBridgeFeatureId(feature);
                    GameBridgeFeatureRuntimeState runtimeState = GetGameBridgeFeatureRuntimeState(feature);
                    bool hasStatus = featureStatuses.TryGetValue(id, out GameBridgeFeatureStatus status);
                    return SanitizeMetricKey(id) +
                        "={registered=1" +
                        "; runtimeState=1" +
                        "; status=" + (hasStatus ? "1" : "0") +
                        "; failureCount=" + (hasStatus ? status!.FailureCount.ToString(CultureInfo.InvariantCulture) : "0") +
                        "; fanoutDispatch=" + runtimeState.FanoutDispatchCount.ToString(CultureInfo.InvariantCulture) +
                        "}";
                })
                .ToArray());
        }

        private GameBridgeFeatureRuntimeState GetGameBridgeFeatureRuntimeState(IGameBridgeFeature feature)
        {
            return GetGameBridgeFeatureRuntimeState(GetGameBridgeFeatureId(feature));
        }

        private GameBridgeFeatureRuntimeState GetGameBridgeFeatureRuntimeState(string id)
        {
            string normalizedId = string.IsNullOrWhiteSpace(id) ? "<unknown>" : id.Trim();
            if (!featureRuntimeStates.TryGetValue(normalizedId, out GameBridgeFeatureRuntimeState state))
            {
                state = new GameBridgeFeatureRuntimeState(normalizedId);
                featureRuntimeStates[normalizedId] = state;
            }

            return state;
        }

        private void PublishGameBridgeFeatureStatusIfNeeded(GameBridgeFeatureStatus status, string hookStatus, string operation, string details)
        {
            PublishGameBridgeFeatureStatusIfNeeded(status, hookStatus, operation, details, forceHookStatusPublication: false);
        }

        private void PublishGameBridgeFeatureStatusIfNeeded(GameBridgeFeatureStatus status, string hookStatus, string operation, string details, bool forceHookStatusPublication)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            runtime.Diagnostics.SetFeatureStatus(status.Id, hookStatus, status.LastOperation, status.LastSucceeded, status.FailureCount, status.LastError, FormatGameBridgeFeatureStatus(status));
            if (!ShouldPublishGameBridgeFeatureStatus(status, hookStatus, operation, now, forceHookStatusPublication))
                return;

            runtime.SetHookStatus(
                "Feature." + status.Id,
                hookStatus,
                "DTMAPI.GameBridge.DolocTown feature host",
                details);
            status.MarkPublished(hookStatus, now);
        }

        private static bool ShouldPublishGameBridgeFeatureStatus(GameBridgeFeatureStatus status, string hookStatus, string operation, DateTimeOffset now, bool forceHookStatusPublication)
        {
            if (!status.HasPublished)
                return true;

            if (forceHookStatusPublication)
                return true;

            if (!string.Equals(status.PublishedHookStatus, hookStatus, StringComparison.OrdinalIgnoreCase))
                return true;

            if (status.PublishedSucceeded != status.LastSucceeded)
                return true;

            if (!IsHighFrequencyGameBridgeOperation(operation))
                return true;

            return now - status.LastPublishedAt >= FeatureStatusPublishHeartbeat;
        }

        private static bool IsHighFrequencyGameBridgeOperation(string operation)
        {
            return string.Equals(operation, "Update", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(operation, "EnvironmentReset", StringComparison.OrdinalIgnoreCase);
        }

        private GameBridgeFeatureStatus RecordGameBridgeFeatureSuccess(string id, string operation)
        {
            GameBridgeFeatureStatus status = GetGameBridgeFeatureStatus(id);
            bool recovered = status.ConsecutiveFailureCount > 0 || (!status.LastSucceeded && status.FailureCount > 0);
            status.LastOperation = operation;
            status.LastSucceeded = true;
            status.ConsecutiveFailureCount = 0;
            status.LastError = string.Empty;
            if (recovered)
                status.LastRecoveredAtUtc = DateTimeOffset.UtcNow;
            RecordGameBridgeFeatureRecoverySuccess(id, operation);
            return status;
        }

        private GameBridgeFeatureStatus RecordGameBridgeFeatureFailure(string id, string operation, Exception ex)
        {
            GameBridgeFeatureStatus status = GetGameBridgeFeatureStatus(id);
            status.LastOperation = operation;
            status.LastSucceeded = false;
            status.FailureCount++;
            status.ConsecutiveFailureCount++;
            status.LastError = FormatGameBridgeExceptionSummary(ex);
            return status;
        }

        private GameBridgeFeatureStatus RecordGameBridgeFeatureDispatchFailure(string id, string operation, Exception ex, out GameBridgeFeatureFailurePublication publication)
        {
            GameBridgeFeatureStatus status = RecordGameBridgeFeatureFailure(id, operation, ex);
            publication = RecordGameBridgeFeatureFailurePublication(id, operation, ex);
            string message = "GameBridge feature '" + id + "' failed during " + operation + ".";
            string errorSummary = FormatGameBridgeExceptionSummary(ex);

            if (publication.RecordDiagnosticsError)
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.Feature." + id, message, FormatGameBridgeExceptionDetails(ex));

            if (publication.LogMode == GameBridgeFeatureFailureLogMode.Full)
                runtime.RuntimeMonitor.Log(message + " " + errorSummary, LogLevel.Error);
            else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Short)
                runtime.RuntimeMonitor.Log("Repeated GameBridge feature failure feature=" + id + " operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " error=" + errorSummary, LogLevel.Warn);
            else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Summary)
                runtime.RuntimeMonitor.Log("Throttled GameBridge feature failures feature=" + id + " operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " lastError=" + errorSummary, LogLevel.Warn);

            return status;
        }

        private GameBridgeFeatureFailurePublication RecordGameBridgeFeatureFailurePublication(string id, string operation, Exception ex)
        {
            string key = GetGameBridgeFeatureFailureKey(id, operation);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!featureFailures.TryGetValue(key, out GameBridgeFeatureFailureState state))
            {
                state = new GameBridgeFeatureFailureState();
                featureFailures[key] = state;
            }

            string fingerprint = FormatGameBridgeExceptionFingerprint(ex);
            bool isNewFingerprint = !string.Equals(state.LastFingerprint, fingerprint, StringComparison.Ordinal);

            state.Count++;
            state.ConsecutiveSuccessCount = 0;
            state.LastError = FormatGameBridgeExceptionSummary(ex);
            state.LastFingerprint = fingerprint;
            state.LastSeenAtUtc = now;

            if (state.Count == 1 || isNewFingerprint)
            {
                state.LastPublishedAtUtc = now;
                return new GameBridgeFeatureFailurePublication(true, GameBridgeFeatureFailureLogMode.Full, state.Count);
            }

            if (state.Count <= FeatureFailureShortLogLimit)
            {
                state.LastPublishedAtUtc = now;
                return new GameBridgeFeatureFailurePublication(false, GameBridgeFeatureFailureLogMode.Short, state.Count);
            }

            if (now - state.LastPublishedAtUtc >= FeatureFailureSummaryInterval)
            {
                state.LastPublishedAtUtc = now;
                return new GameBridgeFeatureFailurePublication(false, GameBridgeFeatureFailureLogMode.Summary, state.Count);
            }

            return new GameBridgeFeatureFailurePublication(false, GameBridgeFeatureFailureLogMode.None, state.Count);
        }

        private bool RecordGameBridgeFeatureRecoverySuccess(string id, string operation)
        {
            string key = GetGameBridgeFeatureFailureKey(id, operation);
            if (!featureFailures.TryGetValue(key, out GameBridgeFeatureFailureState state))
                return false;

            state.ConsecutiveSuccessCount++;
            if (state.ConsecutiveSuccessCount >= FeatureFailureRecoverySuccessThreshold)
            {
                featureFailures.Remove(key);
                return true;
            }

            return false;
        }

        private static string GetGameBridgeFeatureFailureKey(string id, string operation)
        {
            return (string.IsNullOrWhiteSpace(id) ? "<unknown>" : id.Trim()) + "::" + (string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation.Trim());
        }

        private static string FormatGameBridgeExceptionSummary(Exception ex)
        {
            Exception root = UnwrapGameBridgeException(ex);
            string rootSummary = root.GetType().Name + ": " + root.Message;
            return ReferenceEquals(root, ex)
                ? rootSummary
                : rootSummary + " (outer " + ex.GetType().Name + ": " + ex.Message + ")";
        }

        private static string FormatGameBridgeExceptionFingerprint(Exception ex)
        {
            Exception root = UnwrapGameBridgeException(ex);
            string stackHead = string.Empty;
            string? stack = root.StackTrace;
            if (!string.IsNullOrWhiteSpace(stack))
            {
                string[] lines = stack.Replace("\r\n", "\n").Split('\n');
                stackHead = lines.Length == 0 ? string.Empty : lines[0].Trim();
            }

            return root.GetType().FullName + "|" + root.Message + "|" + stackHead;
        }

        private static string FormatGameBridgeExceptionDetails(Exception ex)
        {
            Exception root = UnwrapGameBridgeException(ex);
            if (ReferenceEquals(root, ex))
                return ex.ToString();

            return "RootCause: " + root.GetType().FullName + ": " + root.Message + Environment.NewLine +
                root + Environment.NewLine +
                "OuterException: " + ex.GetType().FullName + ": " + ex.Message + Environment.NewLine +
                ex;
        }

        private static Exception UnwrapGameBridgeException(Exception ex)
        {
            while (ex is TargetInvocationException target && target.InnerException != null)
                ex = target.InnerException;
            return ex;
        }

        private GameBridgeFeatureStatus GetGameBridgeFeatureStatus(string id)
        {
            if (!featureStatuses.TryGetValue(id, out GameBridgeFeatureStatus status))
            {
                status = new GameBridgeFeatureStatus(id);
                featureStatuses[id] = status;
            }

            return status;
        }

        private sealed class GameBridgeFeatureRuntimeState
        {
            internal GameBridgeFeatureRuntimeState(string id)
            {
                Id = id;
            }

            internal string Id { get; }

            internal long FanoutDispatchCount { get; private set; }

            internal int FailureCount { get; private set; }

            internal string LastOperation { get; private set; } = string.Empty;

            internal DateTimeOffset? LastFailureAtUtc { get; private set; }

            internal void RecordDispatch(string operation)
            {
                LastOperation = operation ?? string.Empty;
                FanoutDispatchCount++;
            }

            internal void RecordFailure(string operation, DateTimeOffset now)
            {
                FailureCount++;
                LastOperation = operation ?? string.Empty;
                LastFailureAtUtc = now;
            }

            internal string FormatDispatchSummary()
            {
                return Id +
                    ":fanoutDispatch=" + FanoutDispatchCount.ToString(CultureInfo.InvariantCulture) +
                    ",failures=" + FailureCount.ToString(CultureInfo.InvariantCulture) +
                    ",lastOperation=" + SingleLine(LastOperation);
            }
        }

        private static string FormatGameBridgeFeatureStatus(GameBridgeFeatureStatus status)
        {
            string lastError = string.IsNullOrWhiteSpace(status.LastError) ? "none" : status.LastError;
            string recoveredAt = status.LastRecoveredAtUtc.HasValue ? status.LastRecoveredAtUtc.Value.ToString("O", CultureInfo.InvariantCulture) : "none";
            return "Feature status: id=" + status.Id + ", lastOperation=" + status.LastOperation + ", success=" + status.LastSucceeded.ToString(CultureInfo.InvariantCulture) + ", failureCount=" + status.FailureCount.ToString(CultureInfo.InvariantCulture) + ", consecutiveFailureCount=" + status.ConsecutiveFailureCount.ToString(CultureInfo.InvariantCulture) + ", lastRecoveredAt=" + recoveredAt + ", lastError=" + lastError + ".";
        }

        private static string GetGameBridgeFeatureId(IGameBridgeFeature feature)
        {
            string id = feature.Id;
            if (!string.IsNullOrWhiteSpace(id))
                return id.Trim();

            return feature.GetType().Name;
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string SanitizeMetricKey(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            var builder = new System.Text.StringBuilder(text.Length);
            bool previousUnderscore = false;
            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                {
                    builder.Append(ch);
                    previousUnderscore = false;
                    continue;
                }

                if (!previousUnderscore)
                {
                    builder.Append('_');
                    previousUnderscore = true;
                }
            }

            return builder.ToString().Trim('_');
        }

        private sealed class GameBridgeFeatureStatus
        {
            internal GameBridgeFeatureStatus(string id)
            {
                Id = id;
            }

            internal string Id { get; }

            internal string LastOperation { get; set; } = string.Empty;

            internal bool LastSucceeded { get; set; }

            internal int FailureCount { get; set; }

            internal int ConsecutiveFailureCount { get; set; }

            internal string LastError { get; set; } = string.Empty;

            internal DateTimeOffset? LastRecoveredAtUtc { get; set; }

            internal bool HasPublished { get; private set; }

            internal string PublishedHookStatus { get; private set; } = string.Empty;

            internal bool PublishedSucceeded { get; private set; }

            internal DateTimeOffset LastPublishedAt { get; private set; } = DateTimeOffset.MinValue;

            internal void MarkPublished(string hookStatus, DateTimeOffset publishedAt)
            {
                HasPublished = true;
                PublishedHookStatus = hookStatus;
                PublishedSucceeded = LastSucceeded;
                LastPublishedAt = publishedAt;
            }
        }

        private sealed class GameBridgeFeatureFailureState
        {
            internal int Count { get; set; }
            internal int ConsecutiveSuccessCount { get; set; }
            internal string LastError { get; set; } = string.Empty;
            internal string LastFingerprint { get; set; } = string.Empty;
            internal DateTimeOffset LastSeenAtUtc { get; set; }
            internal DateTimeOffset LastPublishedAtUtc { get; set; }
        }

        private readonly struct GameBridgeFeatureFailurePublication
        {
            internal GameBridgeFeatureFailurePublication(bool recordDiagnosticsError, GameBridgeFeatureFailureLogMode logMode, int count)
            {
                RecordDiagnosticsError = recordDiagnosticsError;
                LogMode = logMode;
                Count = count;
            }

            internal bool RecordDiagnosticsError { get; }

            internal GameBridgeFeatureFailureLogMode LogMode { get; }

            internal int Count { get; }

            internal bool ShouldPublishHookStatus => LogMode != GameBridgeFeatureFailureLogMode.None;
        }

        private enum GameBridgeFeatureFailureLogMode
        {
            None,
            Full,
            Short,
            Summary
        }
    }
}
