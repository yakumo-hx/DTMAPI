using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class NativeUiLayoutDiagnosticsFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public NativeUiLayoutDiagnosticsFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new NativeUiLayoutDiagnosticsService(runtime);
        }

        public string Id => "NativeUiLayoutDiagnostics";

        internal NativeUiLayoutDiagnosticsService Service { get; }

        internal bool GridResetPatched { get; private set; }

        internal bool GridSetCapacityPatched { get; private set; }

        internal bool HomePageRenderTextMenuPatched { get; private set; }

        internal bool HomePageTextMenuResetLayoutPatched { get; private set; }

        internal bool MenuUiSetCapacityPatched { get; private set; }

        internal bool MenuUiResetLayoutPatched { get; private set; }

        internal bool HomePageTextMenuResetLayoutPrefixPatched { get; private set; }

        internal bool MenuUiResetLayoutPrefixPatched { get; private set; }

        internal bool MainMenuPanelStartShowPatched { get; private set; }

        internal bool GameDataPanelSetCapacityPatched { get; private set; }

        internal bool GridLayoutConstraintCountPrefixPatched { get; private set; }

        public void RegisterApis(IManifest manifest)
        {
        }

        public void PublishHookStatuses()
        {
            PublishStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!GridResetPatched)
            {
                GridResetPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.DolocGridUI`1, Assembly-CSharp",
                    "ResetLayoutSize",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DolocGridUiResetLayoutSizePostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            if (!GridSetCapacityPatched)
            {
                GridSetCapacityPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.DolocGridUI`1, Assembly-CSharp",
                    "SetCapacity",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DolocGridUiSetCapacityPostfix), BindingFlags.Public | BindingFlags.Static),
                    2);
            }

            if (!HomePageRenderTextMenuPatched)
            {
                HomePageRenderTextMenuPatched = patcher.TryPatchPostfix(
                    "DolocTown.HomePageUiState, Assembly-CSharp",
                    "RenderTextMenu",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.HomePageRenderTextMenuPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            if (!MenuUiSetCapacityPatched)
            {
                MenuUiSetCapacityPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.MenuUI, Assembly-CSharp",
                    "SetCapacity",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MenuUiSetCapacityPostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            if (!HomePageTextMenuResetLayoutPrefixPatched)
            {
                HomePageTextMenuResetLayoutPrefixPatched = patcher.TryPatchClosedGenericPrefix(
                    "DolocTown.UI.DolocGridUI`1, Assembly-CSharp",
                    new[] { "DolocTown.UI.TextButton, Assembly-CSharp" },
                    "ResetLayoutSize",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.HomePageTextMenuResetLayoutSizePrefix), BindingFlags.Public | BindingFlags.Static),
                    1);
                runtime.RuntimeMonitor.Log("Native UI layout hook install HomePageTextMenu.ResetLayoutSize.Prefix=" + HomePageTextMenuResetLayoutPrefixPatched + ".");
            }

            if (!HomePageTextMenuResetLayoutPatched)
            {
                HomePageTextMenuResetLayoutPatched = patcher.TryPatchClosedGenericPostfix(
                    "DolocTown.UI.DolocGridUI`1, Assembly-CSharp",
                    new[] { "DolocTown.UI.TextButton, Assembly-CSharp" },
                    "ResetLayoutSize",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.HomePageTextMenuResetLayoutSizePostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
                runtime.RuntimeMonitor.Log("Native UI layout hook install HomePageTextMenu.ResetLayoutSize=" + HomePageTextMenuResetLayoutPatched + ".");
            }

            if (!MenuUiResetLayoutPrefixPatched)
            {
                MenuUiResetLayoutPrefixPatched = patcher.TryPatchClosedGenericPrefix(
                    "DolocTown.UI.DolocGridUI`1, Assembly-CSharp",
                    new[] { "DolocTown.UI.MenuButton, Assembly-CSharp" },
                    "ResetLayoutSize",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MenuUiResetLayoutSizePrefix), BindingFlags.Public | BindingFlags.Static),
                    1);
                runtime.RuntimeMonitor.Log("Native UI layout hook install MenuUI.ResetLayoutSize.Prefix=" + MenuUiResetLayoutPrefixPatched + ".");
            }

            if (!MenuUiResetLayoutPatched)
            {
                MenuUiResetLayoutPatched = patcher.TryPatchClosedGenericPostfix(
                    "DolocTown.UI.DolocGridUI`1, Assembly-CSharp",
                    new[] { "DolocTown.UI.MenuButton, Assembly-CSharp" },
                    "ResetLayoutSize",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MenuUiResetLayoutSizePostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
                runtime.RuntimeMonitor.Log("Native UI layout hook install MenuUI.ResetLayoutSize=" + MenuUiResetLayoutPatched + ".");
            }

            if (!MainMenuPanelStartShowPatched)
            {
                MainMenuPanelStartShowPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.MainMenuPanel, Assembly-CSharp",
                    "OnStartShow",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MainMenuPanelOnStartShowPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            if (!GameDataPanelSetCapacityPatched)
            {
                GameDataPanelSetCapacityPatched = patcher.TryPatchPostfix(
                    "DolocTown.UI.GameDataPanel, Assembly-CSharp",
                    "SetCapacity",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.GameDataPanelSetCapacityPostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            PublishStatuses();
        }

        public void Update()
        {
            Service.UpdateActiveMainMenuLayout();
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

        private void PublishStatuses()
        {
            bool targetedReady = HomePageRenderTextMenuPatched &&
                MenuUiSetCapacityPatched &&
                MainMenuPanelStartShowPatched;
            string status = targetedReady ? "diagnostic" : "pending";
            string details = "HomePage.RenderTextMenu=" + HomePageRenderTextMenuPatched +
                ", HomePageTextMenu.ResetLayoutSize=" + HomePageTextMenuResetLayoutPatched +
                ", HomePageTextMenu.ResetLayoutSize.Prefix=" + HomePageTextMenuResetLayoutPrefixPatched +
                ", MainMenuPanel.OnStartShow=" + MainMenuPanelStartShowPatched +
                ", MenuUI.SetCapacity=" + MenuUiSetCapacityPatched +
                ", MenuUI.ResetLayoutSize=" + MenuUiResetLayoutPatched +
                ", MenuUI.ResetLayoutSize.Prefix=" + MenuUiResetLayoutPrefixPatched +
                ", GameDataPanel.SetCapacity=" + GameDataPanelSetCapacityPatched +
                ", DolocGridUI.ResetLayoutSize=" + GridResetPatched +
                ", DolocGridUI.SetCapacity=" + GridSetCapacityPatched +
                ". Logs layout counts and call stacks; no global Unity GridLayoutGroup setter normalization is installed. Update samples are diagnostics only. ResetLayoutSize prefix hooks are best-effort diagnostics for the generic base.";
            runtime.SetHookStatus("UI.NativeLayoutDiagnostics", status, "Harmony diagnostics: official menu layout methods", details);
        }
    }
}
