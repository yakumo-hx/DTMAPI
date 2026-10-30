using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using DTMAPI.Abstractions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using DolocTown;

namespace DTMAPI.Doloc25D
{
    internal sealed class FarmPresentation : IDisposable
    {
        internal const string Owner = "DTMAPI.Doloc25D.Native";
        private static FarmPresentation? current;
        private readonly IDtmHelper helper;
        private readonly Harmony harmony = new Harmony(Owner);
        private readonly string assets;
        private Doloc25DConfig config;
        private VisualGraph? graph;
        private CameraState? hold;
        private object? room;
        private bool disposed, conflict, reportedConflict, shownConflict;
        private string fault = "";
        private float nextDrain;
        private int frameProjection = -1;
        internal readonly ViewProjection Projection = new ViewProjection();
        internal long Frames, Restores, CostTicks;
        internal int RestoreFailures;
        internal bool SupportedRoom => DolocAPI.CurrentRoom != null && DolocAPI.CurrentRoom.RoomId == "farm_type1-平地";
        internal bool Active => !disposed && fault.Length == 0 && !conflict && config.Enabled && SupportedRoom && graph != null;
        internal FarmPresentation(IDtmHelper helper, Doloc25DConfig config, string assets)
        { this.helper = helper; this.config = config; this.assets = assets; }
        internal void Install()
        {
            current = this;
            foreach (string name in new[] { "BeginCameraRendering", "EndCameraRendering" })
                harmony.Patch(typeof(RenderPipelineManager).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(ScriptableRenderContext), typeof(Camera) }, null),
                    postfix: Callback(name == "BeginCameraRendering" ? nameof(BeforeSrp) : nameof(AfterSrp)));
            harmony.Patch(typeof(DolocAPI).GetMethod("ScreenToWorld", new[] { typeof(Vector2) }), prefix: Callback(nameof(ScreenToWorld)));
            harmony.Patch(typeof(DolocAPI).GetMethod("WorldToScreen", new[] { typeof(Vector2) }), prefix: Callback(nameof(WorldToScreen)));
            harmony.Patch(typeof(DolocAPI).GetMethod("WorldToScreen", new[] { typeof(Transform) }), prefix: Callback(nameof(TransformToScreen)));
            foreach (string name in new[] { "OnCreated", "OnReuse" })
                harmony.Patch(typeof(DolocRecyclableObject).GetMethod(name), postfix: Callback(nameof(EntityReady)));
            harmony.Patch(typeof(Tilemap).GetMethod("SetTile", new[] { typeof(Vector3Int), typeof(TileBase) }), postfix: Callback(nameof(TilesChanged)));
            harmony.Patch(typeof(Tilemap).GetMethod("SetTiles", new[] { typeof(Vector3Int[]), typeof(TileBase[]) }), postfix: Callback(nameof(TilesChanged)));
            // Unity ClearAllTiles is an InternalCall; Harmony cannot fully
            // detach its generated native wrapper. Observe managed owners.
            foreach (string name in new[] { "Clear", "OnRecycle" })
                harmony.Patch(typeof(PlatformIndicator).GetMethod(name, Type.EmptyTypes), postfix: Callback(nameof(PreviewCleared)));
        }
        public static void EntityReady(DolocRecyclableObject __instance) { if (current?.Active == true) current.graph?.Track(__instance.gameObject); }
        public static void TilesChanged(Tilemap __instance) { if (current?.Active == true) current.graph?.Changed(__instance); }
        public static void PreviewCleared(PlatformIndicator __instance) { if (current?.Active == true) current.graph?.Changed(__instance.tilemap); }
        private static HarmonyMethod Callback(string name) => new HarmonyMethod(typeof(FarmPresentation).GetMethod(name, BindingFlags.Static | BindingFlags.Public));
        internal void Configure(Doloc25DConfig value) { value.Normalize(); config = value; ClearFault(); }
        internal void ClearFault() { fault = ""; frameProjection = -1; }
        internal void Tick()
        {
            if (disposed) return;
            try
            {
                if (hold != null) { End(); throw new InvalidOperationException("Main camera render-end callback was missed."); }
                if (!ReferenceEquals(room, DolocAPI.CurrentRoom)) { SceneChanged(); room = DolocAPI.CurrentRoom; }
                conflict = helper.ModRegistry.IsLoaded("DTMAPI.ZoomMod");
                if (conflict && !reportedConflict) { reportedConflict = true; helper.Monitor.Log("Doloc25D 已暂停：Zoom 同时加载，请只启用一个相机控制 Mod 后重启。", LogLevel.Warn); }
                if (conflict && !shownConflict && DolocAPI.IsNormalState)
                { shownConflict = true; DolocAPI.ShowMessageBoxSmall("农场立体化已暂停：检测到 Zoom。请只启用一个相机控制 Mod 后重启。", 8); }
                if (!SupportedRoom || !config.Enabled || conflict || fault.Length != 0) { DestroyGraph(); return; }
                if (!DolocAPI.mainCamera) return;
                if (graph == null && DolocAPI.IsNormalState)
                { graph = new VisualGraph(assets, DolocAPI.mainCamera); graph.Discover(); nextDrain = Time.unscaledTime + .15f; }
                else if (graph != null && Time.unscaledTime >= nextDrain)
                { graph.Drain(); nextDrain = Time.unscaledTime + .15f; }
                frameProjection = -1;
            }
            catch (Exception e) { Fail(e); }
        }
        internal void SceneChanged() { End(); DestroyGraph(); frameProjection = -1; room = null; }
        private void DestroyGraph() { graph?.Dispose(); graph = null; }
        private void PrepareProjection()
        {
            if (frameProjection == Time.frameCount) return;
            var c = DolocAPI.mainCamera;
            Vector3 position = hold != null ? hold.WorldPosition : c.transform.position;
            Rect rect = c.pixelRect;
            Projection.Configure(position.x, position.y, c.orthographicSize, config.Yaw, config.Pitch, config.ViewScale,
                rect.x, rect.y, rect.width, rect.height);
            frameProjection = Time.frameCount;
        }
        public static void BeforeSrp(Camera __1) => current?.Begin(__1);
        public static void AfterSrp(Camera __1) { if (current?.hold?.Camera == __1) current.End(); }
        public static bool ScreenToWorld(Vector2 __0, ref Vector2 __result)
        {
            if (current == null || !current.Active) return true;
            current.PrepareProjection(); var p = current.Projection.Unproject(__0.x, __0.y);
            __result = new Vector2((float)p.X, (float)p.Y); return false;
        }
        public static bool WorldToScreen(Vector2 __0, ref Vector2 __result)
        {
            if (current == null || !current.Active) return true;
            current.PrepareProjection(); var p = current.Projection.Project(new Point3(__0.x, __0.y, 0));
            __result = new Vector2((float)p.X, (float)p.Y); return false;
        }
        public static bool TransformToScreen(Transform __0, ref Vector2 __result) => WorldToScreen(__0.position, ref __result);
        private void Begin(Camera camera)
        {
            if (!Active || camera != DolocAPI.mainCamera || hold != null) return;
            long start = Stopwatch.GetTimestamp();
            try
            {
                if (graph!.PreviewDirty) graph.Drain();
                PrepareProjection(); hold = new CameraState(camera);
                var p = Projection.Position;
                camera.transform.SetPositionAndRotation(new Vector3((float)p.X, (float)p.Y, (float)p.Z), Quaternion.Euler((float)config.Pitch, (float)config.Yaw, 0));
                camera.orthographic = false; camera.fieldOfView = 35; camera.nearClipPlane = .1f; camera.farClipPlane = Math.Max(500, camera.farClipPlane);
                camera.transparencySortMode = TransparencySortMode.CustomAxis; camera.transparencySortAxis = Vector3.forward;
                camera.ResetProjectionMatrix(); camera.ResetWorldToCameraMatrix();
                graph!.Begin(Projection, hold.WorldPosition, camera);
                ++Frames;
            }
            catch (Exception e) { Fail(e); }
            finally { CostTicks += Stopwatch.GetTimestamp() - start; }
        }
        private void End()
        {
            if (hold == null) return;
            long start = Stopwatch.GetTimestamp();
            var state = hold;
            try { graph?.End(); }
            finally
            {
                try { state.Restore(); if (!state.IsRestored()) ++RestoreFailures; ++Restores; }
                finally { hold = null; CostTicks += Stopwatch.GetTimestamp() - start; }
            }
        }
        private void Fail(Exception e)
        {
            fault = e.ToString();
            try { End(); DestroyGraph(); } catch (Exception cleanup) { fault += "\nCleanup: " + cleanup; }
            helper.Monitor.Log("Doloc25D 已恢复原版并暂停：" + fault, LogLevel.Error);
        }
        internal object Diagnostics() => new { Active, SupportedRoom, Frames, Restores, RestoreFailures,
            AverageCpuMs = Frames == 0 ? 0 : CostTicks * 1000.0 / Stopwatch.Frequency / Frames,
            Fault = fault, Conflict = conflict, PendingCamera = hold != null, Config = config, Graph = graph?.Diagnostics() };
        internal object? Inventory() => graph?.Inventory();
        public void Dispose()
        {
            if (disposed) return;
            try { End(); DestroyGraph(); }
            finally { harmony.UnpatchSelf(); current = null; disposed = true; }
            int remaining = Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains(Owner));
            UnityEngine.Debug.Log("[Doloc25D] Cleanup: hooks=" + remaining + "; pendingCamera=" + (hold != null) + "; graph=" + (graph != null) + "; restoreFailures=" + RestoreFailures);
        }
        private sealed class CameraState
        {
            internal readonly Camera Camera;
            internal readonly Vector3 WorldPosition;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly bool orthographic;
            private readonly float fov, near, far;
            private readonly TransparencySortMode sortMode;
            private readonly Vector3 sortAxis;
            internal CameraState(Camera c) { Camera = c; WorldPosition = c.transform.position; localPosition = c.transform.localPosition;
                localRotation = c.transform.localRotation; orthographic = c.orthographic; fov = c.fieldOfView; near = c.nearClipPlane; far = c.farClipPlane; sortMode = c.transparencySortMode; sortAxis = c.transparencySortAxis; }
            internal void Restore() { if (!Camera) return; Camera.transform.localPosition = localPosition; Camera.transform.localRotation = localRotation;
                Camera.orthographic = orthographic; Camera.fieldOfView = fov; Camera.nearClipPlane = near; Camera.farClipPlane = far;
                Camera.transparencySortMode = sortMode; Camera.transparencySortAxis = sortAxis;
                Camera.ResetProjectionMatrix(); Camera.ResetWorldToCameraMatrix(); }
            internal bool IsRestored() => !Camera || (Camera.transform.localPosition.Equals(localPosition) &&
                Quaternion.Angle(Camera.transform.localRotation, localRotation) < .0001f && Camera.orthographic == orthographic &&
                Camera.fieldOfView == fov && Camera.nearClipPlane == near && Camera.farClipPlane == far && Camera.transparencySortMode == sortMode && Camera.transparencySortAxis == sortAxis);
        }
    }
}
