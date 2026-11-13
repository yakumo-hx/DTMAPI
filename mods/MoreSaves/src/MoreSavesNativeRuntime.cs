using System;
using System.Globalization;
using System.Reflection;
using global::DTMAPI.Abstractions;

namespace DTMAPI.MoreSaves
{
    internal sealed class MoreSavesNativeRuntime
    {
        internal const int VanillaSlotCount = 6;
        internal const int ExpandedSlotCount = 60;
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(750);
        private readonly IMonitor monitor;
        private readonly Func<object?> managerProvider;
        private readonly Func<object, int> reader;
        private readonly Func<object, int, bool> writer;
        private readonly Func<DateTimeOffset> clock;
        private readonly Action<bool>? retryDemandChanged;
        private readonly Func<object, MoreSavesArchiveMigrationResult> archiveMigration;
        private MoreSavesConfig config = new MoreSavesConfig();
        private bool active;
        private DateTimeOffset retryAfterUtc;
        private static MoreSavesNativeRuntime? current;
        private bool applyPending;
        private bool restorePending;
        private bool retrySchedulingAllowed = true;
        private bool retryDemandPublished;
        private bool archiveMigrationComplete;
        private int nativeSlotCount;
        private string archiveMigrationSummary = "not-run";
        private string status = "not-configured";
        private string lastMessage = "MoreSaves has not configured the native archive count.";

        internal MoreSavesNativeRuntime(
            IMonitor monitor,
            Func<object?>? managerProvider = null,
            Func<object, int>? reader = null,
            Func<object, int, bool>? writer = null,
            Func<DateTimeOffset>? clock = null,
            Action<bool>? retryDemandChanged = null,
            Func<object, MoreSavesArchiveMigrationResult>? archiveMigration = null)
        {
            this.monitor = monitor;
            this.managerProvider = managerProvider ?? ResolveManager;
            this.reader = reader ?? ReadArchiveCount;
            this.writer = writer ?? WriteArchiveCount;
            this.clock = clock ?? (() => DateTimeOffset.UtcNow);
            this.retryDemandChanged = retryDemandChanged;
            this.archiveMigration = archiveMigration ?? MoreSavesArchiveMigration.ExecuteNative;
        }

        internal bool OwnsCapacity => ReferenceEquals(current, this);
        internal bool RetryPending => applyPending || restorePending;
        internal bool RetrySchedulingRequested => retrySchedulingAllowed && RetryPending;
        internal int NativeSlotCount => nativeSlotCount;
        internal string StatusSummary =>
            "state=" + status +
             ", native=" + nativeSlotCount.ToString(CultureInfo.InvariantCulture) +
             ", target=" + (active ? ExpandedSlotCount : VanillaSlotCount).ToString(CultureInfo.InvariantCulture) +
             ", ownsCapacity=" + OwnsCapacity.ToString().ToLowerInvariant() +
             ", pendingWork=" + RetryPending.ToString().ToLowerInvariant() +
             ", retryScheduled=" + RetrySchedulingRequested.ToString().ToLowerInvariant() +
             ", migration=" + archiveMigrationSummary +
             ". " + lastMessage;

        internal void Configure(MoreSavesConfig value, string reason)
        {
            retrySchedulingAllowed = true;
            try
            {
                config = value.Copy();
                active = true;
                ApplyExpandedOrFail(reason);
            }
            finally
            {
                ReconcileRetryDemand();
            }
        }

        internal void Update()
        {
            if (!retrySchedulingAllowed || !RetryPending || clock() < retryAfterUtc)
                return;
            try
            {
                if (active)
                    ApplyExpandedOrFail("bounded native-manager retry");
                else
                    RestoreAndRelease("bounded native-manager restore retry");
            }
            finally
            {
                ReconcileRetryDemand();
            }
        }

        internal void ResetBoundary(string reason)
        {
            if (!active)
                return;
            try
            {
                retryAfterUtc = DateTimeOffset.MinValue;
                ApplyExpandedOrFail(reason);
            }
            finally
            {
                ReconcileRetryDemand();
            }
        }

        internal void DeactivateOwner(string reason)
        {
            active = false;
            applyPending = false;
            retrySchedulingAllowed = false;
            try
            {
                RestoreAndRelease(reason);
            }
            finally
            {
                ReconcileRetryDemand();
            }
            if (OwnsCapacity || restorePending)
                throw new InvalidOperationException("MoreSaves retained exact archive-count ownership because native six-slot restoration did not complete; Loader deactivation removes event delivery, so cleanup must wait for restart or a later explicit cleanup pass. " + lastMessage);
        }

        private void ReconcileRetryDemand()
        {
            bool requested = RetrySchedulingRequested;
            if (requested == retryDemandPublished)
                return;
            retryDemandChanged?.Invoke(requested);
            retryDemandPublished = requested;
        }

        private void AcquireCapacity()
        {
            if (current != null && !ReferenceEquals(current, this))
                throw new InvalidOperationException("MoreSaves capacity is already managed by another active instance.");
            current = this;
        }

