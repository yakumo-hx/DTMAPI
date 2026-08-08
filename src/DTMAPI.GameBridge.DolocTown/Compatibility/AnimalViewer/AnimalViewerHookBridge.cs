using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen IAnimalViewerApi compatibility hook bridge.</summary>
    internal sealed class AnimalViewerHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly AnimalViewerService service;

        public AnimalViewerHookBridge(DtmApiRuntime runtime, AnimalViewerService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool FullInfoDataPatched { get; private set; }

        internal bool ViewerShowPrefixPatched { get; private set; }

        internal bool ViewerShowPatched { get; private set; }

        internal bool PanelUnregisterPatched { get; private set; }

        internal bool HooksReady => FullInfoDataPatched && ViewerShowPrefixPatched && ViewerShowPatched && PanelUnregisterPatched;

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Animals.ViewerRendering",
                "pending",
                "DTMAPI.GameBridge.DolocTown AnimalViewerFeature",
                "Waiting for animal viewer data and UI rendering hooks in this run.");
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!FullInfoDataPatched)
            {
                FullInfoDataPatched = patcher.TryPatchConstructorPostfix(
                    "DolocTown.UI.AnimalFullInfoData, Assembly-CSharp",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalFullInfoDataCtorPostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            if (!ViewerShowPrefixPatched)
                ViewerShowPrefixPatched = patcher.TryPatchPrefix(
                    "DolocTown.UI.AnimalViewer, Assembly-CSharp",
                    "Show",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalViewerShowPrefix), BindingFlags.Public | BindingFlags.Static),
                    1);
            if (!ViewerShowPatched)
                ViewerShowPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.AnimalViewer, Assembly-CSharp",
                    "Show",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalViewerShowPostfix), BindingFlags.Public | BindingFlags.Static),
                    1);

            if (!PanelUnregisterPatched)
                PanelUnregisterPatched = patcher.TryPatchPostfix(
                    "DolocTown.AnimalPanelUiState, Assembly-CSharp",
                    "Unregister",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalPanelUiStateUnregisterPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);

            service.SetAnimalViewerHookInstalled(HooksReady);
            runtime.SetHookStatus(
                "Animals.ViewerRendering",
                HooksReady ? "verified" : "pending",
                "Harmony Prefix/Postfix: AnimalFullInfoData(Animal) + AnimalViewer.Show + AnimalPanelUiState.Unregister",
                HooksReady
                    ? "Patched animal viewer data construction, prefix stale-row clear, postfix inactive-prefill-activate rendering, and the native panel close boundary."
                    : "Waiting for animal viewer data/UI targets to become patchable.");
        }
    }
}
