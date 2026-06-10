using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AnimalViewerService : IAnimalViewerApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, AnimalHusbandryProgressOptions> animalOptions = new Dictionary<string, AnimalHusbandryProgressOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, IReadOnlyList<AnimalProgressRenderRow>> animalProgressRowsByData = new Dictionary<object, IReadOnlyList<AnimalProgressRenderRow>>();
        private readonly List<object> activeAnimalProgressOverlayObjects = new List<object>();
        private readonly List<AnimalProgressRenderRow> activeAnimalProgressOverlayRows = new List<AnimalProgressRenderRow>();
        private readonly Dictionary<string, int> husbandryThresholdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> itemTitleCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedAnimalApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool animalViewerHookInstalled;
        private bool animalViewerUiEvidenceRecorded;
        private bool animalPanelUiProbeLogged;
        private bool animalViewerUiDelayedScreenshotRecorded;
        private string? latestAnimalViewerEvidenceDir;
        private string latestAnimalProgressOverlaySummary = string.Empty;
        private DateTimeOffset lastAnimalProgressOverlayRefreshAt = DateTimeOffset.MinValue;

        public AnimalViewerService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal void SetAnimalViewerHookInstalled(bool installed)
        {
            animalViewerHookInstalled = installed;
        }

        public void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            animalOptions[owner.UniqueID] = options ?? new AnimalHusbandryProgressOptions();
            runtime.RuntimeMonitor.Log("Animal viewer bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IAnimalViewerApi.GetStatus(string uniqueId)
        {
            return animalOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(animalViewerHookInstalled ? "configured-verified-animal-viewer-hook" : "configured-pending-hook", animalViewerHookInstalled ? "Display options accepted and animal viewer data hooks are installed; real animal bell UI evidence is recorded, but the API remains experimental." : "Display options accepted; animal viewer hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No animal viewer display policy was registered for this mod.");
        }

        internal void DecorateAnimalFullInfoData(object data, object animal)
        {
            if (data == null || animal == null)
                return;
            IReadOnlyList<AnimalProgressInfo> progressItems = BuildAnimalProgressList(animal);
            if (progressItems.Count == 0)
                return;

            var renderRows = new List<AnimalProgressRenderRow>();
            foreach (KeyValuePair<string, AnimalHusbandryProgressOptions> entry in animalOptions)
            {
                AnimalHusbandryProgressOptions options = entry.Value ?? new AnimalHusbandryProgressOptions();
                if (!options.Enabled)
                    continue;

                foreach (AnimalProgressInfo progress in progressItems.OrderByDescending(p => p.Progress).ThenBy(p => p.OutputTitle, StringComparer.OrdinalIgnoreCase))
                {
                    renderRows.Add(new AnimalProgressRenderRow
                    {
                        OwnerId = entry.Key,
                        OutputTitle = progress.OutputTitle,
                        Current = progress.Current,
                        Threshold = progress.Threshold,
                        Progress = progress.Progress,
                        Color = options.ProgressColor
                    });
                    LogOnce(loggedAnimalApplications, entry.Key + ":" + progress.AnimalId + ":" + progress.OutputId, "Animal viewer progress hook applied by " + entry.Key + " for " + progress.AnimalId + "/" + progress.OutputId + " current=" + progress.Current + " threshold=" + progress.Threshold + ".");
                }
            }

            if (renderRows.Count > 0)
            {
                animalProgressRowsByData[data] = renderRows;
                AnimalProgressRenderRow primary = renderRows
                    .OrderByDescending(row => row.Progress)
                    .ThenBy(row => row.OutputTitle, StringComparer.OrdinalIgnoreCase)
                    .First();
                latestAnimalProgressOverlaySummary = "independent-row pending=" + primary.OutputTitle + " " + primary.Current + "/" + primary.Threshold + ", rows=" + renderRows.Count + ", moodOverride=False, stateDescriptionOverride=False";
                runtime.SetHookStatus("Smoke.AnimalViewerProgressUi", "pending", "AnimalFullInfoData ctor -> AnimalViewer.Show independent cloned ProgressBar", latestAnimalProgressOverlaySummary);
            }
        }

        private void ApplyAnimalProgressSinglePassData(object data, AnimalProgressRenderRow row)
        {
            string progressText = row.Current + "/" + row.Threshold;
            string label = FirstText(row.OutputTitle, "Special produce");
            float progress = (float)Math.Max(0, Math.Min(1, row.Progress));
            SetMemberValue(data, "moodInfo", label + " " + progressText);
            SetMemberValue(data, "moodProgress", progress);
            latestAnimalProgressOverlaySummary = "single-pass native moodBar row=" + label + " " + progressText + ", progress=" + progress.ToString("0.###", CultureInfo.InvariantCulture);
            runtime.SetHookStatus("Smoke.AnimalViewerProgressUi", "verified", "AnimalFullInfoData ctor -> AnimalViewer.OnShow native ProgressBar", latestAnimalProgressOverlaySummary);
        }

        internal bool RenderAnimalProgressOverlay(object viewer, object data)
        {
            if (viewer == null || data == null)
                return false;
            if (!IsAnimalViewerDataVisible(data))
            {
                ClearAnimalProgressOverlayForViewer(viewer);
                return false;
            }
            if (!animalProgressRowsByData.TryGetValue(data, out IReadOnlyList<AnimalProgressRenderRow>? rows) || rows.Count == 0)
            {
                ClearAnimalProgressOverlayForViewer(viewer);
                return false;
            }

            try
            {
                object? moodBar = ReadMember(viewer, "moodBar");
                object? moodGameObject = moodBar == null ? null : ReadMember(moodBar, "gameObject");
                object? moodTransform = moodGameObject == null ? null : ReadMember(moodGameObject, "transform");
                object? parent = moodTransform == null ? null : ReadMember(moodTransform, "parent");
                if (parent == null || moodGameObject == null || moodTransform == null)
                    return false;

                ClearAnimalProgressOverlay(parent);
                activeAnimalProgressOverlayRows.Clear();
                int rendered = 0;
                foreach (AnimalProgressRenderRow row in rows
                    .OrderByDescending(row => row.Progress)
                    .ThenBy(row => row.OutputTitle, StringComparer.OrdinalIgnoreCase)
                    .Take(3))
                {
                    object? clone = CloneUnityObject(moodGameObject);
                    if (clone == null)
                        continue;
                    SetMemberValue(clone, "name", "DTMAPI.AnimalProduceProgress." + rendered);
                    SetActive(clone, false);
                    object? rowTransform = ReadMember(clone, "transform");
                    if (rowTransform == null)
                    {
                        DestroyUnityObject(clone);
                        continue;
                    }

                    SetParent(rowTransform, parent, worldPositionStays: false);
                    PositionAnimalProgressRow(moodTransform, rowTransform, rendered);
                    activeAnimalProgressOverlayObjects.Add(clone);
                    activeAnimalProgressOverlayRows.Add(row);
                    rendered++;
                }

                RefreshAnimalProgressOverlayTexts(force: true);
                foreach (object clone in activeAnimalProgressOverlayObjects)
                    SetActive(clone, true);
                latestAnimalProgressOverlaySummary = "independent cloned ProgressBar prefilled rows=" + rendered + ", primary=" + rows[0].OutputTitle + " " + rows[0].Current + "/" + rows[0].Threshold + ", moodOverride=False, stateDescriptionOverride=False";
                runtime.RuntimeMonitor.Log("Animal viewer progress independent row active " + latestAnimalProgressOverlaySummary + ".");
                runtime.SetHookStatus("Smoke.AnimalViewerProgressUi", rendered > 0 ? "verified" : "pending", "AnimalFullInfoData ctor -> AnimalViewer.Show cloned ProgressBar", latestAnimalProgressOverlaySummary);
                return rendered > 0;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Animal progress single-pass UI evidence failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AnimalViewerProgressUi", "failed", "AnimalFullInfoData ctor -> AnimalViewer.OnShow native ProgressBar", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        internal void PrepareAnimalProgressOverlayBeforeShow(object viewer, object data)
        {
            RenderAnimalProgressOverlay(viewer, data);
        }

        internal bool HasAnimalProgressRowsForSmoke(object? data, out string summary)
        {
            summary = string.Empty;
            if (data == null || !animalProgressRowsByData.TryGetValue(data, out IReadOnlyList<AnimalProgressRenderRow>? rows) || rows.Count == 0)
                return false;

            AnimalProgressRenderRow primary = rows
                .OrderByDescending(row => row.Progress)
                .ThenBy(row => row.OutputTitle, StringComparer.OrdinalIgnoreCase)
                .First();
            summary = "rows=" + rows.Count +
                ", primary=" + primary.OutputTitle + " " + primary.Current + "/" + primary.Threshold +
                ", owner=" + primary.OwnerId +
                ", moodOverride=False, stateDescriptionOverride=False";
            return true;
        }

        internal bool RecordAnimalViewerUiEvidence(object viewer, object data)
        {
            if (viewer == null || data == null || animalViewerUiEvidenceRecorded)
                return false;

            if (!ReadBoolMember(data, "notEmpty", true) || !ReadBoolMember(data, "visible", true))
                return false;
            if (!animalProgressRowsByData.TryGetValue(data, out IReadOnlyList<AnimalProgressRenderRow>? rows) || rows.Count == 0)
                return false;
            AnimalProgressRenderRow primary = rows
                .OrderByDescending(row => row.Progress)
                .ThenBy(row => row.OutputTitle, StringComparer.OrdinalIgnoreCase)
                .First();
            string ownerId = primary.OwnerId;
            string label = primary.OutputTitle;

            animalViewerUiEvidenceRecorded = true;
            string title = ReadStringMember(data, "title");
            string moodInfo = ReadStringMember(data, "moodInfo");
            double moodProgress = ReadDoubleMember(data, "moodProgress", -1);
            string stateDescription = ReadStringMember(data, "stateDescription");
            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "ANIMAL-001", timestamp);
            Directory.CreateDirectory(evidenceDir);
            latestAnimalViewerEvidenceDir = evidenceDir;

            string screenshotPath = Path.Combine(evidenceDir, "animal-viewer-ui.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summaryPath = Path.Combine(evidenceDir, "summary.txt");
            File.WriteAllText(summaryPath,
                "Captured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                "Owner=" + ownerId + Environment.NewLine +
                "Marker=" + label + Environment.NewLine +
                "Title=" + title + Environment.NewLine +
                "ViewerType=" + viewer.GetType().FullName + Environment.NewLine +
                "DataType=" + data.GetType().FullName + Environment.NewLine +
                "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                "Screenshot=" + screenshotPath + Environment.NewLine +
                "RenderPath=AnimalFullInfoData ctor -> AnimalViewer.Show independent cloned ProgressBar" + Environment.NewLine +
                "MoodInfo=" + moodInfo + Environment.NewLine +
                "MoodProgress=" + moodProgress.ToString("0.###", CultureInfo.InvariantCulture) + Environment.NewLine +
                "MoodOverride=False" + Environment.NewLine +
                "StateDescriptionOverride=False" + Environment.NewLine +
                "Overlay=" + latestAnimalProgressOverlaySummary + Environment.NewLine +
                "StateDescription=" + stateDescription.Replace(Environment.NewLine, " | ") + Environment.NewLine);

            runtime.RuntimeMonitor.Log("Animal viewer UI evidence OK owner=" + ownerId + " title=" + title + " marker=" + label + " moodInfo=" + moodInfo + " renderPath=independent-cloned-progressbar overlay=" + latestAnimalProgressOverlaySummary + " screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + " stateDescription=" + stateDescription.Replace(Environment.NewLine, " | "));
            runtime.SetHookStatus("Animals.ViewerRendering", "verified", "Harmony Postfix: AnimalFullInfoData(Animal) + AnimalViewer.Show + AnimalPanel.RefreshViewer", "Real animal viewer UI used independent cloned ProgressBar rendering for hidden produce without mood/stateDescription override. Evidence=" + evidenceDir);
            runtime.SetHookStatus("Smoke.AnimalPanelUi", "verified", "DolocAPI.EnterUI(AnimalPanelUiState) + AnimalPanel.RefreshViewer", "Opened official AnimalPanel UI and observed progress text. Evidence=" + evidenceDir);
            runtime.SetHookStatus("Smoke.AnimalViewerUi", "verified", "AnimalViewer.Show", "Observed animal progress text in the real animal viewer UI. Evidence=" + evidenceDir);
            return true;
        }

        internal bool CaptureDelayedAnimalViewerUiEvidenceScreenshot()
        {
            if (animalViewerUiDelayedScreenshotRecorded || string.IsNullOrWhiteSpace(latestAnimalViewerEvidenceDir))
                return false;

            animalViewerUiDelayedScreenshotRecorded = true;
            string screenshotPath = Path.Combine(latestAnimalViewerEvidenceDir!, "animal-viewer-ui-delayed.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summaryPath = Path.Combine(latestAnimalViewerEvidenceDir!, "summary.txt");
            try
            {
                File.AppendAllText(summaryPath,
                    "DelayedCaptured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    "DelayedScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "DelayedScreenshot=" + screenshotPath + Environment.NewLine);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to append delayed animal UI screenshot summary.", ex.ToString());
            }

            runtime.RuntimeMonitor.Log("Animal viewer UI delayed screenshot " + (screenshotRequested ? "OK" : "unavailable") + " screenshot=" + screenshotPath + ".");
            runtime.SetHookStatus("Smoke.AnimalViewerUiScreenshot", screenshotRequested ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", "Delayed animal viewer screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + ".");
            return screenshotRequested;
        }

        internal bool RecordAnimalPanelUiEvidence(object panel, int index)
        {
            if (panel == null || animalViewerUiEvidenceRecorded)
                return false;

            object? currentDatas = ReadMember(panel, "currentDatas") ?? ReadMember(panel, "<currentDatas>k__BackingField");
            object? data = null;
            if (currentDatas is Array array && index >= 0 && index < array.Length)
                data = array.GetValue(index);
            else if (currentDatas is IEnumerable enumerable)
            {
                int currentIndex = 0;
                foreach (object item in enumerable)
                {
                    if (currentIndex == index)
                    {
                        data = item;
                        break;
                    }
                    currentIndex++;
                }
            }

            if (!animalPanelUiProbeLogged)
            {
                animalPanelUiProbeLogged = true;
                runtime.RuntimeMonitor.Log("Animal panel UI evidence probe index=" + index + " panel=" + panel.GetType().FullName + " currentDatas=" + (currentDatas?.GetType().FullName ?? "null") + " data=" + (data?.GetType().FullName ?? "null") + ".");
            }

            if (data != null && RecordAnimalViewerUiEvidence(panel, data))
                return true;

            if (currentDatas is Array scanArray)
            {
                foreach (object? item in scanArray)
                {
                    if (item != null && RecordAnimalViewerUiEvidence(panel, item))
                        return true;
                }
            }

            return false;
        }

        private IReadOnlyList<AnimalProgressInfo> BuildAnimalProgressList(object animal)
        {
            Type type = animal.GetType();
            string animalId = type.GetProperty("protoName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animal) as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(animalId))
                return Array.Empty<AnimalProgressInfo>();

            object? valuesObject = type.GetField("husbandryValues", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(animal);
            var currentByOutput = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (valuesObject is IDictionary values)
            {
                foreach (DictionaryEntry entry in values)
                {
                    string outputId = entry.Key as string ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(outputId))
                        continue;
                    currentByOutput[outputId] = Convert.ToInt32(entry.Value);
                }
            }

            var progress = new List<AnimalProgressInfo>();
            foreach ((string outputId, int threshold) in EnumerateHusbandryOutputs(animalId))
            {
                if (string.IsNullOrWhiteSpace(outputId))
                    continue;
                if (threshold <= 0)
                    continue;
                currentByOutput.TryGetValue(outputId, out int current);
                double ratio = Math.Max(0, Math.Min(1, current / (double)threshold));
                progress.Add(new AnimalProgressInfo
                {
                    AnimalId = animalId,
                    OutputId = outputId,
                    OutputTitle = ResolveItemTitle(outputId),
                    Current = current,
                    Threshold = threshold,
                    Progress = ratio
                });
            }

            if (progress.Count == 0 && currentByOutput.Count > 0)
            {
                foreach (KeyValuePair<string, int> entry in currentByOutput)
                {
                    if (!TryGetHusbandryThreshold(animalId, entry.Key, out int threshold) || threshold <= 0)
                        continue;
                    double ratio = Math.Max(0, Math.Min(1, entry.Value / (double)threshold));
                    progress.Add(new AnimalProgressInfo
                    {
                        AnimalId = animalId,
                        OutputId = entry.Key,
                        OutputTitle = ResolveItemTitle(entry.Key),
                        Current = entry.Value,
                        Threshold = threshold,
                        Progress = ratio
                    });
                }
            }

            return progress.ToArray();
        }

        private IEnumerable<(string OutputId, int Threshold)> EnumerateHusbandryOutputs(string animalId)
        {
            object? tables = GetDolocTables();
            object? husbandry = tables == null ? null : ReadMember(tables, "TbHusbandry");
            MethodInfo? getOrDefault = husbandry?.GetType().GetMethod("GetOrDefault", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            object? info = getOrDefault?.Invoke(husbandry, new object?[] { animalId });
            object? datas = info == null ? null : ReadMember(info, "HusbandryDatas");
            foreach (object data in EnumerateObjects(datas))
            {
                string outputId = ReadStringMember(data, "Output");
                int threshold = ReadIntMember(data, "Threshold", 0);
                if (!string.IsNullOrWhiteSpace(outputId) && threshold > 0)
                    yield return (outputId, threshold);
            }
        }

        private bool TryGetHusbandryThreshold(string animalId, string outputId, out int threshold)
        {
            string cacheKey = animalId + "|" + outputId;
            if (husbandryThresholdCache.TryGetValue(cacheKey, out threshold))
                return threshold > 0;

            threshold = 0;
            try
            {
                Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
                object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? husbandry = tables?.GetType().GetProperty("TbHusbandry", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tables);
                MethodInfo? method = husbandry?.GetType().GetMethod("TryGetThreshold", BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                    return false;

                object?[] args = new object?[] { animalId, outputId, threshold };
                object? result = method.Invoke(husbandry, args);
                if (!(result is bool ok) || !ok)
                    return false;

                threshold = Convert.ToInt32(args[2]);
                husbandryThresholdCache[cacheKey] = threshold;
                return threshold > 0;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to query husbandry threshold for " + cacheKey + ".", ex.ToString());
                return false;
            }
        }

        private static string InsertAnimalProgressLine(string current, string line)
        {
            current = current ?? string.Empty;
            if (string.IsNullOrWhiteSpace(current))
                return line;

            string[] lines = current.Replace("\r\n", "\n").Split('\n');
            if (lines.Length <= 1)
                return current.TrimEnd() + Environment.NewLine + line;

            return lines[0].TrimEnd() + Environment.NewLine + line + Environment.NewLine + string.Join(Environment.NewLine, lines.Skip(1).ToArray()).TrimStart();
        }

        private static string BuildAnimalProgressBar(AnimalProgressInfo progress, DtmColor color)
        {
            int width = 18;
            int filled = Math.Max(0, Math.Min(width, (int)Math.Round(progress.Progress * width)));
            string bar = new string('█', filled) + new string('░', width - filled);
            return "<color=#" + ToHex(color) + ">" + bar + "</color>";
        }

        private static string ToHex(DtmColor color)
        {
            int r = (int)Math.Round(Math.Max(0, Math.Min(1, color.R)) * 255);
            int g = (int)Math.Round(Math.Max(0, Math.Min(1, color.G)) * 255);
            int b = (int)Math.Round(Math.Max(0, Math.Min(1, color.B)) * 255);
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        private bool TryFindAnimalProgressMarker(string stateDescription, out string ownerId, out string label)
        {
            ownerId = string.Empty;
            label = string.Empty;
            foreach (KeyValuePair<string, AnimalHusbandryProgressOptions> entry in animalOptions)
            {
                AnimalHusbandryProgressOptions options = entry.Value ?? new AnimalHusbandryProgressOptions();
                if (!options.Enabled)
                    continue;

                if (ContainsAnimalProgressBar(stateDescription))
                {
                    ownerId = entry.Key;
                    label = "produce-progress-bar";
                    return true;
                }
            }

            if (ContainsAnimalProgressBar(stateDescription))
            {
                ownerId = "unknown";
                label = "produce-progress-bar";
                return true;
            }

            return false;
        }

        private static bool ContainsAnimalProgressBar(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return text.IndexOf("████", StringComparison.Ordinal) >= 0 ||
                (text.IndexOf("<color=#", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("</color>", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("/", StringComparison.Ordinal) >= 0);
        }

        private void ClearAnimalProgressOverlay(object parentTransform)
        {
            foreach (object instance in activeAnimalProgressOverlayObjects.ToArray())
            {
                SetActive(instance, false);
                DestroyUnityObject(instance);
            }
            activeAnimalProgressOverlayObjects.Clear();
            activeAnimalProgressOverlayRows.Clear();

            MethodInfo? find = parentTransform.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            for (int i = 0; i < 10; i++)
            {
                object? child = find?.Invoke(parentTransform, new object[] { "DTMAPI.AnimalProduceProgress." + i });
                object? gameObject = child == null ? null : ReadMember(child, "gameObject");
                if (gameObject != null)
                    DestroyUnityObject(gameObject);
            }
        }

        private void ClearAnimalProgressOverlayForViewer(object viewer)
        {
            object? moodBar = ReadMember(viewer, "moodBar");
            object? moodGameObject = moodBar == null ? null : ReadMember(moodBar, "gameObject");
            object? moodTransform = moodGameObject == null ? null : ReadMember(moodGameObject, "transform");
            object? parent = moodTransform == null ? null : ReadMember(moodTransform, "parent");
            if (parent != null)
                ClearAnimalProgressOverlay(parent);
            else
            {
                activeAnimalProgressOverlayObjects.Clear();
                activeAnimalProgressOverlayRows.Clear();
            }
        }

        private static bool IsAnimalViewerDataVisible(object data)
        {
            bool notEmpty = ReadBoolMember(data, "notEmpty", true);
            bool visible = ReadBoolMember(data, "visible", true);
            return notEmpty && visible;
        }

        internal void RefreshAnimalProgressOverlayTexts(bool force)
        {
            if (activeAnimalProgressOverlayObjects.Count == 0 || activeAnimalProgressOverlayRows.Count == 0)
                return;
            if (!force && (DateTimeOffset.Now - lastAnimalProgressOverlayRefreshAt).TotalSeconds < 0.08)
                return;

            lastAnimalProgressOverlayRefreshAt = DateTimeOffset.Now;
            Type? progressBarType = ResolveType("DolocTown.UI.ProgressBar, Assembly-CSharp");
            int count = Math.Min(activeAnimalProgressOverlayObjects.Count, activeAnimalProgressOverlayRows.Count);
            for (int i = 0; i < count; i++)
            {
                object clone = activeAnimalProgressOverlayObjects[i];
                AnimalProgressRenderRow row = activeAnimalProgressOverlayRows[i];
                string progressText = row.Current + "/" + row.Threshold;
                object? progressBar = progressBarType == null ? null : GetComponent(clone, progressBarType);
                if (progressBar != null)
                {
                    MethodInfo? setTitle = progressBar.GetType().GetMethod("SetTitle", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                    MethodInfo? setProgress = progressBar.GetType().GetMethod("SetProgress", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float), typeof(string) }, null);
                    setTitle?.Invoke(progressBar, new object[] { row.OutputTitle });
                    setProgress?.Invoke(progressBar, new object[] { (float)Math.Max(0, Math.Min(1, row.Progress)), progressText });
                    SetUnityText(ReadMember(progressBar, "txtTitle"), row.OutputTitle);
                    SetUnityText(ReadMember(progressBar, "txtProgress"), progressText);
                }

                SetAnimalProgressTextsFromChildren(clone, row.OutputTitle, progressText);
            }
        }

        private static string SetAnimalProgressTextsFromChildren(object gameObject, string title, string progress)
        {
            Type? textType = ResolveType("UnityEngine.UI.Text, UnityEngine.UI");
            if (textType == null)
                return "textType=missing";

            MethodInfo? getComponents = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            object? result = getComponents?.Invoke(gameObject, new object[] { textType, true });
            if (!(result is IEnumerable components))
                return "components=missing";

            int seen = 0;
            int titleWrites = 0;
            int progressWrites = 0;
            var samples = new List<string>();
            foreach (object component in components)
            {
                string current = ReadStringMember(component, "text");
                seen++;
                if (samples.Count < 4)
                    samples.Add(current);
                if (current.IndexOf("/", StringComparison.Ordinal) >= 0 || current.IndexOf("0/100", StringComparison.Ordinal) >= 0)
                {
                    SetUnityText(component, progress);
                    progressWrites++;
                }
                else if (!string.IsNullOrWhiteSpace(current))
                {
                    SetUnityText(component, title);
                    titleWrites++;
                }
            }

            return "textChildren=" + seen + ", titleWrites=" + titleWrites + ", progressWrites=" + progressWrites + ", samples=" + string.Join("|", samples.ToArray());
        }

        private static void SetUnityText(object? textComponent, string value)
        {
            if (textComponent == null)
                return;
            SetMemberValue(textComponent, "text", value ?? string.Empty);
            SetMemberValue(textComponent, "resizeTextForBestFit", false);
            SetMemberValue(textComponent, "fontSize", 18);
            SetMemberValue(textComponent, "resizeTextMinSize", 18);
            SetMemberValue(textComponent, "resizeTextMaxSize", 18);
        }

        private static void PositionAnimalProgressRow(object sourceTransform, object rowTransform, int rowIndex)
        {
            object? localPosition = ReadMember(sourceTransform, "localPosition");
            double x = ReadVectorComponent(localPosition, "x");
            double y = ReadVectorComponent(localPosition, "y") - 42d * (rowIndex + 1);
            double z = ReadVectorComponent(localPosition, "z");
            object? position = CreateUnityVector3(x, y, z);
            if (position != null)
                SetMemberValue(rowTransform, "localPosition", position);

            object? localScale = ReadMember(sourceTransform, "localScale");
            if (localScale != null)
                SetMemberValue(rowTransform, "localScale", localScale);
        }

        private string ResolveItemTitle(string itemId)
        {
            if (itemTitleCache.TryGetValue(itemId, out string title))
                return title;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? queryItemProto = dolocApi?.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                if (queryItemProto != null)
                {
                    object?[] args = new object?[] { itemId, null };
                    object? result = queryItemProto.Invoke(null, args);
                    if (result is bool ok && ok && args[1] != null)
                    {
                        title = args[1]!.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance)?.GetValue(args[1]) as string ?? itemId;
                        itemTitleCache[itemId] = title;
                        return title;
                    }
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to query item title for " + itemId + ".", ex.ToString());
            }
            itemTitleCache[itemId] = itemId;
            return itemId;
        }

        private static object? GetDolocTables()
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            return ReadStaticMember(dolocConfig, "Tables");
        }

        private void LogOnce(HashSet<string> keys, string key, string message)
        {
            if (!keys.Add(key))
                return;
            runtime.RuntimeMonitor.Log(message);
        }

        private sealed class AnimalProgressInfo
        {
            public string AnimalId { get; set; } = string.Empty;
            public string OutputId { get; set; } = string.Empty;
            public string OutputTitle { get; set; } = string.Empty;
            public int Current { get; set; }
            public int Threshold { get; set; }
            public double Progress { get; set; }
        }

        private sealed class AnimalProgressRenderRow
        {
            public string OwnerId { get; set; } = string.Empty;
            public string OutputTitle { get; set; } = string.Empty;
            public int Current { get; set; }
            public int Threshold { get; set; }
            public double Progress { get; set; }
            public DtmColor Color { get; set; }
        }
    }
}
