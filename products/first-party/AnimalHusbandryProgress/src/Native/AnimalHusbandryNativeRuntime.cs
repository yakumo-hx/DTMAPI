using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal sealed class AnimalHusbandryNativeRuntime
    {
        private readonly IMonitor monitor;
        private readonly IItemDisplayNameApi itemDisplayNames;
        private readonly AnimalHusbandryHookInstaller hooks;
        private readonly Dictionary<object, ProgressRowSet> rowsByData = new Dictionary<object, ProgressRowSet>();
        private readonly Dictionary<string, int> thresholdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly AnimalOverlaySessionState<RenderedRow> activeOverlay = new AnimalOverlaySessionState<RenderedRow>();
        private AnimalHusbandryConfig config = new AnimalHusbandryConfig();
        private object? activeParent;
        private object? lastVisibleData;
        private int visibleSequence;
        private string observationSummary = "AnimalHusbandryProgress has not observed a native animal row yet.";

        internal AnimalHusbandryNativeRuntime(IMonitor monitor, IItemDisplayNameApi itemDisplayNames)
        {
            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.itemDisplayNames = itemDisplayNames ?? throw new ArgumentNullException(nameof(itemDisplayNames));
            hooks = new AnimalHusbandryHookInstaller(monitor);
        }

        internal int InstalledPatchCount => hooks.InstalledPatchCount;

        internal void InstallHooksAtomically()
        {
            hooks.InstallAtomically();
            try { AnimalHusbandryCallbacks.Attach(this); }
            catch
            {
                hooks.UnpatchOwnedHooks();
                throw;
            }
        }

        internal void Configure(AnimalHusbandryConfig value)
        {
            config = (value ?? new AnimalHusbandryConfig()).Copy();
            config.Normalize();
            if (!config.Enabled)
                ResetBoundary("disabled by config");
        }

        internal void DecorateAnimalFullInfoData(object data, object animal)
        {
            if (!config.Enabled || data == null || animal == null)
                return;
            try
            {
                ProgressRowSet? rows = BuildRows(animal);
                if (rows == null)
                    return;
                rowsByData[data] = rows;
                observationSummary = BuildObservation(rows.TotalCount, rows.Primary, "pending");
                if (config.VerboseLogging)
                    monitor.Log("AnimalHusbandryProgress product row derived " + observationSummary + ".");
            }
            catch (Exception ex)
            {
                monitor.Log("AnimalHusbandryProgress data derivation failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        internal void PrepareBeforeShow(object viewer, object data)
        {
            try { ClearForViewer(viewer, "show-prefix"); }
            catch (Exception ex) { monitor.Log("AnimalHusbandryProgress prefix cleanup failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn); }
        }

        internal bool RenderAfterShow(object viewer, object data)
        {
            if (!config.Enabled || viewer == null || data == null)
                return false;
            if (!NativeReflection.ReadBoolMember(data, "notEmpty", true) || !NativeReflection.ReadBoolMember(data, "visible", true))
            {
                ClearForViewer(viewer, "data-not-visible");
                return false;
            }
            if (!rowsByData.TryGetValue(data, out ProgressRowSet? rows))
            {
                ClearForViewer(viewer, "data-without-rows");
                return false;
            }

            try
            {
                object? moodBar = NativeReflection.ReadMember(viewer, "moodBar");
                object? moodGameObject = NativeReflection.ReadMember(moodBar, "gameObject");
                object? moodTransform = NativeReflection.ReadMember(moodGameObject, "transform");
                object? parent = NativeReflection.ReadMember(moodTransform, "parent");
                if (moodGameObject == null || moodTransform == null || parent == null)
                    return false;

                ClearOverlay(parent, "replace-overlay");
                activeParent = parent;
                int localizationDisabled = 0;
                Type progressBarType = NativeReflection.ResolveType("DolocTown.UI.ProgressBar, Assembly-CSharp")
                    ?? throw new TypeLoadException("AnimalHusbandryProgress could not resolve DolocTown.UI.ProgressBar.");
                object? progressColor = CreateProgressColor();
                foreach (ProgressRow row in rows.VisibleRows)
                {
                    object? clone = NativeReflection.CloneUnityObject(moodGameObject);
                    if (clone == null)
                        continue;
                    NativeReflection.SetMemberValue(clone, "name", "DTMAPI.AnimalProduceProgress." + activeOverlay.Count.ToString(CultureInfo.InvariantCulture));
                    NativeReflection.SafeSetActive(clone, false);
                    object? rowTransform = NativeReflection.ReadMember(clone, "transform");
                    if (rowTransform == null)
                    {
                        NativeReflection.SafeDestroy(clone);
                        continue;
                    }
                    NativeReflection.SetParent(rowTransform, parent, false);
                    PositionRow(moodTransform, rowTransform, activeOverlay.Count);
                    localizationDisabled += NativeReflection.DisableLocalizationComponents(clone);
                    RenderedRow rendered = CaptureRenderedRow(clone, row, progressBarType, progressColor);
                    activeOverlay.Track(rendered);
                    WriteRenderedRow(rendered);
                }

                foreach (RenderedRow rendered in activeOverlay.Tracked)
                    NativeReflection.SafeSetActive(rendered.Clone, true);
                activeOverlay.ArmNextFrameGuard();
                bool newData = activeOverlay.Count > 0 && !ReferenceEquals(lastVisibleData, data);
                if (newData)
                {
                    lastVisibleData = data;
                    visibleSequence++;
                }
                observationSummary = BuildObservation(rows.TotalCount, rows.Primary, "visible") +
                    ", overlayRows=" + activeOverlay.Count.ToString(CultureInfo.InvariantCulture) +
                    ", localizationDisabled=" + localizationDisabled.ToString(CultureInfo.InvariantCulture) +
                    ", receiptSequence=" + visibleSequence.ToString(CultureInfo.InvariantCulture) +
                    ", receipt=" + (newData ? "new-data" : "same-data-coalesced") +
                    ", writes=initial+next-frame-guard, nextFrameGuard=pending" +
                    ", moodOverride=False, stateDescriptionOverride=False";
                monitor.Log("AnimalHusbandryProgress product viewer active " + observationSummary + ".");
                return activeOverlay.Count > 0;
            }
            catch (Exception ex)
            {
                monitor.Log("AnimalHusbandryProgress render failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                ClearOverlaySession("render-failed");
                return false;
            }
        }

        internal void Update()
        {
            if (!activeOverlay.TryConsumeNextFrameGuard())
                return;
            if (!ValidateOverlay())
                return;
            try
            {
                foreach (RenderedRow rendered in activeOverlay.Tracked)
                    WriteRenderedRow(rendered);
                observationSummary = observationSummary.Replace(
                    "nextFrameGuard=pending",
                    "nextFrameGuard=completed");
            }
            catch (Exception ex)
            {
                monitor.Log("AnimalHusbandryProgress next-frame guard failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                ClearOverlaySession("next-frame-guard-failed");
            }
        }

        internal void CloseNativePanel()
        {
            ClearOverlaySession("native AnimalPanelUiState.Unregister");
            rowsByData.Clear();
            thresholdCache.Clear();
            observationSummary = "AnimalHusbandryProgress native panel closed; product-owned derived rows and clones are zero.";
            monitor.Log(observationSummary);
        }

        internal void ResetBoundary(string reason)
        {
            ClearOverlaySession(reason);
            rowsByData.Clear();
            thresholdCache.Clear();
            lastVisibleData = null;
            observationSummary = "AnimalHusbandryProgress boundary reset reason=" + (reason ?? string.Empty) + ", nativeData=0, overlayObjects=0, overlayRows=0.";
            monitor.Log(observationSummary);
        }

        internal bool TryObserveRows(object data, out string summary)
        {
            summary = observationSummary;
            if (data == null || !rowsByData.TryGetValue(data, out ProgressRowSet? rows))
                return false;
            summary = BuildObservation(rows.TotalCount, rows.Primary, activeOverlay.Count > 0 ? "visible" : "pending");
            return true;
        }

        internal string GetObservationSummary() => observationSummary;

        internal void DeactivateOwner(string reason)
        {
            var failures = new List<Exception>();
            TryCleanup(() => ResetBoundary(reason), failures);
            TryCleanup(() => AnimalHusbandryCallbacks.Detach(this), failures);
            TryCleanup(hooks.UnpatchOwnedHooks, failures);
            if (failures.Count == 1)
                throw failures[0];
            if (failures.Count > 1)
                throw new AggregateException("AnimalHusbandryProgress state cleanup, callback detach and exact-owner Harmony cleanup failed.", failures);
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures)
        {
            try { cleanup(); }
            catch (Exception ex) { failures.Add(ex); }
        }

        private ProgressRowSet? BuildRows(object animal)
        {
            string animalId = NativeReflection.ReadStringMember(animal, "protoName");
            if (string.IsNullOrWhiteSpace(animalId))
                return null;
            var currentByOutput = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (NativeReflection.ReadMember(animal, "husbandryValues") is IDictionary values)
            {
                foreach (DictionaryEntry entry in values)
                {
                    string outputId = entry.Key as string ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(outputId))
                        currentByOutput[outputId] = Convert.ToInt32(entry.Value, CultureInfo.InvariantCulture);
                }
            }

            var rows = new List<ProgressRow>();
            foreach (OutputThreshold output in EnumerateOutputs(animalId))
            {
                currentByOutput.TryGetValue(output.OutputId, out int current);
                rows.Add(CreateRow(animalId, output.OutputId, current, output.Threshold));
            }
            if (rows.Count == 0)
            {
                foreach (KeyValuePair<string, int> entry in currentByOutput)
                    if (TryGetThreshold(animalId, entry.Key, out int threshold))
                        rows.Add(CreateRow(animalId, entry.Key, entry.Value, threshold));
            }
            if (rows.Count == 0)
                return null;
            ProgressRow[] visibleRows = AnimalStableRowPlan.CreateTopRows(
                rows,
                CompareRows,
                maximumRows: 3);
            return new ProgressRowSet(rows.Count, visibleRows);
        }

        private ProgressRow CreateRow(string animalId, string outputId, int current, int threshold)
        {
            string title = outputId;
            itemDisplayNames.TryGetDisplayName(outputId, out title);
            return new ProgressRow
            {
                AnimalId = animalId,
                OutputId = outputId,
                OutputTitle = string.IsNullOrWhiteSpace(title) ? outputId : title,
                Current = current,
                Threshold = threshold,
                Progress = Math.Max(0, Math.Min(1, current / (double)threshold)),
                ProgressText = current.ToString(CultureInfo.InvariantCulture) + "/" + threshold.ToString(CultureInfo.InvariantCulture)
            };
        }

        private IEnumerable<OutputThreshold> EnumerateOutputs(string animalId)
        {
            object? tables = NativeReflection.ReadStaticMember(NativeReflection.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp"), "Tables");
            object? husbandry = NativeReflection.ReadMember(tables, "TbHusbandry");
            MethodInfo? getOrDefault = husbandry?.GetType().GetMethod("GetOrDefault", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            object? info = getOrDefault?.Invoke(husbandry, new object[] { animalId });
            foreach (object data in NativeReflection.EnumerateObjects(NativeReflection.ReadMember(info, "HusbandryDatas")))
            {
                string outputId = NativeReflection.ReadStringMember(data, "Output");
                int threshold = NativeReflection.ReadIntMember(data, "Threshold", 0);
                if (!string.IsNullOrWhiteSpace(outputId) && threshold > 0)
                    yield return new OutputThreshold(outputId, threshold);
            }
        }

        private bool TryGetThreshold(string animalId, string outputId, out int threshold)
        {
            string key = animalId + "|" + outputId;
            if (thresholdCache.TryGetValue(key, out threshold))
                return threshold > 0;
            threshold = 0;
            object? tables = NativeReflection.ReadStaticMember(NativeReflection.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp"), "Tables");
            object? husbandry = NativeReflection.ReadMember(tables, "TbHusbandry");
            MethodInfo? method = husbandry?.GetType().GetMethod("TryGetThreshold", BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                return false;
            object?[] args = new object?[] { animalId, outputId, threshold };
            if (!(method.Invoke(husbandry, args) is bool ok) || !ok)
                return false;
            threshold = Convert.ToInt32(args[2], CultureInfo.InvariantCulture);
            thresholdCache[key] = threshold;
            return threshold > 0;
        }

        private static int CompareRows(ProgressRow left, ProgressRow right)
        {
            int progress = right.Progress.CompareTo(left.Progress);
            return progress != 0
                ? progress
                : StringComparer.OrdinalIgnoreCase.Compare(left.OutputTitle, right.OutputTitle);
        }

        private RenderedRow CaptureRenderedRow(object clone, ProgressRow row, Type progressBarType, object? progressColor)
        {
            object? progressBar = NativeReflection.GetComponent(clone, progressBarType);
            if (progressBar == null)
                throw new MissingMemberException("AnimalHusbandryProgress clone does not contain DolocTown.UI.ProgressBar.");
            Type type = progressBar.GetType();
            MethodInfo? setTitle = type.GetMethod("SetTitle", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            MethodInfo? setProgress = type.GetMethod("SetProgress", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float), typeof(string) }, null);
            object? titleText = NativeReflection.ReadMember(progressBar, "txtTitle");
            object? progressText = NativeReflection.ReadMember(progressBar, "txtProgress");
            object? mask = NativeReflection.ReadMember(progressBar, "progressMask");
            object? graphic = NativeReflection.ReadMember(mask, "graphic") ?? NativeReflection.ReadMember(progressBar, "imgProgress") ?? mask;
            var titleTargets = new List<object>();
            var progressTargets = new List<object>();
            NativeReflection.CaptureChildTextTargets(clone, titleTargets, progressTargets);
            AddUniqueReference(titleTargets, titleText);
            AddUniqueReference(progressTargets, progressText);
            return new RenderedRow(
                clone,
                progressBar,
                row,
                setTitle,
                setProgress,
                titleTargets.ToArray(),
                progressTargets.ToArray(),
                graphic,
                progressColor);
        }

        private static void AddUniqueReference(ICollection<object> targets, object? value)
        {
            if (value == null)
                return;
            foreach (object target in targets)
                if (ReferenceEquals(target, value))
                    return;
            targets.Add(value);
        }

        private static void WriteRenderedRow(RenderedRow rendered)
        {
            rendered.SetTitle?.Invoke(rendered.ProgressBar, new object[] { rendered.Row.OutputTitle });
            rendered.SetProgress?.Invoke(rendered.ProgressBar, new object[] { (float)rendered.Row.Progress, rendered.Row.ProgressText });
            foreach (object title in rendered.TitleTargets)
                NativeReflection.SetText(title, rendered.Row.OutputTitle);
            foreach (object progress in rendered.ProgressTargets)
                NativeReflection.SetText(progress, rendered.Row.ProgressText);
            if (rendered.Graphic != null && rendered.ProgressColor != null)
                NativeReflection.SetMemberValue(rendered.Graphic, "color", rendered.ProgressColor);
        }

        private object? CreateProgressColor()
        {
            Type? colorType = NativeReflection.ResolveType("UnityEngine.Color, UnityEngine.CoreModule") ?? NativeReflection.ResolveType("UnityEngine.Color, UnityEngine");
            ConstructorInfo? ctor = colorType?.GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) });
            if (ctor == null)
                return null;
            string hex = AnimalHusbandryConfig.NormalizeHex(config.ProgressColorHex);
            float r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
            float g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
            float b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
            return ctor.Invoke(new object[] { r, g, b, 1f });
        }

        private void ClearForViewer(object? viewer, string reason)
        {
            object? parent = NativeReflection.ReadMember(NativeReflection.ReadMember(NativeReflection.ReadMember(viewer, "moodBar"), "gameObject"), "transform");
            parent = NativeReflection.ReadMember(parent, "parent");
            if (parent != null)
                ClearOverlay(parent, reason);
            else
                ClearOverlaySession(reason + "-parent-missing");
        }

        private void ClearOverlay(object parent, string reason)
        {
            object? previousParent = activeParent;
            ClearOverlaySession(reason);
            if (!ReferenceEquals(previousParent, parent))
                DestroyNamedChildren(parent, Array.Empty<object>());
        }

        private void ClearOverlaySession(string reason)
        {
            var trackedClones = new object[activeOverlay.Count];
            for (int i = 0; i < activeOverlay.Count; i++)
                trackedClones[i] = activeOverlay.Tracked[i].Clone;
            activeOverlay.Clear(rendered =>
            {
                NativeReflection.SafeSetActive(rendered.Clone, false);
                NativeReflection.SafeDestroy(rendered.Clone);
            });
            if (activeParent != null)
                DestroyNamedChildren(activeParent, trackedClones);
            activeParent = null;
            lastVisibleData = null;
            if (config.VerboseLogging)
                monitor.Log("AnimalHusbandryProgress overlay cleared reason=" + (reason ?? string.Empty) + ".");
        }

        private static void DestroyNamedChildren(object parent, IReadOnlyList<object> alreadyDestroyed)
        {
            MethodInfo? find = parent.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            for (int i = 0; i < 10; i++)
            {
                object? child = find?.Invoke(parent, new object[] { "DTMAPI.AnimalProduceProgress." + i.ToString(CultureInfo.InvariantCulture) });
                object? gameObject = NativeReflection.ReadMember(child, "gameObject");
                bool tracked = false;
                foreach (object prior in alreadyDestroyed)
                    if (ReferenceEquals(prior, gameObject))
                    {
                        tracked = true;
                        break;
                    }
                if (!tracked)
                    NativeReflection.SafeDestroy(gameObject);
            }
        }

        private bool ValidateOverlay()
        {
            if (activeOverlay.Count == 0 || (activeParent != null && !NativeReflection.IsUnityObjectAlive(activeParent)))
            {
                ClearOverlaySession("invalid-overlay-shape");
                return false;
            }
            foreach (RenderedRow rendered in activeOverlay.Tracked)
                if (!NativeReflection.IsUnityObjectAlive(rendered.Clone))
                {
                    ClearOverlaySession("destroyed-clone");
                    return false;
                }
            return true;
        }

        private static void PositionRow(object source, object row, int index)
        {
            object? local = NativeReflection.ReadMember(source, "localPosition");
            object? position = NativeReflection.CreateVector3(
                NativeReflection.ReadVectorComponent(local, "x"),
                NativeReflection.ReadVectorComponent(local, "y") - 42d * (index + 1),
                NativeReflection.ReadVectorComponent(local, "z"));
            if (position != null)
                NativeReflection.SetMemberValue(row, "localPosition", position);
            object? scale = NativeReflection.ReadMember(source, "localScale");
            if (scale != null)
                NativeReflection.SetMemberValue(row, "localScale", scale);
        }

        private static string BuildObservation(int count, ProgressRow primary, string state)
            => "state=" + state + ", rows=" + count.ToString(CultureInfo.InvariantCulture) +
                ", primary=" + primary.OutputTitle + " " + primary.Current + "/" + primary.Threshold +
                ", owner=" + AnimalHusbandryHookInstaller.HarmonyOwner +
                ", moodOverride=False, stateDescriptionOverride=False";

        private sealed class ProgressRow
        {
            internal string AnimalId = string.Empty;
            internal string OutputId = string.Empty;
            internal string OutputTitle = string.Empty;
            internal int Current;
            internal int Threshold;
            internal double Progress;
            internal string ProgressText = string.Empty;
        }

        private sealed class ProgressRowSet
        {
            internal ProgressRowSet(int totalCount, ProgressRow[] visibleRows)
            {
                if (visibleRows == null || visibleRows.Length == 0)
                    throw new ArgumentException("At least one visible row is required.", nameof(visibleRows));
                TotalCount = totalCount;
                VisibleRows = visibleRows;
                Primary = visibleRows[0];
            }

            internal int TotalCount { get; }
            internal ProgressRow[] VisibleRows { get; }
            internal ProgressRow Primary { get; }
        }

        private sealed class RenderedRow
        {
            internal RenderedRow(
                object clone,
                object progressBar,
                ProgressRow row,
                MethodInfo? setTitle,
                MethodInfo? setProgress,
                object[] titleTargets,
                object[] progressTargets,
                object? graphic,
                object? progressColor)
            {
                Clone = clone;
                ProgressBar = progressBar;
                Row = row;
                SetTitle = setTitle;
                SetProgress = setProgress;
                TitleTargets = titleTargets;
                ProgressTargets = progressTargets;
                Graphic = graphic;
                ProgressColor = progressColor;
            }

            internal object Clone { get; }
            internal object ProgressBar { get; }
            internal ProgressRow Row { get; }
            internal MethodInfo? SetTitle { get; }
            internal MethodInfo? SetProgress { get; }
            internal object[] TitleTargets { get; }
            internal object[] ProgressTargets { get; }
            internal object? Graphic { get; }
            internal object? ProgressColor { get; }
        }

        private readonly struct OutputThreshold
        {
            internal OutputThreshold(string outputId, int threshold) { OutputId = outputId; Threshold = threshold; }
            internal string OutputId { get; }
            internal int Threshold { get; }
        }
    }
}
