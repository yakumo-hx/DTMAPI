using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.AudioContent;

namespace Samples.MonsterPlacedSounds
{
    public sealed class ModEntry : DtmMod
    {
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
            MonsterSound RootSound(float volume, AudioRoute route = AudioRoute.NativeMonster) => Voice("Content/DTMAPI/audio/hurt.wav", route, volume == 0 ? float.NegativeInfinity : (float)(20 * Math.Log10(volume)), false);
            MonsterNativeAttackParameters Row(string slot, float gain) => new MonsterNativeAttackParameters { AttackId = slot,
                LandSound = RootSound(gain), CountdownSound = RootSound(gain + .1f), ExplosionSound = RootSound(gain + .2f, AudioRoute.NativeMonsterExplosion) };
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "bomber", TemplateId = "bomber", Health = 500,
                NativeAttacks = { Row("AtkNormal", .2f), Row("AtkSkill01", .25f) } })));
            // Each field has its own declaration and can be replaced/withdrawn
            // while previously placed native skills remain alive.
            var leases = new Dictionary<string, MonsterOverrideLease>();
            void Register(string slot, string field, float volume)
            {
                var row = new MonsterNativeAttackParameters { AttackId = slot };
                if (field == "land") row.LandSound = RootSound(volume);
                else if (field == "countdown") row.CountdownSound = RootSound(volume);
                else row.ExplosionSound = RootSound(volume, AudioRoute.NativeMonsterExplosion);
                var lease = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = slot + "_" + field, TargetId = "bomber", NativeAttacks = { row } }));
                owned.Register(lease); leases[slot + "/" + field] = lease;
            }
            foreach (var slot in new[] { "AtkNormal", "AtkSkill01" })
            { Register(slot, "land", .5f); Register(slot, "countdown", .6f); Register(slot, "explosion", .7f); }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("voice", c => {
                var args = c.Arguments;
                if (args.Count >= 3 && (args[0] == "mine" || args[0] == "bomb") && (args[1] == "land" || args[1] == "countdown" || args[1] == "explosion"))
                {
                    string slot = args[0] == "mine" ? "AtkNormal" : "AtkSkill01", key = slot + "/" + args[1];
                    if (args.Count == 3 && args[2] == "withdraw") { leases[key].Dispose(); c.WriteLine("withdrawn " + leases[key].Snapshot.State); return; }
                    if (args.Count == 4 && args[2] == "set")
                    {
                        float volume = float.Parse(args[3], CultureInfo.InvariantCulture);
                        if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > 1) throw new ArgumentOutOfRangeException("volume");
                        leases[key].Dispose(); Register(slot, args[1], volume); c.WriteLine("registered " + leases[key].Snapshot.State); return;
                    }
                }
                c.WriteLine("voice mine|bomb land|countdown|explosion withdraw | voice mine|bomb land|countdown|explosion set <0..1>");
            }, description: "Select sounds for each placed skill event; native skill timing and item bombs remain unchanged.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
