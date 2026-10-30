using DTMAPI.AudioContent.Preparation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Authoring;
using DTMAPI.Modules.Audio;

internal static class MonsterChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        AudioRegistration Input(string key, AudioRoute use, bool loop = false) => new() {
            Resources = new[] { new AudioResource { Key = key + ".pcm", Path = "same.wav" } },
            Events = new[] { new AudioEvent { Key = key, Resource = key + ".pcm", Use = AudioUse.Effect, Route = use, Loop = loop, VolumeDb = -6.02059991f } }
        };
        var parsed = AudioContentReader.Parse("{\"resources\":[{\"key\":\"r\",\"path\":\"a.wav\"}],\"events\":[{\"key\":\"h\",\"resource\":\"r\",\"use\":\"effect\",\"route\":\"NativeMonster\",\"loop\":true},{\"key\":\"d\",\"resource\":\"r\",\"use\":\"effect\",\"route\":\"NativeMonsterDeath\"}]}");
        check(parsed.Events[0].Loop && !parsed.Events[1].Loop, "Plain Audio JSON distinguishes a loop from a finite Dead tail.");
        var explosionJson = AudioContentReader.Parse("{\"resources\":[{\"key\":\"r\",\"path\":\"a.wav\"}],\"events\":[{\"key\":\"x\",\"resource\":\"r\",\"use\":\"effect\",\"route\":\"NativeMonsterExplosion\"}]}");
        check(explosionJson.Events.Single().Route == AudioRoute.NativeMonsterExplosion, "Plain JSON selects the named explosion recipe.");
        throws(() => new AudioPosition(float.NaN, 0), "Reject nonfinite world position before native calls.");
        foreach (var use in new[] { AudioRoute.NativeMonster, AudioRoute.NativeMonsterDeath, AudioRoute.NativeMonsterExplosion })
        {
            var recipe = new WwisePcmBank.Recipe("monster.layout", AudioEngine.Parent(use), AudioEngine.Bus(use), use == AudioRoute.NativeMonster);
            byte[] bank = WwisePcmBank.Build(wav(), 11, 12, new[] { recipe });
            using var r = new BinaryReader(new MemoryStream(bank));
            while (r.BaseStream.Position < bank.Length)
            {
                string tag = new(r.ReadChars(4)); int length = r.ReadInt32(); long end = r.BaseStream.Position + length;
                if (tag == "HIRC")
                {
                    r.ReadInt32(); check(r.ReadByte() == 2, "Recipe contains a Sound object."); byte[] sound = r.ReadBytes(r.ReadInt32());
                    check(BitConverter.ToUInt32(sound, 26) == WwisePcmBank.MonsterParent && BitConverter.ToUInt32(sound, 22) == recipe.Bus,
                        "Monster recipes share the verified spatial parent and preserve their selected bus.");
                    check(recipe.Bus == (use == AudioRoute.NativeMonsterExplosion ? 2211457092u : use == AudioRoute.NativeMonsterDeath ? 1069431850u : 0), "Explosion routes to Actor, Dead to its bus, ordinary Monster inherits MONSTER.");
                    int props = sound[31]; var ids = sound.Skip(32).Take(props).ToArray();
                    check(ids.Contains((byte)0x54) == recipe.Loop && sound[32 + props + props * 4 + 1] == 0,
                        "Only the looping Sound declares Loop=0, and both inherit positioning rather than disabling attenuation.");
                }
                r.BaseStream.Position = end;
            }
        }
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
        using var finite = engine.Register("Monster.Pack", 1, Input("f", AudioRoute.NativeMonster), _ => new Source(wav()), () => { });
        using var looped = engine.Register("Monster.Pack", 1, Input("l", AudioRoute.NativeMonster, true), _ => new Source(wav()), () => { });
        using var dead = engine.Register("Monster.Pack", 1, Input("d", AudioRoute.NativeMonsterDeath), _ => new Source(wav()), () => { });
        engine.Tick(true);
        check(engine.Snapshot("Monster.Pack").BankCount == 3, "Identical PCM cannot alias different loop/route recipes when sharing a media Bank.");
        var at = new AudioPosition(12, -3, .2f);
        var voice = looped.GetSound("l").Play(-6.02059991f, at);
        check(driver.Positions.Values.Single().Equals(at) && Math.Abs(driver.Gains.Values.Single() - .25f) < 1e-6f, "A dedicated spatial emitter receives caller position and one event-times-request gain.");
        voice.SetPosition(new AudioPosition(-12, -3)); voice.SetVolumeDb(0); engine.Tick(true);
        check(driver.Positions.Values.Single().X == -12 && Math.Abs(driver.Gains.Values.Single() - .5f) < 1e-6f && driver.Pauses.Count == 0, "Position follows explicit control; Tick introduces no logical menu pause or extra gain.");
        uint oldId = driver.Callbacks.Keys.Single(); Action delayed = driver.Callbacks[oldId];
        driver.End(oldId); engine.Tick(true); var next = looped.GetSound("l").Play(0f, at); delayed(); voice.Stop(); engine.Tick(true);
        check(!next.HasEnded && driver.Stopped.Count == 0, "A delayed old End and old Stop cannot address a new request.");
        driver.Fail = "position";
        throws(() => finite.GetSound("f").Play(0f, at), "Initial position failure rejects Post and compensates only its own partial emitter.");
        check(engine.Snapshot("Monster.Pack").VoiceCount == 1 && !next.HasEnded, "Failed spatial setup leaves the other live request owned.");
        driver.Fail = "unload";
        throws(looped.Dispose, "Failed contribution unload remains retryable.");
        check(engine.Snapshot("Monster.Pack").CleanupPendingCount == 1 && !next.HasEnded, "Cleanup failure retains the voice, Bank and contribution until retry succeeds.");
        looped.Dispose(); check(next.HasEnded, "Successful retry ends the retained contribution.");
    }
}
