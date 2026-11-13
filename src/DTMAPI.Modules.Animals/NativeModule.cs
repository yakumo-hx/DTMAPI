using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AnimalContent;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.RuntimeModules.Animals;
using DolocTown;
using HarmonyLib;

namespace DTMAPI.Modules.Animals
{
    internal sealed partial class NativeModule : IAnimalBackend
    {
        private static NativeModule? current;
        private readonly IRuntimeModuleContext context;
        private readonly Harmony harmony = new Harmony("DTMAPI.Animals.Voices");
        private readonly List<Registration> registrations = new List<Registration>();
        private bool installed;
        private static readonly System.Reflection.MethodInfo Target = AccessTools.Method(typeof(Animal), "PlayAnimalSound", Type.EmptyTypes);
        public NativeModule(IRuntimeModuleContext context) { this.context = context; current = this; }
        public IDisposable Register(string owner, int order, AnimalVoiceBinding[] voices, Action guard)
        {
            guard();
            if (!installed)
            {
                context.AuthorizePatches(() =>
                {
                    var begin = AccessTools.Method(typeof(NativeModule), nameof(Begin));
                    var end = AccessTools.Method(typeof(NativeModule), nameof(End));
                    var patches = Harmony.GetPatchInfo(Target);
                    var prefix = patches?.Prefixes.Any(p => p.owner == harmony.Id && p.PatchMethod == begin) == true ? null : new HarmonyMethod(begin);
                    var finalizer = patches?.Finalizers.Any(p => p.owner == harmony.Id && p.PatchMethod == end) == true ? null : new HarmonyMethod(end);
                    if (prefix != null || finalizer != null) harmony.Patch(Target, prefix: prefix, finalizer: finalizer);
                });
                installed = true;
            }
            var registration = new Registration(this, owner, order, voices, guard);
            registrations.Add(registration);
            return registration;
        }
        private static object? Member(object? value, string name) => value == null ? null : AccessTools.Property(value.GetType(), name)?.GetValue(value) ?? AccessTools.Field(value.GetType(), name)?.GetValue(value);
        private static void Begin(object __instance, out IDisposable? __state)
        {
            __state = null;
            var module = current;
            if (module == null || module.registrations.Count == 0 || !module.context.Runtime.IsMainThread) return;
            __state = NativeReplacementScope.Suppress();
            try
            {
                bool child = Member(Member(__instance, "data"), "isChild") is bool value && value;
                string species = Member(__instance, "protoName") as string ?? "";
                string expected = (Member(Member(__instance, "proto"), child ? "SoundEventChild" : "SoundEvent") as string ?? "").ToUpperInvariant();
                var selected = module.registrations.Where(r => !r.Closed).SelectMany(r => r.Voices.Select((v, i) => new { Registration = r, Voice = v, Declaration = i }))
                    .Where(v => v.Voice.SpeciesId == species && v.Voice.Stage == (child ? AnimalStage.Child : AnimalStage.Adult) && v.Voice.NativeEvent == expected)
                    .OrderByDescending(v => v.Registration.Order).ThenByDescending(v => v.Declaration).FirstOrDefault();
                if (selected == null) return;
                selected.Registration.Guard();
                var candidate = NativeReplacementScope.Enter(selected.Voice.Sound, selected.Registration.Owner, expected, selected.Declaration, selected.Registration.Started);
                __state = new CallScope(candidate, __state);
            }
            catch (Exception ex) { module.context.Monitor.Log("Animal voice fallback: " + ex.GetBaseException().Message); }
        }
        private static Exception? End(Exception? __exception, IDisposable? __state) { __state?.Dispose(); return __exception; }
        private sealed class CallScope : IDisposable
        {
            private readonly IDisposable candidate, previous;
            internal CallScope(IDisposable candidate, IDisposable previous) { this.candidate = candidate; this.previous = previous; }
            public void Dispose() { candidate.Dispose(); previous.Dispose(); }
        }
        private void Remove(Registration registration)
        {
            registration.Stop();
            registrations.Remove(registration);
        }
        public void Dispose()
        {
            DisposeDefinitions();
            foreach (var registration in registrations.ToArray()) Remove(registration);
            if (ReferenceEquals(current, this)) current = null;
        }
        private sealed class Registration : IDisposable
        {
            private readonly NativeModule module;
            internal readonly string Owner;
            internal readonly int Order;
            internal readonly AnimalVoiceBinding[] Voices;
            internal readonly Action Guard;
            internal bool Closed;
            private readonly List<AudioPlayback> playing = new List<AudioPlayback>();
            internal Registration(NativeModule module, string owner, int order, AnimalVoiceBinding[] voices, Action guard)
            { this.module = module; Owner = owner; Order = order; Voices = voices; Guard = guard; }
            internal void Started(AudioPlayback playback) { playing.RemoveAll(p => p.HasEnded); playing.Add(playback); }
            internal void Stop()
            {
                Closed = true;
                foreach (var playback in playing) playback.Stop();
                playing.RemoveAll(p => p.HasEnded);
                if (playing.Count != 0) throw new InvalidOperationException("Animal voice playback cleanup pending.");
            }
            public void Dispose() => module.Remove(this);
        }
    }
}
