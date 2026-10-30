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
        // Selection/stop context only. Audio owns the emitter, Playing ID, End,
        // Bank and cleanup retry; this adapter never registers a second voice.
        private sealed class CommonSoundRequest
        {
            internal InstanceScope Scope = null!;
            internal MonsterHandle Rental;
            internal MonsterController Controller = null!;
            internal Monster Logical = null!;
            internal MonsterSound Input = null!;
            internal AudioPlayback Playback = null!;
            internal string Use = "", Slot = "";
            internal object? UseInstance;
            internal long Invocation;
            internal bool Countdown, StopRequested, Failed;
        }
        private readonly List<CommonSoundRequest> commonSoundRequests = new List<CommonSoundRequest>();
        private long commonSoundInvocation;
        private static AudioPlayback commonAudio(MonsterSound sound, float volume, AudioPosition position)
            => (sound.RegisteredSound?.Invoke() ?? throw new InvalidOperationException("Monster sound is withdrawn or unavailable.")).Play(volume, position);
        public void WithdrawSounds(IReadOnlyCollection<IAudioSound> sounds)
        {
            bool Match(MonsterSound input) => input.Sound != null && sounds.Contains(input.Sound);
            StopCommonSounds(r => Match(r.Input));
            StopNativeCommonSounds(r => Match(r.Input));
            StopPlacedSounds(r => Match(r.Input));
            if (commonSoundRequests.Any(r => Match(r.Input) && !r.Playback.HasEnded) || nativeCommonSoundRequests.Any(r => Match(r.Input) && !r.Playback.HasEnded) || placedSoundVoices.Any(r => Match(r.Input) && !r.Playback.HasEnded))
                throw new InvalidOperationException("Monster sound cleanup pending native End.");
        }
        private static AudioPosition CommonPosition(MonsterController controller)
        { var p = controller.transform.position; return new AudioPosition(p.x, p.y, p.z); }
        private MonsterSound? CommonInput(InstanceScope scope, string key)
        {
            if (!bindings.TryGetValue(scope.Binding.Definition.NativeId, out var root) ||
                !ReferenceEquals(root.Definition, scope.Binding.Definition) || root.Definition.State != MonsterDefinitionState.Creatable) return null;
            string selection = scope.Binding.NativeChild ? SoundValidation.CommonPartKey(scope.Binding.Template.Proto.Name, key) : key;
            return root.CommonSounds.TryGetValue(selection, out var input) ? input : null;
        }
        private MonsterSound? FindCommonAttackSound(MonsterController controller, MonsterAttackBehaviour attack, string field, out InstanceScope? scope)
        {
            scope = null;
            // Ready uses its own 103/109 adapter before native fallback.
            // Do not retry a failed shared Ready request through this four-field route.
            if (field == "readySound" || SoundValidation.PlacedField(field) || !SoundValidation.CommonAttackField(field) || controller == null || !Ready || !controllers.TryGetValue(controller, out scope) ||
                !scope.Active || scope.PendingRemoveLogic || !ReferenceEquals(scope.Controller, controller) ||
                !ReferenceEquals(scope.Logical, controller.Monster) || scope.Logical?.IsDead != false || !scope.Host.DM_monster.IsRunning ||
                !controller.gameObject.activeInHierarchy || !ReferenceEquals(scope.Logical.Controller, controller) ||
                scope.Binding.Definition.State != MonsterDefinitionState.Creatable ||
                !ReferenceEquals(AttackSoundControllerField.GetValue(attack), controller) ||
                !controller.attackBehaviourManager.AttackBehaviours.Contains(attack) || !AttackSoundBoundary(attack._proto, field)) return null;
            return CommonInput(scope, SoundValidation.CommonAttackKey(field, attack._proto.AttackId.ToString()));
        }
        private bool TryPlayCommonSound(InstanceScope scope, MonsterSound input, string use, object? useInstance = null, string slot = "", bool countdown = false)
        {
            if (input.RegisteredSound?.Invoke() == null) return false;
            try
            {
                var controller = scope.Controller!;
                var playback = commonAudio
                    (input, input.VolumeDb, CommonPosition(controller));
                var request = new CommonSoundRequest { Scope = scope, Rental = scope.Handle, Controller = controller, Logical = scope.Logical!, Input = input,
                    Playback = playback, Use = use, Slot = slot, UseInstance = useInstance, Invocation = ++commonSoundInvocation, Countdown = countdown };
                commonSoundRequests.Add(request); EnsureSoundDriver(); soundDriver!.enabled = true;
                Log("Common sound owner=" + scope.Binding.Definition.Owner + "; definition=" + scope.Binding.Definition.Id + "; rental=" + scope.Handle.Generation +
                    "; template=" + scope.Binding.Template.Proto.Name + "; invocation=" + request.Invocation + "; use=" + use + "; slot=" + slot + "; event=" + input.ContentSoundId);
                return true;
            }
            catch (Exception ex) { Log("Common sound fallback use=" + use + "; definition=" + scope.Binding.Definition.Id + "; reason=" + ex.Message); return false; }
        }
        private bool TryPlayCommonReady(MonsterController controller, MonsterAttackBehaviour attack, SoundEvents nativeEvent)
        {
            if (NativeSoundController(controller) && ReadySoundBoundary(attack._proto) &&
                ReferenceEquals(AttackSoundControllerField.GetValue(attack), controller) && controller.attackBehaviourManager.AttackBehaviours.Contains(attack))
                return TryPlayNativeCommonSound(controller, NativeAttackValidation.Field(attack._proto.AttackId.ToString(), "readySound"), "ready", attack,
                    attack._proto.AttackId.ToString(), nativeEvent == SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN);
            if (!Ready || !controllers.TryGetValue(controller, out var scope) || !scope.Active || scope.PendingRemoveLogic ||
                !ReferenceEquals(scope.Logical, controller.Monster) || scope.Logical?.IsDead != false || !ReadySoundBoundary(attack._proto) ||
                !ReferenceEquals(AttackSoundControllerField.GetValue(attack), controller) || !controller.attackBehaviourManager.AttackBehaviours.Contains(attack)) return false;
            string slot = attack._proto.AttackId.ToString();
            var input = CommonInput(scope, "ready." + slot);
            return input != null && TryPlayCommonSound(scope, input, "ready", attack, slot, nativeEvent == SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN);
        }
        private bool TryPlayCommonRecovery(MonsterController controller)
        {
            if (NativeSoundController(controller) && controller.MonsterAI != null && RecoverySoundBoundary(controller.MonsterAI.GetType()))
                return TryPlayNativeCommonSound(controller, "sounds.recoveryWarning", "recoveryWarning", controller.MonsterAI.CurrentState, countdown: true);
            if (!Ready || !controllers.TryGetValue(controller, out var scope) || !scope.Active || scope.PendingRemoveLogic ||
                !ReferenceEquals(scope.Logical, controller.Monster) || scope.Logical?.IsDead != false || controller.MonsterAI == null ||
                !RecoverySoundBoundary(controller.MonsterAI.GetType())) return false;
            var input = CommonInput(scope, "recoveryWarning");
            return input != null && TryPlayCommonSound(scope, input, "recoveryWarning", controller.MonsterAI.CurrentState, countdown: true);
        }
        private void StopCommonRequest(CommonSoundRequest request)
        {
            request.StopRequested = true;
            request.Playback.Stop(); // Failure retains this exact request; Audio retains it until End even after success.
            commonSoundRequests.Remove(request);
        }
        private void StopCommonSounds(Func<CommonSoundRequest, bool> predicate)
        {
            var errors = new List<Exception>();
            foreach (var request in commonSoundRequests.Where(predicate).ToArray()) Attempt(errors, () => StopCommonRequest(request));
            ThrowCleanup(errors);
        }
        private void StopCommonCountdown(MonsterController controller)
        {
            StopNativeCountdown(controller);
            if (!controllers.TryGetValue(controller, out var scope)) return;
            foreach (var request in commonSoundRequests.Where(r => r.Countdown &&
                ReferenceEquals(r.Scope, scope) && r.Rental.Equals(scope.Handle) && ReferenceEquals(r.Logical, controller.Monster)).ToArray())
            {
                try { StopCommonRequest(request); }
                catch (Exception ex) { request.Failed = true; Log("Common countdown stop retained invocation=" + request.Invocation + "; reason=" + ex.Message); }
            }
            // Do not suppress the original 104: its old Sound still owns native/fallback requests.
        }
        private void TickCommonSounds()
        {
            foreach (var request in commonSoundRequests.ToArray())
            {
                if (request.Playback.HasEnded) { commonSoundRequests.Remove(request); continue; }
                bool sameRental = request.Scope.Handle.Equals(request.Rental) && ReferenceEquals(request.Scope.Logical, request.Logical) &&
                    request.Controller != null && ReferenceEquals(request.Controller.Monster, request.Logical);
                try
                {
                    if (request.StopRequested || !request.Scope.Host.DM_monster.IsRunning || request.Use != "dead" &&
                        (!sameRental || !request.Scope.Active || request.Scope.PendingRemoveLogic)) StopCommonRequest(request);
                    else if (sameRental) request.Playback.SetPosition(CommonPosition(request.Controller));
                    // Dead freezes at its last position after recycle. Neither ordinary
                    // ready cancellation nor recovery exit changes request lifetime.
                    request.Failed = false;
                }
                catch (Exception ex)
                {
                    if (!request.Failed) Log("Common sound control retained invocation=" + request.Invocation + "; reason=" + ex.Message);
                    request.Failed = true;
                }
            }
        }
    }
}
