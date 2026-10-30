using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DolocTown;
using UnityEngine;

namespace DTMAPI.Internal.HeartMap
{
    internal sealed partial class MapModule
    {
        private static readonly string[] Faults = { "prepare", "publish", "missing-load", "duplicate-load", "late-load", "missing-complete", "duplicate-complete", "unload", "title", "cancel", "recovery" };
        private static FieldInfo TransitionRoom = null!;
        private static FieldInfo TransitionCallback = null!;
        private readonly ConditionalWeakTable<GameStateSceneTransition, NativeTicket> tickets = new ConditionalWeakTable<GameStateSceneTransition, NativeTicket>();
        private TravelContext? travel, redirect;
        private string nextFault = "";
        private static double Now => Time.realtimeSinceStartup;
        private static FieldInfo RequireTransitionField(string name, Type type)
        {
            var field = typeof(GameStateSceneTransition).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != type) throw new MissingFieldException(typeof(GameStateSceneTransition).FullName, name);
            return field;
        }
        private sealed class TravelContext
        {
            internal readonly MapRequest Request;
            internal readonly Room Origin, Target;
            internal readonly Vector2 OriginPosition, TargetPosition;
            internal readonly MapRegistration? TargetMap;
            internal readonly List<GameStateSceneTransition> States = new List<GameStateSceneTransition>();
            internal readonly string Fault;
            internal string Failure = "";
            internal bool Recovering;
            internal Room Expected => Recovering ? Origin : Target;
            internal TravelContext(MapRequest request, Room origin, Vector2 originPosition, Room target, Vector2 targetPosition, MapRegistration? targetMap, string fault)
            { Request = request; Origin = origin; OriginPosition = originPosition; Target = target; TargetPosition = targetPosition; TargetMap = targetMap; Fault = fault; }
        }
        private void BeginTravel(Room target, Vector2 position, MapRegistration? map)
        {
            RestoreOverview(); CollectRetired();
            var req = new MapRequest(++request, generation, target.RoomId, map == null, Now);
            var ctx = new TravelContext(req, DolocAPI.CurrentRoom, DolocAPI.AgentPosition, target, position, map, nextFault); nextFault = "";
            travel = ctx;
            // Native asynchronous built-scene loads have no bounded cancellation API.
            // Keep the original main-farm scene resident while visiting our private maps.
            // Reject any route lacking that known recovery owner before taking input/fade.
            var farm = DolocAPI.archiveHandle.MainFarm;
            if ((!ReferenceEquals(ctx.Origin, farm) && FindRoom(ctx.Origin.RoomId) == null) ||
                !Resident(ctx.Origin) || !Resident(farm) || (map == null && !ReferenceEquals(target, farm)) ||
                !NativeIdle() ||
                DolocAPI.ppm.IsEnabled(PPTypes.FADEINOUT) || DolocAPI.ppm.DisableFadeInOnce || DolocAPI.ppm.DisableFadeOutOnce ||
                (map?.Scene != null && map.Scene.Retired))
            { Finish(ctx, MapResult.FailedBeforeTransition, "route-not-bounded: require resident main farm, current handle and idle fade"); return; }
            Log("travel-start id=" + req.Id + "; generation=" + req.Generation + "; target=" + req.Target + "; phase=" + req.Phase + "; fault=" + ctx.Fault);
            DolocAPI.StartCoroutine(Drive());
            IEnumerator Drive()
            {
                var stack = new Stack<IEnumerator>(); stack.Push(RunTravel(ctx));
                try
                {
                    while (!req.IsDone && stack.Count != 0)
                    {
                        var body = stack.Peek();
                        bool more = false; Exception? failure = null;
                        try { more = body.MoveNext(); } catch (Exception ex) { failure = ex; }
                        if (failure != null) { WorldFault(ctx, "orchestrator-exception: " + failure.Message); yield break; }
                        if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                        // Unity normally drives nested releases itself. Keep them
                        // under this same failure boundary and terminal owner.
                        if (body.Current is IEnumerator nested) { stack.Push(nested); continue; }
                        yield return body.Current;
                    }
                }
                finally { while (stack.Count != 0) (stack.Pop() as IDisposable)?.Dispose(); }
            }
        }
        private IEnumerator RunTravel(TravelContext ctx)
        {
            var req = ctx.Request;
            if (ctx.TargetMap != null && ctx.TargetMap.Scene == null)
            {
                var candidate = new MapScene(ctx.TargetMap.Content, generation, req.Id);
                ctx.TargetMap.Scene = candidate;
                var build = candidate.Build(ctx.Fault == "prepare");
                bool complete = false;
                try
                {
                    while (req.IsCurrent(generation) && !closed && !req.Expired(Now) && ctx.Failure.Length == 0)
                    {
                        bool more = false;
                        try { more = build.MoveNext(); }
                        catch (Exception ex) { ctx.Failure = "prepare-failed: " + ex.Message; }
                        if (candidate.Scene.IsValid() && !ownedScenes.ContainsKey(candidate.Scene.handle)) ownedScenes.Add(candidate.Scene.handle, candidate);
                        if (ctx.Failure.Length > 0) break;
                        if (!more) { complete = true; break; }
                        yield return null;
                    }
                }
                finally { (build as IDisposable)?.Dispose(); }
                if (!req.IsCurrent(generation) || closed) yield break;
                if (!complete)
                {
                    ctx.Failure = ctx.Failure.Length == 0 ? "prepare-timeout" : ctx.Failure;
                    Retire(candidate);
                    yield return ReleaseAndFinish(ctx, ctx.Failure == "cancelled" ? MapResult.Cancelled : MapResult.FailedBeforeTransition);
                    yield break;
                }
                Log("scene-ready map=" + candidate.Content.Definition.Id + "; request=" + req.Id + "; generation=" + generation + "; handle=" + candidate.Scene.handle +
                    "; buildIndex=" + candidate.Scene.buildIndex + "; maxSliceMs=" + candidate.MaxBuildSliceMs.ToString("F3") + "; stage=" + candidate.MaxBuildSliceStage + "; visible=false");
            }
            if (!req.IsCurrent(generation)) yield break;
            // Preparation yields frames. A native dialogue or another room may
            // acquire the world before publication; do not push over that owner.
            if (!NativeIdle() || !Usable(ctx.Origin) || !Resident(DolocAPI.archiveHandle.MainFarm))
            {
                ctx.Failure = "origin-changed-during-prepare; native owner retained";
                yield return ReleaseAndFinish(ctx, MapResult.FailedBeforeTransition); yield break;
            }
            req.BeginNative(Now);
            StartNative(ctx);
            while (req.IsCurrent(generation) && !closed)
            {
                if (ctx.Failure == "title") { DolocAPI.ReturnHome(); yield break; }
                if (ctx.Failure.Length == 0 && req.Ticket?.CallbackReceived == true && Usable(ctx.Target))
                {
                    yield return ReleaseAndFinish(ctx, req.IsExit ? MapResult.Exited : MapResult.Entered); yield break;
                }
                if (ctx.Failure.Length != 0 || req.Expired(Now)) break;
                yield return null;
            }
            if (!req.IsCurrent(generation) || closed) yield break;
            string reason = ctx.Failure.Length == 0 ? "native-timeout phase=" + req.Phase : ctx.Failure;
            if (req.BeginRecovery(reason, Now) == null) { WorldFault(ctx, "recovery-already-attempted"); yield break; }
            ctx.Recovering = true; ctx.Failure = "";
            if (ctx.Target.SceneRawName != ctx.Origin.SceneRawName)
            {
                if (ctx.TargetMap?.Scene != null) ctx.TargetMap.Scene.SetVisible(false);
                else if (DolocAPI.sceneManager.QuerySceneBuildIndex(ctx.Target.SceneRawName, out var nativeTarget) && nativeTarget >= 0)
                    DolocAPI.sceneManager.SetSceneEnabled(nativeTarget, false);
            }
            Log("travel-recover id=" + req.Id + "; reason=" + reason + "; retained=" + ctx.Origin.RoomId);
            if (!CancelNative(ctx) || !Resident(ctx.Origin) || ctx.Fault == "recovery") { WorldFault(ctx, "old-world-unavailable: " + reason); yield break; }
            // Let the native zero-duration fade cancellation settle before a new fade.
            yield return null;
            if (!req.IsCurrent(generation) || closed) yield break;
            StartNative(ctx);
            while (req.IsCurrent(generation) && !closed && !req.Expired(Now) && ctx.Failure.Length == 0)
            {
                if (req.Ticket?.CallbackReceived == true && Usable(ctx.Origin))
                {
                    ctx.Failure = reason;
                    yield return ReleaseAndFinish(ctx, reason == "cancelled" ? MapResult.Cancelled : MapResult.FailedRecovered); yield break;
                }
                yield return null;
            }
            if (req.IsCurrent(generation) && !closed) WorldFault(ctx, "recovery-timeout-or-rejected: " + reason + "; " + ctx.Failure);
        }
        private void StartNative(TravelContext ctx)
        {
            try
            {
                redirect = ctx;
                var ticket = ctx.Request.Ticket!;
                DolocAPI.agent.Status.Velocity = Vector2.zero;
                bool accepted = DolocAPI.EnterRoom(ctx.Expected, ctx.Recovering ? ctx.OriginPosition : ctx.TargetPosition, () =>
                {
                    if (!ctx.Recovering && ctx.Fault == "missing-complete") return;
                    ticket.Complete(generation);
                    if (!ctx.Recovering && ctx.Fault == "duplicate-complete") ticket.Complete(generation);
                });
                if (!accepted || ctx.States.Count == 0) ctx.Failure = "native-start-rejected";
            }
            catch (Exception ex) { ctx.Failure = "native-start-exception: " + ex.Message; }
            finally { redirect = null; }
        }
        private static bool Resident(Room room)
        {
            if (!DolocAPI.sceneManager.IsSceneLoaded(room.SceneRawName)) return false;
            var handle = DolocAPI.sceneManager.GetSceneHandle(room.SceneRawName, room is DungeonRoom ? room.RoomId.Split('.').Last() : null);
            return handle?.GameObject != null;
        }
        private static bool NativeIdle() => DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState is NormalGameState &&
            !DolocAPI.userInput.stateStack.Any() && DolocAPI.archiveHandle.cityData.dialogueManager.UnhandledDialogueNodes.Count == 0 &&
            !DolocAPI.ppm.IsEnabled(PPTypes.FADEINOUT) && !DolocAPI.ppm.DisableFadeInOnce && !DolocAPI.ppm.DisableFadeOutOnce;
        // The native farm uses an inactive authoring handle while FarmRenderer
        // owns its live world. Only our actual map handle is required active.
        private static bool Usable(Room room) => ReferenceEquals(DolocAPI.CurrentRoom, room) && room.SceneHandle?.GameObject != null &&
            (Own(room.SceneRawName) == null || room.SceneHandle.GameObject.activeInHierarchy) &&
            DolocAPI.userInput.CurrentState is NormalGameState && DolocAPI.agent.gameObject.activeInHierarchy &&
            DolocAPI.cameraController.enabled && !DolocAPI.ppm.IsEnabled(PPTypes.FADEINOUT);

