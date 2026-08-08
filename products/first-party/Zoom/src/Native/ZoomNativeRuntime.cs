using System;
using System.Collections.Generic;
using System.Globalization;
using global::DTMAPI.Abstractions;

namespace DTMAPI.Zoom
{
    internal sealed class ZoomNativeRuntime
    {
        private readonly IMonitor monitor;
        private readonly IZoomHookOwner hooks;
        private ZoomConfig config = new ZoomConfig();
        private double currentViewScale = 1d;
        private double vanillaOrthographicSize;
        private double appliedOrthographicSize;
        private Func<double?>? readSizeOverride;
        private Func<double, bool>? writeSizeOverride;
        private Func<bool>? mainCameraAbsentOverride;

        internal ZoomNativeRuntime(
            IMonitor monitor,
            IZoomHookOwner hooks)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            this.hooks = hooks ??
                throw new ArgumentNullException(nameof(hooks));
            Status = "created";
            LastMessage =
                "Zoom ProductNative state has not been configured.";
        }

        internal bool Enabled => config.Enabled;

        internal bool IsHookInstalled =>
            hooks.IsInstalled;

        internal int InstalledPatchCount =>
            hooks.InstalledPatchCount;

        internal double CurrentViewScale =>
            currentViewScale;

        internal double VanillaOrthographicSize =>
            vanillaOrthographicSize;

        internal double AppliedOrthographicSize =>
            appliedOrthographicSize;

        internal string Status { get; private set; }

        internal string LastMessage { get; private set; }

        internal string StatusSummary =>
            string.Format(
                CultureInfo.InvariantCulture,
                "status={0}, enabled={1}, hook={2}, scale={3:0.###}, vanillaSize={4:0.###}, appliedSize={5:0.###}. {6}",
                Status,
                Enabled,
                IsHookInstalled,
                currentViewScale,
                vanillaOrthographicSize,
                appliedOrthographicSize,
                LastMessage);

        internal void Configure(
            ZoomConfig value,
            string reason)
        {
            ZoomConfig next =
                (value ?? new ZoomConfig()).Copy();
            config = next;
            if (!next.Enabled)
            {
                CleanupNativeAndHook(
                    "config disabled " +
                    (reason ?? string.Empty));
                Status = "disabled";
                return;
            }

            try
            {
                hooks.InstallAtomically(this);
                double previousScale = currentViewScale;
                double targetScale = Clamp(
                    currentViewScale,
                    1d,
                    config.MaxViewScale);
                if (targetScale > 1.0001d ||
                    (previousScale > 1.0001d &&
                     targetScale <= 1.0001d))
                {
                    if (!ApplyScale(
                            targetScale,
                            "config " + reason))
                    {
                        return;
                    }
                }
                Status = "configured-product-native";
                LastMessage =
                    "Zoom ProductNative configured reason=" +
                    (reason ?? string.Empty) +
                    ".";
            }
            catch (Exception ex)
            {
                Status = hooks.IsInstalled
                    ? "enable-failed-owned"
                    : "enable-failed-closed";
                LastMessage =
                    "Zoom ProductNative enable failed: " +
                    ex.GetType().Name +
                    ": " +
                    ex.Message;
                throw;
            }
        }

        internal bool Step(
            int direction,
            string reason)
        {
            if (!config.Enabled)
                return false;
            double target = currentViewScale +
                (config.Step * (direction < 0 ? -1d : 1d));
            return SetViewScale(target, reason);
        }

        internal bool SetViewScale(
            double value,
            string reason)
        {
            if (!config.Enabled)
                return false;
            double targetScale = Clamp(
                value,
                1d,
                config.MaxViewScale);
            return ApplyScale(
                targetScale,
                reason ?? string.Empty);
        }

        internal void OnEnvironmentReset()
        {
            if (!config.Enabled)
                return;

            if (!TryReadOrthographicSize(
                    out double nativeSize))
            {
                Status = "pending-camera";
                LastMessage =
                    "SetEnvCamera completed without an available main camera.";
                return;
            }

            if (currentViewScale <= 1.0001d)
            {
                // While the product is at 1x, the live camera is entirely
                // native-owned. A real environment change may therefore
                // establish a new baseline for the next zoom request.
                vanillaOrthographicSize = nativeSize;
                appliedOrthographicSize = nativeSize;
                Status = "vanilla";
                LastMessage =
                    "SetEnvCamera completed at 1x; refreshed the native playable-camera baseline.";
                return;
            }

            // DolocAPI.SetEnvCamera does not normally write orthographicSize.
            // If the value still equals our last write, preserve the original
            // native baseline instead of multiplying the product's own output
            // again. A distinct positive value is an explicit native-owner
            // change and becomes the new baseline.
            if (appliedOrthographicSize <= 0d ||
                !NearlyEqual(
                    nativeSize,
                    appliedOrthographicSize))
            {
                vanillaOrthographicSize = nativeSize;
            }
            ApplyScale(
                Clamp(
                    currentViewScale,
                    1d,
                    config.MaxViewScale),
                "DolocAPI.SetEnvCamera Postfix");
        }

