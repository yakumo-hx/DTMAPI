using System;
using System.Collections.Generic;
using System.Globalization;
using global::DTMAPI.Abstractions;

namespace DTMAPI.ChestLocatorEnhancer
{
    internal sealed class ChestLocatorEnhancerNativeRuntime
    {
        private readonly IMonitor monitor;
        private readonly ChestLocatorEnhancerHookInstaller hooks;
        private bool active;
        private readonly ChestLocatorObservationLogGate
            observationLogGate =
                new ChestLocatorObservationLogGate();
        private int extensionApplications;
        private bool hasTraversalObservation;
        private bool lastUseBox;
        private bool lastNativeAutoUseBox;
        private ChestLocatorTraversalCounts
            lastTraversalObservation;
        private string lastMessage =
            "ProductNative inventory widening has not been configured.";

        internal ChestLocatorEnhancerNativeRuntime(
            IMonitor monitor)
        {
            this.monitor =
                monitor ??
                throw new ArgumentNullException(nameof(monitor));
            hooks =
                new ChestLocatorEnhancerHookInstaller(monitor);
            UiRefresh = new ChestLocatorUiRefresh(monitor);
            Status = "created";
        }

        internal ChestLocatorUiRefresh UiRefresh { get; }

        internal bool Enabled => active;

        internal bool IsHookInstalled =>
            hooks.IsInstalled;

        internal int InstalledPatchCount =>
            hooks.InstalledPatchCount;

        internal int LastAppendedInventoryCount
        {
            get;
            private set;
        }

        internal int LastSharedCaseCount
        {
            get;
            private set;
        }

        internal int LastSharedStorageBoxCount
        {
            get;
            private set;
        }

        internal string Status { get; private set; }

        internal string LastMessage =>
            hasTraversalObservation
                ? FormatTraversalObservation()
                : lastMessage;

        internal string StatusSummary =>
            string.Format(
                CultureInfo.InvariantCulture,
                "status={0}, hook={1}, appended={2}, cases={3}, shelfBoxes={4}. {5}",
                Status,
                IsHookInstalled,
                LastAppendedInventoryCount,
                LastSharedCaseCount,
                LastSharedStorageBoxCount,
                LastMessage);

