using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.AudioContent;

namespace Samples.MonsterFireSounds
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
            MonsterSound RootSound(float volume) => Voice("Content/DTMAPI/audio/hurt.wav", AudioRoute.NativeMonster, volume == 0 ? float.NegativeInfinity : (float)(20 * Math.Log10(volume)), false);
            List<MonsterNativeAttackParameters> Slots(float volume, params string[] slots)
            {
                var result = new List<MonsterNativeAttackParameters>();
                foreach (var slot in slots) result.Add(new MonsterNativeAttackParameters { AttackId = slot, FireSound = RootSound(volume) });
                return result;
            }
            // Sound-only declarations leave the inherited numeric proto intact.
            var targets = new Dictionary<string, string[]> {
                { "bee", new[] { "AtkNormal" } },
                { "drone_99", new[] { "AtkNormal", "AtkSkill01", "AtkSkill02" } },
                { "ball_drone", new[] { "AtkNormal" } },
                { "seed_carrier", new[] { "AtkNormal" } },
                { "tardigrade", new[] { "AtkNormal" } },
                { "fungus", new[] { "AtkNormal" } },
                { "nian", new[] { "AtkSkill01" } }
            };
            foreach (var target in targets)
            {
                var definition = new MonsterDefinition { LocalId = target.Key, TemplateId = target.Key, Health = 500,
                    NativeAttacks = Slots(.2f, target.Value) };
                if (target.Key == "bee") definition.Sounds = new MonsterSounds { Hurt = RootSound(.1f), Dead = RootSound(.3f) };
                if (target.Key == "nian") definition.NativeParts = new List<MonsterNativePartParameters> {
                    new MonsterNativePartParameters { TemplateId = "nian_tail", NativeAttacks = Slots(.25f, "AtkNormal") }
                };
                owned.Register(Require(api.RegisterDefinition(definition)));
                if (target.Key != "bee") owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride {
                    LocalId = target.Key, TargetId = target.Key, NativeAttacks = Slots(.45f, target.Value) })));
            }
            owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "tail", TargetId = "nian_tail", NativeAttacks = Slots(.45f, "AtkNormal") })));
            MonsterOverrideLease sound = null!;
            void Register(float volume)
            {
                sound = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "fire", TargetId = "bee", NativeAttacks = Slots(volume, "AtkNormal") }));
                owned.Register(sound);
            }
            Register(.6f);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("voice", c => {
                var args = c.Arguments;
                if (args.Count == 1 && args[0] == "withdraw") { sound.Dispose(); c.WriteLine("withdrawn " + sound.Snapshot.State); }
                else if (args.Count == 2 && args[0] == "set") {
                    float volume = float.Parse(args[1], CultureInfo.InvariantCulture);
                    if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > 1) throw new ArgumentOutOfRangeException("volume");
                    sound.Dispose(); Register(volume); c.WriteLine("registered " + sound.Snapshot.State);
                } else c.WriteLine("voice withdraw | set <volume 0..1>");
            }, description: "Select original bee FireSound for its next actual shot; withdrawal retires owned tails.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