        internal void ResetToVanilla(string reason)
        {
            RestoreVanilla(reason);
            currentViewScale = 1d;
            Status = config.Enabled
                ? "configured-product-native"
                : "disabled";
        }

        internal void DeactivateOwner(string reason)
        {
            CleanupNativeAndHook(
                "OwnerDeactivation " +
                (reason ?? string.Empty));
            Status = "deactivated";
        }

        internal void ConfigureNativeAccessForTests(
            Func<double?>? reader,
            Func<double, bool>? writer,
            Func<bool>? mainCameraAbsent = null)
        {
            readSizeOverride = reader;
            writeSizeOverride = writer;
            mainCameraAbsentOverride =
                mainCameraAbsent;
        }

        internal ZoomNativeRefreshState BeforeNativeRefreshResolution()
        {
            if (!config.Enabled || currentViewScale <= 1.0001d)
            {
                if (TryReadOrthographicSize(out double nativeSize))
                {
                    vanillaOrthographicSize = nativeSize;
                    appliedOrthographicSize = nativeSize;
                }
                return ZoomNativeRefreshState.Inactive(this);
            }

            if (!TryReadOrthographicSize(out double currentSize))
            {
                Status = "refresh-prefix-failed";
                LastMessage = "Zoom could not read the live orthographic size before native RefreshResolution.";
                throw new InvalidOperationException(LastMessage);
            }

            if (vanillaOrthographicSize <= 0d ||
                appliedOrthographicSize <= 0d ||
                !NearlyEqual(currentSize, appliedOrthographicSize))
            {
                // A value distinct from our last product write is a new native
                // baseline. The original method must still run against that 1x
                // value instead of a product-scaled camera.
                vanillaOrthographicSize = currentSize;
            }

            var state = new ZoomNativeRefreshState(
                this,
                activeScale: currentViewScale,
                baseline: vanillaOrthographicSize,
                shouldReapply: true);
            if (!TryWriteOrthographicSize(vanillaOrthographicSize))
            {
                Status = "refresh-prefix-failed";
                LastMessage = "Zoom could not expose the native 1x baseline to CameraController.RefreshResolution.";
                throw new InvalidOperationException(LastMessage);
            }

            appliedOrthographicSize = vanillaOrthographicSize;
            return state;
        }

        internal Exception? AfterNativeRefreshResolution(
            ZoomNativeRefreshState state,
            Exception? nativeException)
        {
            if (!state.ShouldReapply || !ReferenceEquals(state.Runtime, this))
                return nativeException;

            double baseline = state.Baseline;
            if (TryReadOrthographicSize(out double refreshedNativeSize) &&
                refreshedNativeSize > 0d)
            {
                baseline = refreshedNativeSize;
            }
            if (baseline > 0d)
                vanillaOrthographicSize = baseline;

            double activeScale = config.Enabled
                ? Clamp(currentViewScale, 1d, config.MaxViewScale)
                : 1d;
            double target = vanillaOrthographicSize * activeScale;
            if (vanillaOrthographicSize <= 0d || !TryWriteOrthographicSize(target))
            {
                Status = "refresh-finalizer-failed";
                LastMessage = "Zoom could not restore its orthographic-size presentation after native RefreshResolution.";
                if (nativeException != null)
                    return nativeException;
                return new InvalidOperationException(LastMessage);
            }

            currentViewScale = activeScale;
            appliedOrthographicSize = target;
            Status = activeScale > 1.0001d
                ? "applied-product-native"
                : "vanilla";
            LastMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Native RefreshResolution used baseline={0:0.###}; restored view scale={1:0.###}; orthographicSize={2:0.###}.",
                vanillaOrthographicSize,
                activeScale,
                target);
            return nativeException;
        }

        private bool ApplyScale(
            double targetScale,
            string reason)
        {
            if (!TryReadOrthographicSize(
                    out double currentSize))
            {
                Status = "pending-camera";
                LastMessage =
                    "Zoom could not resolve DolocAPI.mainCamera.orthographicSize.";
                return false;
            }

            if (vanillaOrthographicSize <= 0d)
                vanillaOrthographicSize = currentSize;

            double target =
                vanillaOrthographicSize *
                targetScale;
            if (!TryWriteOrthographicSize(target))
            {
                Status = "failed-closed";
                LastMessage =
                    "Zoom could not write DolocAPI.mainCamera.orthographicSize.";
                return false;
            }

            currentViewScale = targetScale;
            appliedOrthographicSize = target;
            Status = targetScale > 1.0001d
                ? "applied-product-native"
                : "vanilla";
            LastMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Playable view scale={0:0.###}; orthographicSize={1:0.###}; reason={2}.",
                targetScale,
                target,
                reason ?? string.Empty);
            if (config.VerboseLogging)
                monitor.Log("Zoom " + LastMessage);
            return true;
        }

