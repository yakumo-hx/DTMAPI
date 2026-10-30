using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;

namespace Experiment.Doloc3DProbe
{
    // Internal, bounded visual experiment. All temporary native values are returned
    // at endCameraRendering/onPostRender, before the next gameplay/physics tick.
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private static ModEntry? current;
        private readonly Harmony guard = new Harmony("Experiment.Doloc3DProbe.Native");
        private Settings settings = null!;
        private IDtmHelper helper = null!;
        private IInputRegistration? toggle;
        private readonly List<Plane> planes = new List<Plane>();
        private readonly List<Pose> held = new List<Pose>();
        private CameraHold? cameraHold;
        private float started, stableAt, quitAt;
        private string room = "";
        private string? pendingShot;
        private int shotFrame, ticks, mode, frames, restores, restoreFailures, loads, saves, rejectedSaves;
        private int rotationRoundoffs;
        private float maxRotationRoundoff;
        private bool loadRequested, outdoorRequested, ready, disposed, oldBackground;
        private float yaw = 22, pitch = 10, depth = 1;
        private bool orbit;

        public override void Entry(IDtmHelper value)
        {
            helper = value; current = this; settings = value.ReadConfig<Settings>();
            if (!Path.IsPathRooted(settings.Evidence) || DateTime.Parse(settings.ExpiresUtc).ToUniversalTime() < DateTime.UtcNow)
                throw new InvalidOperationException("Fresh bounded probe settings are required.");
            Directory.CreateDirectory(settings.Evidence);
            started = Time.realtimeSinceStartup;
            oldBackground = Application.runInBackground; Application.runInBackground = true;
            var reject = new HarmonyMethod(typeof(ModEntry).GetMethod(nameof(RejectSave)));
            guard.Patch(typeof(DolocAPI).GetMethod("SaveGame", new[] { typeof(int) }), prefix: reject);
            guard.Patch(typeof(DolocTown.GameData.DataPersistenceManager).GetMethod("SaveGame", new[] { typeof(int) }), prefix: reject);
            Camera.onPreCull += BeforeBuiltin;
            Camera.onPostRender += AfterBuiltin;
            // The selected SDK rejects this Unity build's generic Action<,>
            // event signature. Use the two exact verified dispatcher methods,
            // with requiredMembers declared by the build script instead.
            foreach (string name in new[] { "BeginCameraRendering", "EndCameraRendering" })
                guard.Patch(typeof(RenderPipelineManager).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static,
                    null, new[] { typeof(ScriptableRenderContext), typeof(Camera) }, null),
                    postfix: new HarmonyMethod(typeof(ModEntry).GetMethod(name == "BeginCameraRendering" ? nameof(BeforeSrp) : nameof(AfterSrp))));
            helper.Events.GameLoop.UpdateTicked += Tick;
            helper.Events.Save.SaveLoaded += Loaded;
            helper.Events.Save.SaveSaved += Saved;
            helper.Events.GameLoop.ReturnedToTitle += Title;
            toggle = helper.Input.RegisterKeybind("probe.toggle", "F8", DtmInputScope.Gameplay);
            helper.Events.Input.KeybindPressed += Key;
            Write("entry.json", new { Id = helper.ModManifest.UniqueID, Framework = ".NETStandard,Version=v2.0", settings, OriginalRunInBackground = oldBackground });
        }

        public static bool RejectSave(ref bool __result)
        {
            __result = false;
            if (current != null) { ++current.rejectedSaves; current.Write("save-blocked.json", new { current.rejectedSaves }); }
            return false;
        }
        private void Loaded(object sender, SaveLoadedEventArgs e) { ++loads; Write("loaded.json", new { loads, Slot = settings.NativeSlot }); }
        private void Saved(object sender, SaveSavedEventArgs e) { ++saves; Write("unexpected-save.json", new { saves }); }
        private void Title(object sender, ReturnedToTitleEventArgs e) { SetMode(0); ready = false; planes.Clear(); }
        private void Key(object sender, KeybindPressedEventArgs e)
        {
            if (e.OwnerId == "Experiment.Doloc3DProbe" && e.KeybindId == "probe.toggle" && ready)
                SetMode(mode == 0 ? 2 : 0);
        }
        private void Tick(object sender, UpdateTickedEventArgs e)
        {
            try
            {
                ++ticks;
                // A missing render-end callback must never leak a temporary pose
                // into another game-loop iteration. Abort the experiment on it.
                if (cameraHold != null) { Restore(); throw new InvalidOperationException("Render-end callback was missed."); }
                if (restoreFailures > 0 && quitAt == 0) throw new InvalidOperationException("A native transform failed restoration; see restore-mismatch.json.");
                float now = Time.realtimeSinceStartup;
                if (now - started > settings.TimeoutSeconds && quitAt == 0) Quit("deadline");
                if (quitAt > 0 && now >= quitAt) { Application.Quit(); return; }
                if (pendingShot != null && Time.frameCount >= shotFrame)
                {
                    Snapshot(pendingShot); pendingShot = null;
                }
                if (ticks % 15 != 0) return;
                string state = DolocAPI.userInput?.CurrentState?.GetType().Name ?? "";
                if (!loadRequested && state == "HomePageUiState" && now - started > 12)
                {
                    loadRequested = true;
                    // Use the game's existing console load entry: it exits the
                    // title UI before loading. Calling LoadGame alone leaves
                    // HomePageUiState beneath the room transition.
                    typeof(DolocAPI).GetMethod("Command_LoadGame", BindingFlags.Static | BindingFlags.NonPublic,
                        null, new[] { typeof(int) }, null)!.Invoke(null, new object[] { settings.NativeSlot });
                    Write("load-request.json", new { Entry = "DolocAPI.Command_LoadGame", NativeIndex = settings.NativeSlot, PhysicalInput = false });
                }
                if (loads > 0 && DolocAPI.CurrentRoom != null && DolocAPI.IsNormalState)
                {
                    if (!outdoorRequested && settings.EnterOutdoor)
                    {
                        outdoorRequested = true;
                        var house = DolocAPI.CurrentRoom as DolocTown.TemplateRoomInHouse;
                        if (house != null && house.Building != null && house.RootRoom != house)
                        {
                            Write("outdoor-request.json", new { From = house.RoomId, To = house.RootRoom.RoomId, PhysicalInput = false });
                            DolocAPI.EnterRoom(house.RootRoom, house.Building.EntryPosition);
                            stableAt = now + 4;
                            return;
                        }
                    }
                    string nextRoom = DolocAPI.CurrentRoom.RoomId;
                    if (room != nextRoom) { SetMode(0); room = nextRoom; ready = false; stableAt = now + 4; }
                    if (!ready && now >= stableAt)
                    {
                        Discover(); ready = true;
                        Shot("00-original"); Write("ready.json", Status());
                    }
                }
                Write("status.json", Status());
                string path = Path.Combine(settings.Evidence, "command.txt");
                if (!File.Exists(path)) return;
                string command = File.ReadAllText(path).Trim(); File.Delete(path);
                Command(command);
                Write("command-done.json", new { Command = command, Frame = Time.frameCount, Utc = DateTime.UtcNow });
            }
            catch (Exception ex) { Fail(ex); }
        }
        private void Command(string command)
        {
            if (command == "quit") { Quit("command"); return; }
            if (!ready) throw new InvalidOperationException("Scene is not ready.");
            string[] parts = command.Split(':');
            switch (parts[0])
            {
                case "original": SetMode(0); break;
                case "perspective": SetMode(1); break;
                case "depth": SetMode(2); break;
                case "orbit": orbit = !orbit; break;
                case "angle": yaw = Parse(parts[1], -40, 40); pitch = Parse(parts[2], -20, 25); break;
                case "depthscale": depth = Parse(parts[1], 0, 2); break;
                case "snap": Shot(parts[1]); break;
                case "inventory": Discover(); break;
                default: throw new ArgumentException("Unsupported probe command: " + command);
            }
        }
        private static float Parse(string value, float min, float max) => Mathf.Clamp(float.Parse(value, CultureInfo.InvariantCulture), min, max);
        private void SetMode(int value) { Restore(); mode = value; if (value == 0) orbit = false; }
        private void Shot(string label)
        {
            if (label.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("Invalid shot label.");
            pendingShot = label; shotFrame = Time.frameCount + 4;
        }
        private void Quit(string reason)
        {
            SetMode(0); Write("before-quit.json", new { Reason = reason, State = Status() });
            quitAt = Time.realtimeSinceStartup + 2;
        }
        private void Fail(Exception ex)
        {
            mode = 0;
            try { Restore(); } catch (Exception restore) { ++restoreFailures; UnityEngine.Debug.LogException(restore); }
            Write("failure.json", new { Error = ex.ToString(), State = Status() });
            quitAt = Time.realtimeSinceStartup + 2;
        }
        private object Status() => new
        {
            Utc = DateTime.UtcNow, ticks, Frame = Time.frameCount, ready, room, mode, yaw, pitch, depth, orbit,
            frames, restores, restoreFailures, rotationRoundoffs, maxRotationRoundoff, loads, saves, rejectedSaves, Planes = planes.Count,
            Normal = DolocAPI.userInput != null && DolocAPI.IsNormalState,
            NativeState = DolocAPI.userInput?.CurrentState?.GetType().Name,
            Camera = DolocAPI.mainCamera ? CameraInfo(DolocAPI.mainCamera) : null
        };
        private void Discover()
        {
            Restore(); planes.Clear();
            var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
            var unique = new HashSet<Transform>();
            foreach (var r in renderers)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r.GetComponentInParent<Canvas>() != null) continue;
                if (!(r is SpriteRenderer) && r.GetType().Name != "TilemapRenderer" && !(r is MeshRenderer)) continue;
                if (r.sortingLayerName == "SceneUI" || r.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
                if (unique.Add(r.transform)) planes.Add(new Plane { Renderer = r, Transform = r.transform, Level = Level(r.transform) });
            }
            planes.Sort((a, b) => a.Level.CompareTo(b.Level));
            Write("inventory.json", new
            {
                Room = room, Pipeline = GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.GetType().FullName : "Built-in",
                Cameras = Camera.allCameras.Select(CameraInfo).ToArray(),
                Layers = SortingLayer.layers.Select(l => new { l.name, l.id, l.value }).ToArray(),
                Counts = renderers.GroupBy(r => r.GetType().Name).Select(g => new { Type = g.Key, Count = g.Count() }).ToArray(),
                Colliders2D = UnityEngine.Object.FindObjectsOfType<Collider2D>().Length,
                Renderers = planes.Select(p => new { Name = p.Renderer.name, Path = Hierarchy(p.Transform), Type = p.Renderer.GetType().Name,
                    Layer = p.Renderer.sortingLayerName, Order = p.Renderer.sortingOrder, Position = V(p.Transform.position), Scale = V(p.Transform.localScale),
                    BoundsCenter = V(p.Renderer.bounds.center), BoundsSize = V(p.Renderer.bounds.size),
                    Shader = p.Renderer.sharedMaterial ? p.Renderer.sharedMaterial.shader.name : "", Depth = DepthFor(p.Renderer.sortingLayerName) }).ToArray()
            });
        }
        private static int Level(Transform t) { int n = 0; while (t.parent != null) { ++n; t = t.parent; } return n; }
        private static string Hierarchy(Transform t) { string s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
        private static float DepthFor(string layer)
        {
            switch (layer)
            {
                case "Sky": return 36;
                case "Background": return 18;
                case "BuildingsBackground": return 8;
                case "Buildings": return 4;
                case "BuildingsForeground": return 2;
                case "GroundFront": return -1;
                case "Fog": return -2;
                default: return 0;
            }
        }
        private void BeforeBuiltin(Camera camera) { if (!GraphicsSettings.currentRenderPipeline) Before(camera); }
        private void AfterBuiltin(Camera camera) { if (cameraHold != null && cameraHold.Camera == camera) Restore(); }
        public static void BeforeSrp(Camera __1) => current?.Before(__1);
        public static void AfterSrp(Camera __1) { if (current?.cameraHold != null && current.cameraHold.Camera == __1) current.Restore(); }
        private void Before(Camera camera)
        {
            if (disposed || mode == 0 || !ready || camera != DolocAPI.mainCamera || cameraHold != null) return;
            try
            {
                cameraHold = new CameraHold(camera);
                float distance = camera.orthographicSize / Mathf.Tan(35 * Mathf.Deg2Rad * 0.5f);
                Vector3 pivot = new Vector3(camera.transform.position.x, camera.transform.position.y, 0);
                float angle = orbit ? Mathf.Sin((Time.realtimeSinceStartup - started) * 0.65f) * 28 : yaw;
                Quaternion rotation = Quaternion.Euler(pitch, angle, 0);
                if (mode == 2)
                {
                    // Capture ALL local/world baselines before changing any parent.
                    foreach (var plane in planes)
                        if (plane.Transform && plane.Renderer) held.Add(new Pose(plane));
                }
                camera.orthographic = false; camera.fieldOfView = 35;
                camera.nearClipPlane = 0.1f; camera.farClipPlane = Mathf.Max(camera.farClipPlane, 500);
                camera.transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
                if (mode == 2)
                {
                    // Background/fog renderers are camera children in this game.
                    // Set the camera first, then place every plane in world space
                    // so its parent cannot apply the camera orbit a second time.
                    foreach (var pose in held)
                    {
                        float z = DepthFor(pose.Plane.Renderer.sortingLayerName) * depth;
                        // Compensate backdrops at the frontal camera so the original
                        // picture stays filled; oblique views then expose parallax.
                        float factor = z > 0 ? (distance + z) / distance : 1;
                        Vector3 position = pose.World;
                        position.x = pivot.x + (position.x - pivot.x) * factor;
                        position.y = pivot.y + (position.y - pivot.y) * factor;
                        position.z = z;
                        var transform = pose.Plane.Transform;
                        transform.SetPositionAndRotation(position, pose.WorldRotation);
                        Vector3 parentScale = transform.parent ? transform.parent.lossyScale : Vector3.one;
                        transform.localScale = new Vector3(pose.WorldScale.x * factor / parentScale.x,
                            pose.WorldScale.y * factor / parentScale.y, pose.WorldScale.z * factor / parentScale.z);
                    }
                }
                camera.ResetWorldToCameraMatrix(); camera.ResetProjectionMatrix();
                ++frames;
                if (frames == 1 || pendingShot != null)
                    Write("render-state.json", new { mode, Angle = angle, pitch, distance, Planes = held.Count, Camera = CameraInfo(camera), Frame = Time.frameCount });
            }
            catch (Exception ex) { Fail(ex); }
        }
        private void Restore()
        {
            if (cameraHold == null) return;
            var c = cameraHold;
            c.Restore();
            foreach (var pose in held)
            {
                if (!pose.Plane.Transform) continue;
                pose.Plane.Transform.localPosition = pose.Local;
                pose.Plane.Transform.localRotation = pose.Rotation;
                pose.Plane.Transform.localScale = pose.Scale;
                float rotationError = RotationError(pose.Rotation, pose.Plane.Transform.localRotation);
                if (!pose.Plane.Transform.localPosition.Equals(pose.Local) || rotationError > 0.000001f || !pose.Plane.Transform.localScale.Equals(pose.Scale))
                {
                    ++restoreFailures;
                    Write("restore-mismatch.json", new { Kind = "Plane", Frame = Time.frameCount, Path = Hierarchy(pose.Plane.Transform),
                        ExpectedPosition = V(pose.Local), ActualPosition = V(pose.Plane.Transform.localPosition),
                        ExpectedScale = V(pose.Scale), ActualScale = V(pose.Plane.Transform.localScale),
                        ExpectedRotation = Q(pose.Rotation), ActualRotation = Q(pose.Plane.Transform.localRotation) });
                }
            }
            held.Clear();
            if (c.Camera && (!c.Camera.transform.localPosition.Equals(c.Local) || RotationError(c.Rotation, c.Camera.transform.localRotation) > 0.000001f || c.Camera.orthographic != c.Orthographic))
            {
                ++restoreFailures;
                Write("restore-mismatch.json", new { Kind = "Camera", Frame = Time.frameCount,
                    ExpectedPosition = V(c.Local), ActualPosition = V(c.Camera.transform.localPosition),
                    ExpectedRotation = Q(c.Rotation), ActualRotation = Q(c.Camera.transform.localRotation),
                    ExpectedOrthographic = c.Orthographic, ActualOrthographic = c.Camera.orthographic });
            }
            cameraHold = null; ++restores;
        }
        private float RotationError(Quaternion expected, Quaternion actual)
        {
            // Unity normalizes native animated rotations in its setter. run-04
            // measured a 5.96e-8 component roundoff. Keep exact position/scale
            // checks and report rotation roundoff separately, with a 1e-6 bound.
            float error = Mathf.Max(Mathf.Abs(expected.x - actual.x), Mathf.Abs(expected.y - actual.y),
                Mathf.Abs(expected.z - actual.z), Mathf.Abs(expected.w - actual.w));
            if (error > 0) { ++rotationRoundoffs; maxRotationRoundoff = Mathf.Max(maxRotationRoundoff, error); }
            return float.IsNaN(error) ? float.PositiveInfinity : error;
        }
        private void Snapshot(string label)
        {
            Write(label + ".json", new { State = Status(), NativeAgent = DolocAPI.CurrentRoom != null ? V(DolocAPI.AgentPosition) : null,
                Capture = "Unity ScreenCapture; actual rendered game frame; programmatic setup" });
            ScreenCapture.CaptureScreenshot(Path.Combine(settings.Evidence, label + ".png"));
        }
        private static object CameraInfo(Camera c) => new
        {
            Id = c.GetInstanceID(), c.name, c.orthographic, c.orthographicSize, c.fieldOfView, c.nearClipPlane, c.farClipPlane,
            Position = V(c.transform.position), Rotation = V(c.transform.eulerAngles), c.cullingMask, c.depth, c.aspect,
            Components = c.GetComponents<Component>().Where(x => x).Select(x => x.GetType().FullName).ToArray()
        };
        private static object V(Vector3 v) => new { v.x, v.y, v.z };
        private static object Q(Quaternion q) => new { q.x, q.y, q.z, q.w };
        private void Write(string path, object value) => File.WriteAllText(Path.Combine(settings.Evidence, path), JsonConvert.SerializeObject(value, Formatting.Indented));
        public void Dispose()
        {
            if (disposed) return;
            SetMode(0); disposed = true;
            Camera.onPreCull -= BeforeBuiltin; Camera.onPostRender -= AfterBuiltin;
            toggle?.Dispose(); guard.UnpatchSelf(); Application.runInBackground = oldBackground; current = null;
            Write("dispose.json", new { frames, restores, restoreFailures, rotationRoundoffs, maxRotationRoundoff, loads, saves, rejectedSaves, PendingCamera = cameraHold != null,
                PendingPoses = held.Count, RemainingHooks = Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains("Experiment.Doloc3DProbe.Native")) });
            planes.Clear();
        }
        private sealed class Plane { internal Renderer Renderer = null!; internal Transform Transform = null!; internal int Level; }
        private sealed class Pose
        {
            internal readonly Plane Plane;
            internal readonly Vector3 Local, World, Scale, WorldScale;
            internal readonly Quaternion Rotation, WorldRotation;
            internal Pose(Plane p) { Plane = p; Local = p.Transform.localPosition; World = p.Transform.position; Scale = p.Transform.localScale;
                WorldScale = p.Transform.lossyScale; Rotation = p.Transform.localRotation; WorldRotation = p.Transform.rotation; }
        }
        private sealed class CameraHold
        {
            internal readonly Camera Camera;
            internal readonly Vector3 Local;
            internal readonly Quaternion Rotation;
            internal readonly bool Orthographic;
            private readonly float fov, near, far;
            private readonly Matrix4x4 projection, view;
            internal CameraHold(Camera c)
            {
                Camera = c; Local = c.transform.localPosition; Rotation = c.transform.localRotation; Orthographic = c.orthographic;
                fov = c.fieldOfView; near = c.nearClipPlane; far = c.farClipPlane; projection = c.projectionMatrix; view = c.worldToCameraMatrix;
            }
            internal void Restore()
            {
                if (!Camera) return;
                Camera.transform.localPosition = Local; Camera.transform.localRotation = Rotation;
                Camera.orthographic = Orthographic; Camera.fieldOfView = fov; Camera.nearClipPlane = near; Camera.farClipPlane = far;
                Camera.projectionMatrix = projection; Camera.worldToCameraMatrix = view;
                // This game normally uses automatic matrices. Returning those
                // matrices explicitly would freeze native following next frame.
                Camera.ResetProjectionMatrix(); Camera.ResetWorldToCameraMatrix();
            }
        }
    }
    public sealed class Settings
    {
        public string Evidence { get; set; } = "";
        public string ExpiresUtc { get; set; } = "";
        public int NativeSlot { get; set; } = 1;
        public bool EnterOutdoor { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 420;
    }
}
