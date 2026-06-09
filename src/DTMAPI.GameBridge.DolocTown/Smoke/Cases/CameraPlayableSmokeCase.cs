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
using System.Text;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private SmokeAttemptResult TryExerciseZoomForSmoke()
        {
            try
            {
                if (cameraFeature == null)
                    throw new InvalidOperationException("Camera feature is not available.");

                ICameraViewApi cameraViewApi = cameraFeature.ViewApi;
                if (zoomSmokeRun == null)
                {
                    ManifestModel owner = CreateZoomSmokeManifest();
                    string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "CAMERA-PLAYABLE", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(evidenceDir);
                    zoomSmokeRun = new ZoomSmokeRun
                    {
                        Owner = owner,
                        CompetingOwner = CreateCameraViewCompetingSmokeManifest(),
                        EvidenceDir = evidenceDir,
                        Before = cameraViewApi.GetState(owner.UniqueID),
                        BeforeScreenshot = Path.Combine(evidenceDir, "zoom-before.png"),
                        Stage = 1,
                        StageAt = DateTimeOffset.Now
                    };
                    zoomSmokeRun.BeforeScreenshotRequested = TryCaptureScreenshot(zoomSmokeRun.BeforeScreenshot);
                    runtime.RuntimeMonitor.Log("Smoke CameraPlayable before screenshot requested=" + zoomSmokeRun.BeforeScreenshotRequested + " path=" + zoomSmokeRun.BeforeScreenshot + ".");
                    return SmokeAttemptResult.Pending;
                }

                ZoomSmokeRun run = zoomSmokeRun;
                if (run.Stage == 1)
                {
                    if (!WaitForZoomScreenshot(run.BeforeScreenshot, "before", run.BeforeScreenshotRequested, run.StageAt))
                        return SmokeAttemptResult.Pending;

                    run.CompetingLease = cameraViewApi.AcquireLease(run.CompetingOwner, new CameraViewRequest
                    {
                        Enabled = true,
                        ViewScale = 2,
                        MinViewScale = 1,
                        MaxViewScale = 4,
                        Step = 1,
                        Priority = 0,
                        LeaseName = "smoke lower-priority playable view",
                        VerboseLogging = true
                    });
                    run.PrimaryLease = cameraViewApi.AcquireLease(run.Owner, new CameraViewRequest
                    {
                        Enabled = true,
                        ViewScale = 4,
                        MinViewScale = 1,
                        MaxViewScale = 4,
                        Step = 1,
                        Priority = 10,
                        LeaseName = "smoke high-priority playable view",
                        VerboseLogging = true
                    });
                    run.MaxResult = run.PrimaryLease.LastResult;
                    UpdateRuntimeAutomation();
                    run.Max = run.PrimaryLease.GetState();
                    ValidateZoomMax(run.MaxResult, run.Max);
                    run.MaxScreenshot = Path.Combine(run.EvidenceDir, "zoom-4x.png");
                    run.MaxScreenshotRequested = TryCaptureScreenshot(run.MaxScreenshot);
                    run.Stage = 2;
                    run.StageAt = DateTimeOffset.Now;
                    runtime.RuntimeMonitor.Log("Smoke CameraPlayable 4x screenshot requested=" + run.MaxScreenshotRequested + " path=" + run.MaxScreenshot + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (run.Stage == 2)
                {
                    if (!WaitForZoomScreenshot(run.MaxScreenshot, "4x", run.MaxScreenshotRequested, run.StageAt))
                        return SmokeAttemptResult.Pending;

                    if (run.Dynamic4x == null)
                    {
                        if (run.PrimaryLease == null)
                            throw new InvalidOperationException("CameraView 4x smoke lease was not available for dynamic movement.");
                        StartZoomDynamicPhase(run, "4x", 4d, run.PrimaryLease);
                        return SmokeAttemptResult.Pending;
                    }

                    if (!RunZoomDynamicPhase(run, run.Dynamic4x))
                        return SmokeAttemptResult.Pending;
                    ValidateZoomDynamic(run.Dynamic4x, run.Owner.UniqueID, 4d);

                    if (run.PrimaryLease == null || run.CompetingLease == null)
                        throw new InvalidOperationException("CameraView smoke leases were not available for arbitration fallback.");

                    run.FallbackResult = run.PrimaryLease.Release("smoke release high-priority lease");
                    UpdateRuntimeAutomation();
                    run.Fallback = run.CompetingLease.GetState();
                    ValidateZoomFallback(run.FallbackResult, run.Fallback);

                    StartZoomDynamicPhase(run, "2x", 2d, run.CompetingLease);
                    run.Stage = 3;
                    run.StageAt = DateTimeOffset.Now;
                    return SmokeAttemptResult.Pending;
                }

                if (run.Stage == 3)
                {
                    if (run.Dynamic2x == null)
                    {
                        if (run.CompetingLease == null)
                            throw new InvalidOperationException("CameraView 2x fallback smoke lease was not available for dynamic movement.");
                        StartZoomDynamicPhase(run, "2x", 2d, run.CompetingLease);
                        return SmokeAttemptResult.Pending;
                    }

                    if (!RunZoomDynamicPhase(run, run.Dynamic2x))
                        return SmokeAttemptResult.Pending;
                    ValidateZoomDynamic(run.Dynamic2x, run.CompetingOwner.UniqueID, 2d);

                    if (run.CompetingLease == null)
                        throw new InvalidOperationException("CameraView fallback smoke lease was not available for reset.");
                    run.ResetResult = run.CompetingLease.Release("smoke release lower-priority lease");
                    UpdateRuntimeAutomation();
                    TryRestoreZoomSmokePlayerPosition(run);
                    run.After = cameraViewApi.GetState(run.Owner.UniqueID);
                    ValidateZoomReset(run.ResetResult, run.After);
                    run.ResetScreenshot = Path.Combine(run.EvidenceDir, "zoom-reset.png");
                    run.ResetScreenshotRequested = TryCaptureScreenshot(run.ResetScreenshot);
                    run.Stage = 4;
                    run.StageAt = DateTimeOffset.Now;
                    runtime.RuntimeMonitor.Log("Smoke CameraPlayable reset screenshot requested=" + run.ResetScreenshotRequested + " path=" + run.ResetScreenshot + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (run.Stage == 4)
                {
                    if (!WaitForZoomScreenshot(run.ResetScreenshot, "reset", run.ResetScreenshotRequested, run.StageAt))
                        return SmokeAttemptResult.Pending;

                    WriteZoomDynamicTelemetry(run);
                    string dynamic4x = FormatZoomDynamicSummary(run.Dynamic4x);
                    string dynamic2x = FormatZoomDynamicSummary(run.Dynamic2x);
                    string summary = "before={" + FormatZoomState(run.Before) + "}, max={" + FormatZoomState(run.Max) + "}, dynamic4x={" + dynamic4x + "}, fallback={" + FormatZoomState(run.Fallback) + "}, dynamic2x={" + dynamic2x + "}, reset={" + FormatZoomState(run.After) + "}, apply={" + run.MaxResult?.Message + "}, fallbackResult={" + run.FallbackResult?.Message + "}, restore={" + run.ResetResult?.Message + "}, screenshots=" + run.EvidenceDir + ", telemetry=" + run.TelemetryPath;
                    File.WriteAllText(Path.Combine(run.EvidenceDir, "summary.txt"),
                        "Before=" + FormatZoomState(run.Before) + Environment.NewLine +
                        "Max=" + FormatZoomState(run.Max) + Environment.NewLine +
                        "Dynamic4x=" + dynamic4x + Environment.NewLine +
                        "Fallback=" + FormatZoomState(run.Fallback) + Environment.NewLine +
                        "Dynamic2x=" + dynamic2x + Environment.NewLine +
                        "Reset=" + FormatZoomState(run.After) + Environment.NewLine +
                        "Apply=" + run.MaxResult?.Message + Environment.NewLine +
                        "FallbackResult=" + run.FallbackResult?.Message + Environment.NewLine +
                        "Restore=" + run.ResetResult?.Message + Environment.NewLine +
                        "BeforeScreenshot=" + run.BeforeScreenshot + Environment.NewLine +
                        "MaxScreenshot=" + run.MaxScreenshot + Environment.NewLine +
                        "Dynamic4xStartScreenshot=" + run.Dynamic4x?.StartScreenshot + Environment.NewLine +
                        "Dynamic4xMidScreenshot=" + run.Dynamic4x?.MidScreenshot + Environment.NewLine +
                        "Dynamic4xEndScreenshot=" + run.Dynamic4x?.EndScreenshot + Environment.NewLine +
                        "Dynamic2xStartScreenshot=" + run.Dynamic2x?.StartScreenshot + Environment.NewLine +
                        "Dynamic2xMidScreenshot=" + run.Dynamic2x?.MidScreenshot + Environment.NewLine +
                        "Dynamic2xEndScreenshot=" + run.Dynamic2x?.EndScreenshot + Environment.NewLine +
                        "ResetScreenshot=" + run.ResetScreenshot + Environment.NewLine);
                    runtime.RuntimeMonitor.Log("Smoke exercise CameraPlayable OK " + summary);
                    runtime.SetHookStatus("Smoke.CameraPlayable", "verified", "ICameraViewApi -> DolocAPI.mainCamera.orthographicSize", summary);
                    runtime.SetHookStatus("Smoke.Zoom", "verified", "AutoExerciseZoom compatibility flag", "AutoExerciseZoom now verifies Smoke.CameraPlayable.");
                    if (!TryVerifyDiagnosticsSnapshotForSmoke("Camera", "Camera"))
                    {
                        zoomSmokeRun = null;
                        return SmokeAttemptResult.Failed;
                    }
                    zoomSmokeRun = null;
                    return SmokeAttemptResult.Succeeded;
                }

                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                TryWriteZoomDynamicTelemetryAfterFailure(zoomSmokeRun);
                if (cameraFeature != null)
                    TryRestoreZoomSmokeAfterFailure();
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke zoom exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.CameraPlayable", "failed", "ICameraViewApi", ex.GetType().Name + ": " + ex.Message);
                runtime.SetHookStatus("Smoke.Zoom", "failed", "AutoExerciseZoom compatibility flag", ex.GetType().Name + ": " + ex.Message);
                zoomSmokeRun = null;
                return SmokeAttemptResult.Failed;
            }
        }

        private bool WaitForZoomScreenshot(string path, string label, bool requested, DateTimeOffset requestedAt)
        {
            if (ScreenshotFileReady(path))
                return true;
            if ((DateTimeOffset.Now - requestedAt).TotalSeconds < 5)
                return false;
            throw new InvalidOperationException("Zoom " + label + " screenshot was not captured. requested=" + requested + " path=" + path);
        }

        private static bool ScreenshotFileReady(string path)
        {
            try
            {
                var file = new FileInfo(path);
                return file.Exists && file.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void StartZoomDynamicPhase(ZoomSmokeRun run, string label, double expectedScale, ICameraViewLease lease)
        {
            object? currentPosition = ReadCurrentPlayerPositionForSmoke();
            if (!TryReadVectorCoordinates(currentPosition, out double originX, out double originY, out double originZ))
                throw new InvalidOperationException("Could not read DolocAPI.AgentPosition before CameraPlayable dynamic " + label + " smoke.");

            if (!run.OriginalPlayerPositionCaptured)
            {
                run.OriginalPlayerPositionCaptured = true;
                run.OriginalPlayerX = originX;
                run.OriginalPlayerY = originY;
                run.OriginalPlayerZ = originZ;
            }

            var dynamic = new ZoomDynamicRun
            {
                Label = label,
                ExpectedScale = expectedScale,
                ExpectedOwnerId = lease.OwnerId,
                ExpectedLeaseId = lease.LeaseId,
                Lease = lease,
                OriginX = originX,
                OriginY = originY,
                OriginZ = originZ,
                MoveDirection = label.Equals("4x", StringComparison.OrdinalIgnoreCase) ? 1d : -1d,
                StartedAt = DateTimeOffset.Now,
                LastSampleAt = DateTimeOffset.MinValue,
                LastMoveAt = DateTimeOffset.MinValue,
                StartScreenshot = Path.Combine(run.EvidenceDir, "dynamic-" + label + "-start.png"),
                MidScreenshot = Path.Combine(run.EvidenceDir, "dynamic-" + label + "-mid.png"),
                EndScreenshot = Path.Combine(run.EvidenceDir, "dynamic-" + label + "-end.png")
            };

            RecordZoomDynamicSample(dynamic, "start", 0d);
            RequestZoomDynamicScreenshot(dynamic, "start");
            if (label.Equals("4x", StringComparison.OrdinalIgnoreCase))
                run.Dynamic4x = dynamic;
            else
                run.Dynamic2x = dynamic;

            runtime.RuntimeMonitor.Log("Smoke CameraPlayable dynamic " + label + " started durationSeconds=" + FormatSmokeDouble(dynamic.DurationSeconds) + ", owner=" + dynamic.ExpectedOwnerId + ", lease=" + dynamic.ExpectedLeaseId + ", origin=" + FormatSmokeDouble(originX) + "," + FormatSmokeDouble(originY) + "," + FormatSmokeDouble(originZ) + ", movement=smoke-agentposition-camera-setposition.");
        }

        private bool RunZoomDynamicPhase(ZoomSmokeRun run, ZoomDynamicRun dynamic)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            double elapsed = Math.Max(0d, (now - dynamic.StartedAt).TotalSeconds);

            if (elapsed < dynamic.DurationSeconds)
            {
                MoveZoomSmokePlayer(dynamic, elapsed);
                if (!dynamic.MidScreenshotRequested && elapsed >= dynamic.DurationSeconds * 0.5d)
                {
                    RecordZoomDynamicSample(dynamic, "mid", elapsed);
                    RequestZoomDynamicScreenshot(dynamic, "mid");
                }
                else if ((now - dynamic.LastSampleAt).TotalSeconds >= 1d)
                {
                    RecordZoomDynamicSample(dynamic, "sample", elapsed);
                }
                return false;
            }

            MoveZoomSmokePlayer(dynamic, dynamic.DurationSeconds);
            if (!dynamic.EndScreenshotRequested)
            {
                dynamic.CompletedAt = now;
                RecordZoomDynamicSample(dynamic, "end", dynamic.DurationSeconds);
                RequestZoomDynamicScreenshot(dynamic, "end");
                runtime.RuntimeMonitor.Log("Smoke CameraPlayable dynamic " + dynamic.Label + " completed durationSeconds=" + FormatSmokeDouble((dynamic.CompletedAt - dynamic.StartedAt).TotalSeconds) + ", samples=" + dynamic.Samples.Count + ", playerDistance=" + FormatSmokeDouble(CalculateTelemetryDistance(dynamic.Samples, true)) + ", cameraDistance=" + FormatSmokeDouble(CalculateTelemetryDistance(dynamic.Samples, false)) + ".");
            }

            return WaitForZoomDynamicScreenshots(dynamic);
        }

        private void MoveZoomSmokePlayer(ZoomDynamicRun dynamic, double elapsedSeconds)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            if ((now - dynamic.LastMoveAt).TotalMilliseconds < 100d && elapsedSeconds < dynamic.DurationSeconds)
                return;

            double progress = ClampSmoke(elapsedSeconds / Math.Max(1d, dynamic.DurationSeconds), 0d, 1d);
            double x = dynamic.OriginX + 96d * dynamic.MoveDirection * progress;
            double y = dynamic.OriginY + 24d * Math.Sin(progress * Math.PI * 2d) + 36d * dynamic.MoveDirection * progress;
            double z = dynamic.OriginZ;
            if (!TrySetDolocApiAgentPosition(x, y, z, out string agentMessage))
                throw new InvalidOperationException("Could not move DolocAPI.AgentPosition for CameraPlayable dynamic " + dynamic.Label + " smoke: " + agentMessage);
            if (!TrySetSmokeCameraPositionToPlayer(x, y, out string cameraMessage))
                throw new InvalidOperationException("Could not apply CameraController.SetPosition for CameraPlayable dynamic " + dynamic.Label + " smoke: " + cameraMessage);

            dynamic.LastMoveAt = now;
            dynamic.LastTargetX = x;
            dynamic.LastTargetY = y;
            dynamic.LastTargetZ = z;
        }

        private void RecordZoomDynamicSample(ZoomDynamicRun dynamic, string marker, double elapsedSeconds)
        {
            CameraViewState state = dynamic.Lease?.GetState() ?? new CameraViewState();
            CameraPlayableTelemetrySample sample = CaptureCameraPlayableTelemetrySample(dynamic, marker, elapsedSeconds, state);
            dynamic.Samples.Add(sample);
            dynamic.LastSampleAt = DateTimeOffset.Now;
            if (marker.Equals("start", StringComparison.OrdinalIgnoreCase))
                dynamic.StartSample = sample;
            else if (marker.Equals("mid", StringComparison.OrdinalIgnoreCase))
                dynamic.MidSample = sample;
            else if (marker.Equals("end", StringComparison.OrdinalIgnoreCase))
                dynamic.EndSample = sample;
        }

        private CameraPlayableTelemetrySample CaptureCameraPlayableTelemetrySample(ZoomDynamicRun dynamic, string marker, double elapsedSeconds, CameraViewState state)
        {
            Type? dolocApi = patcher?.ResolveType("DolocAPI, Assembly-CSharp");
            object? playerPosition = ReadStaticMember(dolocApi, "AgentPosition");
            TryReadVectorCoordinates(playerPosition, out double playerX, out double playerY, out double playerZ);

            object? cameraController = ReadStaticMember(dolocApi, "cameraController");
            object? controllerPosition = cameraController == null ? null : ReadMember(cameraController, "position2d");
            bool cameraPositionReadable = TryReadVectorCoordinates(controllerPosition, out double cameraX, out double cameraY, out double cameraZ);

            object? camera = ReadStaticMember(dolocApi, "mainCamera") ?? ReadUnityMainCameraForSmoke();
            object? cameraTransform = camera == null ? null : ReadMember(camera, "transform");
            object? cameraPosition = cameraTransform == null ? null : ReadMember(cameraTransform, "position");
            if (!cameraPositionReadable)
                TryReadVectorCoordinates(cameraPosition, out cameraX, out cameraY, out cameraZ);
            double orthographicSize = camera == null ? double.NaN : ReadDoubleMember(camera, "orthographicSize", double.NaN);

            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            string roomId = state.CurrentRoomId;
            string roomTitle = state.CurrentRoomTitle;
            if (room != null)
            {
                roomId = FirstNonEmpty(ReadStringMember(room, "RoomId", string.Empty), ReadStringMember(room, "roomId", string.Empty), state.CurrentRoomId, room.GetType().Name);
                roomTitle = FirstNonEmpty(ReadStringMember(room, "Title", string.Empty), state.CurrentRoomTitle);
            }

            return new CameraPlayableTelemetrySample
            {
                Timestamp = DateTimeOffset.Now,
                Phase = dynamic.Label,
                Marker = marker,
                ExpectedScale = dynamic.ExpectedScale,
                ElapsedSeconds = elapsedSeconds,
                PlayerX = playerX,
                PlayerY = playerY,
                PlayerZ = playerZ,
                CameraX = cameraX,
                CameraY = cameraY,
                CameraZ = cameraZ,
                OrthographicSize = orthographicSize,
                CurrentViewScale = state.CurrentViewScale,
                AppliedViewScale = state.AppliedViewScale,
                VanillaOrthographicSize = state.VanillaOrthographicSize,
                AppliedOrthographicSize = state.AppliedOrthographicSize,
                ActiveOwnerId = state.ActiveOwnerId,
                ActiveLeaseId = state.ActiveLeaseId,
                ArbitrationStatus = state.ArbitrationStatus,
                NativeRefreshStatus = state.NativeRefreshStatus,
                UiScaleStatus = state.UiScaleStatus,
                RoomId = roomId,
                RoomTitle = roomTitle
            };
        }

        private void RequestZoomDynamicScreenshot(ZoomDynamicRun dynamic, string marker)
        {
            string path = GetZoomDynamicScreenshotPath(dynamic, marker);
            bool requested = TryCaptureScreenshot(path);
            DateTimeOffset requestedAt = DateTimeOffset.Now;
            if (marker.Equals("start", StringComparison.OrdinalIgnoreCase))
            {
                dynamic.StartScreenshotRequested = requested;
                dynamic.StartScreenshotRequestedAt = requestedAt;
            }
            else if (marker.Equals("mid", StringComparison.OrdinalIgnoreCase))
            {
                dynamic.MidScreenshotRequested = requested;
                dynamic.MidScreenshotRequestedAt = requestedAt;
            }
            else
            {
                dynamic.EndScreenshotRequested = requested;
                dynamic.EndScreenshotRequestedAt = requestedAt;
            }
            runtime.RuntimeMonitor.Log("Smoke CameraPlayable dynamic " + dynamic.Label + " " + marker + " screenshot requested=" + requested + " path=" + path + ".");
        }

        private static string GetZoomDynamicScreenshotPath(ZoomDynamicRun dynamic, string marker)
        {
            if (marker.Equals("start", StringComparison.OrdinalIgnoreCase))
                return dynamic.StartScreenshot;
            if (marker.Equals("mid", StringComparison.OrdinalIgnoreCase))
                return dynamic.MidScreenshot;
            return dynamic.EndScreenshot;
        }

        private bool WaitForZoomDynamicScreenshots(ZoomDynamicRun dynamic)
        {
            if (!WaitForZoomScreenshot(dynamic.StartScreenshot, dynamic.Label + "-start", dynamic.StartScreenshotRequested, dynamic.StartScreenshotRequestedAt))
                return false;
            if (!WaitForZoomScreenshot(dynamic.MidScreenshot, dynamic.Label + "-mid", dynamic.MidScreenshotRequested, dynamic.MidScreenshotRequestedAt))
                return false;
            if (!WaitForZoomScreenshot(dynamic.EndScreenshot, dynamic.Label + "-end", dynamic.EndScreenshotRequested, dynamic.EndScreenshotRequestedAt))
                return false;
            return true;
        }

        private void ValidateZoomDynamic(ZoomDynamicRun dynamic, string expectedOwnerId, double expectedScale)
        {
            if (dynamic.StartSample == null || dynamic.MidSample == null || dynamic.EndSample == null)
                throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke did not record start/mid/end telemetry.");
            double durationSeconds = (dynamic.CompletedAt - dynamic.StartedAt).TotalSeconds;
            if (durationSeconds < 29.5d)
                throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke ran for less than 30 seconds. durationSeconds=" + FormatSmokeDouble(durationSeconds));
            if (dynamic.Samples.Count < 20)
                throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke did not record enough telemetry samples. samples=" + dynamic.Samples.Count);
            if (!ScreenshotFileReady(dynamic.StartScreenshot) || !ScreenshotFileReady(dynamic.MidScreenshot) || !ScreenshotFileReady(dynamic.EndScreenshot))
                throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke is missing start/mid/end screenshots.");

            double playerDistance = CalculateTelemetryDistance(dynamic.Samples, true);
            if (playerDistance < 8d)
                throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke did not move the player far enough. distance=" + FormatSmokeDouble(playerDistance));

            double cameraDistance = CalculateTelemetryDistance(dynamic.Samples, false);
            if (cameraDistance < 0.05d)
                throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke did not observe native camera movement. distance=" + FormatSmokeDouble(cameraDistance));

            foreach (CameraPlayableTelemetrySample sample in dynamic.Samples)
            {
                if (double.IsNaN(sample.PlayerX) || double.IsNaN(sample.PlayerY))
                    throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke recorded an unreadable player position.");
                if (double.IsNaN(sample.CameraX) || double.IsNaN(sample.CameraY))
                    throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke recorded an unreadable camera position.");
                if (double.IsNaN(sample.OrthographicSize) || sample.OrthographicSize <= 0d)
                    throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke recorded an unreadable orthographic size.");
                if (!sample.ActiveOwnerId.Equals(expectedOwnerId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke active owner changed. expected=" + expectedOwnerId + ", sample=" + sample.ActiveOwnerId);
                if (sample.AppliedViewScale < expectedScale - 0.05d || sample.AppliedViewScale > expectedScale + 0.05d)
                    throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke applied scale drifted. expected=" + FormatSmokeDouble(expectedScale) + ", sample=" + FormatSmokeDouble(sample.AppliedViewScale));
                if (sample.VanillaOrthographicSize > 0d)
                {
                    double expectedOrthographic = sample.VanillaOrthographicSize * expectedScale;
                    double tolerance = Math.Max(0.1d, expectedOrthographic * 0.05d);
                    if (Math.Abs(sample.OrthographicSize - expectedOrthographic) > tolerance)
                        throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke orthographic size drifted. expected=" + FormatSmokeDouble(expectedOrthographic) + ", sample=" + FormatSmokeDouble(sample.OrthographicSize));
                }
            }

            bool hasRoom = dynamic.Samples.Any(sample => !string.IsNullOrWhiteSpace(sample.RoomId) || !string.IsNullOrWhiteSpace(sample.RoomTitle));
            if (!hasRoom)
                throw new InvalidOperationException("CameraPlayable dynamic " + dynamic.Label + " smoke did not record room identity.");
        }

        private void WriteZoomDynamicTelemetry(ZoomSmokeRun run)
        {
            run.TelemetryPath = Path.Combine(run.EvidenceDir, "camera-playable-dynamic-telemetry.csv");
            var builder = new StringBuilder();
            builder.AppendLine("timestamp,phase,marker,expectedScale,elapsedSeconds,playerX,playerY,playerZ,cameraX,cameraY,cameraZ,orthographicSize,currentViewScale,appliedViewScale,vanillaOrthographicSize,appliedOrthographicSize,activeOwner,activeLease,roomId,roomTitle,nativeRefresh,uiScale,arbitration");
            AppendZoomDynamicTelemetry(builder, run.Dynamic4x);
            AppendZoomDynamicTelemetry(builder, run.Dynamic2x);
            File.WriteAllText(run.TelemetryPath, builder.ToString());
        }

        private void TryWriteZoomDynamicTelemetryAfterFailure(ZoomSmokeRun? run)
        {
            try
            {
                if (run == null || string.IsNullOrWhiteSpace(run.EvidenceDir))
                    return;
                if ((run.Dynamic4x == null || run.Dynamic4x.Samples.Count == 0) &&
                    (run.Dynamic2x == null || run.Dynamic2x.Samples.Count == 0))
                    return;
                WriteZoomDynamicTelemetry(run);
                runtime.RuntimeMonitor.Log("Smoke CameraPlayable failure telemetry written path=" + run.TelemetryPath + ".");
            }
            catch (Exception telemetryEx)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke zoom failure telemetry write failed.", telemetryEx.ToString());
            }
        }

        private static void AppendZoomDynamicTelemetry(StringBuilder builder, ZoomDynamicRun? dynamic)
        {
            if (dynamic == null)
                return;
            foreach (CameraPlayableTelemetrySample sample in dynamic.Samples)
            {
                builder.Append(Csv(sample.Timestamp.ToString("O", CultureInfo.InvariantCulture))).Append(',');
                builder.Append(Csv(sample.Phase)).Append(',');
                builder.Append(Csv(sample.Marker)).Append(',');
                builder.Append(FormatCsvDouble(sample.ExpectedScale)).Append(',');
                builder.Append(FormatCsvDouble(sample.ElapsedSeconds)).Append(',');
                builder.Append(FormatCsvDouble(sample.PlayerX)).Append(',');
                builder.Append(FormatCsvDouble(sample.PlayerY)).Append(',');
                builder.Append(FormatCsvDouble(sample.PlayerZ)).Append(',');
                builder.Append(FormatCsvDouble(sample.CameraX)).Append(',');
                builder.Append(FormatCsvDouble(sample.CameraY)).Append(',');
                builder.Append(FormatCsvDouble(sample.CameraZ)).Append(',');
                builder.Append(FormatCsvDouble(sample.OrthographicSize)).Append(',');
                builder.Append(FormatCsvDouble(sample.CurrentViewScale)).Append(',');
                builder.Append(FormatCsvDouble(sample.AppliedViewScale)).Append(',');
                builder.Append(FormatCsvDouble(sample.VanillaOrthographicSize)).Append(',');
                builder.Append(FormatCsvDouble(sample.AppliedOrthographicSize)).Append(',');
                builder.Append(Csv(sample.ActiveOwnerId)).Append(',');
                builder.Append(Csv(sample.ActiveLeaseId)).Append(',');
                builder.Append(Csv(sample.RoomId)).Append(',');
                builder.Append(Csv(sample.RoomTitle)).Append(',');
                builder.Append(Csv(sample.NativeRefreshStatus)).Append(',');
                builder.Append(Csv(sample.UiScaleStatus)).Append(',');
                builder.Append(Csv(sample.ArbitrationStatus)).AppendLine();
            }
        }

        private static string FormatZoomDynamicSummary(ZoomDynamicRun? dynamic)
        {
            if (dynamic == null)
                return "not-run";

            double durationSeconds = dynamic.CompletedAt == DateTimeOffset.MinValue ? (DateTimeOffset.Now - dynamic.StartedAt).TotalSeconds : (dynamic.CompletedAt - dynamic.StartedAt).TotalSeconds;
            double orthographicMin = dynamic.Samples.Count == 0 ? double.NaN : dynamic.Samples.Min(sample => sample.OrthographicSize);
            double orthographicMax = dynamic.Samples.Count == 0 ? double.NaN : dynamic.Samples.Max(sample => sample.OrthographicSize);
            string activeOwners = string.Join("|", dynamic.Samples.Select(sample => sample.ActiveOwnerId).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().Take(4));
            string rooms = string.Join("|", dynamic.Samples.Select(sample => FirstNonEmpty(sample.RoomTitle, sample.RoomId)).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().Take(4));
            return "label=" + dynamic.Label +
                ", durationSeconds=" + FormatSmokeDouble(durationSeconds) +
                ", samples=" + dynamic.Samples.Count +
                ", playerDistance=" + FormatSmokeDouble(CalculateTelemetryDistance(dynamic.Samples, true)) +
                ", cameraDistance=" + FormatSmokeDouble(CalculateTelemetryDistance(dynamic.Samples, false)) +
                ", orthographicSize=" + FormatSmokeDouble(orthographicMin) + "-" + FormatSmokeDouble(orthographicMax) +
                ", activeOwners=" + activeOwners +
                ", activeLease=" + dynamic.ExpectedLeaseId +
                ", room=" + rooms +
                ", screenshots=" + dynamic.StartScreenshot + "|" + dynamic.MidScreenshot + "|" + dynamic.EndScreenshot;
        }

        private object? ReadCurrentPlayerPositionForSmoke()
        {
            Type? dolocApi = patcher?.ResolveType("DolocAPI, Assembly-CSharp");
            return ReadStaticMember(dolocApi, "AgentPosition");
        }

        private object? ReadUnityMainCameraForSmoke()
        {
            Type? cameraType = patcher?.ResolveType("UnityEngine.Camera, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Camera, UnityEngine");
            return cameraType?.GetProperty("main", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        private bool TrySetSmokeCameraPositionToPlayer(double x, double y, out string message)
        {
            message = string.Empty;
            Type? dolocApi = patcher?.ResolveType("DolocAPI, Assembly-CSharp");
            object? cameraController = ReadStaticMember(dolocApi, "cameraController");
            if (cameraController == null)
            {
                message = "DolocAPI.cameraController unavailable.";
                return false;
            }

            MethodInfo? setPosition = cameraController.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                {
                    if (!method.Name.Equals("SetPosition", StringComparison.Ordinal))
                        return false;
                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.FullName != null && parameters[0].ParameterType.FullName.IndexOf("Vector2", StringComparison.OrdinalIgnoreCase) >= 0;
                });
            if (setPosition == null)
            {
                message = "CameraController.SetPosition(Vector2) unavailable.";
                return false;
            }

            object? vector = CreateVectorForMember(setPosition.GetParameters()[0].ParameterType, x, y, 0d);
            if (vector == null)
            {
                message = "UnityEngine.Vector2 unavailable.";
                return false;
            }

            setPosition.Invoke(cameraController, new[] { vector });
            message = "CameraController.SetPosition";
            return true;
        }

        private static bool TryReadVectorCoordinates(object? vector, out double x, out double y, out double z)
        {
            x = double.NaN;
            y = double.NaN;
            z = double.NaN;
            if (vector == null)
                return false;
            x = ReadDoubleMember(vector, "x", double.NaN);
            y = ReadDoubleMember(vector, "y", double.NaN);
            z = ReadDoubleMember(vector, "z", 0d);
            return !double.IsNaN(x) && !double.IsNaN(y);
        }

        private bool TrySetDolocApiAgentPosition(double x, double y, double z, out string message)
        {
            message = string.Empty;
            Type? dolocApi = patcher?.ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
            {
                message = "DolocAPI type unavailable.";
                return false;
            }

            for (Type? current = dolocApi; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty("AgentPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (property != null && property.CanWrite)
                {
                    object? value = CreateVectorForMember(property.PropertyType, x, y, z);
                    if (value != null)
                    {
                        property.SetValue(null, value);
                        message = "property";
                        return true;
                    }
                }

                FieldInfo? field = current.GetField("AgentPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    object? value = CreateVectorForMember(field.FieldType, x, y, z);
                    if (value != null)
                    {
                        field.SetValue(null, value);
                        message = "field";
                        return true;
                    }
                }
            }

            message = "DolocAPI.AgentPosition writable member unavailable.";
            return false;
        }

        private object? CreateVectorForMember(Type memberType, double x, double y, double z)
        {
            object? vector3 = CreateVector3ForSmoke(x, y, z);
            if (vector3 != null && memberType.IsInstanceOfType(vector3))
                return vector3;
            object? vector2 = CreateVector2ForSmoke(x, y);
            if (vector2 != null && memberType.IsInstanceOfType(vector2))
                return vector2;
            string memberName = memberType.FullName ?? memberType.Name;
            if (memberName.IndexOf("Vector3", StringComparison.OrdinalIgnoreCase) >= 0)
                return vector3;
            if (memberName.IndexOf("Vector2", StringComparison.OrdinalIgnoreCase) >= 0)
                return vector2;
            return null;
        }

        private void TryRestoreZoomSmokePlayerPosition(ZoomSmokeRun run)
        {
            if (!run.OriginalPlayerPositionCaptured || run.PlayerPositionRestored)
                return;

            if (TrySetDolocApiAgentPosition(run.OriginalPlayerX, run.OriginalPlayerY, run.OriginalPlayerZ, out string message))
            {
                run.PlayerPositionRestored = true;
                runtime.RuntimeMonitor.Log("Smoke CameraPlayable restored player position via " + message + " to " + FormatSmokeDouble(run.OriginalPlayerX) + "," + FormatSmokeDouble(run.OriginalPlayerY) + "," + FormatSmokeDouble(run.OriginalPlayerZ) + ".");
            }
            else
            {
                runtime.RuntimeMonitor.Log("Smoke CameraPlayable could not restore player position: " + message);
            }
        }

        private static double CalculateTelemetryDistance(IReadOnlyList<CameraPlayableTelemetrySample> samples, bool player)
        {
            CameraPlayableTelemetrySample? origin = samples.FirstOrDefault(sample =>
                !double.IsNaN(player ? sample.PlayerX : sample.CameraX) &&
                !double.IsNaN(player ? sample.PlayerY : sample.CameraY));
            if (origin == null)
                return 0d;

            double originX = player ? origin.PlayerX : origin.CameraX;
            double originY = player ? origin.PlayerY : origin.CameraY;
            double originZ = player ? origin.PlayerZ : origin.CameraZ;
            double max = 0d;
            foreach (CameraPlayableTelemetrySample sample in samples)
            {
                double x = player ? sample.PlayerX : sample.CameraX;
                double y = player ? sample.PlayerY : sample.CameraY;
                double z = player ? sample.PlayerZ : sample.CameraZ;
                if (double.IsNaN(x) || double.IsNaN(y))
                    continue;
                if (double.IsNaN(z))
                    z = originZ;
                double dx = x - originX;
                double dy = y - originY;
                double dz = z - originZ;
                double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (distance > max)
                    max = distance;
            }
            return max;
        }

        private static string FormatCsvDouble(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? string.Empty : value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static double ClampSmoke(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static void ValidateZoomMax(CameraViewResult max, CameraViewState maxState)
        {
            if (!max.Success)
                throw new InvalidOperationException("4x apply failed: " + max.FailureReason + ": " + max.Message);
            if (!maxState.CameraAvailable)
                throw new InvalidOperationException("Camera was not available after 4x apply. state=" + FormatZoomState(maxState));
            if (maxState.CurrentViewScale < 3.95d || maxState.AppliedViewScale < 3.95d)
                throw new InvalidOperationException("Expected 4x view scale after apply. state=" + FormatZoomState(maxState));
            if (maxState.AppliedOrthographicSize <= maxState.VanillaOrthographicSize)
                throw new InvalidOperationException("Expected applied orthographic size to exceed vanilla size. state=" + FormatZoomState(maxState));
            if (!ZoomStatusContains(maxState.NativeRefreshStatus, "not-called-playable"))
                throw new InvalidOperationException("Playable zoom must not call native camera refresh/position/scanner paths. state=" + FormatZoomState(maxState));
            if (!ZoomStatusContains(maxState.ArbitrationStatus, "active-highest-priority-latest"))
                throw new InvalidOperationException("CameraView arbitration was not verified after competing leases. state=" + FormatZoomState(maxState));
            if (!maxState.OwnerId.Equals(maxState.ActiveOwnerId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Expected the high-priority owner to be active. state=" + FormatZoomState(maxState));
        }

        private static void ValidateZoomFallback(CameraViewResult fallback, CameraViewState fallbackState)
        {
            if (!fallback.Success)
                throw new InvalidOperationException("Fallback apply failed: " + fallback.FailureReason + ": " + fallback.Message);
            if (fallbackState.AppliedViewScale < 1.95d || fallbackState.AppliedViewScale > 2.05d)
                throw new InvalidOperationException("Expected lower-priority 2x lease to become active after releasing the high-priority lease. state=" + FormatZoomState(fallbackState));
            if (!fallbackState.OwnerId.Equals(fallbackState.ActiveOwnerId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Expected fallback lease owner to be active. state=" + FormatZoomState(fallbackState));
            if (!ZoomStatusContains(fallbackState.NativeRefreshStatus, "not-called-playable"))
                throw new InvalidOperationException("Fallback playable zoom must not call native refresh/position/scanner paths. state=" + FormatZoomState(fallbackState));
        }

        private static void ValidateZoomReset(CameraViewResult reset, CameraViewState after)
        {
            if (!reset.Success)
                throw new InvalidOperationException("Reset failed: " + reset.FailureReason + ": " + reset.Message);
            if (after.CurrentViewScale > 1.05d)
                throw new InvalidOperationException("Expected vanilla view scale after reset. state=" + FormatZoomState(after));
            if (after.AppliedViewScale > 1.05d)
                throw new InvalidOperationException("Expected applied view scale to return to vanilla after reset. state=" + FormatZoomState(after));
            if (!ZoomStatusContains(after.NativeRefreshStatus, "not-called-playable"))
                throw new InvalidOperationException("Reset playable zoom must not call native refresh/position/scanner paths. state=" + FormatZoomState(after));
        }

        private void TryRestoreZoomSmokeAfterFailure()
        {
            try
            {
                zoomSmokeRun?.PrimaryLease?.Release("smoke failure restore high-priority lease");
                zoomSmokeRun?.CompetingLease?.Release("smoke failure restore lower-priority lease");
                if (zoomSmokeRun != null)
                    TryRestoreZoomSmokePlayerPosition(zoomSmokeRun);
            }
            catch (Exception restoreEx)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke zoom failure restore failed.", restoreEx.ToString());
            }
        }

        private static string FormatZoomState(CameraViewState state)
        {
            if (state == null)
                return "unknown";

            return "owner=" + state.OwnerId +
                ", lease=" + state.LeaseId +
                ", status=" + state.Status +
                ", enabled=" + state.Enabled +
                ", current=" + FormatSmokeDouble(state.CurrentViewScale) +
                ", requested=" + FormatSmokeDouble(state.RequestedViewScale) +
                ", clamped=" + FormatSmokeDouble(state.ClampedViewScale) +
                ", appliedScale=" + FormatSmokeDouble(state.AppliedViewScale) +
                ", activeOwner=" + state.ActiveOwnerId +
                ", activeLease=" + state.ActiveLeaseId +
                ", arbitration=" + state.ArbitrationStatus +
                ", priority=" + state.Priority.ToString(CultureInfo.InvariantCulture) +
                ", range=" + FormatSmokeDouble(state.MinViewScale) + "-" + FormatSmokeDouble(state.MaxViewScale) +
                ", camera=" + state.CameraAvailable +
                ", vanillaSize=" + FormatSmokeDouble(state.VanillaOrthographicSize) +
                ", appliedSize=" + FormatSmokeDouble(state.AppliedOrthographicSize) +
                ", cameraOwner=" + state.CameraOwnerStatus +
                ", nativeRefresh=" + state.NativeRefreshStatus +
                ", lifecycle=" + state.LifecycleStatus +
                ", uiScale=" + state.UiScaleStatus +
                ", room=" + (string.IsNullOrWhiteSpace(state.CurrentRoomTitle) ? state.CurrentRoomId : state.CurrentRoomTitle) +
                ", roomBackground=" + state.CurrentRoomShowsBackground +
                ", message=" + state.LastMessage;
        }

        private static bool ZoomStatusContains(string status, string needle)
        {
            return (status ?? string.Empty).IndexOf(needle ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ZoomStatusHasFailure(string status)
        {
            string normalized = (status ?? string.Empty).ToLowerInvariant();
            return normalized.Contains("failed") || normalized.Contains("missing");
        }

        private static ManifestModel CreateZoomSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Zoom Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.ZoomMod",
                Type = "Smoke"
            };
        }

        private static ManifestModel CreateCameraViewCompetingSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI CameraView Competing Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.CameraViewCompetingSmoke",
                Type = "Smoke"
            };
        }

        private sealed class ZoomSmokeRun
        {
            public ManifestModel Owner { get; set; } = new ManifestModel();
            public ManifestModel CompetingOwner { get; set; } = new ManifestModel();
            public ICameraViewLease? PrimaryLease { get; set; }
            public ICameraViewLease? CompetingLease { get; set; }
            public string EvidenceDir { get; set; } = string.Empty;
            public CameraViewState Before { get; set; } = new CameraViewState();
            public CameraViewState Max { get; set; } = new CameraViewState();
            public CameraViewState Fallback { get; set; } = new CameraViewState();
            public CameraViewState After { get; set; } = new CameraViewState();
            public CameraViewResult? MaxResult { get; set; }
            public CameraViewResult? FallbackResult { get; set; }
            public CameraViewResult? ResetResult { get; set; }
            public string BeforeScreenshot { get; set; } = string.Empty;
            public string MaxScreenshot { get; set; } = string.Empty;
            public string ResetScreenshot { get; set; } = string.Empty;
            public bool BeforeScreenshotRequested { get; set; }
            public bool MaxScreenshotRequested { get; set; }
            public bool ResetScreenshotRequested { get; set; }
            public ZoomDynamicRun? Dynamic4x { get; set; }
            public ZoomDynamicRun? Dynamic2x { get; set; }
            public string TelemetryPath { get; set; } = string.Empty;
            public bool OriginalPlayerPositionCaptured { get; set; }
            public bool PlayerPositionRestored { get; set; }
            public double OriginalPlayerX { get; set; }
            public double OriginalPlayerY { get; set; }
            public double OriginalPlayerZ { get; set; }
            public int Stage { get; set; }
            public DateTimeOffset StageAt { get; set; }
        }

        private sealed class ZoomDynamicRun
        {
            public string Label { get; set; } = string.Empty;
            public double ExpectedScale { get; set; }
            public string ExpectedOwnerId { get; set; } = string.Empty;
            public string ExpectedLeaseId { get; set; } = string.Empty;
            public ICameraViewLease? Lease { get; set; }
            public double DurationSeconds { get; set; } = 30d;
            public double OriginX { get; set; }
            public double OriginY { get; set; }
            public double OriginZ { get; set; }
            public double MoveDirection { get; set; } = 1d;
            public double LastTargetX { get; set; }
            public double LastTargetY { get; set; }
            public double LastTargetZ { get; set; }
            public DateTimeOffset StartedAt { get; set; }
            public DateTimeOffset CompletedAt { get; set; }
            public DateTimeOffset LastSampleAt { get; set; }
            public DateTimeOffset LastMoveAt { get; set; }
            public string StartScreenshot { get; set; } = string.Empty;
            public string MidScreenshot { get; set; } = string.Empty;
            public string EndScreenshot { get; set; } = string.Empty;
            public bool StartScreenshotRequested { get; set; }
            public bool MidScreenshotRequested { get; set; }
            public bool EndScreenshotRequested { get; set; }
            public DateTimeOffset StartScreenshotRequestedAt { get; set; }
            public DateTimeOffset MidScreenshotRequestedAt { get; set; }
            public DateTimeOffset EndScreenshotRequestedAt { get; set; }
            public CameraPlayableTelemetrySample? StartSample { get; set; }
            public CameraPlayableTelemetrySample? MidSample { get; set; }
            public CameraPlayableTelemetrySample? EndSample { get; set; }
            public List<CameraPlayableTelemetrySample> Samples { get; } = new List<CameraPlayableTelemetrySample>();
        }

        private sealed class CameraPlayableTelemetrySample
        {
            public DateTimeOffset Timestamp { get; set; }
            public string Phase { get; set; } = string.Empty;
            public string Marker { get; set; } = string.Empty;
            public double ExpectedScale { get; set; }
            public double ElapsedSeconds { get; set; }
            public double PlayerX { get; set; }
            public double PlayerY { get; set; }
            public double PlayerZ { get; set; }
            public double CameraX { get; set; }
            public double CameraY { get; set; }
            public double CameraZ { get; set; }
            public double OrthographicSize { get; set; }
            public double CurrentViewScale { get; set; }
            public double AppliedViewScale { get; set; }
            public double VanillaOrthographicSize { get; set; }
            public double AppliedOrthographicSize { get; set; }
            public string ActiveOwnerId { get; set; } = string.Empty;
            public string ActiveLeaseId { get; set; } = string.Empty;
            public string RoomId { get; set; } = string.Empty;
            public string RoomTitle { get; set; } = string.Empty;
            public string NativeRefreshStatus { get; set; } = string.Empty;
            public string UiScaleStatus { get; set; } = string.Empty;
            public string ArbitrationStatus { get; set; } = string.Empty;
        }
    }
}