        internal void Activate()
        {
            try
            {
                hooks.InstallAtomically(this);
                active = true;
                Status = "configured-product-native";
                SetMessage("Officially enabled; shared inventories follow native useBox/autoUseBox.");
            }
            catch (Exception ex)
            {
                Status = hooks.IsInstalled ? "enable-failed-owned" : "enable-failed-closed";
                SetMessage("ProductNative enable failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }
        internal Array ExtendAvailableInventories(
            object archive,
            bool useBox,
            Array nativeResult)
        {
            if (!active)
                return nativeResult;

            try
            {
                bool nativeAutoUseBox = ReadNativeAutoUseBox();
                ChestLocatorTraversalResult result =
                    ChestLocatorInventoryTraversal.AppendSharedInventories(
                        archive,
                        ChestLocatorInventoryTraversal.ReadStaticMember(typeof(DolocAPI), "CurrentRootRoom"),
                        useBox,
                        nativeAutoUseBox,
                        nativeResult);
                extensionApplications++;
                LastAppendedInventoryCount =
                    result.Counts.AppendedInventoryCount;
                LastSharedCaseCount =
                    result.Counts.SharedCaseCount;
                LastSharedStorageBoxCount =
                    result.Counts.SharedStorageBoxCount;
                lastUseBox = useBox;
                lastNativeAutoUseBox =
                    nativeAutoUseBox;
                lastTraversalObservation = result.Counts;
                hasTraversalObservation = true;
                Status =
                    result.Counts.AppendedInventoryCount > 0
                        ? "verified-product-native"
                        : "configured-product-native";
                if (observationLogGate.ShouldLog(
                    useBox,
                    nativeAutoUseBox,
                    result.Counts.AppendedInventoryCount > 0))
                {
                    monitor.Log(
                        "ChestLocatorEnhancer ProductNative inventories " +
                        FormatTraversalObservation());
                }

                return result.Counts.AppendedInventoryCount > 0
                    ? result.Inventories
                    : nativeResult;
            }
            catch (Exception ex)
            {
                Status = "failed-closed";
                SetMessage(
                    "Native inventory widening failed closed: " +
                    ex.GetType().Name +
                    ": " +
                    ex.Message);
                monitor.Log(
                    "ChestLocatorEnhancer " +
                    LastMessage,
                    LogLevel.Error);
                return nativeResult;
            }
        }

        internal void ResetBoundary(string reason)
        {
            UiRefresh.Reset();
            extensionApplications = 0;
            LastAppendedInventoryCount = 0;
            LastSharedCaseCount = 0;
            LastSharedStorageBoxCount = 0;
            observationLogGate.Reset();
            Status = active
                ? "configured-product-native"
                : "disabled";
            SetMessage(
                "ProductNative observation state reset reason=" +
                (reason ?? string.Empty) +
                ".");
        }

        internal void DeactivateOwner(string reason)
        {
            active = false;
            var failures = new List<Exception>();
            TryCleanup(
                () => ResetBoundary(reason),
                failures);
            TryCleanup(
                () => ChestLocatorEnhancerCallbacks.Detach(
                    this),
                failures);
            TryCleanup(
                hooks.UnpatchOwnedHooks,
                failures);
            if (failures.Count == 1)
            {
                Status = hooks.IsInstalled
                    ? "deactivation-failed-owned"
                    : "deactivation-failed-clean";
                SetMessage(
                    "ProductNative owner deactivation failed reason=" +
                    (reason ?? string.Empty) +
                    ": " +
                    failures[0].GetType().Name +
                    ": " +
                    failures[0].Message);
                throw failures[0];
            }
            if (failures.Count > 1)
            {
                Status = hooks.IsInstalled
                    ? "deactivation-failed-owned"
                    : "deactivation-failed-clean";
                SetMessage(
                    "ProductNative owner deactivation encountered " +
                    failures.Count +
                    " cleanup failures reason=" +
                    (reason ?? string.Empty) +
                    ".");
                throw new AggregateException(
                    "ChestLocatorEnhancer state cleanup, callback detach and exact-owner Harmony cleanup failed.",
                    failures);
            }

            Status = "deactivated";
            SetMessage(
                "ProductNative owner deactivated reason=" +
                (reason ?? string.Empty) +
                ".");
        }

        private string FormatTraversalObservation() =>
            "useBox=" +
            lastUseBox +
            ", nativeAutoUseBox=" +
            lastNativeAutoUseBox +
            ", base=" +
            lastTraversalObservation.BaseInventoryCount +
            ", appended=" +
            lastTraversalObservation.AppendedInventoryCount +
            ", roots=" +
            lastTraversalObservation.ScannedRootCount +
            ", equipments=" +
            lastTraversalObservation.ScannedEquipmentCount +
            ", sharedCases=" +
            lastTraversalObservation.SharedCaseCount +
            ", sharedStorageBoxes=" +
            lastTraversalObservation.SharedStorageBoxCount +
            ", applications=" +
            extensionApplications +
            ".";

        private void SetMessage(string message)
        {
            hasTraversalObservation = false;
            lastTraversalObservation = default;
            lastMessage = message ?? string.Empty;
        }

        private static bool ReadNativeAutoUseBox()
        {
            object? settings = ChestLocatorInventoryTraversal.ReadStaticMember(typeof(DolocAPI), "userSettings");
            object? value = ChestLocatorInventoryTraversal.ReadMember(settings, "autoUseBox");
            if (value is bool enabled) return enabled;
            throw new InvalidOperationException("Native autoUseBox setting is unavailable.");
        }
        private static void TryCleanup(
            Action cleanup,
            ICollection<Exception> failures)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }
}
