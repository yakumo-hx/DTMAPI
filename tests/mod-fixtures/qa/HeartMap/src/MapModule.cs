using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.GameData;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using NativeScenes = DolocTown.SceneManager;
using SceneInfo = DolocTown.SceneInfo;
using Scenes = UnityEngine.SceneManagement.SceneManager;

namespace DTMAPI.Internal.HeartMap
{
    internal sealed partial class MapModule : IDisposable
    {
        internal const string Owner = "DTMAPI.Internal.HeartMap.Native";
        private static MapModule? active;
        private readonly IDtmHelper helper;
        internal readonly ExperimentConfig Config;
        private readonly Harmony harmony = new Harmony(Owner);
        private readonly List<IDisposable> registrations = new List<IDisposable>();
        private readonly MapCatalog catalog = new MapCatalog();
        private readonly Dictionary<string, MapRegistration> maps = new Dictionary<string, MapRegistration>(StringComparer.Ordinal);
        private readonly Dictionary<int, MapScene> ownedScenes = new Dictionary<int, MapScene>();
        private readonly List<WeakReference> retiredObjects = new List<WeakReference>();
        private MapDiagnostics? diagnostics;
        private GameObject? diagnosticObject, panelObject;
        internal MapPanel? Panel;
        private int generation, completedEntries, completedExits, blockedSaves, snapshots;
        private long request;
        private string defaultMapId = "";
        private bool closed, priorPaused, pauseOwned, overview, loops;
        private float oldCameraSize;
        private bool oldCameraEnabled;
        internal bool IsInMap => FindRoom(DolocAPI.CurrentRoom?.RoomId) != null;
        internal string ControlLabel => Config.DiagnosticControl ? "native-world-control" : maps.Count == 0 ? "host-without-pack" : IsInMap ? "host-pack-in-map" : "host-pack-native-farm";
        internal MapRequest? LastResult { get; private set; }
        private bool transitioning => travel != null;
        private MapRegistration? FindRoom(string? id) => maps.Values.FirstOrDefault(m => m.Content.RoomId == id);
        internal MapModule(IDtmHelper helper, ExperimentConfig config) { this.helper = helper; Config = config; }

