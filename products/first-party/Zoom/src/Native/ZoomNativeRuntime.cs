using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using UnityEngine;

namespace DTMAPI.Zoom
{
    internal sealed class ZoomNativeRuntime
    {
        private readonly IMonitor monitor;
        private readonly IZoomHookOwner hooks;
        private readonly List<ZoomCameraHold> cameras = new List<ZoomCameraHold>();
        private readonly List<ZoomSpriteHold> sprites = new List<ZoomSpriteHold>();
        private readonly List<ZoomSpriteTarget> targets = new List<ZoomSpriteTarget>();
        private ZoomCameraHold? camera;
        private ZoomConfig config = new ZoomConfig();
        private object? background, preset, environment;
        private bool dirty = true;
        private bool deactivated, observing;
        private long generation;
        private int refreshDepth;

        internal ZoomNativeRuntime(IMonitor monitor, IZoomHookOwner hooks)
        { this.monitor = monitor; this.hooks = hooks; }
        internal event Action<bool>? ObservationChanged;
        internal double CurrentViewScale { get; private set; } = 1d;
        internal double VanillaOrthographicSize => camera?.Baseline ?? 0f;
        internal double AppliedOrthographicSize => camera?.LastWrite ?? 0f;
        internal int InstalledPatchCount => hooks.InstalledPatchCount;
        internal bool IsHookInstalled => hooks.IsInstalled;
        internal string Status { get; private set; } = "created";
        internal string LastMessage { get; private set; } = string.Empty;
        internal long DiscoveryCount { get; private set; }
        internal long ScaleWriteCount { get; private set; }
        internal int PendingRestoreCount => cameras.Count + sprites.FindAll(s => !s.Yielded).Count;

