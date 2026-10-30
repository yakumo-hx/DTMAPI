using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.AudioContent;

namespace Samples.MonsterDashSounds
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
                foreach (var slot in slots) result.Add(new MonsterNativeAttackParameters { AttackId = slot, DashSound = RootSound(volume) });
                return result;
            }
            // Sound-only declarations keep the native numeric prototypes unchanged.
            var targets = new Dictionary<string, string[]> {
                { "bee", new[] { "AtkSkill01" } },
                { "battering_ram", new[] { "AtkNormal", "AtkSkill01" } },
                { "fwb_drone", new[] { "AtkNormal" } },
                { "bird", new[] { "AtkSkill01", "AtkSkill02" } }
            };
            foreach (var target in targets)
            {
                var definition = new MonsterDefinition { LocalId = target.Key, TemplateId = target.Key, Health = 500,
                    NativeAttacks = Slots(.2f, target.Value) };
                if (target.Key == "bee") definition.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = "AtkNormal", FireSound = RootSound(.35f) });
                owned.Register(Require(api.RegisterDefinition(definition)));
                if (target.Key != "bee") owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride {
                    LocalId = target.Key, TargetId = target.Key, NativeAttacks = Slots(.45f, target.Value) })));
            }
            // Head declarations are explicit. Neither the root nor original head source can change this head.
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "nian", TemplateId = "nian", Health = 500,
                NativeParts = { new MonsterNativePartParameters { TemplateId = "nian_head", NativeAttacks = Slots(.25f, "AtkNormal") } } })));
            owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "head", TargetId = "nian_head", NativeAttacks = Slots(.45f, "AtkNormal") })));
            owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "bee-fire", TargetId = "bee",
                NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkNormal", FireSound = RootSound(.5f) } } })));
            MonsterOverrideLease sound = null!;
            void Register(float volume)
            {
                sound = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "dash", TargetId = "bee", NativeAttacks = Slots(volume, "AtkSkill01") }));
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
            }, description: "Select original bee DashSound for its next native sting; FireSound remains independent.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