        private void CleanupNativeAndHook(string reason)
        {
            var failures = new List<Exception>();
            bool nativeRestoreSucceeded = false;
            try
            {
                RestoreVanilla(reason);
                nativeRestoreSucceeded = true;
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
            TryCleanup(
                () => ZoomCallbacks.Detach(this),
                failures);
            TryCleanup(
                () => hooks.UnpatchOwnedHooks(this),
                failures);
            if (nativeRestoreSucceeded)
            {
                currentViewScale = 1d;
                vanillaOrthographicSize = 0d;
                appliedOrthographicSize = 0d;
                LastMessage =
                    "Zoom native state and exact owner cleared reason=" +
                    reason +
                    ".";
            }
            else
            {
                Status = hooks.IsInstalled
                    ? "restore-failed-owner-retained"
                    : "restore-failed-owner-unpatched";
                LastMessage =
                    "Zoom native restore failed; the captured snapshot remains retryable after exact-owner cleanup reason=" +
                    reason +
                    ".";
            }
            if (failures.Count == 1)
                throw failures[0];
            if (failures.Count > 1)
            {
                throw new AggregateException(
                    "Zoom restoration, callback detach, and exact-owner unpatch encountered multiple failures.",
                    failures);
            }
        }

        private void RestoreVanilla(string reason)
        {
            if (vanillaOrthographicSize > 0d)
            {
                if (!TryReadOrthographicSize(out _))
                {
                    if (IsMainCameraDefinitelyAbsent())
                    {
                        // A null/destroyed title-transition camera cannot
                        // retain the product's orthographic-size write.
                        vanillaOrthographicSize = 0d;
                        appliedOrthographicSize = 0d;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Zoom could not read a live main camera while restoring the captured native orthographic size.");
                    }
                }
                else if (!TryWriteOrthographicSize(
                             vanillaOrthographicSize))
                {
                    throw new InvalidOperationException(
                        "Zoom could not restore the captured native orthographic size.");
                }
            }
            currentViewScale = 1d;
            appliedOrthographicSize =
                vanillaOrthographicSize;
            LastMessage =
                "Zoom restored the vanilla playable view reason=" +
                (reason ?? string.Empty) +
                ".";
        }

        private bool TryReadOrthographicSize(
            out double value)
        {
            try
            {
                if (readSizeOverride != null)
                {
                    double? result = readSizeOverride();
                    value = result ?? 0d;
                    return result.HasValue &&
                        result.Value > 0d;
                }

                return ZoomNativeAccess.TryReadOrthographicSize(
                    out value);
            }
            catch
            {
                value = 0d;
                return false;
            }
        }

        private bool TryWriteOrthographicSize(
            double value)
        {
            try
            {
                if (writeSizeOverride != null)
                    return writeSizeOverride(value);
                return ZoomNativeAccess
                    .TryWriteOrthographicSize(value);
            }
            catch
            {
                return false;
            }
        }

        private bool IsMainCameraDefinitelyAbsent()
        {
            try
            {
                return mainCameraAbsentOverride != null
                    ? mainCameraAbsentOverride()
                    : ZoomNativeAccess
                        .IsMainCameraDefinitelyAbsent();
            }
            catch
            {
                return false;
            }
        }

        private static double Clamp(
            double value,
            double min,
            double max)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                return min;
            }
            return Math.Min(max, Math.Max(min, value));
        }

        private static bool NearlyEqual(
            double left,
            double right)
        {
            double scale = Math.Max(
                1d,
                Math.Max(
                    Math.Abs(left),
                    Math.Abs(right)));
            return Math.Abs(left - right) <=
                0.000001d * scale;
        }

        private static void TryCleanup(
            Action action,
            ICollection<Exception> failures)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }

    public readonly struct ZoomNativeRefreshState
    {
        internal ZoomNativeRefreshState(
            ZoomNativeRuntime runtime,
            double activeScale,
            double baseline,
            bool shouldReapply)
        {
            Runtime = runtime;
            ActiveScale = activeScale;
            Baseline = baseline;
            ShouldReapply = shouldReapply;
        }

        internal ZoomNativeRuntime? Runtime { get; }
        internal double ActiveScale { get; }
        internal double Baseline { get; }
        internal bool ShouldReapply { get; }

        internal static ZoomNativeRefreshState Inactive(ZoomNativeRuntime runtime) =>
            new ZoomNativeRefreshState(runtime, 1d, 0d, shouldReapply: false);
    }
}
