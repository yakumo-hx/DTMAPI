using System;
using System.IO;
namespace DTMAPI.AudioContent
{
    /// <summary>Validated PCM data layout; no native objects, file handles or playback clock.</summary>
    public sealed class PcmWavInfo
    {
        internal PcmWavInfo(int channels, int rate, int offset, int length)
        { Channels = channels; SampleRate = rate; DataOffset = offset; DataLength = length; }
        public int Channels { get; }
        public int SampleRate { get; }
        public int DataOffset { get; }
        public int DataLength { get; }
        public int Frames => DataLength / (Channels * 2);
        public double DurationSeconds => (double)Frames / SampleRate;
    }
    public static class PcmWav
    {
        // Memory-backed inputs share the actual engine budget; this is not an author file limit.
        public const int MaxBytes = 64 * 1024 * 1024;
        public static PcmWavInfo InspectMusic(byte[] wav) => Inspect(wav);
        public static PcmWavInfo Inspect(byte[] wav)
        {
            if (wav == null || wav.Length < 44) throw new InvalidDataException("Expected a complete internal PCM WAV.");
            bool Tag(int at, string value) => wav[at] == value[0] && wav[at + 1] == value[1] && wav[at + 2] == value[2] && wav[at + 3] == value[3];
            uint U32(int at) => (uint)(wav[at] | wav[at + 1] << 8 | wav[at + 2] << 16 | wav[at + 3] << 24);
            int U16(int at) => wav[at] | wav[at + 1] << 8;
            if (!Tag(0, "RIFF") || !Tag(8, "WAVE") || U32(4) != wav.Length - 8) throw new InvalidDataException("Expected one complete RIFF/WAVE file.");
            int format = -1, data = -1, bytes = 0, at = 12;
            while (at < wav.Length)
            {
                if (wav.Length - at < 8) throw new InvalidDataException("Incomplete WAV chunk header.");
                uint length = U32(at + 4); long end = (long)at + 8 + length, next = end + (length & 1);
                if (next > wav.Length) throw new InvalidDataException("WAV chunk or padding exceeds the RIFF boundary.");
                if (Tag(at, "fmt "))
                {
                    if (format >= 0 || length != 16 && length != 18) throw new InvalidDataException("Expected one PCM fmt chunk of 16/18 bytes.");
                    format = at + 8;
                    if (length == 18 && U16(format + 16) != 0) throw new InvalidDataException("PCM extensions are unsupported.");
                }
                else if (Tag(at, "data"))
                {
                    if (data >= 0 || length == 0) throw new InvalidDataException("Expected one nonempty PCM data chunk.");
                    data = at + 8; bytes = (int)length;
                }
                at = (int)next;
            }
            if (format < 0 || data < 0) throw new InvalidDataException("WAV requires fmt and data chunks.");
            int channels = U16(format + 2); uint rate = U32(format + 4);
            if (U16(format) != 1 || channels < 1 || channels > 2 || rate < 8000 || rate > 48000 || U16(format + 14) != 16)
                throw new InvalidDataException("Expected PCM16 mono/stereo at 8–48 kHz.");
            int align = channels * 2;
            if (U16(format + 12) != align || U32(format + 8) != rate * align || bytes % align != 0)
                throw new InvalidDataException("Invalid PCM alignment/rate.");
            return new PcmWavInfo(channels, (int)rate, data, bytes);
        }
    }
}
