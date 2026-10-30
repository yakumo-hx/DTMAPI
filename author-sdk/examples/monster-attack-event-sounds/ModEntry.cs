using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DTMAPI.AudioContent;

namespace Samples.MonsterAttackEventSounds
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
            // Sound-only inputs preserve every native numeric attack parameter.
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "bomber", TemplateId = "bomber", Health = 500,
                NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkNormal", ThrowSound = RootSound(.2f) },
                    new MonsterNativeAttackParameters { AttackId = "AtkSkill01", ThrowSound = RootSound(.25f) } } })));
            owned.Register(Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "nian", TemplateId = "nian", Health = 500,
                NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkSkill01", VolleySound = RootSound(.3f), FireSound = RootSound(.15f) } } })));
            owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "timebomb", TargetId = "bomber",
                NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkSkill01", ThrowSound = RootSound(.45f) } } })));
            owned.Register(Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "nian-fire", TargetId = "nian",
                NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkSkill01", FireSound = RootSound(.1f) } } })));
            MonsterOverrideLease thrown = null!, volley = null!;
            void Register(bool isVolley, float volume)
            {
                var row = new MonsterNativeAttackParameters { AttackId = isVolley ? "AtkSkill01" : "AtkNormal" };
                if (isVolley) row.VolleySound = RootSound(volume); else row.ThrowSound = RootSound(volume);
                var lease = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = isVolley ? "volley" : "throw", TargetId = isVolley ? "nian" : "bomber", NativeAttacks = { row } }));
                owned.Register(lease); if (isVolley) volley = lease; else thrown = lease;
            }
            Register(false, .6f); Register(true, .5f);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("voice", c => {
                var args = c.Arguments;
                if (args.Count >= 2 && (args[0] == "throw" || args[0] == "volley"))
                {
                    bool isVolley = args[0] == "volley"; var lease = isVolley ? volley : thrown;
                    if (args.Count == 2 && args[1] == "withdraw") { lease.Dispose(); c.WriteLine("withdrawn " + lease.Snapshot.State); return; }
                    if (args.Count == 3 && args[1] == "set") {
                        float volume = float.Parse(args[2], CultureInfo.InvariantCulture);
                        if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > 1) throw new ArgumentOutOfRangeException("volume");
                        lease.Dispose(); Register(isVolley, volume); c.WriteLine("registered " + (isVolley ? volley : thrown).Snapshot.State); return;
                    }
                }
                c.WriteLine("voice throw|volley withdraw | voice throw|volley set <volume 0..1>");
            }, description: "Select native placement or volley sounds; FireSound and the other slot remain independent.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
