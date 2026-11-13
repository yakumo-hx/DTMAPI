using System;
using System.IO;
using System.Text;
using System.Threading;

namespace DTMAPI.AudioContent.Preparation
{
    public enum AudioPreparationChannel { Actor, Ui, Ambience }
    public sealed class AudioPreparationResult
    {
        public string DeclarationPath { get; internal set; } = "";
        public string BankPath { get; internal set; } = "";
        public string? MediaPath { get; internal set; }
        public double DurationSeconds { get; internal set; }
    }
    public static class AudioPreparer
    {
        public static float SuggestedVolumeDb(bool music, AudioPreparationChannel channel, int level = 1)
        {
            if (level < 0 || level > 2 || !Enum.IsDefined(typeof(AudioPreparationChannel), channel)) throw new ArgumentOutOfRangeException();
            return (music ? -32 : channel == AudioPreparationChannel.Ui ? -11 : -26) + 3 * level;
        }
        public static float ParseVolumeDb(string value) => DTMAPI.Internal.AudioVolume.Parse(value);
        public static AudioPreparationResult Prepare(string inputPath, string outputDirectory, bool streamMusic,
            AudioPreparationChannel channel = AudioPreparationChannel.Actor, CancellationToken cancellationToken = default, Action<string>? progress = null, float? volumeDb = null)
        {
            if (!Enum.IsDefined(typeof(AudioPreparationChannel), channel)) throw new ArgumentOutOfRangeException(nameof(channel));
            float db = volumeDb ?? SuggestedVolumeDb(streamMusic, channel);
            string volumeJson = DTMAPI.Internal.AudioVolume.Json(db);
            string root = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(root);
            string name = "audio_" + Guid.NewGuid().ToString("N");
            string mediaDirectory = Path.Combine(root, "PackData", "audio"); Directory.CreateDirectory(mediaDirectory);
            string bankPath = Path.Combine(mediaDirectory, name + ".bnk");
            string eventPrefix = name + ".play";
            uint mediaId = WwisePcmBank.HashId(name + ".media"), bankId = WwisePcmBank.HashId(name + ".bank");
            string wemPath = Path.Combine(mediaDirectory, mediaId + ".wem");
            string declaration = Path.Combine(root, "PackData", "content.json");
            if (File.Exists(declaration)) throw new IOException("Choose a fresh output package; PackData/content.json already exists.");
            uint bus = streamMusic ? 0 : channel == AudioPreparationChannel.Ui ? WwisePcmBank.UiBus : channel == AudioPreparationChannel.Ambience ? WwisePcmBank.AmbienceBus : WwisePcmBank.ActorBus;
            var recipe = new WwisePcmBank.Recipe(eventPrefix, streamMusic ? WwisePcmBank.MusicParent : 0, bus);
            try
            {
                progress?.Invoke("正在读取音频");
                StreamingPcmInfo info;
                using (var input = File.OpenRead(inputPath))
                    info = AudioDecoder.Decode(input, AudioDecoder.DefaultExecutable, Path.Combine(root, ".audio-work"), (pcm, layout) => {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress?.Invoke("正在生成声音包");
                        if (streamMusic) layout.WriteWem(pcm, wemPath + ".part", cancellationToken);
                        using (var bank = new FileStream(bankPath + ".part", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                            WwisePcmBank.Write(bank, pcm, layout, bankId, mediaId, new[] { recipe }, streamMusic, cancellationToken);
                    }, cancellationToken, progress);
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(bankPath + ".part", bankPath);
                if (streamMusic) File.Move(wemPath + ".part", wemPath);
                string bankRelative = "PackData/audio/" + name + ".bnk";
                string media = streamMusic ? "[\"PackData/audio/" + mediaId + ".wem\"]" : "[]";
                string use = streamMusic ? "music" : "effect";
                File.WriteAllText(declaration, "{\n  \"schemaVersion\": 1,\n  \"audio\": {\n  \"resources\": [{\"key\": \"" + name + "\", \"kind\": \"bank\", \"path\": \"" + bankRelative + "\", \"stream\": " + (streamMusic ? "true" : "false") + ", \"media\": " + media + "}],\n  \"events\": [{\"key\": \"" + name + "\", \"resource\": \"" + name + "\", \"use\": \"" + use + "\", \"route\": \"" + (streamMusic ? "music" : channel.ToString().ToLowerInvariant()) + "\", \"bankEvent\": \"" + eventPrefix + ".event\", \"volumeDb\": " + volumeJson + "}]\n  }\n}\n", new UTF8Encoding(false));
                progress?.Invoke("已生成");
                return new AudioPreparationResult { DeclarationPath = declaration, BankPath = bankPath, MediaPath = streamMusic ? wemPath : null, DurationSeconds = (double)info.Bytes / (info.Rate * info.Channels * 2) };
            }
            catch
            {
                foreach (string path in new[] { bankPath, bankPath + ".part", wemPath, wemPath + ".part", declaration }) File.Delete(path);
                throw;
            }
            finally
            {
                string work = Path.Combine(root, ".audio-work");
                if (Directory.Exists(work) && Directory.GetFileSystemEntries(work).Length == 0) Directory.Delete(work);
            }
        }
    }
}
