using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.CameraNativeReflection;

#pragma warning disable CS0618

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraZoomCompatibilityService : ICameraZoomApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly CameraViewService viewService;
        private readonly Dictionary<string, string> cameraZoomCompatibilityLeaseIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CameraZoomOptions> cameraZoomOptions = new Dictionary<string, CameraZoomOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CameraZoomState> cameraZoomStates = new Dictionary<string, CameraZoomState>(StringComparer.OrdinalIgnoreCase);
        private double cameraZoomVanillaOrthographicSize;
        private double cameraZoomCurrentViewScale = 1d;
        private double cameraZoomAppliedOrthographicSize;
        private string cameraZoomActiveOwnerId = string.Empty;
        private double cameraZoomRequestedViewScale = 1d;
        private double cameraZoomClampedViewScale = 1d;
        private double cameraZoomAppliedViewScale = 1d;
        private string cameraZoomCameraControllerStatus = "not-called-playable";
        private string cameraZoomBackgroundStatus = "not-applicable-playable";
        private string cameraZoomFogStatus = "not-applicable-playable";
        private string cameraZoomScannerStatus = "not-called-playable";
        private string cameraZoomLifecycleStatus = "not-restored";
        private string cameraZoomUiScaleStatus = "unchanged";

        public CameraZoomCompatibilityService(DtmApiRuntime runtime, CameraViewService viewService)
        {
            this.runtime = runtime;
            this.viewService = viewService;
        }

        public CameraZoomRegisterResult Register(IManifest owner, CameraZoomOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            ThrowIfManagedProductOwnerBeforeMutation(
                "ICameraZoomApi.Register");
            CameraZoomOptions normalized = NormalizeCameraZoomOptions(options);
            if (normalized.Enabled)
            {
                viewService
                    .EnsureCompatibilityOwnerBeforeMutation(
                        "ICameraZoomApi.Register");
            }
            cameraZoomOptions[owner.UniqueID] = normalized;

            CameraZoomState state = GetCameraZoomState(owner.UniqueID);
            double requested = state.CurrentViewScale <= 0 ? 1d : state.CurrentViewScale;
            CameraViewResult view = UpsertCameraZoomCompatibilityLease(owner, normalized, requested, "obsolete register");
            CameraZoomState updated = UpdateCameraZoomStateFromView(owner.UniqueID, normalized, view, requested);
            updated.Status = normalized.Enabled ? "obsolete-compatibility" : "disabled";
            updated.LastMessage = normalized.Enabled
                ? "ICameraZoomApi is obsolete and redirected to ICameraViewApi."
                : "ICameraZoomApi compatibility policy is disabled.";
            cameraZoomStates[owner.UniqueID] = updated;

            CameraZoomRegisterResult result = ToCameraZoomRegisterResult(owner.UniqueID, normalized, view);
            result.Success = view.Success || !normalized.Enabled;
            result.Message = updated.LastMessage + " " + result.Message;
            runtime.RuntimeMonitor.Log("CameraZoom obsolete API register success=" + result.Success + " owner=" + owner.UniqueID + " range=" + normalized.MinViewScale.ToString("0.##", CultureInfo.InvariantCulture) + "-" + normalized.MaxViewScale.ToString("0.##", CultureInfo.InvariantCulture) + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            return result;
        }

        public CameraZoomResult SetViewScale(IManifest owner, double viewScale, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            ThrowIfManagedProductOwnerBeforeMutation(
                "ICameraZoomApi.SetViewScale");
            CameraZoomOptions options = GetCameraZoomOptions(owner.UniqueID);
            CameraZoomState state = GetCameraZoomState(owner.UniqueID);
            double before = state.CurrentViewScale <= 0 ? 1d : state.CurrentViewScale;
            double requested = double.IsNaN(viewScale) || double.IsInfinity(viewScale) ? 1d : viewScale;
            double clamped = ClampDouble(requested, options.MinViewScale, options.MaxViewScale);
            CameraViewResult view = UpsertCameraZoomCompatibilityLease(owner, options, clamped, "obsolete " + (reason ?? string.Empty));
            CameraZoomState updated = UpdateCameraZoomStateFromView(owner.UniqueID, options, view, requested);
            updated.CurrentViewScale = options.Enabled ? clamped : 1d;
            updated.RequestedViewScale = requested;
            updated.ClampedViewScale = clamped;
            updated.Status = options.Enabled ? "obsolete-compatibility" : "disabled";
            updated.LastMessage = "ICameraZoomApi is obsolete and redirected to ICameraViewApi. Requested " + before.ToString("0.##", CultureInfo.InvariantCulture) + "->" + requested.ToString("0.##", CultureInfo.InvariantCulture) + " reason=" + (reason ?? string.Empty) + ".";
            cameraZoomStates[owner.UniqueID] = updated;

            CameraZoomResult result = ToCameraZoomResult(owner.UniqueID, view);
            result.RequestedViewScale = requested;
            result.ClampedViewScale = clamped;
            result.BeforeViewScale = before;
            result.AfterViewScale = updated.CurrentViewScale;
            if (result.Success)
                result.Message = updated.LastMessage + " " + result.Message;
            return result;
        }

        public CameraZoomResult StepViewScale(IManifest owner, int direction, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            CameraZoomOptions options = GetCameraZoomOptions(owner.UniqueID);
            CameraZoomState state = GetCameraZoomState(owner.UniqueID);
            double current = state.CurrentViewScale <= 0 ? 1d : state.CurrentViewScale;
            int sign = direction < 0 ? -1 : 1;
            return SetViewScale(owner, current + (options.Step * sign), reason);
        }

        public CameraZoomResult ResetViewScale(IManifest owner, string reason)
        {
            return SetViewScale(owner, 1d, reason);
        }

        public CameraZoomState GetState(string uniqueId)
        {
            return CloneCameraZoomState(GetCameraZoomState(uniqueId ?? string.Empty));
        }

        public CameraZoomState GetSnapshot(string uniqueId)
        {
            return CloneCameraZoomState(GetCameraZoomState(uniqueId ?? string.Empty));
        }

        public BridgeFeatureStatus GetStatus(string uniqueId)
        {
            CameraZoomState state = GetCameraZoomState(uniqueId ?? string.Empty);
            return new BridgeFeatureStatus(state.Status, "ICameraZoomApi is obsolete; use ICameraViewApi. " + state.LastMessage + " cameraController=" + state.CameraControllerStatus + "; background=" + state.BackgroundCompensationStatus + "; fog=" + state.FogCompensationStatus + "; scanner=" + state.ScannerRefreshStatus + "; lifecycle=" + state.LifecycleRestoreStatus + "; uiScale=" + state.UiScaleStatus + ".");
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            int removed = 0;
            if (cameraZoomCompatibilityLeaseIds.Remove(ownerId))
                removed++;
            if (cameraZoomOptions.Remove(ownerId))
                removed++;
            if (cameraZoomStates.Remove(ownerId))
                removed++;
            return removed;
        }

        internal void AbandonForManagedProductOwner()
        {
            cameraZoomCompatibilityLeaseIds.Clear();
            cameraZoomOptions.Clear();
            cameraZoomStates.Clear();
            cameraZoomVanillaOrthographicSize = 0d;
            cameraZoomCurrentViewScale = 1d;
            cameraZoomAppliedOrthographicSize = 0d;
            cameraZoomActiveOwnerId = string.Empty;
            cameraZoomRequestedViewScale = 1d;
            cameraZoomClampedViewScale = 1d;
            cameraZoomAppliedViewScale = 1d;
            cameraZoomLifecycleStatus =
                "refused-managed-product-owner";
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            return (cameraZoomCompatibilityLeaseIds.ContainsKey(ownerId) ? 1 : 0) +
                (cameraZoomOptions.ContainsKey(ownerId) ? 1 : 0) +
                (cameraZoomStates.ContainsKey(ownerId) ? 1 : 0);
        }

        internal void SynchronizeFromView(CameraViewResult view)
        {
            foreach (KeyValuePair<string, CameraZoomOptions> entry in cameraZoomOptions.ToArray())
            {
                CameraZoomState current = GetCameraZoomState(entry.Key);
                UpdateCameraZoomStateFromView(entry.Key, entry.Value, view, current.RequestedViewScale <= 0 ? current.CurrentViewScale : current.RequestedViewScale);
            }
        }

        private CameraViewResult UpsertCameraZoomCompatibilityLease(IManifest owner, CameraZoomOptions options, double viewScale, string reason)
        {
            CameraViewRequest request = new CameraViewRequest
            {
                Enabled = options.Enabled,
                ViewScale = viewScale,
                MinViewScale = options.MinViewScale,
                MaxViewScale = options.MaxViewScale,
                Step = options.Step,
                Priority = 0,
                LeaseName = "ICameraZoomApi compatibility",
                VerboseLogging = options.VerboseLogging
            };

            if (!cameraZoomCompatibilityLeaseIds.TryGetValue(owner.UniqueID, out string leaseId) ||
                !viewService.ContainsLease(leaseId))
            {
                ICameraViewLease lease = viewService.AcquireLease(owner, request);
                cameraZoomCompatibilityLeaseIds[owner.UniqueID] = lease.LeaseId;
                return CameraViewService.CloneResult(lease.LastResult);
            }

            return viewService.UpdateLease(leaseId, request, reason);
        }

        private void ThrowIfManagedProductOwnerBeforeMutation(
            string operation) =>
            viewService
                .ThrowIfManagedProductOwnerBeforeNativeMutation(
                    operation);

        private CameraZoomState UpdateCameraZoomStateFromView(string ownerId, CameraZoomOptions options, CameraViewResult view, double requested)
        {
            CameraViewState viewState = viewService.GetState(ownerId);
            var state = new CameraZoomState
            {
                OwnerId = ownerId,
                IsConfigured = true,
                Enabled = options.Enabled,
                MinViewScale = options.MinViewScale,
                MaxViewScale = options.MaxViewScale,
                Step = options.Step,
                RequestedViewScale = requested,
                ClampedViewScale = ClampDouble(requested, options.MinViewScale, options.MaxViewScale),
                CurrentViewScale = viewState.CurrentViewScale,
                AppliedViewScale = view.AppliedViewScale,
                ActiveOwnerId = view.ActiveOwnerId,
                VanillaOrthographicSize = view.VanillaOrthographicSize,
                AppliedOrthographicSize = view.AppliedOrthographicSize,
                CameraAvailable = viewState.CameraAvailable,
                RefreshCameraController = false,
                CompensateBackground = false,
                CompensateDepthFog = false,
                RefreshScanners = false,
                CameraControllerStatus = "not-called-playable",
                BackgroundCompensationStatus = "not-applicable-playable",
                FogCompensationStatus = "not-applicable-playable",
                ScannerRefreshStatus = "not-called-playable",
                LifecycleRestoreStatus = view.LifecycleStatus,
                UiScaleStatus = view.UiScaleStatus,
                CurrentRoomId = viewState.CurrentRoomId,
                CurrentRoomTitle = viewState.CurrentRoomTitle,
                CurrentRoomShowsBackground = viewState.CurrentRoomShowsBackground,
                Status = options.Enabled ? "obsolete-compatibility" : "disabled",
                LastMessage = view.Message
            };
            cameraZoomStates[ownerId] = state;
            cameraZoomVanillaOrthographicSize = view.VanillaOrthographicSize;
            cameraZoomCurrentViewScale = state.CurrentViewScale;
            cameraZoomAppliedOrthographicSize = view.AppliedOrthographicSize;
            cameraZoomActiveOwnerId = view.ActiveOwnerId;
            cameraZoomRequestedViewScale = state.RequestedViewScale;
            cameraZoomClampedViewScale = state.ClampedViewScale;
            cameraZoomAppliedViewScale = view.AppliedViewScale;
            cameraZoomCameraControllerStatus = state.CameraControllerStatus;
            cameraZoomBackgroundStatus = state.BackgroundCompensationStatus;
            cameraZoomFogStatus = state.FogCompensationStatus;
            cameraZoomScannerStatus = state.ScannerRefreshStatus;
            cameraZoomLifecycleStatus = state.LifecycleRestoreStatus;
            cameraZoomUiScaleStatus = state.UiScaleStatus;
            return state;
        }

        private CameraZoomRegisterResult ToCameraZoomRegisterResult(string ownerId, CameraZoomOptions options, CameraViewResult view)
        {
            return new CameraZoomRegisterResult
            {
                Success = view.Success,
                OwnerId = ownerId,
                MinViewScale = options.MinViewScale,
                MaxViewScale = options.MaxViewScale,
                CurrentViewScale = GetCameraZoomState(ownerId).CurrentViewScale,
                AppliedViewScale = view.AppliedViewScale,
                ActiveOwnerId = view.ActiveOwnerId,
                CameraControllerStatus = "not-called-playable",
                BackgroundCompensationStatus = "not-applicable-playable",
                FogCompensationStatus = "not-applicable-playable",
                ScannerRefreshStatus = "not-called-playable",
                LifecycleRestoreStatus = view.LifecycleStatus,
                UiScaleStatus = view.UiScaleStatus,
                FailureReason = view.FailureReason,
                Message = view.Message
            };
        }

        private static CameraZoomResult ToCameraZoomResult(string ownerId, CameraViewResult view)
        {
            return new CameraZoomResult
            {
                Success = view.Success,
                OwnerId = ownerId,
                RequestedViewScale = view.RequestedViewScale,
                ClampedViewScale = view.ClampedViewScale,
                BeforeViewScale = view.BeforeViewScale,
                AfterViewScale = view.AfterViewScale,
                AppliedViewScale = view.AppliedViewScale,
                VanillaOrthographicSize = view.VanillaOrthographicSize,
                AppliedOrthographicSize = view.AppliedOrthographicSize,
                ActiveOwnerId = view.ActiveOwnerId,
                CameraControllerStatus = "not-called-playable",
                BackgroundCompensationStatus = "not-applicable-playable",
                FogCompensationStatus = "not-applicable-playable",
                ScannerRefreshStatus = "not-called-playable",
                LifecycleRestoreStatus = view.LifecycleStatus,
                UiScaleStatus = view.UiScaleStatus,
                FailureReason = view.FailureReason,
                Message = view.Message
            };
        }

        private CameraZoomState GetCameraZoomState(string ownerId)
        {
            ownerId ??= string.Empty;
            if (cameraZoomStates.TryGetValue(ownerId, out CameraZoomState state))
                return state;

            CameraZoomOptions options = GetCameraZoomOptions(ownerId);
            CameraViewState view = viewService.GetState(ownerId);
            return new CameraZoomState
            {
                OwnerId = ownerId,
                IsConfigured = cameraZoomOptions.ContainsKey(ownerId),
                Enabled = options.Enabled,
                MinViewScale = options.MinViewScale,
                MaxViewScale = options.MaxViewScale,
                Step = options.Step,
                RequestedViewScale = view.RequestedViewScale,
                ClampedViewScale = view.ClampedViewScale,
                CurrentViewScale = view.CurrentViewScale,
                AppliedViewScale = view.AppliedViewScale,
                ActiveOwnerId = view.ActiveOwnerId,
                VanillaOrthographicSize = view.VanillaOrthographicSize,
                AppliedOrthographicSize = view.AppliedOrthographicSize,
                CameraAvailable = view.CameraAvailable,
                RefreshCameraController = false,
                CompensateBackground = false,
                CompensateDepthFog = false,
                RefreshScanners = false,
                CameraControllerStatus = "not-called-playable",
                BackgroundCompensationStatus = "not-applicable-playable",
                FogCompensationStatus = "not-applicable-playable",
                ScannerRefreshStatus = "not-called-playable",
                LifecycleRestoreStatus = view.LifecycleStatus,
                UiScaleStatus = view.UiScaleStatus,
                CurrentRoomId = view.CurrentRoomId,
                CurrentRoomTitle = view.CurrentRoomTitle,
                CurrentRoomShowsBackground = view.CurrentRoomShowsBackground,
                Status = cameraZoomOptions.ContainsKey(ownerId) ? "obsolete-compatibility" : "not-configured",
                LastMessage = cameraZoomOptions.ContainsKey(ownerId) ? "ICameraZoomApi is obsolete and redirected to ICameraViewApi." : "No camera zoom policy registered."
            };
        }

        private static CameraZoomState CloneCameraZoomState(CameraZoomState state)
        {
            return new CameraZoomState
            {
                OwnerId = state.OwnerId,
                IsConfigured = state.IsConfigured,
                Enabled = state.Enabled,
                MinViewScale = state.MinViewScale,
                MaxViewScale = state.MaxViewScale,
                Step = state.Step,
                RequestedViewScale = state.RequestedViewScale,
                ClampedViewScale = state.ClampedViewScale,
                CurrentViewScale = state.CurrentViewScale,
                AppliedViewScale = state.AppliedViewScale,
                ActiveOwnerId = state.ActiveOwnerId,
                VanillaOrthographicSize = state.VanillaOrthographicSize,
                AppliedOrthographicSize = state.AppliedOrthographicSize,
                CameraAvailable = state.CameraAvailable,
                RefreshCameraController = state.RefreshCameraController,
                CompensateBackground = state.CompensateBackground,
                CompensateDepthFog = state.CompensateDepthFog,
                RefreshScanners = state.RefreshScanners,
                CameraControllerStatus = state.CameraControllerStatus,
                BackgroundCompensationStatus = state.BackgroundCompensationStatus,
                FogCompensationStatus = state.FogCompensationStatus,
                ScannerRefreshStatus = state.ScannerRefreshStatus,
                LifecycleRestoreStatus = state.LifecycleRestoreStatus,
                UiScaleStatus = state.UiScaleStatus,
                CurrentRoomId = state.CurrentRoomId,
                CurrentRoomTitle = state.CurrentRoomTitle,
                CurrentRoomShowsBackground = state.CurrentRoomShowsBackground,
                Status = state.Status,
                LastMessage = state.LastMessage
            };
        }

        private CameraZoomOptions GetCameraZoomOptions(string ownerId)
        {
            return cameraZoomOptions.TryGetValue(ownerId ?? string.Empty, out CameraZoomOptions? options)
                ? options
                : new CameraZoomOptions { Enabled = false };
        }

        private static CameraZoomOptions NormalizeCameraZoomOptions(CameraZoomOptions? options)
        {
            options ??= new CameraZoomOptions();
            double min = ClampDouble(options.MinViewScale <= 0 ? 1d : options.MinViewScale, 1d, 16d);
            double max = ClampDouble(options.MaxViewScale <= 0 ? 4d : options.MaxViewScale, min, 16d);
            double step = ClampDouble(options.Step <= 0 ? 0.25d : options.Step, 0.05d, Math.Max(0.05d, max - min));
            return new CameraZoomOptions
            {
                Enabled = options.Enabled,
                MinViewScale = min,
                MaxViewScale = max,
                Step = step,
                VerboseLogging = options.VerboseLogging
            };
        }
    }
}

#pragma warning restore CS0618
