using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
                bool pagingOk = TryExerciseOfficialSavePanelPagingForSmoke(panel, Math.Max(panelSlotCount, renderedSlots), out string pagingSummary);
                int visibleSlotsAfterPaging = CountVisibleOfficialSaveSlotsForSmoke(slots);
                bool countOk = archiveFileCount >= 6 && Math.Max(panelSlotCount, renderedSlots) >= archiveFileCount;
                bool containmentOk = archiveFileCount <= 12 || (visibleSlotsAfterPaging > 0 && visibleSlotsAfterPaging <= 12);
                string status = countOk && containmentOk && pagingOk
                    ? "verified"
                    : "pending";
                string summary = "archiveFileCount=" + archiveFileCount + ", panelSlotCount=" + panelSlotCount + ", renderedSlots=" + renderedSlots + ", visibleSlotsAfterPaging=" + visibleSlotsAfterPaging + ", paging={" + pagingSummary + "}, path=DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render -> GameDataPanel.Select.";
                runtime.RuntimeMonitor.Log("MoreSaves official save UI evidence " + summary);
                runtime.SetHookStatus("Smoke.MoreSavesOfficialSaveUi", status, "GameDataUiState official save UI path", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "MoreSaves official save UI evidence failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.MoreSavesOfficialSaveUi", "failed", "GameDataUiState official save UI path", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private bool TryExerciseOfficialSavePanelPagingForSmoke(object? panel, int slotCount, out string summary)
        {
            summary = "panel=missing";
            if (panel == null)
                return false;
            if (slotCount <= 0)
            {
                summary = "slotCount=" + slotCount;
                return false;
            }

            MethodInfo? select = panel.GetType().GetMethod("Select", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            if (select == null)
            {
                summary = "select=missing, slotCount=" + slotCount;
                return false;
            }

            string evidenceDir = EnsureSaveSlotsEvidenceDirForSmoke();
            var targets = new List<int> { 0 };
            if (slotCount > 12)
                targets.Add(Math.Min(12, slotCount - 1));
            if (slotCount > 18)
                targets.Add(slotCount - 1);

            var selected = new List<string>();
            var screenshots = new List<string>();
            foreach (int target in targets)
            {
                select.Invoke(panel, new object[] { target });
                selected.Add((target + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));

                string screenshotPath = Path.Combine(evidenceDir, "official-save-ui-slot-" + (target + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png");
                if (TryCaptureScreenshot(screenshotPath))
                    screenshots.Add(screenshotPath);
            }

            summary = "select=ok, slotCount=" + slotCount + ", targets=" + string.Join("|", selected) + ", screenshots=" + screenshots.Count + ", evidence=" + evidenceDir;
            return true;
        }

        private string EnsureSaveSlotsEvidenceDirForSmoke()
        {
            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "SAVESLOTS-UI", timestamp);
            Directory.CreateDirectory(evidenceDir);
            return evidenceDir;
        }

        private static int CountVisibleOfficialSaveSlotsForSmoke(object? slots)
        {
            if (slots is not IEnumerable enumerable)
                return -1;

            int count = 0;
            foreach (object? slot in enumerable)
            {
                object? gameObject = slot == null ? null : ReadMember(slot, "gameObject");
                if (gameObject != null && (ReadBoolMember(gameObject, "activeSelf", false) || ReadBoolMember(gameObject, "activeInHierarchy", false)))
                    count++;
            }
            return count;
        }
    }
}
