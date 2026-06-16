using System;
using System.Globalization;
using DTMAPI.Abstractions;

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
            DispatchGameBridgeFeatures("PublishHookStatuses", feature => feature.PublishHookStatuses());
        }

        private void InstallGameBridgeFeatureHooks(HarmonyReflectionPatcher patcher)
        {
            agentStateLifecycleHooks?.InstallHooks(patcher);
            toolColliderHitHooks?.InstallHooks(patcher);
            DispatchGameBridgeFeatures("InstallHooks", feature => feature.InstallHooks(patcher));
        }

        private void UpdateGameBridgeFeatures()
        {
            DispatchGameBridgeFeatures("Update", feature => feature.Update());
        }

        private void DispatchGameBridgeFeatures(string operation, Action<IGameBridgeFeature> action)
        {
            foreach (IGameBridgeFeature feature in features)
                DispatchGameBridgeFeature(feature, operation, action);
        }

        private void DispatchGameBridgeFeature(IGameBridgeFeature feature, string operation, Action<IGameBridgeFeature> action)
        {
            string id = GetGameBridgeFeatureId(feature);
            try
            {
                action(feature);
                GameBridgeFeatureStatus status = RecordGameBridgeFeatureSuccess(id, operation);
                string details = FormatGameBridgeFeatureStatus(status);
                PublishGameBridgeFeatureStatusIfNeeded(
                    status,
                    "ready",
                    operation,
                    "Safe feature host dispatch completed " + operation + " for this GameBridge feature. " + details);
            }
            catch (Exception ex)
            {
                GameBridgeFeatureStatus status = RecordGameBridgeFeatureDispatchFailure(id, operation, ex, out GameBridgeFeatureFailurePublication publication);
                string details = FormatGameBridgeFeatureStatus(status);
                PublishGameBridgeFeatureStatusIfNeeded(
                    status,
                    "failed",
                    operation,
                    operation + " failed: " + ex.GetType().Name + ": " + ex.Message + ". " + details,
                    publication.ShouldPublishHookStatus);
            }
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

            if (!string.Equals(operation, "Update", StringComparison.OrdinalIgnoreCase))
                return true;

            return now - status.LastPublishedAt >= FeatureStatusPublishHeartbeat;
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
            status.LastError = ex.GetType().Name + ": " + ex.Message;
            return status;
        }

        private GameBridgeFeatureStatus RecordGameBridgeFeatureDispatchFailure(string id, string operation, Exception ex, out GameBridgeFeatureFailurePublication publication)
        {
            GameBridgeFeatureStatus status = RecordGameBridgeFeatureFailure(id, operation, ex);
            publication = RecordGameBridgeFeatureFailurePublication(id, operation, ex);
            string message = "GameBridge feature '" + id + "' failed during " + operation + ".";

            if (publication.RecordDiagnosticsError)
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.Feature." + id, message, ex.ToString());

            if (publication.LogMode == GameBridgeFeatureFailureLogMode.Full)
                runtime.RuntimeMonitor.Log(message + " " + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
            else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Short)
                runtime.RuntimeMonitor.Log("Repeated GameBridge feature failure feature=" + id + " operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Summary)
                runtime.RuntimeMonitor.Log("Throttled GameBridge feature failures feature=" + id + " operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " lastError=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);

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

            state.Count++;
            state.ConsecutiveSuccessCount = 0;
            state.LastError = ex.GetType().Name + ": " + ex.Message;
            state.LastSeenAtUtc = now;

            if (state.Count == 1)
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

        private void RecordGameBridgeFeatureRecoverySuccess(string id, string operation)
        {
            string key = GetGameBridgeFeatureFailureKey(id, operation);
            if (!featureFailures.TryGetValue(key, out GameBridgeFeatureFailureState state))
                return;

            state.ConsecutiveSuccessCount++;
            if (state.ConsecutiveSuccessCount >= FeatureFailureRecoverySuccessThreshold)
                featureFailures.Remove(key);
        }

        private static string GetGameBridgeFeatureFailureKey(string id, string operation)
        {
            return (string.IsNullOrWhiteSpace(id) ? "<unknown>" : id.Trim()) + "::" + (string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation.Trim());
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
