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
        private ChestLocatorEnhancerConfig config =
            new ChestLocatorEnhancerConfig();
        private readonly ChestLocatorObservationLogGate
            observationLogGate =
                new ChestLocatorObservationLogGate();
        private int extensionApplications;
        private bool hasTraversalObservation;
        private bool lastUseBox;
        private bool lastNativeAutoUseBox;
        private ChestLocatorTraversalResult
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
            Status = "created";
        }

        internal bool Enabled => config.Enabled;

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

        internal void Configure(
            ChestLocatorEnhancerConfig value,
            string reason)
        {
            ChestLocatorEnhancerConfig next =
                (value ??
                 new ChestLocatorEnhancerConfig()).Copy();
            config = next;
            if (next.Enabled)
            {
                try
                {
                    ChestLocatorHookOwnership.ApplyEnabledState(
                        enabled: true,
                        () => hooks.InstallAtomically(this),
                        hooks.UnpatchOwnedHooks);
                }
                catch (Exception ex)
                {
                    Status = hooks.IsInstalled
                        ? "enable-failed-owned"
                        : "enable-failed-closed";
                    SetMessage(
                        "ProductNative enable failed: " +
                        ex.GetType().Name +
                        ": " +
                        ex.Message);
                    throw;
                }
            }
            else
            {
                try
                {
                    ChestLocatorHookOwnership.ApplyEnabledState(
                        enabled: false,
                        () => hooks.InstallAtomically(this),
                        hooks.UnpatchOwnedHooks);
                    ChestLocatorEnhancerCallbacks.Detach(
                        this);
                }
                catch (Exception ex)
                {
                    Status = hooks.IsInstalled
                        ? "disable-failed-owned"
                        : "disable-failed-clean";
                    SetMessage(
                        "ProductNative disable cleanup failed: " +
                        ex.GetType().Name +
                        ": " +
                        ex.Message);
                    throw;
                }
            }

            Status = next.Enabled
                ? "configured-product-native"
                : "disabled";
            SetMessage(
                "Chest locator ProductNative policy configured reason=" +
                (reason ?? string.Empty) +
                ", includeSharedCases=" +
                config.IncludeSharedCases +
                ", includeSharedStorageShelfBoxes=" +
                config.IncludeSharedStorageShelfBoxes +
                ", respectNativeAutoUseBox=" +
                config.RespectNativeAutoUseBoxSetting +
                ".");
        }

        internal Array ExtendAvailableInventories(
            object archive,
            bool useBox,
            Array nativeResult)
        {
            if (!config.Enabled)
                return nativeResult;

            try
            {
                bool nativeAutoUseBox =
                    !config.RespectNativeAutoUseBoxSetting ||
                    IsNativeAutoUseBoxEnabled();
                ChestLocatorTraversalResult result =
                    ChestLocatorInventoryTraversal
                        .AppendSharedInventories(
                            archive,
                            ChestLocatorInventoryTraversal
                                .ReadStaticMember(
                                    typeof(DolocAPI),
                                    "CurrentRootRoom"),
                            useBox,
                            config.IncludeSharedCases,
                            config
                                .IncludeSharedStorageShelfBoxes,
                            nativeAutoUseBox,
                            nativeResult);

                extensionApplications++;
                LastAppendedInventoryCount =
                    result.AppendedInventoryCount;
                LastSharedCaseCount =
                    result.SharedCaseCount;
                LastSharedStorageBoxCount =
                    result.SharedStorageBoxCount;
                lastUseBox = useBox;
                lastNativeAutoUseBox =
                    nativeAutoUseBox;
                lastTraversalObservation = result;
                hasTraversalObservation = true;
                Status =
                    result.AppendedInventoryCount > 0
                        ? "verified-product-native"
                        : "configured-product-native";
                if (observationLogGate.ShouldLog(
                    config.VerboseLogging,
                    useBox,
                    nativeAutoUseBox,
                    result.AppendedInventoryCount > 0))
                {
                    monitor.Log(
                        "ChestLocatorEnhancer ProductNative inventories " +
                        FormatTraversalObservation());
                }

                return result.AppendedInventoryCount > 0
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
            extensionApplications = 0;
            LastAppendedInventoryCount = 0;
            LastSharedCaseCount = 0;
            LastSharedStorageBoxCount = 0;
            observationLogGate.Reset();
            Status = config.Enabled
                ? "configured-product-native"
                : "disabled";
            SetMessage(
                "ProductNative observation state reset reason=" +
                (reason ?? string.Empty) +
                ".");
        }

        internal void DeactivateOwner(string reason)
        {
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
            lastMessage = message ?? string.Empty;
        }

        private static bool IsNativeAutoUseBoxEnabled()
        {
            object? settings =
                ChestLocatorInventoryTraversal.ReadStaticMember(
                    typeof(DolocAPI),
                    "userSettings");
            object? value =
                ChestLocatorInventoryTraversal.ReadMember(
                    settings,
                    "autoUseBox");
            return !(value is bool flag) || flag;
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
