#pragma warning disable CS0618 // This file registers the frozen IActionSpeedApi compatibility island.
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ActionSpeedFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public ActionSpeedFeature(DtmApiRuntime runtime, AgentStateLifecycleHookBridge lifecycleHooks)
        {
            this.runtime = runtime;
            Service = new ActionSpeedService(runtime);
            HookBridge = new ActionSpeedHookBridge(runtime, Service, lifecycleHooks);
        }

        public string Id => "ActionSpeed";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "ActionSpeed",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: false,
            environmentResetSensitive: true,
            hasSaveLifetimeState: true,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal ActionSpeedService Service { get; }

        internal ActionSpeedHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IActionSpeedApi>(manifest, Service, OwnerBoundGameBridgeApis.ForActionSpeed(Service));
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
            Service.RestoreActionSpeed("SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            Service.RestoreActionSpeed("ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
            Service.RestoreActionSpeed("EnvironmentReset:" + reason);
        }
    }
}
