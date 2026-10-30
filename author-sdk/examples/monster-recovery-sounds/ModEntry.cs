using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.AudioContent;

namespace Samples.MonsterRecoverySounds
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
            MonsterSounds RootSound(float volume) => new MonsterSounds { RecoveryWarning = Voice("Content/DTMAPI/audio/hurt.wav", AudioRoute.NativeMonster, volume == 0 ? float.NegativeInfinity : (float)(20 * Math.Log10(volume)), false) };
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition {
                LocalId = "nian", TemplateId = "nian", Health = 500, Sounds = RootSound(.2f),
                NativeParts = {
                    new MonsterNativePartParameters { TemplateId = "nian_head", Sounds = RootSound(.25f) },
                    new MonsterNativePartParameters { TemplateId = "nian_tail", Sounds = RootSound(.3f) }
                } })));
            // A complete encounter can keep the root's native warning while
            // replacing just its parts. Parts are not standalone definitions.
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition {
                LocalId = "parts", TemplateId = "nian", Health = 500,
                NativeParts = {
                    new MonsterNativePartParameters { TemplateId = "nian_head", Sounds = RootSound(.35f) },
                    new MonsterNativePartParameters { TemplateId = "nian_tail", Sounds = RootSound(.4f) }
                } })));
            owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "head", TargetId = "nian_head", Sounds = RootSound(.5f) })));
            owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "tail", TargetId = "nian_tail", Sounds = RootSound(.55f) })));
            MonsterOverrideLease root = null!;
            void Register(float volume)
            {
                root = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "root", TargetId = "nian", Sounds = RootSound(volume) }));
                owned.Register(root);
            }
            Register(.45f);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("voice", c => {
                var args = c.Arguments;
                if (args.Count == 1 && args[0] == "withdraw") { root.Dispose(); c.WriteLine("withdrawn " + root.Snapshot.State); }
                else if (args.Count == 2 && args[0] == "set") {
                    float volume = float.Parse(args[1], CultureInfo.InvariantCulture);
                    if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > 1) throw new ArgumentOutOfRangeException("volume");
                    root.Dispose(); Register(volume); c.WriteLine("registered " + root.Snapshot.State);
                } else c.WriteLine("voice withdraw | set <volume 0..1>");
            }, description: "Change the original Nian recovery warning; independent parts and other events retain their own sources.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
