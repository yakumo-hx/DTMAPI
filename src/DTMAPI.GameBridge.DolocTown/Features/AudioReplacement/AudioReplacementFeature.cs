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

        internal AudioReplacementService Service { get; }

        internal AudioReplacementHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IAudioReplacementApi>(manifest, Service);
        }

        public void PublishHookStatuses()
        {
            HookBridge.PublishHookStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            HookBridge.InstallHooks(patcher);
        }

        public void Update()
        {
            Service.Update();
        }

        public void SaveLoaded(bool isNewGame)
        {
        }

        public void ReturnedToTitle()
        {
        }

        public void EnvironmentReset(string reason)
        {
        }
    }
}

