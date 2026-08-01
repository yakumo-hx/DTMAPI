#pragma warning disable CS0618 // This file registers the frozen IActionCompletionApi compatibility island.
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Frozen 0.5.5 compatibility host for already-built IActionCompletionApi consumers.
    /// The managed OneActionComplete Advanced product does not use this feature.
    /// </summary>
    internal sealed class ActionCompletionFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public ActionCompletionFeature(DtmApiRuntime runtime, Func<bool> isToolColliderPostfixPatched, Func<bool> isInteractExitPatched)
        {
            this.runtime = runtime;
            Service = new ActionCompletionService(runtime);
            HookBridge = new ActionCompletionHookBridge(runtime, Service, isToolColliderPostfixPatched, isInteractExitPatched);
        }

        public string Id => "ActionCompletion";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "ActionCompletion",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: false,
            environmentResetSensitive: false,
            hasSaveLifetimeState: false,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal ActionCompletionService Service { get; }

        internal ActionCompletionHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IActionCompletionApi>(manifest, Service, OwnerBoundGameBridgeApis.ForActionCompletion(Service));
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
