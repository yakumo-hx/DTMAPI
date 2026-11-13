using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.AudioContent.Preparation
{
    // One DATA media with a set of independently addressed Sound/Action/Event
    // recipes. All bytes are DTMAPI-authored Wwise 150 PCM structures.
    internal static class WwisePcmBank
    {
        internal const uint ResourceParent = 120847793;
        internal const uint PaperBoxParent = ResourceParent;
        internal const uint ResourceHitParent = 261908628;
        internal const uint AnimalParent = 69260018;
        internal const uint ActorBus = 2211457092;
        internal const uint UiBus = 3862737079;
        internal const uint AmbienceBus = 3583497273;
        internal const uint MonsterParent = 433085697;
        internal const uint MonsterDeadBus = 1069431850;
        internal const uint MusicParent = 922742495;
        internal sealed class Recipe
        {
            internal uint Sound, Action, Event, Parent, Bus;
            // The verified hit mixer adds +100 cents. Cancel that inherited
            // pitch for authored PCM while retaining its native route.
            internal readonly float Pitch;
            internal readonly bool Loop;
            internal Recipe(string prefix, uint parent = PaperBoxParent, uint bus = 0, bool loop = false)
            { Sound = HashId(prefix + ".sound"); Action = HashId(prefix + ".action"); Event = HashId(prefix + ".event"); Parent = parent; Bus = bus; Pitch = parent == ResourceHitParent ? -100f : 0f; Loop = loop; }
        }
        internal static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        internal static string Digest(byte[] bytes) { using (var hash = SHA256.Create()) return Hex(hash.ComputeHash(bytes)); }
        internal static uint HashId(string name)
        {
            uint value = 2166136261;
            foreach (byte b in Encoding.ASCII.GetBytes(name.ToLowerInvariant())) value = unchecked(value * 16777619) ^ b;
            return value;
        }
        private static void Tag(BinaryWriter w, string value) => w.Write(Encoding.ASCII.GetBytes(value));
        private static void Chunk(BinaryWriter w, string value, int size) { Tag(w, value); w.Write(size); }
        internal static int Estimate(int pcmBytes, int recipes, int pitchedRecipes = 0, int loopedRecipes = 0, int routedRecipes = 0) => checked(80 + 52 + pcmBytes + 12 + recipes * (SoundSize + 22 + 9 + 15) + 5 * (pitchedRecipes + loopedRecipes) + routedRecipes);
        private const int SoundSize = 54;
        internal static long FileSize(StreamingPcmInfo info, IReadOnlyList<Recipe> recipes) =>
            144L + info.Bytes + recipes.Count * (long)(SoundSize + 22 + 9 + 15) + 5L * recipes.Count(r => r.Pitch != 0) + 5L * recipes.Count(r => r.Loop) + recipes.Count(r => r.Parent == 0);
        internal static byte[] BuildStreaming(uint bank, uint media, IReadOnlyList<Recipe> recipes) => Build(Array.Empty<byte>(), bank, media, recipes, true);
        internal static byte[] Build(byte[] wav, uint bank, uint media, IReadOnlyList<Recipe> recipes, bool streaming = false)
        {
            using (var input = new MemoryStream(wav, false))
            using (var output = new MemoryStream())
            {
                var info = streaming ? null : StreamingPcmInfo.Inspect(input, System.Threading.CancellationToken.None);
                Write(output, input, info, bank, media, recipes, streaming);
                return output.ToArray();
            }
        }
        internal static void Write(Stream stream, Stream pcm, StreamingPcmInfo? info, uint bank, uint media, IReadOnlyList<Recipe> recipes, bool streaming,
            System.Threading.CancellationToken cancel = default)
        {
            if (streaming && recipes.Any(r => r.Parent != MusicParent || r.Loop)) throw new InvalidDataException("AudioStreamingMusicRecipe");
            if (recipes.Count == 0) throw new ArgumentException("AudioRecipeCount");
            uint[] ids = new[] { bank, media }.Concat(recipes.SelectMany(r => new[] { r.Sound, r.Action, r.Event })).ToArray();
            if (ids.Any(id => id == 0 || id == uint.MaxValue) || ids.Distinct().Count() != ids.Length) throw new InvalidDataException("AudioIdCollision");
            int pitched = recipes.Count(r => r.Pitch != 0);
            int looped = recipes.Count(r => r.Loop);
            int mediaLength = streaming ? 0 : checked(52 + info!.Bytes), hircLength = checked(4 + recipes.Count * (SoundSize + 22 + 9 + 15) + 5 * (pitched + looped) + recipes.Count(r => r.Parent == 0));
            long expectedLength = streaming ? 60 + hircLength : 88L + mediaLength + hircLength;
            using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                Chunk(w, "BKHD", 44); w.Write(150u); w.Write(bank); w.Write(0u); w.Write(16u); w.Write(18880u); w.Write(0u); w.Write(new byte[20]);
                if (!streaming)
                {
                Chunk(w, "DIDX", 12); w.Write(media); w.Write(0u); w.Write(mediaLength);
                Chunk(w, "DATA", mediaLength);
                Chunk(w, "RIFF", mediaLength - 8); Tag(w, "WAVE"); Chunk(w, "fmt ", 24);
                w.Write((ushort)0xfffe); w.Write((ushort)info!.Channels); w.Write(info.Rate); w.Write(info.Rate * info.Channels * 2);
                w.Write((ushort)(info.Channels * 2)); w.Write((ushort)16); w.Write((ushort)6); w.Write((ushort)0); w.Write(info.Channels == 1 ? 0x4101u : 0x3102u);
                Chunk(w, "data", info.Bytes); pcm.Position = info.Offset;
                Copy(pcm, stream, info.Bytes, cancel);
                }
                int hircAt = (int)stream.Position;
                Chunk(w, "HIRC", hircLength); w.Write(recipes.Count * 3);
                foreach (var r in recipes)
                {
                    int soundSize = SoundSize + (r.Pitch == 0 ? 0 : 5) + (r.Loop ? 5 : 0) + (r.Parent == 0 ? 1 : 0);
                    w.Write((byte)2); w.Write(soundSize); long start = stream.Position;
                    w.Write(r.Sound); w.Write(0x10001u); w.Write((byte)(streaming ? 2 : 0)); w.Write(media); w.Write(mediaLength); w.Write((byte)0);
                    w.Write(0u); w.Write(r.Bus); w.Write(r.Parent); w.Write((byte)0);
                    w.Write((byte)(1 + (r.Pitch == 0 ? 0 : 1) + (r.Loop ? 1 : 0)));
                    w.Write((byte)0);
                    if (r.Pitch != 0) w.Write((byte)1);
                    if (r.Loop) w.Write((byte)0x54);
                    w.Write(0f);
                    if (r.Pitch != 0) w.Write(r.Pitch);
                    if (r.Loop) w.Write(0); // Loop = 0 means infinite; omission is finite.
                    w.Write((byte)0);
                    // Parentless SFX need listener routing for output-bus volume.
                    // Keep nonspatial; native parent recipes retain their positioning.
                    w.Write((byte)(r.Parent == 0 ? 3 : 0));
                    if (r.Parent == 0) w.Write((byte)0);
                    w.Write((byte)0); w.Write(0u); w.Write(new byte[] { 0, 1, 0, 0, 0, 0 }); w.Write(0u);
                    if (stream.Position - start != soundSize) throw new InvalidDataException("AudioSoundLayout");
                    w.Write((byte)3); w.Write(22); w.Write(r.Action); w.Write((ushort)0x0403); w.Write(r.Sound); w.Write(new byte[] { 0, 0, 0, 4 }); w.Write(bank); w.Write(0u);
                    w.Write((byte)4); w.Write(9); w.Write(r.Event); w.Write((byte)1); w.Write(r.Action);
                }
                if (stream.Position != expectedLength) throw new InvalidDataException("AudioBankLayout");
                w.Flush();
                using (var md5 = MD5.Create())
                {
                    // Wwise's existing structural checksum; no source-file hash.
                    void Feed(long at, int length)
                    {
                        stream.Position = at; var buffer = new byte[65536];
                        while (length != 0)
                        {
                            cancel.ThrowIfCancellationRequested(); int count = stream.Read(buffer, 0, Math.Min(length, buffer.Length));
                            if (count == 0) throw new EndOfStreamException("AudioBankLayout");
                            md5.TransformBlock(buffer, 0, count, buffer, 0); length -= count;
                        }
                    }
                    if (!streaming) Feed(80, mediaLength);
                    Feed(hircAt + 8, hircLength);
                    md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    stream.Position = 32; w.Write(md5.Hash!);
                }
            }
        }
        internal static void Copy(Stream input, Stream output, long bytes, System.Threading.CancellationToken cancel)
        {
            var buffer = new byte[65536];
            while (bytes > 0)
            {
                cancel.ThrowIfCancellationRequested(); int count = input.Read(buffer, 0, (int)Math.Min(bytes, buffer.Length));
                if (count == 0) throw new EndOfStreamException("AudioSourceChanged");
                output.Write(buffer, 0, count); bytes -= count;
            }
        }
    }
}