        private void ApplyExpandedOrFail(string reason)
        {
            try
            {
                AcquireCapacity();
                restorePending = false;
                TryApply(ExpandedSlotCount, reason);
            }
            catch (Exception activationFailure)
            {
                applyPending = false;
                active = false;
                try
                {
                    RestoreAndRelease("activation-failed:" + reason);
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(
                        "MoreSaves activation failed and native-six owner cleanup also failed.",
                        activationFailure,
                        cleanupFailure);
                }
                throw;
            }
        }

        private bool TryApply(int target, string reason)
        {
            object? manager;
            try
            {
                manager = managerProvider();
            }
            catch (Exception ex)
            {
                return MarkPending(target, reason, "native-manager-read-failed", ex);
            }
            if (manager == null)
                return MarkPending(target, reason, "missing-game-manager", null);

            if (target == ExpandedSlotCount && !archiveMigrationComplete)
            {
                MoreSavesArchiveMigrationResult migrationResult;
                try
                {
                    migrationResult = archiveMigration(manager);
                }
                catch (Exception ex)
                {
                    status = "legacy-archive-migration-failed";
                    archiveMigrationSummary = "failed";
                    lastMessage = ex.GetType().Name + ": " + ex.Message;
                    Log(reason, LogLevel.Error);
                    throw new InvalidOperationException(
                        "MoreSaves refused to expose expanded slots because the Doloc Town 1.00 legacy archive migration did not complete.",
                        ex);
                }
                archiveMigrationComplete = true;
                archiveMigrationSummary = migrationResult.Summary;
                if (migrationResult.PreservedDestinationCount > 0)
                {
                    monitor.Log(
                        "MoreSaves preserved existing 1.00 archive destinations and did not overwrite or delete their legacy sources. " +
                        migrationResult.Summary,
                        LogLevel.Info);
                }
                else if (!migrationResult.IsNoOp || config.VerboseLogging)
                    monitor.Log("MoreSaves completed the bounded Doloc Town 1.00 legacy archive migration. " + migrationResult.Summary, LogLevel.Info);
            }

            try
            {
                int previous = reader(manager);
                bool wrote = writer(manager, target);
                int actual = reader(manager);
                nativeSlotCount = actual;
                bool success = wrote && actual == target;
                applyPending = target == ExpandedSlotCount && !success;
                restorePending = target == VanillaSlotCount && !success;
                status = success ? "configured-official-archive-count" : "archive-count-set-failed";
                lastMessage = success
                    ? "ProductNative set GameManager.archiveFileCount " + previous.ToString(CultureInfo.InvariantCulture) + "->" + actual.ToString(CultureInfo.InvariantCulture) + "; native files and UI remain official owners."
                    : "ProductNative could not set GameManager.archiveFileCount to " + target.ToString(CultureInfo.InvariantCulture) + "; native=" + actual.ToString(CultureInfo.InvariantCulture) + ".";
                if (!success)
                    retryAfterUtc = clock() + RetryInterval;
                Log(reason, success ? LogLevel.Info : LogLevel.Warn);
                return success;
            }
            catch (Exception ex)
            {
                return MarkPending(target, reason, "native-archive-count-failed", ex);
            }
        }

        private void RestoreAndRelease(string reason)
        {
            if (!OwnsCapacity)
            {
                restorePending = false;
                status = "disabled-no-native-owner";
                lastMessage = "MoreSaves is disabled and retains no native archive-count owner.";
                return;
            }

            if (TryApply(VanillaSlotCount, reason))
            {
                current = null;
                restorePending = false;
                status = "restored-native-six";
                lastMessage = "ProductNative restored GameManager.archiveFileCount to six and released exact ownership.";
                Log(reason, LogLevel.Info);
            }
        }

        private bool MarkPending(int target, string reason, string failure, Exception? exception)
        {
            applyPending = target == ExpandedSlotCount;
            restorePending = target == VanillaSlotCount;
            retryAfterUtc = clock() + RetryInterval;
            status = "pending-native-game-manager";
            lastMessage = failure + ": " + (exception == null
                ? "DolocAPI.gameManager is unavailable."
                : exception.GetType().Name + ": " + exception.Message);
            Log(reason, LogLevel.Warn);
            return false;
        }

        private void Log(string reason, LogLevel level)
        {
            if (level >= LogLevel.Warn || config.VerboseLogging || status == "configured-official-archive-count" || status == "restored-native-six")
                monitor.Log("MoreSaves ProductNative " + StatusSummary + " reason=" + reason + ".", level);
        }

        private static object? ResolveManager()
        {
            Type dolocApi = typeof(global::DolocAPI);
            return dolocApi.GetField("gameManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) ??
                dolocApi.GetProperty("gameManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        }

        private static int ReadArchiveCount(object manager)
        {
            Type type = manager.GetType();
            object? value = type.GetField("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manager) ??
                type.GetProperty("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manager);
            return value == null ? VanillaSlotCount : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static bool WriteArchiveCount(object manager, int count)
        {
            Type type = manager.GetType();
            FieldInfo? field = type.GetField("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(manager, Convert.ChangeType(count, field.FieldType, CultureInfo.InvariantCulture));
                return true;
            }
            PropertyInfo? property = type.GetProperty("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property?.CanWrite == true)
            {
                property.SetValue(manager, Convert.ChangeType(count, property.PropertyType, CultureInfo.InvariantCulture));
                return true;
            }
            return false;
        }
    }
}
