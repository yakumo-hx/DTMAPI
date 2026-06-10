using System;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void RecordMoreSavesOfficialSaveUiEvidence(Type dolocApi, object gameDataUiState)
        {
            try
            {
                object? gameManager = ReadStaticMember(dolocApi, "gameManager");
                int archiveFileCount = gameManager == null ? -1 : ReadIntMember(gameManager, "archiveFileCount", -1);
                object? panel = ReadMember(gameDataUiState, "panel");
                int panelSlotCount = panel == null ? -1 : ReadIntMember(panel, "slotCount", -1);
                object? slots = panel == null ? null : ReadMember(panel, "slots");
                int renderedSlots = CountEnumerableForSmoke(slots);
                string status = archiveFileCount >= 12 && (panelSlotCount >= 12 || renderedSlots >= 12)
                    ? "verified"
                    : "pending";
                string summary = "archiveFileCount=" + archiveFileCount + ", panelSlotCount=" + panelSlotCount + ", renderedSlots=" + renderedSlots + ", path=DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render.";
                runtime.RuntimeMonitor.Log("MoreSaves official save UI evidence " + summary);
                runtime.SetHookStatus("Smoke.MoreSavesOfficialSaveUi", status, "GameDataUiState official save UI path", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "MoreSaves official save UI evidence failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.MoreSavesOfficialSaveUi", "failed", "GameDataUiState official save UI path", ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
