using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.AudioContent;

namespace Samples.MonsterNativeHurtSounds
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
            MonsterOverrideLease sound = null!;
            void Register(float volume)
            {
                var next = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "hurt", TargetId = "bee",
                    Sounds = new MonsterSounds { Hurt = Voice("Content/DTMAPI/audio/hurt.wav", AudioRoute.NativeMonster, volume == 0 ? float.NegativeInfinity : (float)(20 * Math.Log10(volume)), false) } }));
                sound = next; owned.Register(next);
            }
            Register(.6f);
            // A new definition has its own event; the native target override does not
            // replace its independent setting, even when it inherits the same template.
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "control", TemplateId = "bee", Health = 500,
                Sounds = new MonsterSounds { Hurt = Voice("Content/DTMAPI/audio/hurt.wav", AudioRoute.NativeMonster, -13.9794001f, false) } })));
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("voice", c => {
                var args = c.Arguments;
                if (args.Count == 1 && args[0] == "withdraw") { sound.Dispose(); c.WriteLine("withdrawn " + sound.Snapshot.State); }
                else if (args.Count == 2 && args[0] == "set") {
                    float volume = float.Parse(args[1], CultureInfo.InvariantCulture);
                    if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > 1) throw new ArgumentOutOfRangeException("volume");
                    sound.Dispose(); Register(volume); c.WriteLine("registered " + sound.Snapshot.State);
                } else c.WriteLine("voice withdraw | set <volume 0..1>");
            }, description: "Override native bee Hurt; independent new definitions retain their own sounds.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
