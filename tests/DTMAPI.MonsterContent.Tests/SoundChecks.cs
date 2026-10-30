using System;
using System.IO;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static byte[] SoundWave(bool ancillary = false)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        void Tag(string tag) { writer.Write(System.Text.Encoding.ASCII.GetBytes(tag)); }
        Tag("RIFF"); writer.Write(0); Tag("WAVE");
        if (ancillary) { Tag("JUNK"); writer.Write(1); writer.Write((byte)7); writer.Write((byte)0); }
        Tag("fmt "); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(22050); writer.Write(44100); writer.Write((short)2); writer.Write((short)16);
        Tag("data"); writer.Write(8); foreach (short value in new short[] { -32768, -2, 2, 32767 }) writer.Write(value);
        writer.Flush(); stream.Position = 4; writer.Write((int)stream.Length - 8); writer.Flush(); return stream.ToArray();
    }
    static void SoundChecks()
    {
        var sound = new MonsterSound { ResourcePath = "audio/root.wav", VolumeDb = .5f };
        var d = Definition(); d.Sounds = new MonsterSounds { Hurt = sound, Dead = sound, Humming = sound };
        Check(MonsterValidation.Validate("A", d) == null, "Root events accept the same public resource with independent settings.");
        var frozen = new DefinitionRecord("A", d, 1).Definition;
        sound.ResourcePath = "audio/changed.wav"; sound.VolumeDb = 0;
        Check(frozen.Sounds!.Hurt!.ResourcePath == "audio/root.wav" && frozen.Sounds.Dead!.VolumeDb == .5f &&
            !ReferenceEquals(frozen.Sounds.Hurt, frozen.Sounds.Dead), "Definition freeze copies event settings without owning PCM.");
        foreach (string field in new[] { "Hurt", "Dead", "Humming", "RecoveryWarning" })
        {
            var property = typeof(MonsterSounds).GetProperty(field)!;
            foreach (var bad in new[] { new MonsterSound(), new MonsterSound { ResourcePath = "../escape.wav" }, new MonsterSound { ResourcePath = "audio/root.wav", VolumeDb = float.NaN } })
            {
                var value = Definition(); value.Sounds = new MonsterSounds(); property.SetValue(value.Sounds, bad);
                Check(MonsterValidation.Validate("A", value)?.Code == "InvalidSound", "Root resource admission rejects missing/escaping paths and nonfinite gain: " + field);
            }
        }
        d.Sounds = new MonsterSounds(); Check(MonsterValidation.Validate("A", d)?.Field == "sounds", "Empty sound intent rejects.");
        var publisher = new Publisher { Ready = true, Generation = 1, Failure = "WaitingNative" };
        var registry = new DefinitionRegistry(publisher, () => { });
        d = Definition(); d.Sounds = frozen.Sounds;
        var waiting = registry.Register("Pending", d).Value!;
        Check(waiting.Snapshot.State == MonsterDefinitionState.WaitingNative && registry.CountOwnerResources("Pending") == 1,
            "Pending native publication retains its independently withdrawable definition.");
        publisher.Failure = null; registry.Refresh();
        Check(waiting.Snapshot.State == MonsterDefinitionState.Creatable, "Native readiness publishes the same registered definition.");
        waiting.Dispose();
        Check(publisher.Last!.Definition.Sounds == null && registry.CountOwnerResources("Pending") == 0, "Withdrawal releases frozen resource inputs.");
        publisher.Failure = "WaitingNative"; var cancelled = registry.Register("Cancelled", d).Value!; cancelled.Dispose();
        int publishes = publisher.Publishes; publisher.Failure = null; registry.Refresh();
        Check(cancelled.Snapshot.State == MonsterDefinitionState.Withdrawn && publisher.Publishes == publishes,
            "A definition withdrawn while waiting cannot publish later.");
        publisher.Failure = "SoundUnavailable"; var rejected = registry.Register("Rejected", d);
        Check(!rejected.Success && rejected.Diagnostic!.Code == "SoundUnavailable" && publisher.Last!.Definition.Sounds == null,
            "Unavailable publication keeps its diagnostic without retaining sound inputs.");
    }
}
