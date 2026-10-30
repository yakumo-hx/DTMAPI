#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.GameData;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace DTMAPI.Internal.AudioR3Monster
{
    // QA only: native factory/lifecycles, instrumented Post (never a second Post).
    // Autonomous monster updates are held for deterministic acoustic isolation.
    public sealed class ModEntry : DtmMod, IDisposable
    {
        static ModEntry current;
        readonly Harmony patches = new Harmony("DTMAPI.Internal.AudioR3Monster.Native");
        readonly object logLock = new object();
        readonly List<Voice> voices = new List<Voice>();
        readonly Dictionary<int, Identity> identities = new Dictionary<int, Identity>();
        readonly List<string> scopes = new List<string>();
        readonly List<Step> steps = new List<Step>();
        readonly List<Monster> rentals = new List<Monster>();
        Settings settings;
        IDtmRuntimeContext runtime;
        string last = "", spawning = "", scenario = "";
        bool ready, world, preparing, arenaReady, disposed, originalBackground, prepared;
        bool originalInvincible, originalCutscenes;
        float nextSample, deadline;
        double started;
        int loads, saves, request, generation;
        IMonsterHost host;
        Vector3 originalAgent, anchor;
        Monster active;
        MonsterAttackBehaviour impact;
        object impactSlot;
        MainMenuUiState menu;
        static double Clock => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        static object Member(object value, string name) => value == null ? null : AccessTools.Property(value.GetType(), name)?.GetValue(value) ?? AccessTools.Field(value.GetType(), name)?.GetValue(value);
        static object Invoke(object value, string name, params object[] args) => AccessTools.Method(value.GetType(), name).Invoke(value, args);
        static void Need(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
        static float[] XYZ(Vector3 value) => new[] { value.x, value.y, value.z };
        public override void Entry(IDtmHelper helper)
        {
            settings = helper.ReadConfig<Settings>();
            Need(Path.IsPathRooted(settings.Evidence) && DateTime.Parse(settings.ExpiresUtc).ToUniversalTime() > DateTime.UtcNow, "Fresh expiring QA settings required");
            Directory.CreateDirectory(settings.Evidence); current = this;
            runtime = helper.GetRequiredService<IDtmRuntimeContext>("0.7.0", "0.7.0");
            originalBackground = Application.runInBackground; Application.runInBackground = true;
            // All patches installed within authorized Entry, before periodic owner audit.
            foreach (var method in new[] { AccessTools.Method(typeof(DolocAPI), "SaveGame", new[] { typeof(int) }), AccessTools.Method(typeof(DataPersistenceManager), "SaveGame", new[] { typeof(int) }) })
                patches.Patch(method, prefix: new HarmonyMethod(typeof(ModEntry), nameof(NoSave)));
            patches.Patch(AccessTools.Method(typeof(MonsterController), "Run"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(RunBefore)));
            foreach (string name in new[] { "OnUpdate", "OnFixedUpdate" })
                patches.Patch(AccessTools.Method(typeof(MonsterController), name), prefix: new HarmonyMethod(typeof(ModEntry), nameof(HoldAutonomy)));
            foreach (var method in new[] {
                AccessTools.Method(typeof(MonsterController), "Run"), AccessTools.Method(typeof(MonsterController), "OnDead"), AccessTools.Method(typeof(MonsterController), "OnRecycle"),
                AccessTools.Method(typeof(MonsterController), "Hurt", new[] { typeof(Transform), typeof(float), typeof(bool), typeof(bool) }), AccessTools.Method(typeof(MonsterController), "StopAttack"),
                AccessTools.Method(typeof(MonsterAttackBehaviourImpact), "Invoke"), AccessTools.Method(typeof(MonsterAttackBehaviourImpact), "StartImpact"),
                AccessTools.Method(typeof(MonsterAI_Nian.DropState), "OnEnter"), AccessTools.Method(typeof(MonsterAI_Nian.DropState), "OnFixedUpdate"), AccessTools.Method(typeof(MonsterAI_Nian.DropState), "OnExit") })
                patches.Patch(method, prefix: new HarmonyMethod(typeof(ModEntry), nameof(ScopeBefore)), finalizer: new HarmonyMethod(typeof(ModEntry), nameof(ScopeAfter)));
            var post = AccessTools.Method(typeof(AkSoundEngine), "PostEvent", new[] { typeof(string), typeof(GameObject), typeof(uint), typeof(AkCallbackManager.EventCallback), typeof(object) });
            Need(post != null, "Exact native string PostEvent overload required");
            patches.Patch(post, prefix: new HarmonyMethod(typeof(ModEntry), nameof(PostBefore)), postfix: new HarmonyMethod(typeof(ModEntry), nameof(PostAfter)));
            patches.Patch(AccessTools.Method(typeof(WwiseSoundManager), "UnregisterGameObject"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(UnregisterBefore)));
            helper.Events.Save.SaveLoaded += (_, e) => { loads++; Record("save-loaded", new { e.SaveSlot, loads }); };
            helper.Events.GameLoop.UpdateTicked += Tick;
            deadline = Time.realtimeSinceStartup + 350;
            Record("entry", new { pid = Process.GetCurrentProcess().Id, hooks = patches.GetPatchedMethods().Select(m => m.DeclaringType.FullName + "." + m.Name).ToArray(), productModules = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("DTMAPI.Modules")).Select(a => a.GetName().Name).ToArray(), controlled = "native factory + real methods; held autonomous updates; no synthetic Post or native save" });
        }
        public static void NoSave() { current.saves++; current.Record("unexpected-save", new { current.saves }); throw new InvalidOperationException("Audio R3 NoNativeSave"); }
        static void RunBefore(MonsterController __instance, Monster monster)
        {
            var p = current; if (p == null || p.spawning.Length == 0) return;
            var id = new Identity { Label = p.spawning, Template = monster.Name, Generation = ++p.generation, Controller = __instance };
            p.identities[__instance.gameObject.GetInstanceID()] = id;
            p.Record("native-run", new { id.Label, id.Template, id.Generation, controller = __instance.GetInstanceID(), emitter = __instance.gameObject.GetInstanceID() });
        }
        static bool HoldAutonomy(MonsterController __instance) => current == null || !current.identities.ContainsKey(__instance.gameObject.GetInstanceID());
        static void ScopeBefore(object __instance, MethodBase __originalMethod, out string __state)
        {
            __state = null; var p = current; if (p == null) return;
            var c = __instance as MonsterController ?? Member(__instance, "_controller") as MonsterController;
            if (c == null || !p.identities.TryGetValue(c.gameObject.GetInstanceID(), out var id)) return;
            __state = __originalMethod.DeclaringType.FullName + "." + __originalMethod.Name;
            p.scopes.Add(__state); p.Record("native-enter", new { method = __state, id.Label, id.Generation, emitter = c.gameObject.GetInstanceID(), slot = Member(Member(__instance, "_proto"), "AttackId")?.ToString() });
        }
        static Exception ScopeAfter(Exception __exception, string __state)
        {
            var p = current; if (p != null && __state != null) { p.Record("native-exit", new { method = __state, error = __exception?.Message }); p.scopes.RemoveAt(p.scopes.Count - 1); }
            return __exception;
        }
        static void PostBefore(string __0, GameObject __1, ref uint __2, ref AkCallbackManager.EventCallback __3, object __4, out Voice __state)
        {
            __state = null; var p = current;
            if (p == null || __1 == null || !p.identities.TryGetValue(__1.GetInstanceID(), out var id)) return;
            var v = new Voice { Request = ++p.request, Event = __0, Identity = id, Emitter = __1.GetInstanceID(), Started = Clock, Context = p.scopes.ToArray() };
            var previous = __3; var cookie = __4;
            __2 |= (uint)(AkCallbackType.AK_EndOfEvent | AkCallbackType.AK_EnableGetSourcePlayPosition);
            __3 = (c, type, info) => {
                if (type == AkCallbackType.AK_EndOfEvent) { v.Ended = true; v.End = Clock; p.Record("end", new { v.Request, v.Event, id.Label, id.Generation, playingId = (info as AkEventCallbackInfo)?.playingID, elapsed = v.End - v.Started }); }
                previous?.Invoke(cookie, type, info);
            };
            p.voices.Add(v); __state = v;
        }
        static void PostAfter(uint __result, Voice __state)
        {
            if (__state == null) return; __state.Id = __result;
            current.Record("post", new { __state.Request, __state.Event, playingId = __result, __state.Emitter, __state.Identity.Label, __state.Identity.Template, __state.Identity.Generation, context = __state.Context });
        }
        static void UnregisterBefore(GameObject __0)
        { if (current != null && __0 != null && current.identities.TryGetValue(__0.GetInstanceID(), out var id)) current.Record("unregister", new { emitter = __0.GetInstanceID(), id.Label, id.Generation }); }
        Monster Spawn(string template, string label, float offset = 2)
        {
            Need(DolocAPI.assets.monsters.QueryMonster(template, out var proto), "Native template: " + template);
            spawning = label;
            try { var m = host.GenerateMonster(proto, (Vector2)anchor + Vector2.right * offset, true); Need(m?.Controller != null, "Actual native spawn"); rentals.Add(m); return m; }
            finally { spawning = ""; }
        }
        void Clear()
        {
            if (menu != null) { DolocAPI.userInput.TryPopState(menu); menu = null; }
            foreach (var m in rentals.ToArray()) if (m.Controller != null) host.RemoveMonster(m);
            rentals.Clear(); active = null; steps.Clear(); DolocAPI.AgentPosition = anchor;
        }
        void At(double seconds, string name, Action action) => steps.Add(new Step { At = seconds, Name = name, Action = action });
        void Begin(string name) { Clear(); scenario = name; started = Clock; Record("scenario", new { name }); }
        void Position(float x) { active.Controller.position2d = (Vector2)anchor + Vector2.right * x; Record("position", new { who = "emitter", requestedX = x, actual = XYZ(active.Controller.transform.position), listener = XYZ(DolocAPI.AgentPosition) }); }
        void Gain(float gain) { var result = AkSoundEngine.SetGameObjectOutputBusVolume(AkSoundEngine.GetAkGameObjectID(active.Controller.gameObject), ulong.MaxValue, gain); Record("gain", new { gain, result = result.ToString(), controlled = "isolated native emitter author-gain probe, not a native default" }); }
        void Prepare()
        {
            Need(world && !prepared, "One preparation in ready world"); prepared = true;
            originalAgent = DolocAPI.AgentPosition;
            originalInvincible = DolocAPI.gameManager.gameInitConfig.agentInvincible; DolocAPI.gameManager.gameInitConfig.agentInvincible = true;
            originalCutscenes = (bool)Member(DolocAPI.gameStateManager.normalGameState, "_supportCutscenes"); AccessTools.Field(typeof(DolocTownGameStateBase), "_supportCutscenes").SetValue(DolocAPI.gameStateManager.normalGameState, false);
            Need(DolocAPI.QueryRoom("dungeon_多洛可河谷.stage2_巨大的树", out var arena), "Existing native arena");
            var at = arena.MonsterGenInfo.groundSpawnGroup.SpawnPoints.First(p => p.IsValid).Positions.First() + Vector2.up * 2;
            preparing = true; Need(DolocAPI.EnterRoom(arena, at, () => arenaReady = true), "Native EnterRoom accepted");
        }
        void StartBee()
        { Begin("bee"); active = Spawn("bee", "bee"); At(27, "bee-finished", () => Write("bee-done.json", new { elapsed = Clock - started })); }
        void StartDrone()
        {
            Begin("drone"); active = Spawn("fwb_drone", "drone");
            At(3, "right-far", () => Position(12)); At(6, "left-far", () => Position(-12)); At(9, "right-near", () => Position(2));
            At(12, "listener-far", () => { DolocAPI.AgentPosition = anchor + Vector3.left * 10; Record("position", new { who = "listener", actual = XYZ(DolocAPI.AgentPosition) }); });
            At(15, "listener-home", () => DolocAPI.AgentPosition = anchor);
            At(17, "menu-open", () => { menu = DolocAPI.EnterUI<MainMenuUiState>(); Record("menu", new { open = true, paused = DolocAPI.gameLoop.IsPaused, state = DolocAPI.userInput.CurrentState.GetType().FullName }); });
            At(21, "menu-close", () => { Need(DolocAPI.userInput.TryPopState(menu), "Native menu close"); menu = null; Record("menu", new { open = false, paused = DolocAPI.gameLoop.IsPaused, state = DolocAPI.userInput.CurrentState.GetType().FullName }); });
            At(23, "gain-half", () => Gain(.5f)); At(26, "gain-normal", () => Gain(1));
            At(46, "drone-finished", () => Write("drone-done.json", new { elapsed = Clock - started }));
        }
        void StartDeath()
        {
            Begin("death"); var a = Spawn("amoeba", "death-target"); var b = Spawn("fwb_drone", "scope-control", -2);
            active = a;
            At(.5, "actual-hurt", () => { Need(a.Controller.Decorator.OnSwordAttack(1, false, a.Controller.position2d, out bool dead) && !dead, "Actual nonlethal native sword damage"); });
            At(.56, "actual-lethal", () => { Need(a.Controller.Decorator.OnSwordAttack(100000, false, a.Controller.position2d, out bool dead) && dead, "Actual lethal native sword damage"); });
            At(5, "scope-control-remove", () => host.RemoveMonster(b));
            At(7, "death-finished", () => Write("death-done.json", new { elapsed = Clock - started }));
        }
        void StartNian()
        {
            Begin("nian"); active = Spawn("nian", "nian");
            var manager = Member(active.Controller, "attackBehaviourManager");
            var dict = (IDictionary)Member(manager, "attackBehaviours");
            foreach (DictionaryEntry pair in dict) if (pair.Value is MonsterAttackBehaviourImpact value) { impact = value; impactSlot = pair.Key; break; }
            Need(impact != null, "Actual Nian Impact slot");
            Record("nian-slot", new { slot = impactSlot.ToString(), readyDuration = Member(Member(impact, "_proto"), "ReadyDuration"), controller = active.Controller.GetInstanceID() });
            At(.5, "ready-request", () => Invoke(manager, "Attack", impactSlot, DolocAPI.AgentTransform, null));
            At(.8, "pre-attack-cancel", () => active.Controller.StopAttack());
            At(1.1, "recovery-request", () => {
                Need((bool)Invoke(active.Controller.Decorator, "TryEnterDropState"), "Real DropState entry");
                Need((bool)Invoke(active.Controller.Decorator, "TryCountDownDropState", .5f), "Real countdown");
                active.Controller.MonsterAI.CurrentState.OnFixedUpdate(.01f);
            });
            At(1.4, "recovery-exit", () => Invoke(active.Controller.Decorator, "TryQuitDropState"));
            At(2.1, "next-ready", () => Invoke(manager, "Attack", impactSlot, DolocAPI.AgentTransform, null));
            At(2.4, "actual-start-impact-104", () => { float duration = (float)Member(Member(impact, "_proto"), "ReadyDuration"); impact.OnUpdate(duration + .05f); });
            At(5, "nian-remove", () => host.RemoveMonster(active));
            At(7, "nian-finished", () => Write("nian-done.json", new { elapsed = Clock - started }));
        }
        void Sample()
        {
            foreach (var v in voices.ToArray()) {
                if (v.Ended || v.Id == 0 || v.Event.StartsWith("STOP_")) continue;
                var result = AkSoundEngine.GetSourcePlayPosition(v.Id, out int ms);
                var c = v.Identity.Controller;
                Record("progress", new { v.Request, v.Event, playingId = v.Id, v.Identity.Label, v.Identity.Generation, elapsed = Clock - v.Started, ms, result = result.ToString(), paused = DolocAPI.gameLoop.IsPaused, globalPaused = DolocAPI.gameLoop.IsGlobalPaused, listener = XYZ(DolocAPI.AgentPosition), emitter = c == null ? null : XYZ(c.transform.position), active = c != null && c.gameObject.activeSelf, logical = c?.Monster?.Name });
            }
        }
        void Tick(object sender, UpdateTickedEventArgs e)
        {
            try {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Bounded R3 QA lifetime");
                if (!ready && runtime.Snapshot.Phase == DtmRuntimePhase.Title) { ready = true; Write("ready.json", new { pid = Process.GetCurrentProcess().Id }); }
                if (!world && loads > 0 && runtime.Snapshot.IsWorldReady && DolocAPI.userInput.CurrentState is NormalGameState) { world = true; Write("world-ready.json", new { loads, scene = DolocAPI.CurrentRoom.RoomId }); }
                if (preparing && arenaReady && DolocAPI.userInput.CurrentState is NormalGameState) {
                    preparing = false; host = (IMonsterHost)DolocAPI.CurrentRoom;
                    foreach (var m in host.DM_monster.AllMonsters.ToArray()) host.RemoveMonster(m);
                    anchor = DolocAPI.AgentPosition;
                    DolocAPI.Sound.SetSoundParameter(SoundParameters.VOLUME_MUSIC, 0); DolocAPI.Sound.SetSoundParameter(SoundParameters.VOLUME_SFX_AMBIENCE, 0); DolocAPI.Sound.SetSoundParameter(SoundParameters.VOLUME_SFX_UI, 0);
                    Record("arena", new { scene = DolocAPI.CurrentRoom.RoomId, anchor = XYZ(anchor), controls = "native transition; native room monsters removed in Working only; Music/Ambience/UI RTPC 0 for capture, restored on dispose; autonomous fixture updates held" });
                    Write("arena-ready.json", new { scene = DolocAPI.CurrentRoom.RoomId });
                }
                if (world && Time.realtimeSinceStartup >= nextSample) { nextSample = Time.realtimeSinceStartup + .1f; Sample(); }
                foreach (var step in steps.Where(s => !s.Done && Clock - started >= s.At).ToArray()) { step.Done = true; Record("step", new { scenario, step.Name, elapsed = Clock - started }); step.Action(); }
                var path = Path.Combine(settings.Evidence, "control.txt"); if (!File.Exists(path)) return;
                string control; using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) using (var reader = new StreamReader(input)) control = reader.ReadToEnd().Trim();
                if (control.Length == 0 || control == last) return; last = control;
                var action = control.Split('|').Last();
                Record("command", new { action });
                switch (action) {
                    case "prepare": Prepare(); break; case "bee": StartBee(); break; case "drone": StartDrone(); break; case "death": StartDeath(); break; case "nian": StartNian(); break;
                    case "clear": Clear(); break;
                    case "quit": Clear(); Write("finished.json", new { loads, saves, outstanding = voices.Where(v => !v.Ended).Select(v => new { v.Request, v.Event, v.Id }).ToArray() }); Application.Quit(); break;
                    default: throw new InvalidOperationException("Unknown R3 control " + action);
                }
                Write("ack.json", new { control });
            } catch (IOException) { /* atomic command publication may race one read; retry next tick */ }
            catch (Exception error) { Record("failure", new { error = error.ToString() }); Write("failure.json", new { error = error.ToString() }); Application.Quit(); }
        }
        void Write(string file, object value) { var path = Path.Combine(settings.Evidence, file); File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented)); }
        void Record(string name, object value) { lock (logLock) File.AppendAllText(Path.Combine(settings.Evidence, "events.jsonl"), JsonConvert.SerializeObject(new { utc = DateTimeOffset.UtcNow.ToString("o"), clock = Clock, name, value }) + "\n"); }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (prepared) { Clear(); DolocAPI.gameManager.gameInitConfig.agentInvincible = originalInvincible; AccessTools.Field(typeof(DolocTownGameStateBase), "_supportCutscenes").SetValue(DolocAPI.gameStateManager.normalGameState, originalCutscenes); Invoke(DolocAPI.Sound, "ResetVolumes"); }
            Record("dispose", new { loads, saves, outstanding = voices.Count(v => !v.Ended), restoredVolumes = prepared });
            patches.UnpatchSelf(); Application.runInBackground = originalBackground; current = null;
        }
        public sealed class Settings { public string Evidence { get; set; } = ""; public string ExpiresUtc { get; set; } = ""; }
        public sealed class Voice { public int Request, Emitter; public string Event; public uint Id; public Identity Identity; public string[] Context; public double Started, End; int endFlag; public bool Ended { get => System.Threading.Volatile.Read(ref endFlag) != 0; set => System.Threading.Volatile.Write(ref endFlag, value ? 1 : 0); } }
        public sealed class Identity { public string Label, Template; public int Generation; public MonsterController Controller; }
        sealed class Step { public double At; public string Name; public Action Action; public bool Done; }
    }
}