        internal void Configure(ZoomConfig value, string reason)
        {
            if (deactivated) throw new ObjectDisposedException(nameof(ZoomNativeRuntime));
            hooks.InstallAtomically(this);
            config = value.Copy();
            if (CurrentViewScale > config.MaxViewScale) SetViewScale(config.MaxViewScale, reason);
        }
        internal bool Step(int direction, string reason) =>
            SetViewScale(CurrentViewScale + config.Step * (direction < 0 ? -1d : 1d), reason);
        internal bool SetViewScale(double value, string reason)
        {
            if (deactivated) return false;
            double next = double.IsNaN(value) || double.IsInfinity(value) ? 1d : Math.Max(1d, Math.Min(config.MaxViewScale, value));
            if (next == 1d) { ResetToVanilla(reason); return true; }
            if (CurrentViewScale == 1d) dirty = true;
            CurrentViewScale = next;
            try { Observe(); }
            finally { UpdateObservation(); }
            if (config.VerboseLogging) monitor.Log("Zoom scale=" + CurrentViewScale + " reason=" + reason + " status=" + Status);
            return camera != null && CurrentViewScale == next;
        }
        internal void OnEnvironmentReset() { if (CurrentViewScale > 1d) { dirty = true; Tick(); } }
        internal void OnNativeContentChanged() { if (CurrentViewScale > 1d) dirty = true; }
        internal void BeforeBackgroundReload(object holder)
        {
            if (CurrentViewScale == 1d && sprites.Count == 0) return;
            var failures = new List<Exception>();
            for (int i = sprites.Count - 1; i >= 0; --i)
                if (ReferenceEquals(sprites[i].Holder, holder)) ReleaseSpriteAt(i, failures);
            dirty = true;
            // Native content loading continues; a failed record cannot become a new baseline.
            Report(failures);
        }
        internal void Tick()
        {
            try { Observe(); }
            catch (Exception ex) { Status = "observation-failed"; LastMessage = ex.Message; }
            finally { UpdateObservation(); }
        }
        private void Observe()
        {
            if (CurrentViewScale == 1d || deactivated) { RestoreAll(); return; }
            var live = DolocAPI.mainCamera;
            if (camera != null && (!camera.Camera || !ReferenceEquals(camera.Camera, live)))
            {
                ++generation; refreshDepth = 0; camera = null; dirty = true;
            }
            var failures = new List<Exception>();
            for (int i = cameras.Count - 1; i >= 0; --i)
                if (!ReferenceEquals(cameras[i], camera)) ReleaseCameraAt(i, failures);
            Report(failures);
            if (camera == null && live)
            {
                if (cameras.Exists(c => ReferenceEquals(c.Camera, live))) return;
                float size = live.orthographicSize;
                if (!(size > 0f) || float.IsInfinity(size)) return;
                camera = new ZoomCameraHold(live, size);
                cameras.Add(camera); ++generation; dirty = true;
            }
            if (camera == null) { Status = "pending-camera"; return; }
            if (camera.Camera.orthographicSize != camera.LastWrite)
            {
                // Neither SetEnvCamera nor RefreshResolution identifies an unknown size writer.
                ResetToVanilla("observable external camera write");
                Status = "camera-yielded"; return;
            }
            if (refreshDepth != 0) return;
            WriteCamera(camera, (float)(camera.Baseline * CurrentViewScale));
            var bg = DolocAPI.envBackgroundEx;
            object? nextPreset = bg ? bg.CurrentPreset : null;
            var env = DolocAPI.EnvCovariantController;
            if (!ReferenceEquals(background, bg) || !ReferenceEquals(preset, nextPreset) || !ReferenceEquals(environment, env)) dirty = true;
            background = bg; preset = nextPreset; environment = env;
            if (dirty)
            {
                ZoomNativeAccess.Discover(targets); ++DiscoveryCount; dirty = false;
                for (int i = sprites.Count - 1; i >= 0; --i)
                    if (!targets.Exists(t => ReferenceEquals(t.Renderer, sprites[i].Renderer) && ReferenceEquals(t.Holder, sprites[i].Holder))) ReleaseSpriteAt(i, failures);
                foreach (var target in targets)
                    if (!sprites.Exists(s => ReferenceEquals(s.Renderer, target.Renderer))) sprites.Add(new ZoomSpriteHold(target, Aspect()));
            }
            float aspect = Aspect();
            for (int i = sprites.Count - 1; i >= 0; --i)
            {
                var hold = sprites[i];
                if (!hold.Renderer) { sprites.RemoveAt(i); continue; }
                if (hold.Retiring)
                {
                    if (TryRestoreSprite(hold, failures)) { sprites.RemoveAt(i); dirty = true; }
                    continue;
                }
                if (!ReferenceEquals(hold.Sprite, hold.Renderer.sprite))
                {
                    if (!TryRestoreSprite(hold, failures)) continue;
                    sprites.RemoveAt(i);
                    if (hold.Renderer.sprite && targets.Exists(t => ReferenceEquals(t.Renderer, hold.Renderer)))
                    {
                        hold = new ZoomSpriteHold(new ZoomSpriteTarget(hold.Renderer, hold.Holder), aspect);
                        sprites.Insert(i, hold);
                    }
                    else continue;
                }
                if (hold.Yielded) continue;
                Vector3 value = hold.Renderer.transform.localScale;
                if (value.x != hold.LastX || value.y != hold.LastY) { hold.Yielded = true; continue; }
                float x = (float)(hold.Baseline.x * CurrentViewScale * Math.Max(1f, aspect / hold.Aspect));
                float y = (float)(hold.Baseline.y * CurrentViewScale);
                if (value.x == x && value.y == y) continue;
                try
                {
                    hold.Renderer.transform.localScale = new Vector3(x, y, value.z);
                    hold.LastX = x; hold.LastY = y; ++ScaleWriteCount;
                }
                catch (Exception ex) { failures.Add(ex); }
            }
            Report(failures);
            Status = failures.Count == 0 ? "applied-product-native" : "restore-or-write-pending";
        }
        internal void ResetToVanilla(string reason)
        {
            ++generation; refreshDepth = 0; CurrentViewScale = 1d; camera = null; dirty = true;
            try { RestoreAll(); Status = "vanilla"; LastMessage = reason; }
            finally { UpdateObservation(); }
        }
        internal void DeactivateOwner(string reason)
        {
            deactivated = true;
            var failures = new List<Exception>();
            try { ResetToVanilla(reason); } catch (Exception ex) { failures.Add(ex); }
            try { ZoomCallbacks.Detach(this); } catch (Exception ex) { failures.Add(ex); }
            try { hooks.UnpatchOwnedHooks(this); } catch (Exception ex) { failures.Add(ex); }
            if (failures.Count != 0) throw new AggregateException("Zoom cleanup remains retryable.", failures);
            Status = "deactivated";
        }
        internal ZoomNativeRefreshState BeforeNativeRefreshResolution(object controller)
        {
            if (deactivated || CurrentViewScale == 1d || camera == null ||
                !ReferenceEquals(controller, DolocAPI.cameraController) || !ReferenceEquals(camera.Camera, DolocAPI.mainCamera)) return default;
            if (camera.Camera.orthographicSize != camera.LastWrite)
            { ResetToVanilla("external write before refresh"); return default; }
            if (refreshDepth == 0) WriteCamera(camera, camera.Baseline);
            ++refreshDepth;
            return new ZoomNativeRefreshState(this, camera, controller, generation);
        }
        internal Exception? AfterNativeRefreshResolution(ZoomNativeRefreshState state, Exception? nativeException)
        {
            if (!ReferenceEquals(state.Runtime, this) || state.Generation != generation || !ReferenceEquals(state.Camera, camera) || refreshDepth == 0) return nativeException;
            --refreshDepth;
            if (refreshDepth != 0) return nativeException;
            try
            {
                if (!ReferenceEquals(state.Controller, DolocAPI.cameraController) || !ReferenceEquals(camera!.Camera, DolocAPI.mainCamera))
                { ++generation; camera = null; dirty = true; return nativeException; }
                if (camera.Camera.orthographicSize != camera.LastWrite)
                { ResetToVanilla("external write during refresh"); return nativeException; }
                WriteCamera(camera, (float)(camera.Baseline * CurrentViewScale)); dirty = true;
            }
            catch (Exception ex) { Status = "refresh-restore-pending"; LastMessage = ex.Message; return nativeException ?? ex; }
            return nativeException;
        }
        private void RestoreAll()
        {
            var failures = new List<Exception>();
            for (int i = cameras.Count - 1; i >= 0; --i) ReleaseCameraAt(i, failures);
            for (int i = sprites.Count - 1; i >= 0; --i) ReleaseSpriteAt(i, failures);
            if (failures.Count != 0) { Report(failures); throw new AggregateException("Zoom native restoration pending.", failures); }
            targets.Clear(); background = null; preset = null; environment = null;
        }
        private void ReleaseCameraAt(int index, List<Exception> failures)
        {
            var hold = cameras[index];
            try
            {
                if (hold.Camera && hold.Camera.orthographicSize == hold.LastWrite) WriteCamera(hold, hold.Baseline);
                cameras.RemoveAt(index);
            }
            catch (Exception ex) { failures.Add(ex); }
        }
        private void ReleaseSpriteAt(int index, List<Exception> failures)
        {
            sprites[index].Retiring = true;
            if (TryRestoreSprite(sprites[index], failures)) sprites.RemoveAt(index);
        }
        private bool TryRestoreSprite(ZoomSpriteHold hold, List<Exception> failures)
        {
            try
            {
                if (!hold.Renderer || hold.Yielded) return true;
                Vector3 value = hold.Renderer.transform.localScale;
                if (value.x != hold.LastX || value.y != hold.LastY) return true;
                if (value.x != hold.Baseline.x || value.y != hold.Baseline.y)
                {
                    hold.Renderer.transform.localScale = new Vector3(hold.Baseline.x, hold.Baseline.y, value.z);
                    ++ScaleWriteCount;
                }
                return true;
            }
            catch (Exception ex) { failures.Add(ex); return false; }
        }
        private static void WriteCamera(ZoomCameraHold hold, float size)
        {
            if (hold.Camera.orthographicSize == size) return;
            hold.Camera.orthographicSize = size; hold.LastWrite = size;
        }
        private static float Aspect() => DolocAPI.mainCamera && DolocAPI.mainCamera.aspect > 0f ? DolocAPI.mainCamera.aspect : 1f;
        private void Report(List<Exception> failures)
        { if (failures.Count != 0) { Status = "restore-or-write-pending"; LastMessage = failures[0].Message; } }
        private void UpdateObservation()
        {
            bool next = !deactivated && (CurrentViewScale > 1d || cameras.Count != 0 || sprites.Count != 0);
            if (next == observing) return;
            observing = next; ObservationChanged?.Invoke(next);
        }
    }
    internal sealed class ZoomCameraHold
    {
        internal ZoomCameraHold(Camera camera, float baseline) { Camera = camera; Baseline = LastWrite = baseline; }
        internal readonly Camera Camera;
        internal readonly float Baseline;
        internal float LastWrite;
    }
    internal sealed class ZoomSpriteHold
    {
        internal ZoomSpriteHold(ZoomSpriteTarget target, float aspect)
        {
            Renderer = target.Renderer; Holder = target.Holder; Sprite = Renderer.sprite;
            Baseline = Renderer.transform.localScale; LastX = Baseline.x; LastY = Baseline.y; Aspect = aspect;
        }
        internal readonly SpriteRenderer Renderer;
        internal readonly object Holder;
        internal readonly Sprite Sprite;
        internal readonly Vector3 Baseline;
        internal readonly float Aspect;
        internal float LastX, LastY;
        internal bool Yielded, Retiring;
    }
    public readonly struct ZoomNativeRefreshState
    {
        internal ZoomNativeRefreshState(ZoomNativeRuntime runtime, ZoomCameraHold camera, object controller, long generation)
        { Runtime = runtime; Camera = camera; Controller = controller; Generation = generation; }
        internal ZoomNativeRuntime? Runtime { get; }
        internal ZoomCameraHold? Camera { get; }
        internal object? Controller { get; }
        internal long Generation { get; }
    }
}
