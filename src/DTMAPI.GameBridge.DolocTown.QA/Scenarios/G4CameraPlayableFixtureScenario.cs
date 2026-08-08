#pragma warning disable CS0618 // This QA scenario intentionally exercises the frozen CameraView compatibility ABI.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraPlayablePositionSample
    {
        internal DateTimeOffset Timestamp { get; set; }
        internal string Marker { get; set; } = string.Empty;
        internal double AgentX { get; set; }
        internal double AgentY { get; set; }
        internal double AgentZ { get; set; }
        internal double CameraX { get; set; }
        internal double CameraY { get; set; }
        internal double CameraZ { get; set; }
        internal double AppliedScale { get; set; }
        internal string ActiveOwnerId { get; set; } = string.Empty;
    }

    internal sealed class CameraPlayableMovementFixtureScenario : IDisposable
    {
        private readonly ICameraViewApi api;
        private readonly Func<string, int> countOwnerRoots;
        private readonly Func<AgentPositionFixtureReceipt> captureAgentPosition;
        private readonly Func<AgentPositionFixtureReceipt, bool> restoreAgentPosition;
        private readonly Func<CameraPositionFixtureReceipt> captureCameraPosition;
        private readonly Func<CameraPositionFixtureReceipt, bool> restoreCameraPosition;
        private readonly Func<double, double, double, CameraPlayablePositionSample> moveAndCapture;
        private readonly Action applyRuntimeAutomation;
        private readonly Func<string, bool> requestScreenshot;
        private readonly string evidenceDirectory;
        private readonly string lowOwnerId;
        private readonly string highOwnerId;
        private readonly List<CameraPlayablePositionSample> samples = new List<CameraPlayablePositionSample>();
        private readonly Dictionary<string, DateTimeOffset> screenshotRequests = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        private ICameraViewLease? lowLease;
        private ICameraViewLease? highLease;
        private AgentPositionFixtureReceipt? originalAgent;
        private CameraPositionFixtureReceipt? originalCamera;
        private CameraViewState before = new CameraViewState();
        private DateTimeOffset movementStartedAt;
        private DateTimeOffset lastMoveAt;
        private DateTimeOffset presentationSettleStartedAt;
        private int stage;
        private bool highReleased;
        private bool lowReleased;
        private bool agentRestored;
        private bool cameraRestored;
        private bool advanceInProgress;
        private bool closeInProgress;
        private bool terminal;
        private string summary = "not-completed";

        internal CameraPlayableMovementFixtureScenario(
            ICameraViewApi api,
            string runId,
            Func<string, int> countOwnerRoots,
            Func<AgentPositionFixtureReceipt> captureAgentPosition,
            Func<AgentPositionFixtureReceipt, bool> restoreAgentPosition,
            Func<CameraPositionFixtureReceipt> captureCameraPosition,
            Func<CameraPositionFixtureReceipt, bool> restoreCameraPosition,
            Func<double, double, double, CameraPlayablePositionSample> moveAndCapture,
            Action applyRuntimeAutomation,
            Func<string, bool> requestScreenshot,
            string evidenceDirectory)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.countOwnerRoots = countOwnerRoots ?? throw new ArgumentNullException(nameof(countOwnerRoots));
            this.captureAgentPosition = captureAgentPosition ?? throw new ArgumentNullException(nameof(captureAgentPosition));
            this.restoreAgentPosition = restoreAgentPosition ?? throw new ArgumentNullException(nameof(restoreAgentPosition));
            this.captureCameraPosition = captureCameraPosition ?? throw new ArgumentNullException(nameof(captureCameraPosition));
            this.restoreCameraPosition = restoreCameraPosition ?? throw new ArgumentNullException(nameof(restoreCameraPosition));
            this.moveAndCapture = moveAndCapture ?? throw new ArgumentNullException(nameof(moveAndCapture));
            this.applyRuntimeAutomation = applyRuntimeAutomation ?? throw new ArgumentNullException(nameof(applyRuntimeAutomation));
            this.requestScreenshot = requestScreenshot ?? throw new ArgumentNullException(nameof(requestScreenshot));
            this.evidenceDirectory = evidenceDirectory ?? throw new ArgumentNullException(nameof(evidenceDirectory));
            string suffix = string.IsNullOrWhiteSpace(runId) ? "unknown" : runId;
            lowOwnerId = "DTMAPI.QA.Camera.Low." + suffix;
            highOwnerId = "DTMAPI.QA.Camera.High." + suffix;
        }

        internal G4FixtureStepResult Advance()
        {
            if (terminal)
                return G4FixtureStepResult.Verified(summary);
            if (advanceInProgress || closeInProgress)
                return G4FixtureStepResult.Pending("CameraPlayable transaction is already in progress; synchronous re-entry was suppressed.");
            advanceInProgress = true;
            try
            {
                Directory.CreateDirectory(evidenceDirectory);
                if (stage == 0)
                {
                    if (presentationSettleStartedAt == default)
                    {
                        presentationSettleStartedAt = DateTimeOffset.UtcNow;
                        return G4FixtureStepResult.Pending("Waiting for the playable presentation to settle before CameraPlayable capture.");
                    }
                    if ((DateTimeOffset.UtcNow - presentationSettleStartedAt).TotalSeconds < 2d)
                        return G4FixtureStepResult.Pending("Waiting for the playable presentation to settle before CameraPlayable capture.");
                    before = api.GetState(lowOwnerId);
                    originalAgent = captureAgentPosition();
                    if (!originalAgent.Captured)
                        return G4FixtureStepResult.Failed("DolocAPI.AgentPosition could not be captured before CameraPlayable movement.");
                    originalCamera = captureCameraPosition();
                    if (!originalCamera.Captured)
                        return G4FixtureStepResult.Failed("CameraController.position2d could not be captured before CameraPlayable movement; no mutation was attempted.");
                    stage = 1;
                    RequestScreenshot("before.png");
                    return G4FixtureStepResult.Pending("Captured independent original AgentPosition and CameraController.position2d receipts and requested the before screenshot.");
                }

                if (stage == 1)
                {
                    if (!ScreenshotReady("before.png"))
                        return G4FixtureStepResult.Pending("Waiting for CameraPlayable before screenshot.");
                    // Commit the activation stage before any API or runtime callback can
                    // synchronously re-enter this QA state machine.
                    stage = 2;
                    lowLease = api.AcquireLease(CreateManifest(lowOwnerId), CreateRequest("qa-low", 2d, 100));
                    highLease = api.AcquireLease(CreateManifest(highOwnerId), CreateRequest("qa-high", 4d, 200));
                    applyRuntimeAutomation();
                    CameraViewState high = highLease.GetState();
                    if (!high.ActiveOwnerId.Equals(highOwnerId, StringComparison.OrdinalIgnoreCase) || Math.Abs(high.AppliedViewScale - 4d) > 0.05d)
                        throw new InvalidOperationException("The 4x QA CameraView lease did not become active.");
                    RequestScreenshot("scale-4x.png");
                    return G4FixtureStepResult.Pending("Two QA leases acquired; 4x owner active; waiting for screenshot.");
                }

                if (stage == 2)
                {
                    if (lowLease == null || highLease == null || !screenshotRequests.ContainsKey("scale-4x.png"))
                        return G4FixtureStepResult.Pending("CameraPlayable lease activation is still committing.");
                    if (!ScreenshotReady("scale-4x.png"))
                        return G4FixtureStepResult.Pending("Waiting for 4x CameraPlayable screenshot.");
                    movementStartedAt = DateTimeOffset.UtcNow;
                    lastMoveAt = DateTimeOffset.MinValue;
                    RequestScreenshot("movement-start.png");
                    stage = 3;
                    return G4FixtureStepResult.Pending("Starting bounded 3-second AgentPosition/native-camera movement under the 4x lease.");
                }

                if (stage == 3)
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    double elapsed = Math.Max(0d, (now - movementStartedAt).TotalSeconds);
                    double progress = Math.Min(1d, elapsed / 3d);
                    if ((now - lastMoveAt).TotalMilliseconds >= 100d || progress >= 1d)
                    {
                        double x = originalAgent!.X + 24d * progress;
                        // The third-save fixture now starts beside the large barn's right edge. Keep the
                        // camera exercise in the cleared area and never drive the player down into the
                        // map-boundary mask which begins immediately below the visible farm ground.
                        double y = originalAgent.Y - 2d * Math.Sin(progress * Math.PI);
                        CameraPlayablePositionSample sample = moveAndCapture(x, y, originalAgent.Z);
                        sample.Timestamp = now;
                        sample.Marker = progress >= 1d ? "end" : progress >= 0.5d ? "mid" : "sample";
                        CameraViewState state = highLease!.GetState();
                        sample.AppliedScale = state.AppliedViewScale;
                        sample.ActiveOwnerId = state.ActiveOwnerId;
                        samples.Add(sample);
                        lastMoveAt = now;
                    }
                    if (progress >= 0.5d && !screenshotRequests.ContainsKey("movement-mid.png"))
                        RequestScreenshot("movement-mid.png");
                    if (progress < 1d)
                        return G4FixtureStepResult.Pending("Bounded CameraPlayable movement in progress; samples=" + samples.Count + ".");
                    if (!screenshotRequests.ContainsKey("movement-end.png"))
                        RequestScreenshot("movement-end.png");
                    if (!ScreenshotReady("movement-start.png") || !ScreenshotReady("movement-mid.png") || !ScreenshotReady("movement-end.png"))
                        return G4FixtureStepResult.Pending("Movement completed; waiting for start/mid/end screenshot receipts.");
                    ValidateMovement();
                    stage = 4;
                    highLease!.Release("G4 camera arbitration fallback");
                    highReleased = highLease.IsReleased;
                    applyRuntimeAutomation();
                    CameraViewState fallback = lowLease!.GetState();
                    if (!fallback.ActiveOwnerId.Equals(lowOwnerId, StringComparison.OrdinalIgnoreCase) || Math.Abs(fallback.AppliedViewScale - 2d) > 0.05d)
                        throw new InvalidOperationException("The 2x QA fallback lease was not restored after releasing 4x.");
                    RequestScreenshot("fallback-2x.png");
                    return G4FixtureStepResult.Pending("Movement/readback verified; 4x released and 2x fallback active.");
                }

                if (stage == 4)
                {
                    if (!ScreenshotReady("fallback-2x.png"))
                        return G4FixtureStepResult.Pending("Waiting for 2x fallback screenshot.");
                    stage = 5;
                    lowLease!.Release("G4 camera reset");
                    lowReleased = lowLease.IsReleased;
                    applyRuntimeAutomation();
                    agentRestored = restoreAgentPosition(originalAgent!);
                    if (!agentRestored)
                        throw new InvalidOperationException("Original AgentPosition readback did not restore after movement.");
                    cameraRestored = restoreCameraPosition(originalCamera!);
                    if (!cameraRestored)
                        throw new InvalidOperationException("Original CameraController.position2d readback did not restore after movement.");
                    RequestScreenshot("reset.png");
                    return G4FixtureStepResult.Pending("Both leases released and independent AgentPosition/camera receipts restored; waiting for reset screenshot.");
                }

                if (!ScreenshotReady("reset.png"))
                    return G4FixtureStepResult.Pending("Waiting for CameraPlayable reset screenshot.");
                stage = 6;
                string close = Close();
                WriteTelemetry();
                summary = "movementSeconds=3; path=rightward-ground-safe; maxDownwardExcursion=0; samples=" + samples.Count + "; screenshots=" + evidenceDirectory + "; telemetry=" + Path.Combine(evidenceDirectory, "telemetry.csv") + "; close={" + close + "}";
                return G4FixtureStepResult.Verified(summary);
            }
            catch (Exception ex)
            {
                try { Close(); } catch { }
                return G4FixtureStepResult.Failed(ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                advanceInProgress = false;
            }
        }

        internal string Close()
        {
            if (terminal)
                return summary;
            if (closeInProgress)
                return "close-in-progress";
            closeInProgress = true;
            try
            {
                var failures = new List<string>();
                Release(highLease, "G4 camera close high", ref highReleased, failures, "high");
                Release(lowLease, "G4 camera close low", ref lowReleased, failures, "low");
                try { applyRuntimeAutomation(); } catch (Exception ex) { failures.Add("automation:" + ex.GetType().Name); }
                if (originalAgent == null || !originalAgent.Captured)
                    agentRestored = true;
                else
                {
                    try { agentRestored = restoreAgentPosition(originalAgent); }
                    catch (Exception ex) { failures.Add("agent-restore:" + ex.GetType().Name); }
                }
                if (originalCamera == null || !originalCamera.Captured)
                    cameraRestored = true;
                else
                {
                    try { cameraRestored = restoreCameraPosition(originalCamera); }
                    catch (Exception ex) { failures.Add("camera-restore:" + ex.GetType().Name); }
                }
                int roots = countOwnerRoots(lowOwnerId) + countOwnerRoots(highOwnerId);
                CameraViewState after = api.GetState(lowOwnerId);
                bool ownerRestored = string.Equals(after.ActiveOwnerId, before.ActiveOwnerId, StringComparison.OrdinalIgnoreCase);
                bool vanillaRestored = !string.IsNullOrWhiteSpace(before.ActiveOwnerId) || after.AppliedViewScale <= 1.0001d;
                if (!highReleased || !lowReleased) failures.Add("lease-release");
                if (!agentRestored) failures.Add("agent-restore");
                if (!cameraRestored) failures.Add("camera-restore");
                if (roots != 0) failures.Add("root=" + roots);
                if (!ownerRestored) failures.Add("owner-restore");
                if (!vanillaRestored) failures.Add("vanilla-restore");
                string close = "acquired=" + ((lowLease == null ? 0 : 1) + (highLease == null ? 0 : 1)) + "; released=" + ((lowReleased ? 1 : 0) + (highReleased ? 1 : 0)) + "; root=" + roots + "; agentRestored=" + agentRestored + "; cameraRestored=" + cameraRestored + "; ownerRestored=" + ownerRestored + "; vanillaRestored=" + vanillaRestored;
                if (failures.Count > 0)
                    throw new InvalidOperationException("CameraPlayable cleanup is not terminal: " + close + "; failures=" + string.Join("|", failures) + ".");
                terminal = true;
                summary = close;
                return close;
            }
            finally
            {
                closeInProgress = false;
            }
        }

        public void Dispose() => Close();

        private void RequestScreenshot(string fileName)
        {
            string path = Path.Combine(evidenceDirectory, fileName);
            if (!requestScreenshot(path))
                throw new InvalidOperationException("Unity ScreenCapture rejected " + fileName + ".");
            screenshotRequests[fileName] = DateTimeOffset.UtcNow;
        }

        private bool ScreenshotReady(string fileName)
        {
            string path = Path.Combine(evidenceDirectory, fileName);
            try
            {
                var file = new FileInfo(path);
                if (file.Exists && file.Length > 0)
                    return true;
            }
            catch (IOException) { }
            if (screenshotRequests.TryGetValue(fileName, out DateTimeOffset requestedAt) && (DateTimeOffset.UtcNow - requestedAt).TotalSeconds > 10d)
                throw new InvalidOperationException("CameraPlayable screenshot timed out: " + path + ".");
            return false;
        }

        private void ValidateMovement()
        {
            if (samples.Count < 10)
                throw new InvalidOperationException("CameraPlayable movement recorded too few telemetry samples: " + samples.Count + ".");
            CameraPlayablePositionSample first = samples.First();
            CameraPlayablePositionSample last = samples.Last();
            double agentDistance = Distance(first.AgentX, first.AgentY, last.AgentX, last.AgentY);
            double cameraDistance = Distance(first.CameraX, first.CameraY, last.CameraX, last.CameraY);
            if (agentDistance < 16d)
                throw new InvalidOperationException("CameraPlayable AgentPosition distance was too small: " + Format(agentDistance) + ".");
            if (cameraDistance < 8d)
                throw new InvalidOperationException("CameraPlayable native camera distance was too small: " + Format(cameraDistance) + ".");
            if (samples.Any(item => item.AgentY > originalAgent!.Y + 0.05d || item.CameraY > originalAgent.Y + 0.05d))
                throw new InvalidOperationException("CameraPlayable movement crossed below the third-save ground-safe boundary beside the large barn.");
            if (samples.Any(item => !item.ActiveOwnerId.Equals(highOwnerId, StringComparison.OrdinalIgnoreCase) || Math.Abs(item.AppliedScale - 4d) > 0.05d))
                throw new InvalidOperationException("CameraPlayable owner/scale drifted during movement.");
        }

        private void WriteTelemetry()
        {
            var text = new StringBuilder("timestamp,marker,agentX,agentY,agentZ,cameraX,cameraY,cameraZ,appliedScale,activeOwner\n");
            foreach (CameraPlayablePositionSample sample in samples)
            {
                text.Append(sample.Timestamp.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.Marker).Append(',')
                    .Append(Format(sample.AgentX)).Append(',').Append(Format(sample.AgentY)).Append(',').Append(Format(sample.AgentZ)).Append(',')
                    .Append(Format(sample.CameraX)).Append(',').Append(Format(sample.CameraY)).Append(',').Append(Format(sample.CameraZ)).Append(',')
                    .Append(Format(sample.AppliedScale)).Append(',').Append(sample.ActiveOwnerId).Append('\n');
            }
            File.WriteAllText(Path.Combine(evidenceDirectory, "telemetry.csv"), text.ToString());
        }

        private static void Release(ICameraViewLease? lease, string reason, ref bool released, List<string> failures, string label)
        {
            if (lease == null) { released = true; return; }
            if (released) return;
            try
            {
                if (!lease.IsReleased) lease.Release(reason);
                released = lease.IsReleased;
                if (!released) failures.Add(label + "-release-not-observed");
            }
            catch (Exception ex) { failures.Add(label + "-release:" + ex.GetType().Name); }
        }

        private static ManifestModel CreateManifest(string uniqueId) => new ManifestModel
        {
            Name = "DTMAPI G4 Camera QA", Author = "DTMAPI", Version = DtmApiRuntime.ApiVersion, UniqueID = uniqueId, Type = "QA"
        };

        private static CameraViewRequest CreateRequest(string name, double scale, int priority) => new CameraViewRequest
        {
            Enabled = true, ViewScale = scale, MinViewScale = 1d, MaxViewScale = 4d, Step = 0.25d, Priority = priority, LeaseName = name, VerboseLogging = true
        };

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double x = x2 - x1;
            double y = y2 - y1;
            return Math.Sqrt(x * x + y * y);
        }

        private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
