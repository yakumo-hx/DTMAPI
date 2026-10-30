using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.AudioContent;
using DTMAPI.MonsterContent;
using DolocTown;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        // Selection/activity only. Audio owns media, budget, voice and End.
        private sealed class NativeCommonSoundRequest
        {
            internal MonsterController Controller = null!;
            internal Monster Logical = null!;
            internal MonsterManager Manager = null!;
            internal MonsterSound Input = null!;
            internal AudioPlayback Playback = null!;
            internal string Owner = "", OverrideId = "", Target = "", Field = "", Use = "", Slot = "";
            internal object? UseInstance;
            internal long Rental, Invocation;
            internal bool Countdown, StopRequested, Failed;
        }
        private readonly List<NativeCommonSoundRequest> nativeCommonSoundRequests = new List<NativeCommonSoundRequest>();
        private readonly Dictionary<MonsterController, long> nativeSoundRentals = new Dictionary<MonsterController, long>();
        private long nativeSoundRentalSequence;
        private long NativeSoundRental(MonsterController controller)
        {
            if (!nativeSoundRentals.TryGetValue(controller, out long rental)) nativeSoundRentals.Add(controller, rental = ++nativeSoundRentalSequence);
            return rental;
        }
        private bool NativeSoundController(MonsterController controller, bool dead = false)
        {
            var logical = controller?.Monster;
            return Ready && logical?.Host != null && logical.IsDead == dead && logical.Host.DM_monster.IsRunning &&
                controller!.gameObject.activeInHierarchy && ReferenceEquals(logical.Controller, controller) &&
                !controllers.ContainsKey(controller) && !bindings.ContainsKey(logical.proto.Name);
        }
        private bool TryPlayNativeCommonSound(MonsterController controller, string field, string use, object? useInstance = null, string slot = "", bool countdown = false)
        {
            var logical = controller.Monster;
            var input = nativeOverrides.ResolveSound(logical.proto.Name, field);
            if (input?.RegisteredSound?.Invoke() == null) return false;
            string owner = nativeOverrides.SoundOwner(logical.proto.Name, field), winner = nativeOverrides.SoundWinner(logical.proto.Name, field);
            try
            {
                EnsureSoundDriver(); soundDriver!.enabled = true;
                var playback = commonAudio
                    (input, input.VolumeDb, CommonPosition(controller));
                var request = new NativeCommonSoundRequest { Controller = controller, Logical = logical, Manager = logical.Host.DM_monster,
                    Owner = owner, OverrideId = winner, Target = logical.proto.Name, Field = field, Use = use, Slot = slot, UseInstance = useInstance,
                    Input = input, Playback = playback, Rental = NativeSoundRental(controller), Invocation = ++commonSoundInvocation, Countdown = countdown };
                nativeCommonSoundRequests.Add(request);
                Log("Common native owner=" + owner + "; override=" + winner + "; target=" + request.Target + "; rental=" + request.Rental +
                    "; invocation=" + request.Invocation + "; use=" + use + "; slot=" + slot + "; event=" + input.ContentSoundId);
                return true;
            }
            catch (Exception ex) { Log("Common native fallback owner=" + owner + "; override=" + winner + "; use=" + use + "; slot=" + slot + "; reason=" + ex.Message); return false; }
        }
        private void StopNativeCommonSounds(Func<NativeCommonSoundRequest, bool> predicate)
        {
            var errors = new List<Exception>();
            foreach (var request in nativeCommonSoundRequests.Where(predicate).ToArray())
            {
                request.StopRequested = true;
                Attempt(errors, () => { request.Playback.Stop(); nativeCommonSoundRequests.Remove(request); });
            }
            ThrowCleanup(errors);
        }
        private void RefreshNativeSounds()
        {
            StopNativeCommonSounds(r => !ReferenceEquals(nativeOverrides.ResolveSound(r.Target, r.Field), r.Input));
            StopPlacedSounds(v => v.Source.Origin.Scope == null && !ReferenceEquals(nativeOverrides.ResolveAttackSound(v.Source.Origin.Target, v.Source.Origin.Slot, v.Field), v.Input));
        }
        private void ClearNativeSounds()
        {
            StopNativeCommonSounds(_ => true);
            nativeSoundRentals.Clear();
        }
        private void StopNativeCountdown(MonsterController controller)
        {
            try { StopNativeCommonSounds(r => r.Countdown && ReferenceEquals(r.Controller, controller) && ReferenceEquals(r.Logical, controller.Monster) &&
                nativeSoundRentals.TryGetValue(controller, out long rental) && r.Rental == rental); }
            catch (Exception ex) { Log("Common native countdown stop retained reason=" + ex.Message); }
        }
        private void TickNativeSounds()
        {
            foreach (var request in nativeCommonSoundRequests.ToArray())
            {
                if (request.Playback.HasEnded) { nativeCommonSoundRequests.Remove(request); continue; }
                bool sameRental = request.Controller != null && nativeSoundRentals.TryGetValue(request.Controller, out long rental) && rental == request.Rental &&
                    ReferenceEquals(request.Controller.Monster, request.Logical) && ReferenceEquals(request.Logical.Controller, request.Controller);
                try
                {
                    if (request.StopRequested || !Ready || !request.Manager.IsRunning || !ReferenceEquals(nativeOverrides.ResolveSound(request.Target, request.Field), request.Input) ||
                        request.Use != "dead" && (!sameRental || request.Logical.IsDead || !request.Controller!.gameObject.activeInHierarchy))
                        StopNativeCommonSounds(r => ReferenceEquals(r, request));
                    else if (sameRental) request.Playback.SetPosition(CommonPosition(request.Controller!));
                    // Dead freezes when its native controller rental ends.
                    request.Failed = false;
                }
                catch (Exception ex)
                {
                    if (!request.Failed) Log("Common native control retained override=" + request.OverrideId + "; invocation=" + request.Invocation + "; reason=" + ex.Message);
                    request.Failed = true;
                }
            }
        }
    }
}
