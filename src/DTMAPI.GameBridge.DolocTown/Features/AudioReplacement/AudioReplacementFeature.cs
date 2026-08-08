using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AudioReplacementFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public AudioReplacementFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new AudioReplacementService(runtime);
            HookBridge = new AudioReplacementHookBridge(runtime, Service);
        }

        public string Id => "AudioReplacement";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "AudioReplacement",
            requiresSave: false,
            allowsTitleScreen: true,
            requiresNativeScene: false,
            requiresUi: false,
            environmentResetSensitive: false,
            hasSaveLifetimeState: true,
            hasTitleLifetimeState: true,
            canAutoPauseAfterFailure: false);

        internal AudioReplacementService Service { get; }

        internal AudioReplacementHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IAudioReplacementApi>(manifest, Service, OwnerBoundGameBridgeApis.ForAudioReplacement(Service));
        }

        public void PublishHookStatuses()
        {
            Service.RefreshContentPackDefinitions("PublishHookStatuses", force: false);
            HookBridge.PublishHookStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            HookBridge.InstallHooks(patcher);
        }

        public void Update()
        {
            Service.RefreshContentPackDefinitions("Update", force: false);
            Service.Update();
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.ClearSaveLifetimeState("SaveLoaded isNewGame=" + isNewGame);
            Service.RefreshContentPackDefinitions("SaveLoaded", force: false);
        }

        public void ReturnedToTitle()
        {
            Service.ClearSaveLifetimeState("ReturnedToTitle");
            Service.RefreshContentPackDefinitions("ReturnedToTitle", force: false);
        }

        public void EnvironmentReset(string reason)
        {
            Service.RefreshContentPackDefinitions("EnvironmentReset " + (reason ?? string.Empty), force: false);
        }
    }
}

