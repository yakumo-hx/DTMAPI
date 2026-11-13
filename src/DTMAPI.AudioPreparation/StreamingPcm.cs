using System;
using System.IO;
using System.Text;
using System.Threading;

namespace DTMAPI.AudioContent.Preparation
{
    internal sealed class StreamingPcmInfo
    {
        internal int Channels, Rate, Offset, Bytes;
        internal long WemBytes => 52L + Bytes;
        internal static StreamingPcmInfo Inspect(Stream source, CancellationToken cancel)
        {
            using (var r = new BinaryReader(source, Encoding.UTF8, true))
            {
                string Tag() => Encoding.ASCII.GetString(r.ReadBytes(4));
                if (source.Length < 44 || Tag() != "RIFF" || r.ReadUInt32() != source.Length - 8 || Tag() != "WAVE")
                    throw new InvalidDataException("AudioStreamingWav: expected complete PCM16 RIFF/WAVE.");
                var info = new StreamingPcmInfo(); bool format = false, data = false;
                while (source.Position < source.Length)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (source.Length - source.Position < 8) throw new InvalidDataException("AudioStreamingWavChunk");
                    string tag = Tag(); uint size = r.ReadUInt32(); long start = source.Position, next = start + size + (size & 1);
                    if (next > source.Length) throw new InvalidDataException("AudioStreamingWavBoundary");
                    if (tag == "fmt ")
                    {
                        if (format || size != 16 && size != 18 || r.ReadUInt16() != 1) throw new InvalidDataException("AudioStreamingPcm16Required");
                        format = true; info.Channels = r.ReadUInt16(); info.Rate = r.ReadInt32(); int byteRate = r.ReadInt32(), align = r.ReadUInt16(), bits = r.ReadUInt16();
                        if (info.Channels < 1 || info.Channels > 2 || info.Rate < 8000 || info.Rate > 48000 || bits != 16 ||
                            byteRate != info.Rate * info.Channels * 2 || align != info.Channels * 2 || size == 18 && r.ReadUInt16() != 0)
                            throw new InvalidDataException("AudioStreamingPcm16Required: mono/stereo 8..48kHz.");
                    }
                    else if (tag == "data")
                    {
                        if (data || size == 0 || size > int.MaxValue || start > int.MaxValue) throw new InvalidDataException("AudioStreamingWavData");
                        data = true; info.Offset = (int)start; info.Bytes = (int)size;
                    }
                    source.Position = next;
                }
                if (!format || !data || info.Bytes % (info.Channels * 2) != 0) throw new InvalidDataException("AudioStreamingWavAlignment");
                return info;
            }
        }
        internal void WriteWem(Stream source, string path, CancellationToken cancel)
        {
            using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536))
            using (var w = new BinaryWriter(output, Encoding.UTF8, true))
            {
                void Tag(string tag) => w.Write(Encoding.ASCII.GetBytes(tag));
                Tag("RIFF"); w.Write(44 + Bytes); Tag("WAVE"); Tag("fmt "); w.Write(24);
                w.Write((ushort)0xfffe); w.Write((ushort)Channels); w.Write(Rate); w.Write(Rate * Channels * 2);
                w.Write((ushort)(Channels * 2)); w.Write((ushort)16); w.Write((ushort)6); w.Write((ushort)0); w.Write(Channels == 1 ? 0x4101u : 0x3102u);
                Tag("data"); w.Write(Bytes); source.Position = Offset;
                var buffer = new byte[65536]; int left = Bytes;
                while (left != 0)
                {
                    cancel.ThrowIfCancellationRequested();
                    int count = source.Read(buffer, 0, Math.Min(left, buffer.Length));
                    if (count == 0) throw new EndOfStreamException("AudioStreamingSourceChanged");
                    output.Write(buffer, 0, count); left -= count;
                }
                if (output.Length != WemBytes) throw new InvalidDataException("AudioStreamingWemLength");
                output.Flush();
            }
        }
    }
}
