using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.CameraNativeReflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraViewService : ICameraViewApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly CameraDiagnosticsService diagnostics;
        private readonly Dictionary<string, CameraViewLeaseRuntime> cameraViewLeases = new Dictionary<string, CameraViewLeaseRuntime>(StringComparer.OrdinalIgnoreCase);
        private long cameraViewLeaseSequence;
        private double cameraViewVanillaOrthographicSize;
        private double cameraViewCurrentViewScale = 1d;
        private double cameraViewAppliedOrthographicSize;
        private string cameraViewActiveOwnerId = string.Empty;
        private string cameraViewActiveLeaseId = string.Empty;
        private string cameraViewArbitrationStatus = "vanilla-no-active-lease";
        private string cameraViewCameraOwnerStatus = "not-applied";
        private string cameraViewNativeRefreshStatus = "not-called-playable";
        private string cameraViewLifecycleStatus = "not-restored";
        private string cameraViewUiScaleStatus = "unchanged";

        public CameraViewService(DtmApiRuntime runtime, CameraDiagnosticsService diagnostics)
        {
            this.runtime = runtime;
            this.diagnostics = diagnostics;
        }

        public event Action<CameraViewResult>? ViewApplied;

        public void NotifyEnvironmentReset(string reason)
        {
            if (cameraViewLeases.Count == 0 && cameraViewCurrentViewScale <= 1.0001d)
                return;

            CameraViewResult result = ApplyCameraViewTarget("environment reset " + (reason ?? string.Empty));
            runtime.RuntimeMonitor.Log("CameraView environment refresh reason=" + (reason ?? string.Empty) + " success=" + result.Success + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        public ICameraViewLease AcquireLease(IManifest owner, CameraViewRequest request)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            CameraViewRequest normalized = NormalizeCameraViewRequest(request);
            string leaseId = Guid.NewGuid().ToString("N");
            var lease = new CameraViewLeaseRuntime
            {
                LeaseId = leaseId,
                OwnerId = owner.UniqueID ?? string.Empty,
                Request = normalized,
                CreatedOrder = ++cameraViewLeaseSequence,
                UpdatedOrder = cameraViewLeaseSequence
            };
            cameraViewLeases[leaseId] = lease;

            CameraViewResult result = ApplyCameraViewTarget("lease acquire " + FirstText(normalized.LeaseName, leaseId));
            result.OwnerId = lease.OwnerId;
            result.LeaseId = lease.LeaseId;
            lease.LastResult = CloneResult(result);

            if (normalized.VerboseLogging || result.AppliedViewScale > 1.0001d)
                runtime.RuntimeMonitor.Log("CameraView lease acquired owner=" + lease.OwnerId + " lease=" + lease.LeaseId + " priority=" + normalized.Priority.ToString(CultureInfo.InvariantCulture) + " scale=" + normalized.ViewScale.ToString("0.###", CultureInfo.InvariantCulture) + " success=" + result.Success + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);

            return new CameraViewLeaseHandle(this, lease.OwnerId, lease.LeaseId);
        }

        public CameraViewState GetState(string uniqueId)
        {
            return CloneState(GetCameraViewState(uniqueId ?? string.Empty));
        }

        public CameraViewState GetSnapshot(string uniqueId)
        {
            return CloneState(GetCameraViewState(uniqueId ?? string.Empty));
        }

        public BridgeFeatureStatus GetStatus(string uniqueId)
        {
            CameraViewState state = GetCameraViewState(uniqueId ?? string.Empty);
            return new BridgeFeatureStatus(state.Status, state.LastMessage + " arbitration=" + state.ArbitrationStatus + "; cameraOwner=" + state.CameraOwnerStatus + "; nativeRefresh=" + state.NativeRefreshStatus + "; lifecycle=" + state.LifecycleStatus + "; uiScale=" + state.UiScaleStatus + ".");
        }

        public void RefreshForRuntime()
        {
            if (cameraViewLeases.Count == 0 && cameraViewCurrentViewScale <= 1.0001d)
                return;

            ApplyCameraViewTarget("runtime refresh");
        }

        public void ResetForLifecycleBoundary(string reason)
        {
            if (cameraViewLeases.Count == 0 && cameraViewCurrentViewScale <= 1.0001d)
                return;

            foreach (CameraViewLeaseRuntime lease in cameraViewLeases.Values.ToArray())
            {
                if (lease.IsReleased)
                    continue;
                CameraViewRequest request = CloneRequest(lease.Request);
                request.ViewScale = 1d;
                lease.Request = request;
                lease.UpdatedOrder = ++cameraViewLeaseSequence;
            }

            CameraViewResult result = ApplyCameraViewTarget("lifecycle " + (reason ?? string.Empty));
            runtime.RuntimeMonitor.Log("CameraView lifecycle reset reason=" + (reason ?? string.Empty) + " success=" + result.Success + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        internal bool ContainsLease(string leaseId)
        {
            return cameraViewLeases.ContainsKey(leaseId ?? string.Empty);
        }

        internal CameraViewResult UpdateLease(string leaseId, CameraViewRequest request, string reason)
        {
            if (!cameraViewLeases.TryGetValue(leaseId ?? string.Empty, out CameraViewLeaseRuntime lease) || lease.IsReleased)
                return CameraViewLeaseMissing(leaseId, "released-or-missing", "Camera view lease is already released or missing.");

            CameraViewRequest normalized = NormalizeCameraViewRequest(request);
            if (string.IsNullOrWhiteSpace(normalized.LeaseName))
                normalized.LeaseName = lease.Request.LeaseName;
            lease.Request = normalized;
            lease.UpdatedOrder = ++cameraViewLeaseSequence;

            CameraViewResult result = ApplyCameraViewTarget("lease update " + (reason ?? string.Empty));
            result.OwnerId = lease.OwnerId;
            result.LeaseId = lease.LeaseId;
            lease.LastResult = CloneResult(result);

            if (normalized.VerboseLogging || result.AppliedViewScale > 1.0001d || !result.Success)
                runtime.RuntimeMonitor.Log("CameraView lease update owner=" + lease.OwnerId + " lease=" + lease.LeaseId + " priority=" + normalized.Priority.ToString(CultureInfo.InvariantCulture) + " scale=" + normalized.ViewScale.ToString("0.###", CultureInfo.InvariantCulture) + " success=" + result.Success + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            return result;
        }

        internal static CameraViewResult CloneResult(CameraViewResult result)
        {
            return new CameraViewResult
            {
                Success = result.Success,
                OwnerId = result.OwnerId,
                LeaseId = result.LeaseId,
                ActiveOwnerId = result.ActiveOwnerId,
                ActiveLeaseId = result.ActiveLeaseId,
                ArbitrationStatus = result.ArbitrationStatus,
                RequestedViewScale = result.RequestedViewScale,
                ClampedViewScale = result.ClampedViewScale,
                BeforeViewScale = result.BeforeViewScale,
                AfterViewScale = result.AfterViewScale,
                AppliedViewScale = result.AppliedViewScale,
                VanillaOrthographicSize = result.VanillaOrthographicSize,
                AppliedOrthographicSize = result.AppliedOrthographicSize,
                CameraOwnerStatus = result.CameraOwnerStatus,
                NativeRefreshStatus = result.NativeRefreshStatus,
                UiScaleStatus = result.UiScaleStatus,
                LifecycleStatus = result.LifecycleStatus,
                FailureReason = result.FailureReason,
                Message = result.Message
            };
        }

        private CameraViewResult SetLeaseScale(string leaseId, double viewScale, string reason)
        {
            if (!cameraViewLeases.TryGetValue(leaseId ?? string.Empty, out CameraViewLeaseRuntime lease) || lease.IsReleased)
                return CameraViewLeaseMissing(leaseId, "released-or-missing", "Camera view lease is already released or missing.");

            CameraViewRequest request = CloneRequest(lease.Request);
            request.ViewScale = viewScale;
            return UpdateLease(lease.LeaseId, request, reason);
        }

        private CameraViewResult ReleaseLease(string leaseId, string reason)
        {
            if (!cameraViewLeases.TryGetValue(leaseId ?? string.Empty, out CameraViewLeaseRuntime lease))
                return CameraViewLeaseMissing(leaseId, "released-or-missing", "Camera view lease is already released or missing.");

            lease.IsReleased = true;
            lease.UpdatedOrder = ++cameraViewLeaseSequence;
            cameraViewLeases.Remove(lease.LeaseId);

            CameraViewResult result = ApplyCameraViewTarget("lease release " + (reason ?? string.Empty));
            result.OwnerId = lease.OwnerId;
            result.LeaseId = lease.LeaseId;
            lease.LastResult = CloneResult(result);
            runtime.RuntimeMonitor.Log("CameraView lease released owner=" + lease.OwnerId + " lease=" + lease.LeaseId + " success=" + result.Success + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            return result;
        }

        private CameraViewResult ApplyCameraViewTarget(string reason)
        {
            CameraViewTarget target = ComputeCameraViewTarget();
            var result = new CameraViewResult
            {
                Success = false,
                RequestedViewScale = target.RequestedScale,
                ClampedViewScale = target.Scale,
                BeforeViewScale = cameraViewCurrentViewScale <= 0 ? 1d : cameraViewCurrentViewScale,
                AfterViewScale = target.Scale,
                AppliedViewScale = cameraViewCurrentViewScale <= 0 ? 1d : cameraViewCurrentViewScale,
                ActiveOwnerId = target.OwnerId,
                ActiveLeaseId = target.LeaseId,
                ArbitrationStatus = target.ArbitrationStatus,
                NativeRefreshStatus = "not-called-playable",
                UiScaleStatus = "unchanged",
                LifecycleStatus = BuildCameraViewLifecycleStatus(reason, target.Scale <= 1.0001d)
            };

            if (!TryReadCameraOrthographicSize(out double currentSize, out string readMessage))
            {
                result.FailureReason = "missing-camera";
                result.Message = readMessage;
                result.CameraOwnerStatus = "missing-main-camera";
                UpdateCameraViewGlobalState(result, cameraAvailable: false, vanillaSize: cameraViewVanillaOrthographicSize, appliedSize: cameraViewAppliedOrthographicSize);
                diagnostics.SetViewPending(readMessage);
                ViewApplied?.Invoke(result);
                return result;
            }

            if (cameraViewVanillaOrthographicSize <= 0 ||
                (result.BeforeViewScale <= 1.0001d && target.Scale > 1.0001d) ||
                (result.BeforeViewScale <= 1.0001d && target.Scale <= 1.0001d))
                cameraViewVanillaOrthographicSize = currentSize > 0 ? currentSize : cameraViewVanillaOrthographicSize;
            if (cameraViewVanillaOrthographicSize <= 0)
                cameraViewVanillaOrthographicSize = currentSize;

            double targetSize = target.Scale <= 1.0001d
                ? cameraViewVanillaOrthographicSize
                : Math.Max(0.01d, cameraViewVanillaOrthographicSize * target.Scale);
            string writeMessage = string.Empty;
            if (Math.Abs(currentSize - targetSize) > 0.01d && !TryWriteCameraOrthographicSize(targetSize, out writeMessage))
            {
                result.FailureReason = "camera-write-failed";
                result.Message = writeMessage;
                result.CameraOwnerStatus = "orthographic-write-failed";
                UpdateCameraViewGlobalState(result, cameraAvailable: true, vanillaSize: cameraViewVanillaOrthographicSize, appliedSize: currentSize);
                diagnostics.SetViewFailed(writeMessage);
                ViewApplied?.Invoke(result);
                return result;
            }

            result.Success = true;
            result.AppliedViewScale = target.Scale;
            result.VanillaOrthographicSize = cameraViewVanillaOrthographicSize;
            result.AppliedOrthographicSize = targetSize;
            result.CameraOwnerStatus = Math.Abs(currentSize - targetSize) > 0.01d
                ? "orthographic-size " + currentSize.ToString("0.###", CultureInfo.InvariantCulture) + "->" + targetSize.ToString("0.###", CultureInfo.InvariantCulture)
                : "orthographic-size unchanged " + targetSize.ToString("0.###", CultureInfo.InvariantCulture);
            result.Message = "Playable camera view " + currentSize.ToString("0.###", CultureInfo.InvariantCulture) + "->" + targetSize.ToString("0.###", CultureInfo.InvariantCulture) +
                " viewScale=" + target.Scale.ToString("0.##", CultureInfo.InvariantCulture) +
                " activeOwner=" + FirstText(target.OwnerId, "none") +
                " activeLease=" + FirstText(target.LeaseId, "none") +
                " arbitration=" + target.ArbitrationStatus +
                " nativeRefresh=not-called-playable uiScale=unchanged reason=" + (reason ?? string.Empty) + ".";
            UpdateCameraViewGlobalState(result, cameraAvailable: true, vanillaSize: cameraViewVanillaOrthographicSize, appliedSize: targetSize);
            diagnostics.SetViewApplied(result);
            ViewApplied?.Invoke(result);
            return result;
        }

        private void UpdateCameraViewGlobalState(CameraViewResult result, bool cameraAvailable, double vanillaSize, double appliedSize)
        {
            cameraViewCurrentViewScale = result.Success ? result.AppliedViewScale : cameraViewCurrentViewScale;
            if (result.Success && result.AppliedViewScale <= 1.0001d)
                cameraViewCurrentViewScale = 1d;
            cameraViewAppliedOrthographicSize = appliedSize;
            cameraViewActiveOwnerId = result.ActiveOwnerId;
            cameraViewActiveLeaseId = result.ActiveLeaseId;
            cameraViewArbitrationStatus = result.ArbitrationStatus;
            cameraViewCameraOwnerStatus = result.CameraOwnerStatus;
            cameraViewNativeRefreshStatus = result.NativeRefreshStatus;
            cameraViewLifecycleStatus = result.LifecycleStatus;
            cameraViewUiScaleStatus = result.UiScaleStatus;

            foreach (CameraViewLeaseRuntime lease in cameraViewLeases.Values.ToArray())
            {
                CameraViewResult leaseResult = CloneResult(result);
                leaseResult.OwnerId = lease.OwnerId;
                leaseResult.LeaseId = lease.LeaseId;
                lease.LastResult = leaseResult;
            }

            CameraViewRoomSnapshot room = GetCameraViewRoomSnapshot();
            string message = result.Message ?? string.Empty;
            foreach (CameraViewLeaseRuntime lease in cameraViewLeases.Values.ToArray())
            {
                lease.LastState = BuildCameraViewState(lease.OwnerId, lease, room, cameraAvailable, vanillaSize, appliedSize, message);
            }
        }

        private CameraViewTarget ComputeCameraViewTarget()
        {
            CameraViewTarget target = new CameraViewTarget
            {
                Scale = 1d,
                RequestedScale = 1d,
                ArbitrationStatus = "vanilla-no-active-lease"
            };

            foreach (CameraViewLeaseRuntime lease in cameraViewLeases.Values)
            {
                if (lease.IsReleased || !lease.Request.Enabled)
                    continue;

                double requested = NormalizeScale(lease.Request.ViewScale, 1d);
                double clamped = ClampDouble(requested, lease.Request.MinViewScale, lease.Request.MaxViewScale);
                if (clamped <= 1.0001d)
                    continue;

                if (target.Lease == null ||
                    lease.Request.Priority > target.Priority ||
                    (lease.Request.Priority == target.Priority && lease.UpdatedOrder > target.Order))
                {
                    target.Lease = lease;
                    target.LeaseId = lease.LeaseId;
                    target.OwnerId = lease.OwnerId;
                    target.RequestedScale = requested;
                    target.Scale = clamped;
                    target.Priority = lease.Request.Priority;
                    target.Order = lease.UpdatedOrder;
                    target.ArbitrationStatus = "active-highest-priority-latest";
                }
            }

            return target;
        }

        private CameraViewState GetCameraViewState(string uniqueId)
        {
            uniqueId ??= string.Empty;
            CameraViewRoomSnapshot room = GetCameraViewRoomSnapshot();
            bool cameraAvailable = TryGetMainCameraObject() != null;
            if (cameraViewLeases.TryGetValue(uniqueId, out CameraViewLeaseRuntime lease))
                return BuildCameraViewState(lease.OwnerId, lease, room, cameraAvailable, cameraViewVanillaOrthographicSize, cameraViewAppliedOrthographicSize, lease.LastResult.Message);

            CameraViewLeaseRuntime? ownerLease = cameraViewLeases.Values
                .Where(l => !l.IsReleased && l.OwnerId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.LeaseId.Equals(cameraViewActiveLeaseId, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(l => l.Request.Priority)
                .ThenByDescending(l => l.UpdatedOrder)
                .FirstOrDefault();
            return BuildCameraViewState(uniqueId, ownerLease, room, cameraAvailable, cameraViewVanillaOrthographicSize, cameraViewAppliedOrthographicSize, ownerLease?.LastResult.Message ?? "No camera view lease registered.");
        }

        private CameraViewState BuildCameraViewState(string ownerId, CameraViewLeaseRuntime? lease, CameraViewRoomSnapshot room, bool cameraAvailable, double vanillaSize, double appliedSize, string message)
        {
            CameraViewRequest request = lease?.Request ?? new CameraViewRequest { Enabled = false };
            double requested = lease == null ? 1d : NormalizeScale(request.ViewScale, 1d);
            double clamped = lease == null ? 1d : ClampDouble(requested, request.MinViewScale, request.MaxViewScale);
            bool isActive = lease != null && lease.LeaseId.Equals(cameraViewActiveLeaseId, StringComparison.OrdinalIgnoreCase);
            int leaseCount = cameraViewLeases.Values.Count(l => !l.IsReleased && l.OwnerId.Equals(ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            return new CameraViewState
            {
                OwnerId = ownerId ?? string.Empty,
                LeaseId = lease?.LeaseId ?? string.Empty,
                LeaseName = request.LeaseName ?? string.Empty,
                IsConfigured = lease != null,
                Enabled = lease != null && request.Enabled,
                IsReleased = lease?.IsReleased ?? false,
                Priority = request.Priority,
                LeaseCount = leaseCount,
                ActiveOwnerId = cameraViewActiveOwnerId,
                ActiveLeaseId = cameraViewActiveLeaseId,
                ArbitrationStatus = cameraViewArbitrationStatus,
                MinViewScale = request.MinViewScale,
                MaxViewScale = request.MaxViewScale,
                Step = request.Step,
                RequestedViewScale = requested,
                ClampedViewScale = clamped,
                CurrentViewScale = lease != null && request.Enabled ? clamped : 1d,
                AppliedViewScale = cameraViewCurrentViewScale <= 0 ? 1d : cameraViewCurrentViewScale,
                VanillaOrthographicSize = vanillaSize,
                AppliedOrthographicSize = appliedSize,
                CameraAvailable = cameraAvailable,
                CameraOwnerStatus = cameraViewCameraOwnerStatus,
                NativeRefreshStatus = cameraViewNativeRefreshStatus,
                UiScaleStatus = cameraViewUiScaleStatus,
                LifecycleStatus = cameraViewLifecycleStatus,
                CurrentRoomId = room.RoomId,
                CurrentRoomTitle = room.RoomTitle,
                CurrentRoomShowsBackground = room.ShouldShowBackground,
                Status = lease == null ? "not-configured" : lease.IsReleased ? "released" : !request.Enabled ? "disabled" : isActive ? "active" : clamped > 1.0001d ? "waiting-arbitration" : "vanilla",
                LastMessage = message ?? string.Empty
            };
        }

        private static CameraViewRequest NormalizeCameraViewRequest(CameraViewRequest? request)
        {
            request ??= new CameraViewRequest();
            double min = ClampDouble(request.MinViewScale <= 0 ? 1d : request.MinViewScale, 1d, 16d);
            double max = ClampDouble(request.MaxViewScale <= 0 ? 4d : request.MaxViewScale, min, 16d);
            double step = ClampDouble(request.Step <= 0 ? 0.25d : request.Step, 0.05d, Math.Max(0.05d, max - min));
            double viewScale = ClampDouble(NormalizeScale(request.ViewScale, 1d), min, max);
            return new CameraViewRequest
            {
                Enabled = request.Enabled,
                ViewScale = viewScale,
                MinViewScale = min,
                MaxViewScale = max,
                Step = step,
                Priority = request.Priority,
                LeaseName = (request.LeaseName ?? string.Empty).Trim(),
                VerboseLogging = request.VerboseLogging
            };
        }

        private static CameraViewRequest CloneRequest(CameraViewRequest request)
        {
            request ??= new CameraViewRequest();
            return new CameraViewRequest
            {
                Enabled = request.Enabled,
                ViewScale = request.ViewScale,
                MinViewScale = request.MinViewScale,
                MaxViewScale = request.MaxViewScale,
                Step = request.Step,
                Priority = request.Priority,
                LeaseName = request.LeaseName ?? string.Empty,
                VerboseLogging = request.VerboseLogging
            };
        }

        private static CameraViewState CloneState(CameraViewState state)
        {
            return new CameraViewState
            {
                OwnerId = state.OwnerId,
                LeaseId = state.LeaseId,
                LeaseName = state.LeaseName,
                IsConfigured = state.IsConfigured,
                Enabled = state.Enabled,
                IsReleased = state.IsReleased,
                Priority = state.Priority,
                LeaseCount = state.LeaseCount,
                ActiveOwnerId = state.ActiveOwnerId,
                ActiveLeaseId = state.ActiveLeaseId,
                ArbitrationStatus = state.ArbitrationStatus,
                MinViewScale = state.MinViewScale,
                MaxViewScale = state.MaxViewScale,
                Step = state.Step,
                RequestedViewScale = state.RequestedViewScale,
                ClampedViewScale = state.ClampedViewScale,
                CurrentViewScale = state.CurrentViewScale,
                AppliedViewScale = state.AppliedViewScale,
                VanillaOrthographicSize = state.VanillaOrthographicSize,
                AppliedOrthographicSize = state.AppliedOrthographicSize,
                CameraAvailable = state.CameraAvailable,
                CameraOwnerStatus = state.CameraOwnerStatus,
                NativeRefreshStatus = state.NativeRefreshStatus,
                UiScaleStatus = state.UiScaleStatus,
                LifecycleStatus = state.LifecycleStatus,
                CurrentRoomId = state.CurrentRoomId,
                CurrentRoomTitle = state.CurrentRoomTitle,
                CurrentRoomShowsBackground = state.CurrentRoomShowsBackground,
                Status = state.Status,
                LastMessage = state.LastMessage
            };
        }

        private static double NormalizeScale(double value, double fallback)
        {
            return double.IsNaN(value) || double.IsInfinity(value) || value <= 0 ? fallback : value;
        }

        private static CameraViewResult CameraViewLeaseMissing(string? leaseId, string reason, string message)
        {
            return new CameraViewResult
            {
                Success = false,
                LeaseId = leaseId ?? string.Empty,
                FailureReason = reason,
                Message = message,
                NativeRefreshStatus = "not-called-playable",
                UiScaleStatus = "unchanged"
            };
        }

        private static object? TryGetMainCameraObject()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? camera = ReadStaticMember(dolocApi, "mainCamera");
            if (camera != null)
                return camera;

            Type? cameraType = ResolveType("UnityEngine.Camera, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Camera, UnityEngine");
            return ReadStaticMember(cameraType, "main");
        }

        private static bool TryReadCameraOrthographicSize(out double size, out string message)
        {
            size = 0;
            object? camera = TryGetMainCameraObject();
            if (camera == null)
            {
                message = "Main Unity camera is not available yet.";
                return false;
            }

            object? value = ReadMember(camera, "orthographicSize");
            if (value == null)
            {
                message = "Camera.orthographicSize is not readable.";
                return false;
            }

            try
            {
                size = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                message = string.Empty;
                return size > 0;
            }
            catch (Exception ex)
            {
                message = "Camera.orthographicSize conversion failed: " + ex.GetType().Name + ".";
                return false;
            }
        }

        private static bool TryWriteCameraOrthographicSize(double size, out string message)
        {
            object? camera = TryGetMainCameraObject();
            if (camera == null)
            {
                message = "Main Unity camera is not available yet.";
                return false;
            }

            for (Type? type = camera.GetType(); type != null; type = type.BaseType)
            {
                PropertyInfo? property = type.GetProperty("orthographicSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        object value = property.PropertyType == typeof(float) ? (object)(float)size : Convert.ChangeType(size, property.PropertyType, CultureInfo.InvariantCulture);
                        property.SetValue(camera, value, null);
                        message = string.Empty;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        message = "Camera.orthographicSize property write failed: " + ex.GetType().Name + ".";
                        return false;
                    }
                }

                FieldInfo? field = type.GetField("orthographicSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        object value = field.FieldType == typeof(float) ? (object)(float)size : Convert.ChangeType(size, field.FieldType, CultureInfo.InvariantCulture);
                        field.SetValue(camera, value);
                        message = string.Empty;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        message = "Camera.orthographicSize field write failed: " + ex.GetType().Name + ".";
                        return false;
                    }
                }
            }

            message = "Camera.orthographicSize is not writable.";
            return false;
        }

        private static CameraViewRoomSnapshot GetCameraViewRoomSnapshot()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            if (room == null)
                return new CameraViewRoomSnapshot();

            return new CameraViewRoomSnapshot
            {
                RoomId = FirstText(ReadStringMember(room, "RoomId"), ReadStringMember(room, "roomId"), room.GetType().Name),
                RoomTitle = ReadStringMember(room, "Title"),
                ShouldShowBackground = ReadBoolMember(room, "ShouldShowBackground", false)
            };
        }

        private string BuildCameraViewLifecycleStatus(string reason, bool restored)
        {
            reason ??= string.Empty;
            if (reason.IndexOf("lifecycle", StringComparison.OrdinalIgnoreCase) >= 0)
                return restored ? "restored:" + reason : "restore-requested:" + reason;
            if (reason.IndexOf("ReturnedToTitle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("SaveLoaded", StringComparison.OrdinalIgnoreCase) >= 0)
                return restored ? "restored:" + reason : "restore-requested:" + reason;
            if (reason.IndexOf("environment reset", StringComparison.OrdinalIgnoreCase) >= 0)
                return "reapplied-orthographic-only:" + reason;
            return restored ? "vanilla" : cameraViewCurrentViewScale > 1.0001d ? "active" : "not-needed";
        }

        private bool IsLeaseReleased(string leaseId)
        {
            return !cameraViewLeases.TryGetValue(leaseId ?? string.Empty, out CameraViewLeaseRuntime lease) || lease.IsReleased;
        }

        private CameraViewResult GetLeaseLastResult(string leaseId)
        {
            return cameraViewLeases.TryGetValue(leaseId ?? string.Empty, out CameraViewLeaseRuntime lease)
                ? CloneResult(lease.LastResult)
                : CameraViewLeaseMissing(leaseId, "released-or-missing", "Camera view lease is already released or missing.");
        }

        private sealed class CameraViewLeaseRuntime
        {
            public string LeaseId { get; set; } = string.Empty;
            public string OwnerId { get; set; } = string.Empty;
            public CameraViewRequest Request { get; set; } = new CameraViewRequest();
            public long CreatedOrder { get; set; }
            public long UpdatedOrder { get; set; }
            public bool IsReleased { get; set; }
            public CameraViewResult LastResult { get; set; } = new CameraViewResult();
            public CameraViewState LastState { get; set; } = new CameraViewState();
        }

        private sealed class CameraViewTarget
        {
            public CameraViewLeaseRuntime? Lease { get; set; }
            public string LeaseId { get; set; } = string.Empty;
            public string OwnerId { get; set; } = string.Empty;
            public double RequestedScale { get; set; } = 1d;
            public double Scale { get; set; } = 1d;
            public int Priority { get; set; }
            public long Order { get; set; }
            public string ArbitrationStatus { get; set; } = string.Empty;
        }

        private sealed class CameraViewRoomSnapshot
        {
            public string RoomId { get; set; } = string.Empty;
            public string RoomTitle { get; set; } = string.Empty;
            public bool ShouldShowBackground { get; set; }
        }

        private sealed class CameraViewLeaseHandle : ICameraViewLease
        {
            private readonly CameraViewService service;

            public CameraViewLeaseHandle(CameraViewService service, string ownerId, string leaseId)
            {
                this.service = service;
                OwnerId = ownerId ?? string.Empty;
                LeaseId = leaseId ?? string.Empty;
            }

            public string LeaseId { get; }
            public string OwnerId { get; }
            public bool IsReleased => service.IsLeaseReleased(LeaseId);
            public CameraViewResult LastResult => service.GetLeaseLastResult(LeaseId);
            public CameraViewResult SetViewScale(double viewScale, string reason) => service.SetLeaseScale(LeaseId, viewScale, reason);
            public CameraViewResult Update(CameraViewRequest request, string reason) => service.UpdateLease(LeaseId, request, reason);
            public CameraViewResult Release(string reason) => service.ReleaseLease(LeaseId, reason);
            public CameraViewState GetState() => service.GetState(LeaseId);
            public void Dispose()
            {
                if (!IsReleased)
                    Release("Dispose");
            }
        }
    }
}
