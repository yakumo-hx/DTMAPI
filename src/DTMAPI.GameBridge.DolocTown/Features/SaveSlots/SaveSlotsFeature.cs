using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class SaveSlotsFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;

        public SaveSlotsFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            Service = new SaveSlotsService(runtime);
        }

        public string Id => "SaveSlots";

        internal SaveSlotsService Service { get; }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<ISaveSlotsApi>(manifest, Service);
        }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Save.MoreSlotsApi",
                "contract",
                "DTMAPI.GameBridge.DolocTown SaveSlotsFeature",
                "0.2.9 experimental save-slot contract adjusts DolocAPI.gameManager.archiveFileCount so official LocalSave and GameDataPanel paths own archive discovery/render/load/delete/copy behavior.");
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
        }

        public void Update()
        {
            Service.RefreshSaveSlotExpansionForRuntime(force: false, reason: "runtime refresh");
        }

        public void SaveLoaded(bool isNewGame)
        {
            Service.RefreshSaveSlotExpansionForRuntime(force: true, reason: "SaveLoaded");
        }

        public void ReturnedToTitle()
        {
        }

        public void EnvironmentReset(string reason)
        {
        }
    }
}
