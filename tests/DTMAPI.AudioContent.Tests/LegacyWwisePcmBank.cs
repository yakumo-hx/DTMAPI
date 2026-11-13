// Test-only encoder baseline for the current Audio bank comparison checks.
// Excluded from the Monster runtime assembly; all live Monster media use public Audio.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.Modules.Monsters
{
    // Original, bounded PCM + Sound/Play/Event objects for the verified Wwise 150
    // format. No extracted audio or native bank objects are copied into this bank.
    internal static class WwisePcmBank
    {
        internal const uint ActorBus = 2211457092;
        internal static uint[] Ids(string definitionId, byte[] wav)
        {
            using (var hash = SHA256.Create())
            {
                string key = "dtmapi.monsters.hurt." + Hex(hash.ComputeHash(Encoding.UTF8.GetBytes(definitionId))) + "." + Hex(hash.ComputeHash(wav));
                return new[] { "bank", "sound", "action", "event", "media" }.Select(name => HashId(key + "." + name)).ToArray();
            }
        }
        internal static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        internal static uint HashId(string name)
        {
            uint value = 2166136261;
            foreach (byte b in Encoding.ASCII.GetBytes(name.ToLowerInvariant())) value = unchecked(value * 16777619) ^ b;
            return value;
        }
        private static byte[] Write(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            { using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) write(writer); return stream.ToArray(); }
        }
        private static void Chunk(BinaryWriter writer, string tag, byte[] data)
        { writer.Write(Encoding.ASCII.GetBytes(tag)); writer.Write((uint)data.Length); writer.Write(data); }
        internal static byte[] Build(byte[] wav, uint[] ids, bool loop = false)
        {
            var info = DTMAPI.AudioContent.PcmWav.Inspect(wav);
            if (ids == null || ids.Length != 5 || ids.Distinct().Count() != 5 || ids.Any(id => id == 0 || id == uint.MaxValue))
                throw new ArgumentException("Bank, sound, action, event and media need five distinct nonzero IDs.");
            byte[] media = Write(w => Chunk(w, "RIFF", Write(r =>
            {
                r.Write(Encoding.ASCII.GetBytes("WAVE"));
                Chunk(r, "fmt ", Write(f =>
                {
                    f.Write((ushort)0xfffe); f.Write((ushort)info.Channels); f.Write((uint)info.SampleRate);
                    f.Write((uint)(info.SampleRate * info.Channels * 2)); f.Write((ushort)(info.Channels * 2));
                    f.Write((ushort)16); f.Write((ushort)6); f.Write((ushort)0);
                    f.Write(info.Channels == 1 ? 0x4101u : 0x3102u);
                }));
                r.Write(Encoding.ASCII.GetBytes("data")); r.Write((uint)info.DataLength);
                r.Write(wav, info.DataOffset, info.DataLength);
            })));
            byte[] sound = Write(w =>
            {
                w.Write(ids[1]); w.Write(0x10001u); w.Write((byte)0); w.Write(ids[4]); w.Write((uint)media.Length); w.Write((byte)0);
                w.Write(new byte[4]); w.Write(ActorBus); w.Write(0u); w.Write((byte)0);
                // Version 150: property IDs precede their four-byte values.
                // Loop (0x54) = 0 is infinite; omission keeps one-shot playback.
                w.Write(loop ? new byte[] { 2, 0, 0x54 } : new byte[] { 1, 0 });
                w.Write(0f); if (loop) w.Write(0); w.Write((byte)0);
                // Explicit listener-relative routing is needed for the emitter's
                // output-bus gain. Keep spatialization and native attenuation off:
                // the author event applies its own distance attenuation.
                w.Write((byte)3); w.Write((byte)0);
                w.Write((byte)0); w.Write(0u);
                w.Write(new byte[] { 0, 1, 0, 0, 0, 0 }); w.Write(new byte[4]);
            });
            byte[] action = Write(w =>
            { w.Write(ids[2]); w.Write((ushort)0x0403); w.Write(ids[1]); w.Write(new byte[] { 0, 0, 0, 4 }); w.Write(ids[0]); w.Write(0u); });
            byte[] evt = Write(w => { w.Write(ids[3]); w.Write((byte)1); w.Write(ids[2]); });
            byte[] hirc = Write(w =>
            {
                w.Write(3u);
                foreach (var item in new[] { Tuple.Create((byte)2, sound), Tuple.Create((byte)3, action), Tuple.Create((byte)4, evt) })
                { w.Write(item.Item1); w.Write((uint)item.Item2.Length); w.Write(item.Item2); }
            });
            byte[] header = Write(w =>
            {
                w.Write(150u); w.Write(ids[0]); w.Write(0u); w.Write(16u); w.Write(18880u); w.Write(0u);
                using (var md5 = MD5.Create()) w.Write(md5.ComputeHash(media.Concat(hirc).ToArray()));
                w.Write(new byte[4]); // DATA begins at 80, aligned to 16 bytes.
            });
            return Write(w =>
            {
                Chunk(w, "BKHD", header);
                Chunk(w, "DIDX", Write(d => { d.Write(ids[4]); d.Write(0u); d.Write((uint)media.Length); }));
                Chunk(w, "DATA", media); Chunk(w, "HIRC", hirc);
            });
        }
    }
}
