using System;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;

namespace ManboCardboardAudioMod
{
    public sealed class ModEntry : DtmMod
    {
        private const string TargetEvent = "PLAY_RESOURCE_PAPER_BOX";
        private IDtmHelper helper = null!;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            RegisterAudioReplacement("Entry");
            RegisterConfigMenu();
        }

        private void RegisterAudioReplacement(string reason)
        {
            IAudioReplacementApi? api = helper.ModRegistry.GetApi<IAudioReplacementApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
            {
                helper.Monitor.Log("ManboCardboardAudio API missing; cannot register audio replacement. reason=" + reason, LogLevel.Warn);
                return;
            }

            string audioPath = Path.Combine(GetModDirectory(), "assets", "manbo.wav");
            AudioReplacementRegisterResult result = api.RegisterReplacement(helper.ModManifest, new AudioReplacementOptions
            {
                Enabled = true,
                ReplacementId = "manbo-paper-box",
                NativeSoundEvent = TargetEvent,
                AudioPath = audioPath,
                SuppressNativeWhenReady = true,
                Volume = 1.0,
                CooldownMilliseconds = 50,
                VerboseLogging = true
            });

            helper.Monitor.Log("ManboCardboardAudio register success=" + result.Success +
                " event=" + result.NativeSoundEvent +
                " hook=" + result.HookInstalled +
                " preload=" + result.PreloadReady +
                " message=" + result.Message,
                result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, () => RegisterAudioReplacement("config-reset"), () => RegisterAudioReplacement("config-save"));
            menu.SetDisplayName(helper.ModManifest, () => helper.ModManifest.Name);
            menu.AddSectionTitle(helper.ModManifest, () => "曼波纸箱音效");
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
        }

        private string BuildStatusText()
        {
            IAudioReplacementApi? api = helper.ModRegistry.GetApi<IAudioReplacementApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
                return "AudioReplacement API 未加载。";

            AudioReplacementState state = api.GetState(helper.ModManifest.UniqueID);
            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            return "事件: " + TargetEvent +
                "\n状态: " + status.Status +
                "\n详情: " + status.Details +
                "\n最近命中: " + (string.IsNullOrWhiteSpace(state.LastNativeSoundEvent) ? "无" : state.LastNativeSoundEvent) +
                "\n已播放替换: " + state.LastReplacementPlayed +
                "\n已压制原声: " + state.LastNativeSuppressed;
        }

        private static string GetModDirectory()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string? directory = Path.GetDirectoryName(location);
            return string.IsNullOrWhiteSpace(directory) ? AppDomain.CurrentDomain.BaseDirectory : directory;
        }
    }
}

