#pragma warning disable CS0618 // This file implements the frozen IAnimalViewerApi compatibility island.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen IAnimalViewerApi compatibility executor for unknown old ABI consumers.</summary>
    internal sealed class AnimalViewerService : IAnimalViewerApi
    {
        private const string ManagedProductUniqueId = "Yuuka.DTMAPI.AnimalHusbandryProgress";
        private const string ManagedProductHarmonyOwner = "dtmapi.mod.yuuka.dtmapi.animalhusbandryprogress";
        private readonly DtmApiRuntime runtime;
        private readonly Func<bool> managedProductOwnerPresent;
        private readonly Dictionary<string, AnimalHusbandryProgressOptions> animalOptions = new Dictionary<string, AnimalHusbandryProgressOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, IReadOnlyList<AnimalProgressRenderRow>> animalProgressRowsByData = new Dictionary<object, IReadOnlyList<AnimalProgressRenderRow>>();
        private readonly List<object> activeAnimalProgressOverlayObjects = new List<object>();
        private readonly List<AnimalProgressRenderRow> activeAnimalProgressOverlayRows = new List<AnimalProgressRenderRow>();
        private readonly Dictionary<string, int> husbandryThresholdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> itemTitleCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedAnimalApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool animalViewerHookInstalled;
        private string latestAnimalProgressOverlaySummary = string.Empty;
        private DateTimeOffset lastAnimalProgressOverlayRefreshAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastAnimalProgressOverlayLifecycleLogAt = DateTimeOffset.MinValue;
        private string lastAnimalProgressOverlayLifecycleLogKey = string.Empty;
        private object? activeAnimalProgressOverlayParent;
        private object? lastAnimalProgressVisibleReceiptData;
        private int animalProgressOverlayGeneration;
        private int animalProgressVisibleReceiptSequence;

        public AnimalViewerService(DtmApiRuntime runtime)
            : this(runtime, IsManagedProductOwnerPresent)
        {
        }

        internal AnimalViewerService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.managedProductOwnerPresent = managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent));
        }

        internal void SetAnimalViewerHookInstalled(bool installed)
        {
            animalViewerHookInstalled = installed;
        }

        internal string GetAnimalViewerLifecycleSummary()
        {
            int nativeRowCount = animalProgressRowsByData.Values.Sum(rows => rows?.Count ?? 0);
            return "options=" + animalOptions.Count.ToString(CultureInfo.InvariantCulture) +
                ", nativeData=" + animalProgressRowsByData.Count.ToString(CultureInfo.InvariantCulture) +
                ", nativeRows=" + nativeRowCount.ToString(CultureInfo.InvariantCulture) +
                ", overlayObjects=" + activeAnimalProgressOverlayObjects.Count.ToString(CultureInfo.InvariantCulture) +
                ", overlayRows=" + activeAnimalProgressOverlayRows.Count.ToString(CultureInfo.InvariantCulture) +
                ", overlayParent=" + (activeAnimalProgressOverlayParent == null ? "none" : IsUnityObjectAlive(activeAnimalProgressOverlayParent).ToString(CultureInfo.InvariantCulture)) +
                ", generation=" + animalProgressOverlayGeneration.ToString(CultureInfo.InvariantCulture) +
                ", hookInstalled=" + animalViewerHookInstalled.ToString(CultureInfo.InvariantCulture);
        }

        public void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            AnimalHusbandryProgressOptions normalized = options ?? new AnimalHusbandryProgressOptions();
            if (normalized.Enabled && managedProductOwnerPresent())
                throw new InvalidOperationException("Frozen IAnimalViewerApi compatibility refused native viewer ownership because managed product " + ManagedProductUniqueId + " is active.");
            animalOptions[owner.UniqueID] = normalized;
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AnimalViewer, owner.UniqueID, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "progress-policy", normalized.Enabled, "animal viewer progress policy configured");
            runtime.RuntimeMonitor.Log("Animal viewer bridge configured by " + owner.UniqueID + ".");
        }

        internal int ReconcileManagedProductOwnerBeforeHookInstall()
        {
            if (animalOptions.Count == 0 || !managedProductOwnerPresent())
                return 0;
            var owners = new List<string>(animalOptions.Keys);
            int removed = 0;
            foreach (string ownerId in owners)
                removed += RemoveOwner(ownerId, "managed AnimalHusbandryProgress product acquired native ownership before compatibility Hook installation");
            animalViewerHookInstalled = false;
            runtime.RuntimeMonitor.Log("Frozen IAnimalViewerApi compatibility removed " + removed + " pending resource(s) because the managed AnimalHusbandryProgress product owns the reviewed animal-viewer targets.");
            return removed;
        }

        private static bool IsManagedProductOwnerPresent()
        {
            Type? dataType = ResolveType("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp");
            Type? viewerType = ResolveType("DolocTown.UI.AnimalViewer, Assembly-CSharp");
            Type? panelType = ResolveType("DolocTown.AnimalPanelUiState, Assembly-CSharp");
            MethodBase? ctor = dataType?.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(value => value.GetParameters().Length == 1);
            MethodBase? show = viewerType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(value => value.Name == "Show" && value.GetParameters().Length == 1);
            MethodBase? unregister = panelType?.GetMethod("Unregister", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            return HasManagedProductOwner(ctor) || HasManagedProductOwner(show) || HasManagedProductOwner(unregister);
        }

        private static bool HasManagedProductOwner(MethodBase? target)
        {
            Type? harmonyType = ResolveType("HarmonyLib.Harmony, 0Harmony");
            MethodInfo? getPatchInfo = harmonyType?.GetMethod("GetPatchInfo", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(MethodBase) }, null);
            object? info = target == null || getPatchInfo == null ? null : getPatchInfo.Invoke(null, new object[] { target });
            object? ownerList = info?.GetType().GetProperty("Owners", BindingFlags.Public | BindingFlags.Instance)?.GetValue(info);
            if (!(ownerList is IEnumerable owners))
                return false;
            foreach (object? ownerValue in owners)
                if ((ownerValue as string ?? string.Empty).Equals(ManagedProductHarmonyOwner, StringComparison.Ordinal))
                    return true;
            return false;
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AnimalViewer, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "progress-policy", false, "animal viewer owner cleanup " + (reason ?? string.Empty));
            if (!animalOptions.Remove(ownerId))
                return 0;
            loggedAnimalApplications.RemoveWhere(key => key.IndexOf(ownerId, StringComparison.OrdinalIgnoreCase) >= 0);
            // Rows and Unity overlays can mix multiple owners. Clear the derived view
            // on every owner removal; remaining policies will rebuild it on demand.
            ResetAnimalViewerRuntimeState("owner cleanup " + (reason ?? string.Empty));
            return 1;
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            int ownedDataRows = animalProgressRowsByData.Values.Sum(rows => rows?.Count(row => row.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) ?? 0);
            int ownedOverlayRows = activeAnimalProgressOverlayRows.Count(row => row.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            int derivedOverlayRoots = ownedDataRows + ownedOverlayRows > 0
                ? activeAnimalProgressOverlayObjects.Count + (activeAnimalProgressOverlayParent == null ? 0 : 1)
                : 0;
            return (animalOptions.ContainsKey(ownerId) ? 1 : 0) + ownedDataRows + ownedOverlayRows + derivedOverlayRoots;
        }

        BridgeFeatureStatus IAnimalViewerApi.GetStatus(string uniqueId)
        {
            return animalOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(animalViewerHookInstalled ? "configured-animal-viewer-hook" : "configured-pending-hook", animalViewerHookInstalled ? "Display options accepted and animal viewer data hooks are installed; the API remains experimental." : "Display options accepted; animal viewer hooks are not yet installed.")
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
                runtime.SetHookStatus("Animals.ViewerProgressUi", "pending", "AnimalFullInfoData ctor -> AnimalViewer.Show independent cloned ProgressBar", latestAnimalProgressOverlaySummary);
            }
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
                activeAnimalProgressOverlayParent = parent;
                animalProgressOverlayGeneration++;
                activeAnimalProgressOverlayRows.Clear();
                int rendered = 0;
                int localizationComponentsDisabled = 0;
                foreach (AnimalProgressRenderRow row in rows
                    .OrderByDescending(row => row.Progress)
                    .ThenBy(row => row.OutputTitle, StringComparer.OrdinalIgnoreCase)
                    .Take(3))
                {
                    object? clone = CloneUnityObject(moodGameObject);
                    if (clone == null)
                        continue;
                    SetMemberValue(clone, "name", "DTMAPI.AnimalProduceProgress." + rendered);
                    SafeSetActive(clone, false);
                    object? rowTransform = ReadMember(clone, "transform");
                    if (rowTransform == null)
                    {
                        SafeDestroyUnityObject(clone);
                        continue;
                    }

                    SetParent(rowTransform, parent, worldPositionStays: false);
                    PositionAnimalProgressRow(moodTransform, rowTransform, rendered);
                    localizationComponentsDisabled += DisableAnimalProgressCloneLocalization(clone);
                    activeAnimalProgressOverlayObjects.Add(clone);
                    activeAnimalProgressOverlayRows.Add(row);
                    rendered++;
                }

                RefreshAnimalProgressOverlayTexts(force: true);
                foreach (object clone in activeAnimalProgressOverlayObjects.ToArray())
                    SafeSetActive(clone, true);
                RefreshAnimalProgressOverlayTexts(force: true);
                string firstFrameGuardSummary = ValidateAnimalProgressOverlayTextsForFirstFrameGuard();
                AnimalProgressRenderRow primary = rows
                    .OrderByDescending(row => row.Progress)
                    .ThenBy(row => row.OutputTitle, StringComparer.OrdinalIgnoreCase)
                    .First();
                bool publishVisibleReceipt = rendered > 0 && !ReferenceEquals(lastAnimalProgressVisibleReceiptData, data);
                if (publishVisibleReceipt)
                {
                    lastAnimalProgressVisibleReceiptData = data;
                    animalProgressVisibleReceiptSequence++;
                }
                latestAnimalProgressOverlaySummary = "independent cloned ProgressBar prefilled rows=" + rendered +
                    ", primary=" + primary.OutputTitle + " " + primary.Current + "/" + primary.Threshold +
                    ", localizationDisabled=" + localizationComponentsDisabled +
                    ", firstFrameGuard=" + firstFrameGuardSummary +
                    ", receiptSequence=" + animalProgressVisibleReceiptSequence.ToString(CultureInfo.InvariantCulture) +
                    ", receipt=" + (publishVisibleReceipt ? "new-data" : "same-data-coalesced") +
                    ", moodOverride=False, stateDescriptionOverride=False";
                runtime.RuntimeMonitor.Log("Animal viewer progress independent row active " + latestAnimalProgressOverlaySummary + ".");
                if (publishVisibleReceipt)
                    runtime.SetHookStatus("Animals.ViewerProgressUi", "visible", "AnimalFullInfoData ctor -> AnimalViewer.Show cloned ProgressBar", latestAnimalProgressOverlaySummary);
                else if (rendered == 0)
                    runtime.SetHookStatus("Animals.ViewerProgressUi", "pending", "AnimalFullInfoData ctor -> AnimalViewer.Show cloned ProgressBar", latestAnimalProgressOverlaySummary);
                runtime.SetHookStatus(
                    "Animals.ViewerFirstFrameGuard",
                    rendered > 0 && firstFrameGuardSummary.IndexOf("moodTitleHits=0", StringComparison.Ordinal) >= 0 ? "active" : "pending",
                    "AnimalViewer.Show inactive-prefill-activate",
                    firstFrameGuardSummary + "; manual repeated animal-switch confirmation is still required for visual first-frame QA.");
                SetAnimalViewerSessionDemand(rendered > 0, "overlay rendered");
                return rendered > 0;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Animal progress UI rendering failed.", ex.ToString());
                runtime.SetHookStatus("Animals.ViewerProgressUi", "failed", "AnimalFullInfoData ctor -> AnimalViewer.OnShow native ProgressBar", ex.GetType().Name + ": " + ex.Message);
                ClearAnimalProgressOverlaySession("render-failed");
                return false;
            }
        }

        internal void ResetAnimalViewerRuntimeState(string reason)
        {
            ClearAnimalProgressOverlaySession(reason);
            animalProgressRowsByData.Clear();
            lastAnimalProgressOverlayRefreshAt = DateTimeOffset.MinValue;
            latestAnimalProgressOverlaySummary = "animal viewer runtime state reset for " + (reason ?? string.Empty) + ".";
            runtime.SetHookStatus("Animals.ViewerRenderingLifecycle", "reset", "AnimalViewer clone session boundary", latestAnimalProgressOverlaySummary);
        }

        internal void NotifyAnimalPanelUnregistered()
        {
            const string reason = "native AnimalPanelUiState.Unregister";
            runtime.RuntimeMonitor.Log("Animal viewer native lifecycle closed reason=" + reason + ".");
            runtime.SetHookStatus(
                "Animals.ViewerNativeLifecycle",
                "closed",
                "AnimalPanelUiState.Unregister Postfix",
                "The native animal panel completed Unregister; clearing only DTMAPI-owned progress rows.");
            ClearAnimalProgressOverlaySession(reason);
            animalProgressRowsByData.Clear();
            lastAnimalProgressOverlayRefreshAt = DateTimeOffset.MinValue;
            runtime.SetHookStatus(
                "Animals.ViewerProgressUi",
                "closed",
                "AnimalPanelUiState.Unregister Postfix",
                "The native panel closed and all derived AnimalFullInfoData rows and cloned progress rows were released.");
        }

        internal void NotifyAnimalViewerEnvironmentReset(string reason)
        {
            string actualReason = "EnvironmentReset " + (reason ?? string.Empty);
            bool valid = ValidateAnimalProgressOverlaySession(actualReason);
            latestAnimalProgressOverlaySummary = "animal viewer environment reset validation reason=" + actualReason +
                ", valid=" + valid.ToString(CultureInfo.InvariantCulture) +
                ", objects=" + activeAnimalProgressOverlayObjects.Count.ToString(CultureInfo.InvariantCulture) +
                ", rows=" + activeAnimalProgressOverlayRows.Count.ToString(CultureInfo.InvariantCulture) +
                ", generation=" + animalProgressOverlayGeneration.ToString(CultureInfo.InvariantCulture) + ".";
            runtime.SetHookStatus(
                "Animals.ViewerRenderingLifecycle",
                valid ? "observed" : "reset",
                "AnimalViewer non-destructive EnvironmentReset validation",
                latestAnimalProgressOverlaySummary);
        }

        internal void PrepareAnimalProgressOverlayBeforeShow(object viewer, object data)
        {
            if (viewer != null)
                ClearAnimalProgressOverlayForViewer(viewer);
            latestAnimalProgressOverlaySummary = "prefix cleared stale cloned ProgressBar rows before native AnimalViewer.Show.";
            runtime.SetHookStatus("Animals.ViewerFirstFrameGuard", "prepared", "AnimalViewer.Show prefix clear -> postfix inactive-prefill-activate", latestAnimalProgressOverlaySummary);
        }

        internal bool HasAnimalProgressRowsForObservation(object? data, out string summary)
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
                SafeSetActive(instance, false);
                SafeDestroyUnityObject(instance);
            }
            activeAnimalProgressOverlayObjects.Clear();
            activeAnimalProgressOverlayRows.Clear();
            activeAnimalProgressOverlayParent = null;
            SetAnimalViewerSessionDemand(false, "overlay cleared");

            DestroyAnimalProgressOverlayChildren(parentTransform);
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
                ClearAnimalProgressOverlaySession("viewer-parent-missing");
        }

        private void ClearAnimalProgressOverlaySession(string reason)
        {
            int objects = activeAnimalProgressOverlayObjects.Count;
            object? parent = activeAnimalProgressOverlayParent;
            foreach (object instance in activeAnimalProgressOverlayObjects.ToArray())
            {
                SafeSetActive(instance, false);
                SafeDestroyUnityObject(instance);
            }

            if (parent != null)
                DestroyAnimalProgressOverlayChildren(parent);

            activeAnimalProgressOverlayObjects.Clear();
            activeAnimalProgressOverlayRows.Clear();
            activeAnimalProgressOverlayParent = null;
            lastAnimalProgressVisibleReceiptData = null;
            lastAnimalProgressOverlayRefreshAt = DateTimeOffset.MinValue;
            animalProgressOverlayGeneration++;
            SetAnimalViewerSessionDemand(false, "overlay session cleared " + (reason ?? string.Empty));
            if (objects > 0)
                RecordAnimalProgressOverlayLifecycle("session-cleared", "reason=" + (reason ?? string.Empty) + ", objects=" + objects.ToString(CultureInfo.InvariantCulture) + ", generation=" + animalProgressOverlayGeneration.ToString(CultureInfo.InvariantCulture), failed: false);
        }

        private void SetAnimalViewerSessionDemand(bool active, string reason)
        {
            GameBridgeDemandRoutes.SetOwnerDemand(
                runtime,
                GameBridgeDemandRoutes.AnimalViewerSession,
                GameBridgeDemandRoutes.OperationOwner,
                RuntimeDemandSourceType.CapabilitySession,
                RuntimeDemandLifetime.Session,
                "overlay",
                active,
                reason);
        }

        private void DestroyAnimalProgressOverlayChildren(object parentTransform)
        {
            try
            {
                MethodInfo? find = parentTransform.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                for (int i = 0; i < 10; i++)
                {
                    object? child = find?.Invoke(parentTransform, new object[] { "DTMAPI.AnimalProduceProgress." + i });
                    object? gameObject = child == null ? null : ReadMember(child, "gameObject");
                    if (gameObject != null)
                        SafeDestroyUnityObject(gameObject);
                }
            }
            catch (Exception ex)
            {
                RecordAnimalProgressOverlayLifecycle("clear-parent-failed", ex.GetType().Name + ": " + ex.Message, failed: true);
            }
        }

        private bool ValidateAnimalProgressOverlaySession(string reason)
        {
            if (activeAnimalProgressOverlayObjects.Count != activeAnimalProgressOverlayRows.Count)
            {
                string summary = "reason=" + (reason ?? string.Empty) +
                    ", objects=" + activeAnimalProgressOverlayObjects.Count.ToString(CultureInfo.InvariantCulture) +
                    ", rows=" + activeAnimalProgressOverlayRows.Count.ToString(CultureInfo.InvariantCulture) +
                    ", generation=" + animalProgressOverlayGeneration.ToString(CultureInfo.InvariantCulture);
                ClearAnimalProgressOverlaySession(reason + ":row-count-mismatch");
                RecordAnimalProgressOverlayLifecycle("session-row-count-mismatch", summary, failed: false);
                return false;
            }

            if (activeAnimalProgressOverlayParent != null && !IsUnityObjectAlive(activeAnimalProgressOverlayParent))
            {
                ClearAnimalProgressOverlaySession(reason + ":parent-destroyed");
                RecordAnimalProgressOverlayLifecycle("session-parent-destroyed", "reason=" + (reason ?? string.Empty), failed: false);
                return false;
            }

            foreach (object instance in activeAnimalProgressOverlayObjects.ToArray())
            {
                if (IsUnityObjectAlive(instance))
                    continue;

                ClearAnimalProgressOverlaySession(reason + ":clone-destroyed");
                RecordAnimalProgressOverlayLifecycle("session-clone-destroyed", "reason=" + (reason ?? string.Empty), failed: false);
                return false;
            }

            return true;
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
            if (!ValidateAnimalProgressOverlaySession("refresh"))
                return;

            lastAnimalProgressOverlayRefreshAt = DateTimeOffset.Now;
            Type? progressBarType = ResolveType("DolocTown.UI.ProgressBar, Assembly-CSharp");
            int count = Math.Min(activeAnimalProgressOverlayObjects.Count, activeAnimalProgressOverlayRows.Count);
            try
            {
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
            catch (Exception ex)
            {
                ClearAnimalProgressOverlaySession("refresh-failed");
                RecordAnimalProgressOverlayLifecycle("refresh-failed", ex.GetType().Name + ": " + ex.Message, failed: true);
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

        private static int DisableAnimalProgressCloneLocalization(object gameObject)
        {
            Type? componentType = ResolveType("UnityEngine.Component, UnityEngine.CoreModule") ??
                ResolveType("UnityEngine.Component, UnityEngine");
            if (componentType == null)
                return 0;

            MethodInfo? getComponents = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            object? result = getComponents?.Invoke(gameObject, new object[] { componentType, true });
            if (!(result is IEnumerable components))
                return 0;

            int disabled = 0;
            foreach (object component in components)
            {
                string typeName = component.GetType().FullName ?? component.GetType().Name;
                if (!LooksLikeLocalizationComponent(typeName))
                    continue;

                if (TrySetComponentEnabled(component, false))
                    disabled++;
            }

            return disabled;
        }

        private static bool LooksLikeLocalizationComponent(string typeName)
        {
            return typeName.IndexOf("Localization", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Localisation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Localize", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TrySetComponentEnabled(object component, bool enabled)
        {
            try
            {
                for (Type? type = component.GetType(); type != null; type = type.BaseType)
                {
                    PropertyInfo? property = type.GetProperty("enabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                    {
                        property.SetValue(component, enabled);
                        return true;
                    }

                    FieldInfo? field = type.GetField("enabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && field.FieldType == typeof(bool))
                    {
                        field.SetValue(component, enabled);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private string ValidateAnimalProgressOverlayTextsForFirstFrameGuard()
        {
            Type? textType = ResolveType("UnityEngine.UI.Text, UnityEngine.UI");
            if (textType == null)
                return "textType=missing";

            int inspectedTexts = 0;
            int moodTitleHits = 0;
            int expectedTitleHits = 0;
            int expectedProgressHits = 0;
            int count = Math.Min(activeAnimalProgressOverlayObjects.Count, activeAnimalProgressOverlayRows.Count);
            for (int i = 0; i < count; i++)
            {
                object clone = activeAnimalProgressOverlayObjects[i];
                AnimalProgressRenderRow row = activeAnimalProgressOverlayRows[i];
                string progressText = row.Current + "/" + row.Threshold;
                MethodInfo? getComponents = clone.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
                object? result = getComponents?.Invoke(clone, new object[] { textType, true });
                if (!(result is IEnumerable components))
                    continue;

                foreach (object component in components)
                {
                    string text = ReadStringMember(component, "text");
                    if (string.IsNullOrWhiteSpace(text))
                        continue;
                    inspectedTexts++;
                    if (IsNativeMoodTitleText(text))
                        moodTitleHits++;
                    if (text.IndexOf(row.OutputTitle, StringComparison.Ordinal) >= 0)
                        expectedTitleHits++;
                    if (text.IndexOf(progressText, StringComparison.Ordinal) >= 0)
                        expectedProgressHits++;
                }
            }

            return "sameCallbackTextCheck texts=" + inspectedTexts.ToString(CultureInfo.InvariantCulture) +
                ", expectedTitleHits=" + expectedTitleHits.ToString(CultureInfo.InvariantCulture) +
                ", expectedProgressHits=" + expectedProgressHits.ToString(CultureInfo.InvariantCulture) +
                ", moodTitleHits=" + moodTitleHits.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsNativeMoodTitleText(string text)
        {
            text = (text ?? string.Empty).Trim();
            return text.Equals("心情", StringComparison.Ordinal) ||
                text.Equals("Mood", StringComparison.OrdinalIgnoreCase);
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

        private static bool IsUnityObjectAlive(object? instance)
        {
            if (instance == null)
                return false;

            try
            {
                Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
                if (objectType != null && objectType.IsInstanceOfType(instance))
                {
                    MethodInfo? equality = objectType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType, objectType }, null);
                    if (equality != null)
                    {
                        object? equalsNull = equality.Invoke(null, new object?[] { instance, null });
                        if (equalsNull is bool isNull && isNull)
                            return false;
                    }
                }

                MethodInfo? getInstanceId = instance.GetType().GetMethod("GetInstanceID", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                getInstanceId?.Invoke(instance, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SafeSetActive(object? gameObject, bool active)
        {
            if (!IsUnityObjectAlive(gameObject))
                return;
            try
            {
                SetActive(gameObject!, active);
            }
            catch
            {
            }
        }

        private static void SafeDestroyUnityObject(object? gameObject)
        {
            if (!IsUnityObjectAlive(gameObject))
                return;
            try
            {
                DestroyUnityObject(gameObject!);
            }
            catch
            {
            }
        }

        private void RecordAnimalProgressOverlayLifecycle(string key, string summary, bool failed)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!key.Equals("session-cleared", StringComparison.OrdinalIgnoreCase) &&
                lastAnimalProgressOverlayLifecycleLogKey.Equals(key, StringComparison.OrdinalIgnoreCase) &&
                (now - lastAnimalProgressOverlayLifecycleLogAt).TotalSeconds < 30)
                return;

            lastAnimalProgressOverlayLifecycleLogKey = key;
            lastAnimalProgressOverlayLifecycleLogAt = now;
            runtime.RuntimeMonitor.Log("Animal viewer progress overlay lifecycle " + key + " " + (summary ?? string.Empty) + ".");
            runtime.SetHookStatus(
                "Animals.ViewerRenderingLifecycle",
                failed ? "failed" : "experimental",
                "AnimalViewer cloned ProgressBar lifecycle",
                key + ": " + (summary ?? string.Empty));
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
