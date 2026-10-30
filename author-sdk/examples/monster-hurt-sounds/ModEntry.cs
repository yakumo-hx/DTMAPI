using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.AudioContent;

namespace Samples.MonsterHurtSounds
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterHurtSounds";
        public override void Entry(IDtmHelper helper)
        {
            var audioApi = helper.ModRegistry.GetApi<IAudioContentApi>(AudioModule.ProviderId)
                ?? throw new InvalidOperationException("Runtime Audio provider unavailable.");
            MonsterSound Voice(string path, AudioRoute route, float volumeDb = 0, bool loop = false)
            {
                var registration = audioApi.Register(new AudioRegistration {
                    Resources = new[] { new AudioResource { Key = "media", Path = path } },
                    Events = new[] { new AudioEvent { Key = "voice", Resource = "media", Use = AudioUse.Effect, Route = route, Loop = loop } } });
                helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0").Register(registration);
                return new MonsterSound { Sound = registration.GetSound("voice"), VolumeDb = volumeDb };
            }
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            const string wav = "Content/DTMAPI/audio/hurt.wav";
            const string stereo = "Content/DTMAPI/audio/stereo.wav";
            var leases = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "voice", "stereo", "quiet", "native", "dead", "both", "quiet-dead", "humming", "humming-stereo", "humming-all", "quiet-humming" })
            {
                var definition = new MonsterDefinition { LocalId = id, TemplateId = "amoeba", Health = 500, Defense = 0 };
                string sound = id == "stereo" || id == "both" || id == "humming-stereo" ? stereo : wav;
                if (id.Contains("humming")) {
                    definition.TemplateId = "bee";
                    definition.Sounds = new MonsterSounds { Humming = Voice(sound, AudioRoute.NativeMonster, id == "quiet-humming" ? float.NegativeInfinity : -6.0206f, false) };
                    if (id == "humming-all") {
                        definition.Sounds.Hurt = Voice(wav, AudioRoute.NativeMonster, -13.9794001f, false);
                        definition.Sounds.Dead = Voice(wav, AudioRoute.NativeMonsterDeath, -10.4575749f, false);
                    }
                }
                else if (id == "dead" || id == "quiet-dead") definition.Sounds = new MonsterSounds {
                    Dead = Voice(wav, AudioRoute.NativeMonsterDeath, id == "quiet-dead" ? float.NegativeInfinity : -3.7417329f, false) };
                else if (id == "both") definition.Sounds = new MonsterSounds {
                    Hurt = Voice(wav, AudioRoute.NativeMonster, -3.74173287f, false),
                    Dead = Voice(sound, AudioRoute.NativeMonsterDeath, -10.4575749f, false) };
                else if (id != "native") definition.Sounds = new MonsterSounds {
                    Hurt = Voice(sound, AudioRoute.NativeMonster, id == "quiet" ? float.NegativeInfinity : id == "stereo" ? -10.4575749f : -3.7417329f, false) };
                var lease = Require(api.RegisterDefinition(definition)); leases.Add(lease); owned.Register(lease);
            }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("sounds", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var result = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1],
                        X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) }));
                    c.WriteLine("spawned " + result.Handle);
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    foreach (var lease in leases) lease.Dispose(); c.WriteLine("withdrawn");
                } else c.WriteLine("sounds spawn <voice|stereo|quiet|native|dead|both|quiet-dead|humming|humming-stereo|humming-all|quiet-humming> <x> <y> | withdraw");
            }, description: "Independent public Hurt/Dead/Humming resources on new monsters; quiet selects explicit silence, native inherits events.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
