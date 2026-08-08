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
            RepairService = new NativeUiLayoutRepairService(runtime);
        }

        public string Id => "NativeUiLayoutDiagnostics";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "NativeUiLayoutDiagnostics",
            requiresSave: false,
            allowsTitleScreen: true,
            requiresNativeScene: false,
            requiresUi: true,
            environmentResetSensitive: false,
            hasSaveLifetimeState: false,
            hasTitleLifetimeState: true,
            canAutoPauseAfterFailure: false);

        internal NativeUiLayoutRepairService RepairService { get; }

        internal bool HomePageRepairReceiptReady =>
            RepairService != null && HomePageRenderTextMenuPatched;

        internal bool MainMenuRepairReceiptReady =>
            RepairService != null && MainMenuPanelStartShowPatched && MenuUiSetCapacityPatched;

        internal bool TargetedRepairReady => HomePageRepairReceiptReady && MainMenuRepairReceiptReady;

        internal bool HomePageRenderTextMenuPatched { get; private set; }

        internal bool MenuUiSetCapacityPatched { get; private set; }

        internal bool MainMenuPanelStartShowPatched { get; private set; }

        internal bool GameDataPanelSetCapacityPatched { get; private set; }

        public void RegisterApis(IManifest manifest)
        {
        }

        public void PublishHookStatuses()
        {
            PublishStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
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
            RepairService.UpdateActiveMenuLayout();
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
            bool targetedReady = TargetedRepairReady;
            string status = targetedReady ? "ready" : "pending";
            string details = "HomePage.RenderTextMenu=" + HomePageRenderTextMenuPatched +
                ", MainMenuPanel.OnStartShow=" + MainMenuPanelStartShowPatched +
                ", MenuUI.SetCapacity=" + MenuUiSetCapacityPatched +
                ", GameDataPanel.SetCapacity=" + GameDataPanelSetCapacityPatched +
                ". Production repair is always active through exact menu hooks and active-menu observation; optional QA observes through a read-only fixture facade. Generic DolocGridUI<T> hooks and global Unity GridLayoutGroup setter normalization are not installed.";
            runtime.SetHookStatus("UI.NativeLayoutRepair", status, "Harmony repair: official menu layout methods", details);
        }
    }
}
