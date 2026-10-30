using System;
using System.Collections;
using System.Linq;
using DolocTown;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DTMAPI.Internal.HeartMap
{
    // Explicit internal NoNativeSave acceptance, never started by ordinary entry.
    // Exercises the real native state machine and keyboard input in a live process.
    internal sealed partial class MapModule
    {
        private bool acceptanceStarted;
        private void StartAcceptance()
        {
            if (acceptanceStarted || string.IsNullOrEmpty(Config.AcceptanceScenario) || DolocAPI.gameManager == null || DolocAPI.userInput == null) return;
            acceptanceStarted = true;
            DolocAPI.StartCoroutine(ObserveAcceptance(Acceptance()));
        }
        private IEnumerator ObserveAcceptance(IEnumerator body)
        {
            // Unity drives nested enumerators otherwise; this stack keeps failures
            // observable in one terminal QA line and always releases synthetic keys.
            var stack = new System.Collections.Generic.Stack<IEnumerator>(); stack.Push(body);
            bool pass = false;
            try
            {
                while (stack.Count > 0 && !closed)
                {
                    var current = stack.Peek(); bool more = false; Exception? error = null;
                    try { more = current.MoveNext(); } catch (Exception ex) { error = ex; }
                    if (error != null) { Log("QA FAIL scenario=" + Config.AcceptanceScenario + "; " + error); yield break; }
                    if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current.Current is IEnumerator nested) { stack.Push(nested); continue; }
                    yield return current.Current;
                }
                pass = !closed;
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                Log("QA END scenario=" + Config.AcceptanceScenario + "; pass=" + pass);
                if (Config.AcceptanceQuit) Application.Quit();
            }
        }
        private IEnumerator Acceptance()
        {
            if (Config.AcceptanceScenario != "editor" && Config.AcceptanceScenario != "core" && Config.AcceptanceScenario != "title" && Config.AcceptanceScenario != "unload" && Config.AcceptanceScenario != "recovery")
                throw new InvalidOperationException("Unknown acceptance scenario");
            Log("QA START scenario=" + Config.AcceptanceScenario + "; UI slot=" + Config.AcceptanceSaveSlot + "; NoNativeSave=true");
            if (Config.AcceptanceSaveSlot < 1) throw new InvalidOperationException("Explicit existing acceptance slot required");
            if (Config.AcceptanceLoadSave)
            {
                yield return Await(() => DolocAPI.userInput?.CurrentState?.GetType().Name == "HomePageUiState", 60, "native title");
                LoadAcceptanceSave();
            }
            // The normal smoke runner may own the initial load and its before-
            // cleanup archive proof. Do not issue a second load beside that host.
            yield return Await(Ready, 90, "native world");
            yield return PrepareAcceptanceFarm();
            if (Config.AcceptanceScenario == "editor") { yield return EditorAcceptance(); LogStatus("qa-final"); yield break; }
            if (maps.Count != 2 || maps.Values.Count(m => m.Registered) != 2) throw new InvalidOperationException("Two private maps were not registered");
            Log("QA PASS native-load slot=" + Config.AcceptanceSaveSlot + "; two maps registered");
            if (Config.AcceptanceScenario == "core")
            {
                CheckNativeRegistration();
                yield return Exercise("enter heart-map", MapResult.Entered);
                Capture("qa-heart"); yield return Movement("heart");
                CheckRetainedFarmHidden();
                int firstHandle = FindRoom(DolocAPI.CurrentRoom.RoomId)!.Scene!.Scene.handle;
                Execute("bad-input");
                yield return Exercise("enter asymmetric-map", MapResult.Entered);
                int secondHandle = FindRoom(DolocAPI.CurrentRoom.RoomId)!.Scene!.Scene.handle;
                if (firstHandle == secondHandle) throw new InvalidOperationException("Two scene identities alias");
                Log("QA PASS independent-scene-handles first=" + firstHandle + "; second=" + secondHandle);
                Capture("qa-asymmetric"); yield return Movement("asymmetric");
                CheckRetainedFarmHidden();
                yield return Exercise("farm", MapResult.Exited);
                foreach (string fault in new[] { "prepare", "publish", "missing-load", "missing-complete", "duplicate-load", "duplicate-complete", "late-load", "cancel" })
                {
                    MapResult expected = fault == "prepare" ? MapResult.FailedBeforeTransition : fault.StartsWith("duplicate", StringComparison.Ordinal) ? MapResult.Entered : fault == "cancel" ? MapResult.Cancelled : MapResult.FailedRecovered;
                    yield return Exercise("fault " + fault, expected);
                    if (fault == "late-load") { yield return new WaitForSecondsRealtime(3); LogStatus("after-late-callback"); }
                    yield return Movement(fault);
                    if (IsInMap) yield return Exercise("farm", MapResult.Exited);
                }
                yield return Exercise("enter heart-map", MapResult.Entered);
                foreach (string fault in new[] { "missing-load", "missing-complete" })
                {
                    nextFault = fault;
                    yield return Exercise("farm", MapResult.FailedRecovered);
                    CheckRetainedFarmHidden();
                    yield return Movement("exit-" + fault);
                }
                yield return Exercise("farm", MapResult.Exited);
                int entries = completedEntries, exits = completedExits;
                Execute("cycles"); yield return Await(() => !loops, 400, "20 alternate round trips");
                if (completedEntries - entries != 20 || completedExits - exits != 20) throw new InvalidOperationException("Round trip count mismatch");
                yield return new WaitForSecondsRealtime(.5f); CollectRetired();
                if (ownedScenes.Count != 0) throw new InvalidOperationException("Owned scenes retained after 20 cycles");
                Log("QA PASS 20 alternate round trips; ownedScenes=0");
            }
            else if (Config.AcceptanceScenario == "title")
            {
                yield return Exercise("fault title", MapResult.CancelledAtTitle, false);
                yield return Await(() => !DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState?.GetType().Name == "HomePageUiState", 30, "title after interruption");
                yield return new WaitForSecondsRealtime(.5f); CollectRetired();
                if (ownedScenes.Count != 0) throw new InvalidOperationException("Old generation scene retained at title");
                LoadAcceptanceSave();
                yield return Await(Ready, 40, "same-slot reload");
                yield return PrepareAcceptanceFarm();
                yield return Exercise("enter asymmetric-map", MapResult.Entered);
                yield return Movement("new-generation");
                yield return Exercise("farm", MapResult.Exited);
            }
            else if (Config.AcceptanceScenario == "unload")
            {
                yield return Exercise("enter heart-map", MapResult.Entered);
                yield return Exercise("fault unload", MapResult.FaultedUnload);
                yield return Movement("failed-unload");
                long previous = request; Execute("enter heart-map");
                if (request != previous) throw new InvalidOperationException("Quarantined map admitted new request");
                yield return Exercise("enter asymmetric-map", MapResult.Entered);
                yield return Exercise("farm", MapResult.Exited);
                DolocAPI.ReturnHome();
                yield return Await(() => !DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState?.GetType().Name == "HomePageUiState", 30, "quarantine cleanup title");
                yield return new WaitForSecondsRealtime(.5f); CollectRetired();
                if (ownedScenes.Count != 0) throw new InvalidOperationException("Quarantine did not release at title");
                Log("QA PASS failed unload isolated; other map entered; title cleanup complete");
            }
            else
            {
                yield return Exercise("fault recovery", MapResult.FaultedRecovery, false);
                yield return Await(() => !DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState?.GetType().Name == "HomePageUiState", 30, "fault terminal native title");
                Log("QA PASS old-world-unavailable terminal; no retry/save; title usable");
            }
            LogStatus("qa-final");
        }
        private IEnumerator PrepareAcceptanceFarm()
        {
            yield return Await(() => Ready() && NativeIdle(), 30, "native patch/dialogue completion before fixture setup");
            var farm = DolocAPI.archiveHandle.MainFarm;
            if (!ReferenceEquals(DolocAPI.CurrentRoom, farm))
            {
                LogStatus("qa-before-native-farm-setup");
                if (!farm.Geometry.GetNearestGroundPositionCanPlaceAgent(new Vector2Int(20, 0), out var ground))
                    throw new InvalidOperationException("Native farm setup has no standing point");
                var position = farm.Geometry.CalcWorldPosition(ground, new Vector2(.5f, 0)) + new Vector2(0, .1f);
                // Fixture setup uses the ordinary native route before any owned
                // map request; it is not evidence for recovery from arbitrary rooms.
                if (!DolocAPI.EnterRoom(farm, position)) throw new InvalidOperationException("Native farm setup rejected");
            }
            double idleSince = -1;
            yield return Await(() =>
            {
                if (!Ready() || !NativeIdle() || !Usable(farm)) { idleSince = -1; return false; }
                if (idleSince < 0) idleSince = Now;
                return Now - idleSince >= 1;
            }, 30, "native farm fixture setup");
            LogStatus("qa-native-farm-ready");
        }
        private void LoadAcceptanceSave()
        {
            var title = DolocAPI.userInput.CurrentState as HomePageUiState;
            if (title == null) throw new InvalidOperationException("Reload requires native title owner");
            title.Kill();
            if (!DolocAPI.userInput.TryPopState(title)) throw new InvalidOperationException("Native title did not exit for load");
            if (!DolocAPI.LoadGame(Config.AcceptanceSaveSlot - 1)) throw new InvalidOperationException("Native LoadGame rejected existing slot");
        }
        private void CheckRetainedFarmHidden()
        {
            var farm = DolocAPI.archiveHandle.MainFarm;
            var handle = DolocAPI.sceneManager.GetSceneHandle(farm.SceneRawName, null!);
            if (!DolocAPI.sceneManager.QuerySceneBuildIndex(farm.SceneRawName, out var index) || index < 0)
                throw new InvalidOperationException("Retained farm has no native build index");
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(index);
            if (!IsInMap || handle?.GameObject == null || !scene.IsValid() || !scene.isLoaded || scene.GetRootGameObjects().Any(root => root.activeInHierarchy))
                throw new InvalidOperationException("Retained native farm is absent or still active beside private map");
            Log("QA PASS retained farm hidden; current map handle active");
        }
        private IEnumerator Exercise(string command, MapResult expected, bool requireWorld = true)
        {
            long previous = request;
            Execute(command);
            yield return Await(() => LastResult != null && LastResult.Id > previous && travel == null, 45, command);
            if (LastResult!.Result != expected || LastResult.CompletionCount != 1 || (requireWorld && (!Ready() || !Usable(DolocAPI.CurrentRoom))))
                throw new InvalidOperationException("Unexpected native result command=" + command + "; expected=" + expected + "; actual=" + LastResult.Result);
            Log("QA PASS " + command + "; result=" + expected + "; id=" + LastResult.Id + "; exactlyOnce=true");
        }
        private void CheckNativeRegistration()
        {
            var tables = DolocTown.Config.DolocConfig.Tables;
            var heart = maps.Values.Single(m => m.Content.Definition.Id == "heart-map");
            var second = maps.Values.Single(m => m.Content.Definition.Id == "asymmetric-map");
            var ownedRow = tables.TbScene.DataMap[heart.Content.SceneName];
            var foreignRow = tables.TbScene.DataList.First(r => r.Id != heart.Content.SceneName && r.Id != second.Content.SceneName);
            try
            {
                tables.TbScene.DataMap[heart.Content.SceneName] = foreignRow;
                bool refused = false;
                try { heart.Register(); } catch (InvalidOperationException ex) { refused = ex.Message.StartsWith("native-id-conflict", StringComparison.Ordinal); }
                if (!refused || !ReferenceEquals(tables.TbScene.DataMap[heart.Content.SceneName], foreignRow) || !second.Registered)
                    throw new InvalidOperationException("Native conflict overwrote foreign row or disabled other map");
            }
            finally { tables.TbScene.DataMap[heart.Content.SceneName] = ownedRow; }
            using var stream = typeof(MapModule).Assembly.GetManifestResourceStream("Maps.asymmetric.dtmap.json")!;
            using var reader = new System.IO.StreamReader(stream);
            string probe = reader.ReadToEnd().Replace("asymmetric-map", "transaction-probe");
            var testCatalog = new MapCatalog();
            if (!testCatalog.TryAdd(probe, out var definition, out var error)) throw new InvalidOperationException(error);
            foreach (int stage in new[] { 1, 2 })
            {
                int rooms = tables.TbRoom.DataList.Count, scenes = tables.TbScene.DataList.Count;
                var registration = new MapRegistration(definition!); bool refused = false;
                try { registration.Register(stage); } catch (InvalidOperationException ex) { refused = ex.Message.StartsWith("MAP-INJECTED:", StringComparison.Ordinal); }
                if (!refused || tables.TbRoom.DataList.Count != rooms || tables.TbScene.DataList.Count != scenes ||
                    tables.TbRoom.DataMap.ContainsKey(definition!.RoomId) || tables.TbScene.DataMap.ContainsKey(definition.SceneName) ||
                    DolocAPI.assets.dungeons.QueryDungeonProto(definition.DungeonName, out _) || !heart.Registered || !second.Registered)
                    throw new InvalidOperationException("Native publication rollback failed at stage " + stage);
            }
            Log("QA PASS native registration conflict preserved foreign record; both partial publications rolled back; two maps retained");
        }
        private IEnumerator Movement(string label)
        {
            yield return new WaitForSecondsRealtime(.5f);
            var keyboard = Keyboard.current;
            if (keyboard == null || keyboard.allKeys.Any(k => k.isPressed)) throw new InvalidOperationException("Keyboard absent/in use; native input check cannot run");
            var before = DolocAPI.AgentPosition;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(.25f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(.4f);
            float dx = DolocAPI.AgentPosition.x - before.x;
            if (Math.Abs(dx) < .05f || !Usable(DolocAPI.CurrentRoom)) throw new InvalidOperationException("Native movement/camera/input unavailable after " + label + "; dx=" + dx);
            Log("QA PASS native-keyboard label=" + label + "; dx=" + dx + "; camera=true; normal=true; fade=false");
        }
        private IEnumerator Await(Func<bool> condition, float seconds, string label)
        {
            double until = Now + seconds;
            while (!closed && !condition() && Now < until) yield return null;
            if (closed || !condition())
            {
                LogStatus("qa-deadline " + label);
                throw new TimeoutException("QA deadline: " + label + "; nativeState=" + DolocAPI.userInput?.CurrentState?.GetType().Name);
            }
        }
    }
}