        internal void Start()
        {
            if (!Config.NoNativeSave) throw new InvalidOperationException("HeartMap only supports NoNativeSave.");
            if (active != null) throw new InvalidOperationException("Only one HeartMap module may be active.");
            active = this;
            Patch(typeof(DolocAPI), "SaveGame", new[] { typeof(int) }, nameof(BlockSave));
            Patch(typeof(LocalSave), "SaveGame", new[] { typeof(ArchiveDataHandle), typeof(int) }, nameof(BlockSave));
            if (Config.Enabled && Config.RegisterMap)
            {
                var files = new MapFileInput(Path.Combine(Path.GetDirectoryName(helper.Config.GetConfigPath(helper.ModManifest))!, "DTMAPI.Internal.HeartMap.inputs"), Log);
                if ((Config.EditorFiles?.Length ?? 0) > 8) Log("file-rejected field=EditorFiles; at most eight explicit files; list rejected");
                else foreach (string input in (Config.EditorFiles ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase))
                    if (files.Load(input, catalog, out var content)) AddContent(content!);
                foreach (string input in (Config.PrivateMaps ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal))
                {
                    using var stream = typeof(MapModule).Assembly.GetManifestResourceStream("Maps." + input + ".dtmap.json");
                    if (stream == null) { Log("input-rejected name=" + input + "; private input absent"); continue; }
                    using var reader = new StreamReader(stream);
                    if (!catalog.TryAdd(reader.ReadToEnd(), out var content, out var error)) { Log("input-rejected name=" + input + "; " + error); continue; }
                    AddContent(content!);
                }
                if (maps.Count != 0)
                {
                    TransitionRoom = RequireTransitionField("targetRoom", typeof(Room));
                    TransitionCallback = RequireTransitionField("callback", typeof(Action));
                    Patch(typeof(DolocAPI), "LoadGame", new[] { typeof(int) }, nameof(BeforeLoad));
                    Patch(typeof(DolocAPI), "NewGame", new[] { typeof(int) }, nameof(BeforeLoad));
                    Patch(typeof(DolocAPI), "ReturnHome", new[] { typeof(bool) }, nameof(BeforeTitle));
                    Patch(typeof(DolocBundleManager), "LoadDungeons", Type.EmptyTypes, nameof(AfterDungeons), true);
                    Patch(typeof(NativeScenes), "QueryScene", new[] { typeof(string), typeof(SceneInfo).MakeByRefType() }, nameof(QueryScene));
                    Patch(typeof(NativeScenes), "IsSceneExists", new[] { typeof(string) }, nameof(Exists));
                    Patch(typeof(NativeScenes), "IsSceneLoaded", new[] { typeof(string) }, nameof(Loaded));
                    Patch(typeof(NativeScenes), "LoadSceneAsync", new[] { typeof(string), typeof(Action) }, nameof(LoadScene));
                    Patch(typeof(NativeScenes), "UnloadSceneAsync", new[] { typeof(string), typeof(Action) }, nameof(UnloadScene));
                    Patch(typeof(NativeScenes), "GetSceneHandle", new[] { typeof(string), typeof(string) }, nameof(GetHandle));
                    Patch(typeof(NativeScenes), "GetAllSceneHandles", new[] { typeof(string) }, nameof(GetHandles));
                    Patch(typeof(NativeScenes), "UnloadAll", Type.EmptyTypes, nameof(UnloadAll), true);
                    Patch(typeof(GameStateManager), "TransitScene", new[] { typeof(Room), typeof(Vector2), typeof(Action), typeof(bool) }, nameof(BeforeTransport));
                    Patch(typeof(GameStateSceneTransition), "Start", new[] { typeof(Room), typeof(Vector2), typeof(Action), typeof(bool) }, nameof(BeforeTransition));
                    Patch(typeof(GameStateSceneTransition), "TryScreenTransitionFadeIn", Type.EmptyTypes, nameof(BeforeFade));
                    Patch(typeof(GameStateSceneTransition), "OnSceneLoaded", Type.EmptyTypes, nameof(BeforeSceneLoaded));
                    Register("Entry");
                }
            }
            helper.Events.Save.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnTitle;
            helper.Events.Input.ButtonPressed += OnButton;
            for (int key = 5; key <= 12; key++) helper.Input.RegisterButton("F" + key);
            var commands = helper.GetOptionalService<IDtmCommands>();
            if (commands != null) registrations.Add(commands.Register("map", c => Execute(string.Join(" ", c.Arguments)), description: "内部地图 / 不保存", parameters: "enter [map-id]|exit|farm|fault [stage]|cancel|status"));
            if (Config.DiagnosticPanel)
            {
                panelObject = new GameObject("HeartMap opt-in test panel"); Object.DontDestroyOnLoad(panelObject);
                Panel = panelObject.AddComponent<MapPanel>(); Panel.Owner = this;
            }
            Log("enabled; maps=" + maps.Count + "; noNativeSave=true; hooks=" + harmony.GetPatchedMethods().Count() + "; no frame subscription");
            if (!string.IsNullOrEmpty(Config.AcceptanceScenario))
            {
                helper.Events.GameLoop.GameLaunched += (_, __) => StartAcceptance();
                StartAcceptance();
            }
        }
        private void AddContent(MapContent content)
        {
            maps.Add(content.SceneName, new MapRegistration(content));
            if (defaultMapId.Length == 0) defaultMapId = content.Definition.Id;
            Log("input-accepted map=" + content.Definition.Id + "; room=" + content.Room.Id + "; native=" + content.RoomId + "; size=" + content.Room.Width + "x" + content.Room.Height + "; revision=" + content.Definition.Revision);
        }
        private void Patch(Type type, string name, Type[] args, string callback, bool postfix = false)
        {
            MapMetrics.ReflectionResolutions += 2;
            MethodInfo target = AccessTools.Method(type, name, args) ?? throw new MissingMethodException(type.FullName, name);
            var hook = new HarmonyMethod(typeof(MapModule).GetMethod(callback, BindingFlags.Static | BindingFlags.NonPublic)!);
            harmony.Patch(target, prefix: postfix ? null : hook, postfix: postfix ? hook : null);
        }
        private void Register(string boundary)
        {
            foreach (var map in maps.Values)
                try { map.Register(); Log("registration boundary=" + boundary + "; map=" + map.Content.Definition.Id + "; ready=" + map.Registered + "; index=-1 descriptor only"); }
                catch (Exception ex) { Log("registration-rejected map=" + map.Content.Definition.Id + "; other maps retained; " + ex.Message); }
        }
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs args)
        {
            using var metric = MapMetrics.Measure();
            if (closed) return;
            Invalidate("save-generation", true);
            priorPaused = DolocAPI.gameLoop.IsGlobalPaused; DolocAPI.gameLoop.IsGlobalPaused = true; pauseOwned = true;
            int epoch = generation;
            DolocAPI.StartCoroutine(PauseAfterNativeLoad());
            IEnumerator PauseAfterNativeLoad()
            {
                var until = Time.realtimeSinceStartup + 20;
                while (!closed && epoch == generation && Time.realtimeSinceStartup < until)
                {
                    if (DolocAPI.CurrentRoom != null && DolocAPI.userInput.CurrentState is NormalGameState)
                    {
                        DolocAPI.gameLoop.IsGlobalPaused = true;
                        Log("calendar paused after native world readiness; movement parameters unchanged");
                        yield break;
                    }
                    yield return null;
                }
            }
            Log("SaveLoaded slot=" + args.SaveSlot + "; generation=" + generation + "; registeredMaps=" + maps.Values.Count(m => m.Registered) + "; worldReady=" + (DolocAPI.CurrentRoom != null));
        }
        private void OnTitle(object? sender, ReturnedToTitleEventArgs args)
        {
            using var metric = MapMetrics.Measure();
            if (closed) return;
            Invalidate("title-event", true);
            if (pauseOwned && DolocAPI.gameLoop != null) DolocAPI.gameLoop.IsGlobalPaused = priorPaused;
            pauseOwned = false;
            Safe(() => diagnostics?.Stop("title"), "diagnostics-title");
            Log("ReturnedToTitle; generation=" + generation + "; no retained Room/player request; ownedScenes=" + ownedScenes.Count);
            if (Config.EditorFiles?.Length > 0) Log("editor-input boundary=title; accepted definitions retained; restart game to apply file/config changes");
        }
        private void OnButton(object? sender, ButtonPressedEventArgs args)
        {
            using var metric = MapMetrics.Measure();
            switch (args.Button)
            {
                case "F8": Execute("enter"); break; case "F9": Execute("exit"); break;
                case "F10": Execute("overview"); break; case "F11": Execute("capture"); break;
                case "F12": Execute("cycles"); break; case "F7": Execute("fault prepare"); break;
                case "F6": Execute("measure"); break; case "F5": Execute("trace"); break;
            }
        }
        internal void Execute(string command)
        {
            if (closed) return;
            string[] args = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string action = args.Length == 0 ? "status" : args[0].ToLowerInvariant();
            try
            {
                switch (action)
                {
                    case "enter": Enter(args.Length > 1 ? args[1] : defaultMapId); break;
                    case "second": Enter("asymmetric-map"); break;
                    case "exit": Exit(true); break; case "farm": Exit(false); break;
                    case "cancel": if (travel != null) travel.Failure = "cancelled"; break;
                    case "fault":
                        if (!Ready()) break;
                        string fault = args.Length > 1 ? args[1] : "prepare";
                        if (!Faults.Contains(fault)) throw new InvalidOperationException("Unknown injection " + fault);
                        nextFault = fault;
                        if (fault == "unload" && IsInMap) Exit(false); else Enter(IsInMap ? "asymmetric-map" : "heart-map");
                        break;
                    case "bad-input": catalog.TryAdd("{", out _, out var error); Log("bad-input rejected=" + error + "; retainedMaps=" + maps.Count); break;
                    case "overview": ToggleOverview(); break;
                    case "cycles": if (!loops && Ready()) DolocAPI.StartCoroutine(Cycles()); break;
                    case "capture": Capture("manual"); break; case "measure": BeginMeasure(); break;
                    case "trace": BeginMeasure(true); break; default: LogStatus("requested"); break;
                }
            }
            catch (Exception ex) { Log("operation rejected " + command + ": " + ex); }
        }
        internal bool Ready() => !closed && DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState is NormalGameState && DolocAPI.CurrentRoom != null && !transitioning;
        private void Enter(string id)
        {
            if (!Ready()) return;
            var map = maps.Values.FirstOrDefault(m => m.Content.Definition.Id == id);
            if (map == null || !map.Registered || map.Quarantined) { Log("entry-rejected map=" + id + "; unavailable/conflicted/quarantined"); return; }
            if (DolocAPI.CurrentRoom.RoomId == map.Content.RoomId) return;
            if (!DolocAPI.QueryRoom(map.Content.RoomId, out var room)) { Log("entry-rejected native room absent " + id); return; }
            BeginTravel(room, map.Spawn, map);
        }
        private void Exit(bool requireAtExit)
        {
            if (!Ready() || !IsInMap) return;
            var map = FindRoom(DolocAPI.CurrentRoom.RoomId)!;
            if (requireAtExit && Vector2.Distance(DolocAPI.AgentPosition, map.Exit) > 5f) { Log("Return to the exit sign and press F9."); return; }
            var farm = DolocAPI.archiveHandle.MainFarm;
            if (!farm.Geometry.GetNearestGroundPositionCanPlaceAgent(new Vector2Int(20, 0), out var ground)) throw new InvalidOperationException("Main farm has no validated standing point.");
            BeginTravel(farm, farm.Geometry.CalcWorldPosition(ground, new Vector2(.5f, 0)) + new Vector2(0, .1f), null);
        }
        private IEnumerator Cycles()
        {
            loops = true; int epoch = generation;
            for (int i = 1; i <= 20 && epoch == generation && !closed; i++)
            {
                if (IsInMap) { Exit(false); while (transitioning && epoch == generation) yield return null; }
                if (!Ready() || epoch != generation) break;
                Enter(i % 2 == 0 ? "asymmetric-map" : "heart-map");
                while (transitioning && epoch == generation) yield return null;
                if (!IsInMap || !Ready() || LastResult?.Result != MapResult.Entered) break;
                Exit(false); while (transitioning && epoch == generation) yield return null;
                if (!Ready() || IsInMap || LastResult?.Result != MapResult.Exited) break;
                LogStatus("cycle-" + i);
            }
            loops = false; Log("cycles-finished entries=" + completedEntries + "; exits=" + completedExits);
        }
        private void ToggleOverview()
        {
            if (!Ready() || !IsInMap) return;
            if (overview) { RestoreOverview(); return; }
            oldCameraSize = DolocAPI.mainCamera.orthographicSize; oldCameraEnabled = DolocAPI.cameraController.enabled;
            DolocAPI.cameraController.enabled = false;
            var room = FindRoom(DolocAPI.CurrentRoom.RoomId)!.Content.Room;
            DolocAPI.mainCamera.orthographicSize = Math.Max(room.Height * .8f, 10);
            var p = DolocAPI.mainCamera.transform.position;
            DolocAPI.mainCamera.transform.position = new Vector3(room.Width * .75f, room.Height * .75f, p.z);
            overview = true;
        }
        private void RestoreOverview()
        {
            if (!overview) return;
            overview = false;
            DolocAPI.mainCamera.orthographicSize = oldCameraSize; DolocAPI.cameraController.enabled = oldCameraEnabled;
            DolocAPI.cameraController.SetResolution(Screen.width, Screen.height);
            if (DolocAPI.IsDataLoaded) DolocAPI.cameraController.SetPosition(DolocAPI.AgentPosition);
        }
        internal void Capture(string label)
        {
            LogStatus(label); if (string.IsNullOrWhiteSpace(Config.EvidenceDirectory)) return;
            Directory.CreateDirectory(Config.EvidenceDirectory);
            string path = Path.Combine(Config.EvidenceDirectory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + (++snapshots).ToString("D3") + "-" + label + ".png");
            ScreenCapture.CaptureScreenshot(path); Log("screenshot=" + path);
        }
        internal void LogStatus(string label)
        {
            CollectRetired();
            var currentHandle = DolocAPI.CurrentRoom?.SceneHandle?.GameObject;
            Log("status " + label + "; room=" + DolocAPI.CurrentRoom?.RoomId + "; position=" + (DolocAPI.IsDataLoaded ? DolocAPI.AgentPosition.ToString("F2") : "-") +
                "; normal=" + (DolocAPI.IsDataLoaded && DolocAPI.IsNormalState) + "; agentActive=" + (DolocAPI.IsDataLoaded && DolocAPI.agent.gameObject.activeInHierarchy) +
                "; nativeState=" + DolocAPI.userInput.CurrentState?.GetType().Name + "; stack=" + string.Join(",", DolocAPI.userInput.stateStack) +
                "; pendingDialogue=" + (DolocAPI.IsDataLoaded ? DolocAPI.archiveHandle.cityData.dialogueManager.UnhandledDialogueNodes.Count : -1) +
                "; handleActive=" + (currentHandle != null && currentHandle.activeInHierarchy) +
                "; fadeOnce=" + DolocAPI.ppm.DisableFadeInOnce + "/" + DolocAPI.ppm.DisableFadeOutOnce +
                "; camera=" + DolocAPI.cameraController.enabled + "; fade=" + DolocAPI.ppm.IsEnabled(PPTypes.FADEINOUT) +
                "; ownScenes=" + ownedScenes.Count + "; unityScenes=" + Scenes.sceneCount + "; ownColliders=" + ownedScenes.Values.Where(s => !s.Retired).Sum(s => s.ColliderCount) +
                "; retiredUnityObjectsAlive=" + retiredObjects.Count(w => w.Target is Object value && value != null) + "; blockedSaves=" + blockedSaves +
                "; result=" + LastResult?.Result + "; phase=" + travel?.Request.Phase);
        }
        internal void LogResources() { foreach (var scene in ownedScenes.Values.Where(s => !s.Retired)) Log(scene.DescribeResources()); }
        private void BeginMeasure(bool trace = false)
        {
            if (!Ready() || string.IsNullOrWhiteSpace(Config.EvidenceDirectory)) return;
            if (diagnosticObject == null) { diagnosticObject = new GameObject("HeartMap bounded diagnostics"); Object.DontDestroyOnLoad(diagnosticObject); diagnostics = diagnosticObject.AddComponent<MapDiagnostics>(); }
            diagnostics!.Begin(this, trace);
        }
        internal void Log(string text) => helper.Monitor.Log("HeartMap " + text);
        private void Safe(Action action, string stage) { try { action(); } catch (Exception ex) { Log("cleanup-failed stage=" + stage + "; " + ex.Message); } }
        private void Retire(MapScene scene, bool inject = false)
        {
            if (scene.Retired) return;
            if (scene.Root != null) retiredObjects.Add(new WeakReference(scene.Root));
            foreach (var asset in scene.OwnedAssets) retiredObjects.Add(new WeakReference(asset));
            Safe(() => scene.Unload(inject), "scene-unload-" + scene.Content.Definition.Id);
            if (maps.TryGetValue(scene.Content.SceneName, out var map) && scene.UnloadFailed) map.Quarantined = true;
        }
        private void CollectRetired()
        {
            foreach (var pair in ownedScenes.Where(p => p.Value.Retired && !p.Value.UnloadFailed && (p.Value.Unloading == null || p.Value.Unloading.isDone) && (!p.Value.Scene.IsValid() || !p.Value.Scene.isLoaded)).ToArray())
            {
                ownedScenes.Remove(pair.Key);
                if (maps.TryGetValue(pair.Value.Content.SceneName, out var map) && ReferenceEquals(map.Scene, pair.Value)) { map.Scene = null; map.Quarantined = false; }
            }
            retiredObjects.RemoveAll(w => !(w.Target is Object value) || value == null);
            // CreateScene can fail before a handle enters ownedScenes. The map's
            // candidate still belongs to this generation and must be releasable.
            foreach (var map in maps.Values)
                if (map.Scene?.Retired == true && !map.Scene.Scene.IsValid()) { map.Scene = null; map.Quarantined = false; }
        }
        private void ReleaseAll()
        {
            foreach (var scene in ownedScenes.Values.Concat(maps.Values.Select(m => m.Scene).OfType<MapScene>()).Distinct().ToArray())
            {
                if (scene.Retired && scene.UnloadFailed) Safe(scene.RetryUnloadAtTitle, "title-quarantine-unload");
                else Retire(scene);
            }
            CollectRetired();
        }
        public void Dispose()
        {
            if (closed) return;
            if (DolocAPI.IsDataLoaded && (IsInMap || travel != null)) Safe(() => DolocAPI.ReturnHome(), "dispose-native-title");
            Invalidate("dispose", true); closed = true;
            foreach (var r in registrations) Safe(r.Dispose, "command"); registrations.Clear();
            Safe(() => diagnostics?.Stop("dispose"), "diagnostics-dispose");
            if (diagnosticObject != null) Object.Destroy(diagnosticObject); if (panelObject != null) Object.Destroy(panelObject);
            diagnostics = null; diagnosticObject = panelObject = null;
            Safe(RestoreOverview, "camera"); Safe(ReleaseAll, "scenes");
            if (pauseOwned && DolocAPI.gameLoop != null) DolocAPI.gameLoop.IsGlobalPaused = priorPaused;
            foreach (var map in maps.Values) Safe(map.RemoveOwnedRows, "metadata");
            harmony.UnpatchSelf(); active = null;
            Log("disposed; prototypes process-scoped; no native save; quarantined=" + ownedScenes.Count);
        }
        private static bool BlockSave(ref bool __result) { using var metric = MapMetrics.Measure(); if (active == null) return true; active.blockedSaves++; active.Log("BLOCKED native save attempt (NoNativeSave)"); __result = false; return false; }
        private static void BeforeLoad() { using var metric = MapMetrics.Measure(); active?.Register("LoadGame-before-deserialization"); }
        private static void AfterDungeons() { using var metric = MapMetrics.Measure(); active?.Register("native-LoadDungeons-postfix"); }
        private static MapRegistration? Own(string name) => active != null && active.maps.TryGetValue(name, out var map) && map.Registered ? map : null;
        private static bool QueryScene(string __0, ref SceneInfo __1, ref bool __result) { using var metric = MapMetrics.Measure(); var map = Own(__0); if (map == null || !map.Registered) return true; __1 = map.SceneInfo; __result = true; return false; }
        private static bool Exists(string __0, ref bool __result) { using var metric = MapMetrics.Measure(); var map = Own(__0); if (map == null) return true; __result = map.Registered; return false; }
        private static bool Loaded(string __0, ref bool __result) { using var metric = MapMetrics.Measure(); var map = Own(__0); if (map == null) return true; __result = map.Scene?.Published == true && !map.Scene.Retired; return false; }
        private static bool GetHandle(string __0, string __1, ref ISceneHandle __result) { using var metric = MapMetrics.Measure(); var map = Own(__0); if (map == null) return true; __result = (string.IsNullOrEmpty(__1) || __1 == map.Content.RoomName) && map.Scene?.Published == true ? map.Scene.Handle : null!; return false; }
        private static bool GetHandles(string __0, ref ISceneHandle[] __result) { using var metric = MapMetrics.Measure(); var map = Own(__0); if (map == null) return true; __result = map.Scene?.Published == true ? map.Scene.Handles : Array.Empty<ISceneHandle>(); return false; }
        private static void UnloadAll() { using var metric = MapMetrics.Measure(); active?.ReleaseAll(); }
    }
}