        private IEnumerator ReleaseAndFinish(TravelContext ctx, MapResult result)
        {
            ctx.Request.Releasing(Now);
            var releases = ownedScenes.Values.Where(s => !s.Retired && s.Content.RoomId != DolocAPI.CurrentRoom?.RoomId &&
                (s.Content.RoomId == ctx.Target.RoomId || s.Content.RoomId == ctx.Origin.RoomId)).ToArray();
            // Include a candidate already retired by preparation failure.
            releases = releases.Concat(ownedScenes.Values.Where(s => s.RequestId == ctx.Request.Id && s.Retired)).Distinct().ToArray();
            foreach (var scene in releases) Retire(scene, ctx.Fault == "unload");
            while (ctx.Request.IsCurrent(generation) && !closed)
            {
                if (releases.Any(s => s.UnloadFailed)) { Finish(ctx, MapResult.FaultedUnload, "unload-refused; current world usable; map quarantined"); yield break; }
                if (releases.All(s => (!s.Scene.IsValid() || !s.Scene.isLoaded) && (s.Unloading == null || s.Unloading.isDone)))
                { CollectRetired(); Finish(ctx, result, ctx.Failure); yield break; }
                if (ctx.Request.Expired(Now))
                {
                    foreach (var scene in releases.Where(s => s.Scene.IsValid() && s.Scene.isLoaded)) maps[scene.Content.SceneName].Quarantined = true;
                    Finish(ctx, MapResult.FaultedUnload, "unload-timeout; current world usable; map quarantined"); yield break;
                }
                yield return null;
            }
        }
        private bool CancelNative(TravelContext ctx)
        {
            foreach (var state in ctx.States)
            {
                if (tickets.TryGetValue(state, out var ticket)) ticket.Cancel();
                if (ReferenceEquals(DolocAPI.userInput.CurrentState, state))
                {
                    DolocAPI.ppm.FadeOut(0, null, false, "HeartMapCancel");
                    DolocAPI.ppm.SetEnabled(PPTypes.FADEINOUT, false);
                    if (!DolocAPI.userInput.TryPopState(state)) return false;
                }
                else if (DolocAPI.userInput.TryGetStateInStack(state)) return false;
            }
            return DolocAPI.userInput.CurrentState is NormalGameState;
        }
        private void WorldFault(TravelContext ctx, string reason)
        {
            Safe(() => CancelNative(ctx), "fault-native-cancel");
            Finish(ctx, MapResult.FaultedRecovery, reason + "; native ReturnHome, no save, no retry");
            Safe(() => DolocAPI.ReturnHome(), "fault-native-title");
        }
        private void Finish(TravelContext ctx, MapResult result, string detail)
        {
            if (!ctx.Request.Finish(result, detail)) return;
            LastResult = ctx.Request;
            foreach (var state in ctx.States)
            {
                if (tickets.TryGetValue(state, out var ticket)) ticket.Cancel();
                TransitionCallback.SetValue(state, null); TransitionRoom.SetValue(state, null);
            }
            ctx.States.Clear();
            if (ReferenceEquals(travel, ctx)) travel = null;
            if (result == MapResult.Entered) completedEntries++; if (result == MapResult.Exited) completedExits++;
            Log("travel-result id=" + ctx.Request.Id + "; generation=" + ctx.Request.Generation + "; result=" + result + "; count=" + ctx.Request.CompletionCount + "; " + ctx.Request.Detail);
            LogStatus("terminal");
        }
        private void Invalidate(string reason, bool release)
        {
            if (travel != null)
            {
                var ctx = travel;
                Safe(() => CancelNative(ctx), "invalidate-native");
                Finish(ctx, MapResult.CancelledAtTitle, reason);
            }
            generation++; loops = false; redirect = null;
            Safe(RestoreOverview, "invalidate-camera");
            if (release) ReleaseAll();
        }
        private static void BeforeTitle() { using var metric = MapMetrics.Measure(); active?.Invalidate("native-title-before-unload", true); }
        private static bool BeforeTransport(Room __0, Vector2 __1, Action __2, bool __3)
        {
            using var metric = MapMetrics.Measure();
            var module = active; if (module == null) return true;
            var ctx = module.redirect;
            if (ctx != null && ReferenceEquals(__0, ctx.Expected) && ctx.Request.Ticket != null)
            {
                // Preserve DolocAPI.EnterRoom's native callback (dungeon, room, camera).
                // Give each attempt a separate native state, so old bound delegates
                // cannot operate on fields reused by the singleton's next request.
                var state = new GameStateSceneTransition(DolocAPI.userInput);
                module.tickets.Add(state, ctx.Request.Ticket); ctx.States.Add(state);
                state.Start(__0, __1, __2, __3);
                return false;
            }
            if (module.FindRoom(__0?.RoomId) != null || module.IsInMap)
            { module.Log("direct private-map transition rejected before input/fade; use prepared request"); return false; }
            return true;
        }
        private static bool BeforeTransition(GameStateSceneTransition __instance, Room __0)
        {
            using var metric = MapMetrics.Measure();
            var module = active; if (module == null) return true;
            if (module.tickets.TryGetValue(__instance, out var ticket)) return ticket.Valid(module.generation);
            // Raw callers cannot skip preparation. Normal native routes through
            // TransitScene are rejected before Core observes a Start boundary.
            return module.FindRoom(__0?.RoomId) == null && !module.IsInMap;
        }
        private static bool BeforeFade(GameStateSceneTransition __instance)
        { using var metric = MapMetrics.Measure(); return active == null || !active.tickets.TryGetValue(__instance, out var ticket) || ticket.TakeFade(active.generation); }
        private static bool BeforeSceneLoaded(GameStateSceneTransition __instance)
        { using var metric = MapMetrics.Measure(); return active == null || !active.tickets.TryGetValue(__instance, out var ticket) || ticket.TakeLoad(active.generation); }
        private static bool LoadScene(string __0, Action __1, ref bool __result)
        {
            using var metric = MapMetrics.Measure();
            var module = active; if (module == null) return true;
            var map = Own(__0);
            if (!(__1?.Target is GameStateSceneTransition state) || !module.tickets.TryGetValue(state, out var callbackTicket))
            { if (map == null) return true; __result = false; return false; }
            var ctx = module.travel;
            var ticket = ctx?.Request.Ticket;
            if (ctx == null || ticket == null || !ReferenceEquals(ticket, callbackTicket) || !ticket.Valid(module.generation) || ctx.Expected.SceneRawName != __0) { __result = false; return false; }
            __result = false;
            try
            {
                if (map != null)
                {
                    if (map.Scene == null || map.Scene.Retired || map.Scene.Generation != module.generation) throw new InvalidOperationException("candidate absent/retired/generation mismatch");
                    map.Scene.Publish(!ctx.Recovering && ctx.Fault == "publish");
                    module.Log("scene-published map=" + map.Content.Definition.Id + "; request=" + ctx.Request.Id + "; handle=" + map.Scene.Scene.handle);
                }
                else
                {
                    if (!DolocAPI.sceneManager.IsSceneLoaded(__0) || !DolocAPI.sceneManager.QuerySceneBuildIndex(__0, out var index) || index < 0)
                        throw new InvalidOperationException("resident native target lost");
                    DolocAPI.sceneManager.SetSceneEnabled(index, true);
                }
                ctx.Request.Published(); __result = true;
                if (!ctx.Recovering && (ctx.Fault == "missing-load" || ctx.Fault == "recovery")) return false;
                if (!ctx.Recovering && (ctx.Fault == "title" || ctx.Fault == "cancel")) { ctx.Failure = ctx.Fault == "title" ? "title" : "cancelled"; return false; }
                if (!ctx.Recovering && ctx.Fault == "late-load") { DolocAPI.StartCoroutine(module.InvokeDelayed(ticket, __1, ctx.Request.Id)); return false; }
                if (ticket.Valid(module.generation)) __1?.Invoke();
                if (!ctx.Recovering && ctx.Fault == "duplicate-load" && ticket.Valid(module.generation)) __1?.Invoke();
            }
            catch (Exception ex) { ctx.Failure = "publication-or-callback-failed: " + ex.Message; module.Log(ctx.Failure); }
            return false;
        }
        private IEnumerator InvokeDelayed(NativeTicket ticket, Action? callback, long id)
        {
            yield return new WaitForSecondsRealtime(10);
            if (ticket.Valid(generation)) callback?.Invoke(); else Log("late-callback-ignored request=" + id);
        }
        private static bool UnloadScene(string __0, Action __1)
        {
            using var metric = MapMetrics.Measure();
            var module = active; if (module == null) return true;
            var ctx = module.travel;
            if (ctx != null && ctx.Request.Phase != MapPhase.Releasing && (__0 == ctx.Origin.SceneRawName || __0 == ctx.Target.SceneRawName))
            {
                var retained = Own(__0);
                if (retained != null) retained.Scene?.SetVisible(false);
                else if (DolocAPI.sceneManager.QuerySceneBuildIndex(__0, out var index) && index >= 0) DolocAPI.sceneManager.SetSceneEnabled(index, false);
                // Native QuitCurrentRoom has no callback. An unrelated caller needing
                // completion must not be told this retained scene has been unloaded.
                if (__1 != null) module.Log("unload-deferred-with-callback rejected during owned transition");
                return false;
            }
            var map = Own(__0); if (map == null) return true;
            if (map.Scene == null) { __1?.Invoke(); return false; }
            var scene = map.Scene; module.Retire(scene);
            if (__1 != null) DolocAPI.StartCoroutine(Wait());
            return false;
            IEnumerator Wait()
            {
                int epoch = module.generation; double until = Now + 8;
                while (!module.closed && epoch == module.generation && Now < until && scene.Scene.IsValid() && scene.Scene.isLoaded && !scene.UnloadFailed) yield return null;
                if (!module.closed && epoch == module.generation && (!scene.Scene.IsValid() || !scene.Scene.isLoaded)) __1?.Invoke();
                else if (epoch == module.generation) { map.Quarantined = true; module.Log("unload-result FaultedUnload map=" + map.Content.Definition.Id + "; callback not reported as success"); }
            }
        }
    }
}
