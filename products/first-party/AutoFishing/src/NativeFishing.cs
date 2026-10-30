using System;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using HarmonyLib;
using RedSaw;
using UnityEngine;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class NativeFishing : IDisposable
    {
        internal const string Owner = "Yuuka.DTMAPI.AutoFishing.Native";
        private static NativeFishing? instance;
        private readonly Harmony harmony = new Harmony(Owner);
        private readonly IMonitor monitor;
        private readonly FishingLoop loop = new FishingLoop();
        private readonly NoteInputPolicy notes = new NoteInputPolicy();
        private readonly FishingMotion motion = new FishingMotion();
        private AutoFishingConfig config;
        private BodyController? body;
        private FishingPhase lastPhase;
        private bool inputActive;
        private FishingInput input;
        private int completed;

        private static readonly AccessTools.FieldRef<AgentStateBase, BodyController> StateBody = AccessTools.FieldRefAccess<AgentStateBase, BodyController>("body");
        private static readonly AccessTools.FieldRef<AgentStateFishingReady, CastTimer> Charge = AccessTools.FieldRefAccess<AgentStateFishingReady, CastTimer>("_castTimer");
        private static readonly AccessTools.FieldRef<AgentStateFishingWait, RSTimer> BiteTimer = AccessTools.FieldRefAccess<AgentStateFishingWait, RSTimer>("_tuCounter");
        private static readonly AccessTools.FieldRef<AgentStateFishingWait, float> BiteChance = AccessTools.FieldRefAccess<AgentStateFishingWait, float>("_hookProbability");
        private static readonly AccessTools.FieldRef<AgentStateFishingBattle, FishingGameScrollBar> BattleGame = AccessTools.FieldRefAccess<AgentStateFishingBattle, FishingGameScrollBar>("gameHandle");
        private static readonly AccessTools.FieldRef<FishingGameScrollBar, float> StartTime = AccessTools.FieldRefAccess<FishingGameScrollBar, float>("startTime");
        private static readonly AccessTools.FieldRef<FishingGameScrollBar, FishingNoteData> CurrentNote = AccessTools.FieldRefAccess<FishingGameScrollBar, FishingNoteData>("currentNote");
        private static readonly AccessTools.FieldRef<FishingGameScrollBar, int> NoteIndex = AccessTools.FieldRefAccess<FishingGameScrollBar, int>("currentNoteIndex");
        private static readonly AccessTools.FieldRef<FishingGameScrollBar, FishingNoteSpawner> Spawner = AccessTools.FieldRefAccess<FishingGameScrollBar, FishingNoteSpawner>("noteSpawner");
        private static readonly AccessTools.FieldRef<FishingGameScrollBar, FishingGameController.GameStatus> GameStatus = AccessTools.FieldRefAccess<FishingGameScrollBar, FishingGameController.GameStatus>("currentGameStatus");
        private static readonly AccessTools.FieldRef<AgentStateFishingPull, float> PullDuration = AccessTools.FieldRefAccess<AgentStateFishingPull, float>("_pullDuration");

        public NativeFishing(AutoFishingConfig config, IMonitor monitor) { this.config = config; this.monitor = monitor; }
        public void Configure(AutoFishingConfig value) { value.Normalize(); config = value; }

        public void Install()
        {
            instance = this;
            try
            {
                Patch(typeof(AgentStateFishingReady), "NextState", nameof(ReadyInput), null, nameof(EndInput));
                Patch(typeof(AgentStateFishingWait), "NextState", nameof(WaitInput), nameof(WaitResult), nameof(EndInput));
                Patch(typeof(AgentStateFishingWait), "OnPlay", nameof(WaitTick), nameof(WaitTickResult));
                Patch(typeof(FishingGameScrollBar), "UpdateGame", nameof(MiniInput), null, nameof(EndInput));
                Patch(typeof(FishingGameScrollBar), "StartGame", null, nameof(MiniStarted));
                Patch(typeof(AgentStateFishingPull), "NextState", null, nameof(PullResult));
                Patch(typeof(AgentStateFishingCast), "NextState", null, nameof(CastResult));
                Patch(typeof(AgentStateFishingPull), "OnEnter", null, nameof(PullEntered));
                Patch(typeof(AgentStateFishing), "BreakFishing", nameof(Broken));
                Patch(typeof(AgentStateBase), "Update", null, nameof(StateUpdated));
                foreach (string name in new[] { "NormalUseTool", "NormalUseItem", "NormalFishing" })
                {
                    Patch(typeof(DolocUserInput), "get_" + name, nameof(ReadPress));
                    Patch(typeof(DolocUserInput), "get_" + name + "InProgress", nameof(ReadHold));
                }
                motion.Install(harmony);
            }
            catch { Dispose(); throw; }
        }

        private void Patch(Type type, string name, string? prefix = null, string? postfix = null, string? finalizer = null)
        {
            MethodInfo target = AccessTools.DeclaredMethod(type, name) ?? throw new MissingMethodException(type.FullName, name);
            harmony.Patch(target, Method(prefix), Method(postfix), finalizer: Method(finalizer));
        }
        private static HarmonyMethod? Method(string? name) => name == null ? null : new HarmonyMethod(typeof(NativeFishing), name);
        private bool Owns(AgentStateBase state) => loop.Enabled && body != null && ReferenceEquals(StateBody(state), body);
        private bool Owns(FishingGameScrollBar game) => loop.Enabled && body != null &&
            ReferenceEquals(BattleGame(body.StateManager.GetState<AgentStateFishingBattle>()), game);

        internal static FishingPhase Phase(AgentStateBase? state)
        {
            if (state is AgentStateFishingReady) return FishingPhase.Ready;
            if (state is AgentStateFishingCast) return FishingPhase.Cast;
            if (state is AgentStateFishingWait) return FishingPhase.Wait;
            if (state is AgentStateFishingBattle) return FishingPhase.Battle;
            if (state is AgentStateFishingPull) return FishingPhase.Pull;
            return state is AgentStateIdle ? FishingPhase.Idle : FishingPhase.Other;
        }

        public void Toggle()
        {
            if (loop.Enabled) { Stop("hotkey"); return; }
            try
            {
                body = DolocAPI.agent;
                if (body == null || body.Pause) return;
                var phase = Phase(body.StateManager.current);
                if (phase == FishingPhase.Other || (phase == FishingPhase.Idle && !(DolocAPI.SelectedItem is ItemFishingRod)))
                { monitor.Log("AutoFishing: select a fishing rod and stand facing water."); body = null; return; }
                loop.Enable(phase, Time.time);
                lastPhase = phase;
                notes.Reset();
                motion.Bind(body);
                monitor.Log("AutoFishing enabled; charge=" + config.CastChargeRatio + "; instant=" + config.InstantBite + "; skip=" + config.SkipMiniGame + "; fast=" + config.FastAnimations + "; multiplier=" + config.AnimationMultiplier);
                Tick();
            }
            catch (Exception ex) { Fault(ex); }
        }

        public void Tick()
        {
            if (!loop.Enabled) return;
            try
            {
                if (body == null || !ReferenceEquals(body, DolocAPI.agent)) { Stop("native-player-changed"); return; }
                var phase = Phase(body.StateManager.current);
                bool cast = loop.Observe(phase, Time.time);
                if (!loop.Enabled) { Stop("native-interruption:" + phase); return; }
                if (phase != lastPhase)
                {
                    monitor.Log("AutoFishing phase=" + phase, LogLevel.Debug);
                    lastPhase = phase;
                }
                motion.Apply(phase, config.FastAnimations ? config.AnimationMultiplier : 1f);
                if (!cast || body.Pause) return;
                if (!(DolocAPI.SelectedItem is ItemFishingRod rod)) { Stop("rod-no-longer-selected"); return; }
                body.UseFishRod(rod);
                if (!(body.StateManager.current is AgentStateFishingReady)) { Stop("native-cast-declined"); return; }
                loop.Started();
                motion.Apply(FishingPhase.Ready, config.FastAnimations ? config.AnimationMultiplier : 1f);
            }
            catch (Exception ex) { Fault(ex); }
        }

        public void Stop(string reason)
        {
            bool hadBody = body != null;
            loop.Stop(); inputActive = false; input = default; notes.Reset();
            body = null;
            motion.Restore();
            if (hadBody) monitor.Log("AutoFishing disabled; reason=" + reason + "; completed=" + completed);
        }
        private void Fault(Exception error)
        {
            try { Stop("error"); }
            catch (Exception cleanup) { monitor.Log("AutoFishing restoration still pending: " + cleanup, LogLevel.Error); }
            monitor.Log("AutoFishing stopped: " + error, LogLevel.Error);
        }
        public void Dispose()
        {
            try { Cleanup.Run(() => Stop("owner-disposed"), harmony.UnpatchSelf, motion.Dispose); }
            finally { if (ReferenceEquals(instance, this)) instance = null; }
        }

        private static void ReadyInput(AgentStateFishingReady __instance)
        {
            var self = instance;
            if (self == null || !self.Owns(__instance)) return;
            self.inputActive = true;
            self.input = new FishingInput(false, Charge(__instance).Progress < self.config.CastChargeRatio);
        }
        private static void WaitInput(AgentStateFishingWait __instance)
        {
            var self = instance;
            if (self == null || !self.Owns(__instance)) return;
            self.inputActive = true;
            self.input = new FishingInput(!__instance.IsWaitNow, false);
        }
        private static void WaitResult(AgentStateFishingWait __instance, AgentStateBase __result)
        {
            var self = instance;
            // This condition is meaningful only after native NextState actually chose Pull.
            if (self != null && self.Owns(__instance) && __result is AgentStateFishingPull && self.body!.Status.MoveModifier.inputMultiplier != 0f)
                self.Stop("native-wait-movement");
        }
        private static void WaitTick(AgentStateFishingWait __instance)
        {
            var self = instance;
            if (self == null || !self.Owns(__instance) || !self.config.InstantBite || !__instance.IsWaitNow) return;
            BiteTimer(__instance).SetInterval(0f);
            BiteChance(__instance) = 1f;
        }
        private static void WaitTickResult(AgentStateFishingWait __instance)
        {
            var self = instance;
            if (self != null && self.Owns(__instance) && !ReferenceEquals(self.body!.StateManager.current, __instance))
                self.Stop("native-wait-physical-interruption");
        }
        private static void MiniStarted(FishingGameScrollBar __instance)
        {
            var self = instance;
            if (self != null && self.Owns(__instance)) self.notes.Reset();
        }
        private static bool MiniInput(FishingGameScrollBar __instance)
        {
            var self = instance;
            if (self == null || !self.Owns(__instance) || !__instance.IsValid) return true;
            if (self.config.SkipMiniGame)
            {
                // StartGame and native rod-level RollFish already ran; never manufacture a fish.
                if (__instance.CurrentGameStatus == FishingGameController.GameStatus.Running)
                    GameStatus(__instance) = FishingGameController.GameStatus.Success;
                return false;
            }
            float time = Time.time - StartTime(__instance);
            var spawner = Spawner(__instance);
            self.inputActive = true;
            self.input = default;
            if (time < spawner.delayTime) return true;
            var note = CurrentNote(__instance);
            if (note == null || time > note.endTime) note = spawner.GetNote(NoteIndex(__instance));
            if (note != null)
                self.input = self.notes.Read((NoteKind)note.noteType, note.index, time, note.startTime, note.endTime);
            return true;
        }
        private static Exception? EndInput(Exception? __exception)
        {
            var self = instance;
            if (self != null) { self.inputActive = false; self.input = default; if (__exception != null) self.Fault(__exception); }
            return __exception;
        }
        private static bool ReadPress(ref bool __result)
        {
            if (instance == null || !instance.inputActive) return true;
            __result = instance.input.Press; return false;
        }
        private static bool ReadHold(ref bool __result)
        {
            if (instance == null || !instance.inputActive) return true;
            __result = instance.input.Hold; return false;
        }
        private static void PullEntered(AgentStateFishingPull __instance)
        {
            var self = instance;
            if (self == null || !self.Owns(__instance)) return;
            float rate = self.config.FastAnimations ? self.config.AnimationMultiplier : 1f;
            PullDuration(__instance) /= rate;
            self.motion.Apply(FishingPhase.Pull, rate);
        }
        private static void PullResult(AgentStateFishingPull __instance, AgentStateBase __result)
        {
            var self = instance;
            if (self == null || !self.Owns(__instance) || !(__result is AgentStateIdle)) return;
            self.completed++;
            self.loop.Completed(Time.time);
            self.monitor.Log("AutoFishing native-settlement=" + self.completed + "; failed=" + __instance.IsFailed + "; fish=" + (self.body!.FishingCache.FishProto?.Id ?? "none"));
        }
        private static void CastResult(AgentStateFishingCast __instance, AgentStateBase __result)
        {
            var self = instance;
            if (self != null && self.Owns(__instance) && __result is AgentStateIdle)
            {
                self.loop.Completed(Time.time);
                self.monitor.Log("AutoFishing native-cast-missed-water; ordinary retry.", LogLevel.Debug);
            }
        }
        private static void Broken() => instance?.Stop("native-BreakFishing");
        private static void StateUpdated(AgentStateBase __instance)
        {
            var self = instance;
            if (self != null && self.Owns(__instance)) self.Tick();
        }
    }
}
