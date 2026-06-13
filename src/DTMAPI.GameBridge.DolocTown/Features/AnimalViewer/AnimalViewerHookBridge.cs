using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
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

        internal bool PanelRefreshViewerPatched { get; private set; }

        internal bool HooksReady => FullInfoDataPatched && ViewerShowPrefixPatched && ViewerShowPatched && PanelRefreshViewerPatched;

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Animals.ViewerRendering",
                "pending",
                "DTMAPI.GameBridge.DolocTown AnimalViewerFeature",
                "Animal bell UI evidence exists in ANIMAL-001; waiting for animal viewer hooks in this run.");
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

            if (!ViewerShowPatched)
            {
                ViewerShowPrefixPatched = patcher.TryPatchPrefix(
                    "DolocTown.UI.AnimalViewer, Assembly-CSharp",
                    "Show",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalViewerShowPrefix), BindingFlags.Public | BindingFlags.Static),
                    1);
                ViewerShowPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.AnimalViewer, Assembly-CSharp",
                    "Show",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalViewerShowPostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            if (!PanelRefreshViewerPatched)
            {
                PanelRefreshViewerPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.AnimalPanel, Assembly-CSharp",
                    "RefreshViewer",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalPanelRefreshViewerPostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            service.SetAnimalViewerHookInstalled(HooksReady);
            runtime.SetHookStatus(
                "Animals.ViewerRendering",
                HooksReady ? "verified" : "pending",
                "Harmony Prefix/Postfix: AnimalFullInfoData(Animal) + AnimalViewer.Show + AnimalPanel.RefreshViewer",
                HooksReady
                    ? "Patched animal viewer data construction plus prefix stale-row clear and postfix inactive-prefill-activate independent progress rows, with real UI refresh evidence in ANIMAL-001."
                    : "Waiting for animal viewer data/UI targets to become patchable.");
        }
    }
}
