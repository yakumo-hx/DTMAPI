using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace DTMAPI.AudioContent.Preparation
{
    internal static class AudioDecoder
    {
        internal const string Profile = "pcm16-48000-stereo-no-loudness-v1";
        internal static string DefaultExecutable => Path.Combine(Path.GetDirectoryName(typeof(AudioDecoder).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory, "audio-decoder", "ffmpeg.exe");

        // All hosts stage only their already-authorized readable stream. No
        // package path, shell command or network URL is passed through to FFmpeg.
        internal static StreamingPcmInfo Decode(Stream input, string executable, string workDirectory, Action<Stream, StreamingPcmInfo> consume, CancellationToken cancel, Action<string>? progress = null)
        {
            input.Position = 0;
            StreamingPcmInfo? direct = null;
            try { direct = StreamingPcmInfo.Inspect(input, cancel); }
            catch (InvalidDataException) { }
            input.Position = 0;
            if (direct != null) { consume(input, direct); return direct; }
            progress?.Invoke("正在转换为后端需要的 PCM 格式");
            if (!File.Exists(executable)) throw new FileNotFoundException("AudioDecoderMissing: reinstall the Audio provider or preparation tool.", executable);
            Directory.CreateDirectory(workDirectory);
            string source = Path.Combine(workDirectory, Guid.NewGuid().ToString("N") + ".input");
            string decoded = source + ".pcm";
            try
            {
                using (var output = new FileStream(source, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    WwisePcmBank.Copy(input, output, input.Length, cancel);
                var errors = new StringBuilder();
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo(executable,
                        "-nostdin -hide_banner -loglevel error -xerror -protocol_whitelist file,pipe -i " + Quote(source) +
                        " -map 0:a:0 -vn -sn -dn -c:a pcm_s16le -ar 48000 -ac 2 -f s16le pipe:1")
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
                    process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (errors) { if (errors.Length < 8192) errors.AppendLine(e.Data); } };
                    cancel.ThrowIfCancellationRequested();
                    if (!process.Start()) throw new IOException("AudioDecoderStartFailed");
                    process.BeginErrorReadLine();
                    using (cancel.Register(() => { try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { } }))
                    {
                        try
                        {
                            // The host owns every cache write. A bounded pipe also
                            // works when a game-launched child cannot create files
                            // in the host's cache directory.
                            using (var output = new FileStream(decoded, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536))
                            {
                                var buffer = new byte[65536]; int count;
                                while ((count = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) != 0)
                                { cancel.ThrowIfCancellationRequested(); output.Write(buffer, 0, count); }
                            }
                            process.WaitForExit(); cancel.ThrowIfCancellationRequested();
                            if (process.ExitCode != 0) throw new InvalidDataException("AudioDecodeFailed: " + errors.ToString().Trim());
                        }
                        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(); } }
                    }
                }
                using (var pcm = File.OpenRead(decoded))
                {
                    if (pcm.Length == 0 || pcm.Length % 4 != 0 || pcm.Length > int.MaxValue)
                        throw new InvalidDataException("AudioDecodeFailed: invalid PCM length for the backend RIFF format.");
                    var info = new StreamingPcmInfo { Channels = 2, Rate = 48000, Offset = 0, Bytes = checked((int)pcm.Length) };
                    consume(pcm, info); return info;
                }
            }
            finally { File.Delete(decoded); File.Delete(source); }
        }
        private static string Quote(string path)
        {
            if (path.IndexOf('"') >= 0) throw new ArgumentException("AudioDecoderPath");
            return "\"" + path + "\"";
        }
    }
}
