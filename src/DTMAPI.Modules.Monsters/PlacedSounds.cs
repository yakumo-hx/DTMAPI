using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.AudioContent;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        // Original skills can outlive their emitting controller. Capture stable
        // target/slot/room provenance, not the controller's next pooled tenant.
        private sealed class PlacedSoundOrigin
        {
            internal InstanceScope? Scope;
            internal MonsterHandle? Instance;
            internal MonsterActionHandle? Action;
            internal MonsterManager Manager = null!;
            internal string Target = "", Template = "", Slot = "";
            internal bool Landmine, Cancelled;
        }
        private sealed class PlacedSoundSource
        {
            internal readonly PlacedSoundOrigin Origin;
            internal readonly Component Entity;
            internal readonly long Generation;
            internal bool Retired, Cancelled;
            internal PlacedSoundSource(PlacedSoundOrigin origin, Component entity, long generation) { Origin = origin; Entity = entity; Generation = generation; }
        }
        private sealed class PlacedSoundVoice
        {
            internal PlacedSoundSource Source = null!;
            internal string Field = "";
            internal MonsterSound Input = null!;
            internal AudioPlayback Playback = null!;
            internal bool StopRequested, Failed;
        }
        private readonly Dictionary<Skill, PlacedSoundSource> placedSoundSkills = new Dictionary<Skill, PlacedSoundSource>();
        private readonly Dictionary<Boom, PlacedSoundSource> placedSoundBooms = new Dictionary<Boom, PlacedSoundSource>();
        private readonly List<PlacedSoundVoice> placedSoundVoices = new List<PlacedSoundVoice>();
        private PlacedSoundSource? invokingPlacedBoom;
        private long placedSoundGeneration;
        private int placedSoundPlays, placedSoundFallbacks;

        private PlacedSoundOrigin? CapturePlacedSoundOrigin(MonsterAttackBehaviour attack, InstanceScope? scope)
        {
            if (!Ready || !(attack._proto is ILandmine) && !(attack._proto is ITimeBomb)) return null;
            var controller = AttackSoundControllerField.GetValue(attack) as MonsterController;
            var logical = controller?.Monster;
            if (controller == null || logical?.Host == null || logical.IsDead || !ReferenceEquals(logical.Controller, controller) ||
                !controller.attackBehaviourManager.AttackBehaviours.Contains(attack)) return null;
            if (scope != null ? !scope.Active || scope.PendingRemoveLogic || !ReferenceEquals(scope.Logical, logical) : bindings.ContainsKey(logical.proto.Name)) return null;
            return new PlacedSoundOrigin { Scope = scope, Instance = scope?.Handle, Action = scope?.Action?.Handle,
                Manager = logical.Host.DM_monster, Target = logical.proto.Name, Template = scope?.Binding.Template.Proto.Name ?? logical.proto.Name,
                Slot = attack._proto.AttackId.ToString(), Landmine = attack._proto is ILandmine };
        }
        private static void PlacedSoundRaised(Skill skill, bool __result)
        {
            var module = current;
            var invocation = module?.raisingNativeSkill;
            var origin = invocation?.SoundOrigin;
            if (!__result || skill == null || origin == null || invocation!.Cancelled || origin.Cancelled ||
                skill.GetType() != (origin.Landmine ? typeof(Landmine) : typeof(TimeBomb))) return;
            module!.RetirePlacedSkill(skill);
            module.placedSoundSkills.Add(skill, new PlacedSoundSource(origin, skill, ++module.placedSoundGeneration));
        }
        private static bool PlacedOriginActive(PlacedSoundOrigin origin) => !origin.Cancelled && origin.Manager.IsRunning &&
            (origin.Scope == null || origin.Scope.Active && !origin.Scope.PendingRemoveLogic && origin.Scope.Handle.Equals(origin.Instance!.Value));
        private bool TryPlayPlacedSound(PlacedSoundSource source, string field, Vector2 position)
        {
            if (source.Cancelled || source.Retired) return false;
            var scope = source.Origin.Scope;
            // Roots, declared encounter parts and native overrides share one
            // route. A failed start returns to the original Post exactly once.
            {
                if (!Ready || !PlacedOriginActive(source.Origin) || scope != null && scope.Binding.Definition.State != MonsterDefinitionState.Creatable ||
                    !PlacedSoundBoundary(source.Origin.Landmine, field)) return false;
                string selection = NativeAttackValidation.Field(source.Origin.Slot, field);
                var input = scope == null ? nativeOverrides.ResolveAttackSound(source.Origin.Target, source.Origin.Slot, field) :
                    CommonInput(scope, SoundValidation.CommonAttackKey(field, source.Origin.Slot));
                if (input == null) return false;
                string owner = scope?.Binding.Definition.Owner ?? nativeOverrides.SoundOwner(source.Origin.Target, selection);
                string id = scope?.Binding.Definition.Id ?? nativeOverrides.SoundWinner(source.Origin.Target, selection);
                try
                {
                    EnsureSoundDriver(); soundDriver!.enabled = true;
                    var playback = commonAudio
                        (input, input.VolumeDb, new AudioPosition(position.x, position.y));
                    placedSoundVoices.Add(new PlacedSoundVoice { Source = source, Field = field, Input = input, Playback = playback });
                    Log("Common placed owner=" + owner + "; definition=" + id +
                        "; template=" + source.Origin.Template + "; rental=" + (source.Origin.Instance?.Generation ?? 0) + "; generation=" + source.Generation + "; slot=" + source.Origin.Slot + "; use=" + field + "; event=" + input.ContentSoundId);
                    placedSoundPlays++; return true;
                }
                catch (Exception ex)
                {
                    placedSoundFallbacks++;
                    Log("Common placed fallback use=" + field + "; definition=" + id + "; reason=" + ex.Message);
                    return false;
                }
            }
        }
        private void StopPlacedSounds(Func<PlacedSoundVoice, bool> predicate)
        {
            foreach (var record in placedSoundVoices.Where(predicate).ToArray())
            {
                record.StopRequested = true;
                try { record.Playback.Stop(); placedSoundVoices.Remove(record); }
                catch (Exception ex)
                {
                    if (!record.Failed) Log("Placed sound stop retained field=" + record.Field + " generation=" + record.Source.Generation + " reason=" + ex.Message);
                    record.Failed = true;
                }
            }
        }
        private void RetirePlacedSkill(Skill skill)
        {
            if (!placedSoundSkills.TryGetValue(skill, out var source)) return;
            source.Retired = true; placedSoundSkills.Remove(skill);
            StopPlacedSounds(v => ReferenceEquals(v.Source, source));
        }
        private void RetirePlacedOrigins(Func<PlacedSoundOrigin, bool> predicate)
        {
            foreach (var pair in placedSoundSkills.Where(p => predicate(p.Value.Origin)).ToArray())
            { pair.Value.Origin.Cancelled = true; placedSoundSkills.Remove(pair.Key); }
            foreach (var pair in placedSoundBooms.Where(p => predicate(p.Value.Origin)).ToArray())
            { pair.Value.Origin.Cancelled = true; placedSoundBooms.Remove(pair.Key); }
            StopPlacedSounds(v => { if (!predicate(v.Source.Origin)) return false; v.Source.Origin.Cancelled = true; return true; });
        }
        private void TickPlacedSounds()
        {
            foreach (var request in placedSoundVoices.ToArray())
            {
                try
                {
                    if (request.Playback.HasEnded) { placedSoundVoices.Remove(request); continue; }
                    // Recycle/reuse retires this source before a new tenant can
                    // move it. An explosion freezes at its own final position.
                    if (!request.StopRequested && !request.Source.Retired && request.Source.Entity != null)
                    { var p = request.Source.Entity.transform.position; request.Playback.SetPosition(new AudioPosition(p.x, p.y, p.z)); }
                }
                catch (Exception ex)
                { if (!request.Failed) Log("Placed sound control retained generation=" + request.Source.Generation + "; reason=" + ex.Message); request.Failed = true; }
            }
            StopPlacedSounds(v => v.StopRequested || v.Source.Cancelled ||
                !PlacedOriginActive(v.Source.Origin) ||
                v.Field != "explosionSound" && (v.Source.Retired || v.Source.Entity == null || !v.Source.Entity.gameObject.activeInHierarchy) ||
                v.Source.Origin.Scope == null && !ReferenceEquals(nativeOverrides.ResolveAttackSound(v.Source.Origin.Target, v.Source.Origin.Slot, v.Field), v.Input));
        }
    }
}
