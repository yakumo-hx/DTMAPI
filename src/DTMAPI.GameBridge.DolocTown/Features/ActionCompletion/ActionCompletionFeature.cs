using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ActionCompletionFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public ActionCompletionFeature(DtmApiRuntime runtime, Func<string, bool, string, string> rollOilDropFromCoal, Func<bool> isInteractExitPatched)
        {
            this.runtime = runtime;
            Service = new ActionCompletionService(runtime, rollOilDropFromCoal);
            HookBridge = new ActionCompletionHookBridge(runtime, Service, isInteractExitPatched);
        }

        public string Id => "ActionCompletion";

        internal ActionCompletionService Service { get; }

        internal ActionCompletionHookBridge HookBridge { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IActionCompletionApi>(manifest, Service);
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
