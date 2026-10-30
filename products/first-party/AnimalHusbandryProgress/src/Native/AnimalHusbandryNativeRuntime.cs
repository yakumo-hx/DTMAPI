using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using DolocTown;
using DolocTown.Config;
using DolocTown.UI;
using global::DTMAPI.Abstractions;
using UnityEngine;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal sealed class AnimalHusbandryNativeRuntime : IDisposable
    {
        private readonly IMonitor monitor;
        private readonly IItemDisplayNameApi names;
        private readonly IReflectionHelper reflection;
        private readonly AnimalHusbandryHookInstaller hooks;
        private ConditionalWeakTable<AnimalFullInfoData, Animal> animals = new ConditionalWeakTable<AnimalFullInfoData, Animal>();
        private AnimalHusbandryConfig config = new AnimalHusbandryConfig();
        private AnimalHusbandryView? view;
        private Animal? selectedAnimal;
        private AnimalFullInfoData? selectedData;
        private AnimalProgressRow[] rows = Array.Empty<AnimalProgressRow>();
        private float refreshAt;
        private bool guardPending;
        private int visibleSequence;
        private bool disposed;
        private string observation = "AnimalHusbandryProgress has not observed a native animal row yet.";

        internal AnimalHusbandryNativeRuntime(IMonitor monitor, IItemDisplayNameApi names, IReflectionHelper reflection, AnimalHusbandryHookInstaller hooks)
        { this.monitor = monitor; this.names = names; this.reflection = reflection; this.hooks = hooks; }
        internal int InstalledPatchCount => hooks.InstalledPatchCount;
        internal void InstallHooksAtomically() { hooks.InstallAtomically(); AnimalHusbandryCallbacks.Attach(this); }
        internal void Configure(AnimalHusbandryConfig value)
        {
            config = value.Copy();
            config.Normalize();
            refreshAt = 0;
        }
        internal void DecorateAnimalFullInfoData(AnimalFullInfoData data, Animal animal)
        {
            if (disposed) return;
            animals.Remove(data);
            animals.Add(data, animal);
        }
        internal bool RenderAfterShow(AnimalViewer viewer, AnimalFullInfoData data)
        {
            if (disposed) return false;
            try
            {
                if (!viewer || data == null || !data.notEmpty || !data.visible || !animals.TryGetValue(data, out Animal animal))
                { ClearSelection("unavailable-selection"); return false; }
                ProgressBar mood;
                using (var field = reflection.GetField<ProgressBar>(viewer, "moodBar")) mood = field.GetValue();
                if (!mood || !mood.transform.parent) { ClearSelection("unavailable-template"); return false; }
                if (view != null && !view.Matches(mood)) CloseView();
                rows = BuildRows(animal);
                if (rows.Length == 0) { ClearSelection("no-valid-outputs"); return false; }
                if (view == null) view = new AnimalHusbandryView(mood, reflection);
                bool changed = !ReferenceEquals(selectedData, data);
                selectedData = data;
                selectedAnimal = animal;
                view.Bind(rows, config.ProgressColorHex, changed);
                if (changed) visibleSequence++;
                guardPending = true;
                refreshAt = Time.unscaledTime + 0.25f;
                observation = Describe(rows, "visible") + ", overlayRows=" + rows.Length + ", receiptSequence=" + visibleSequence +
                    ", receipt=" + (changed ? "new-data" : "same-data-coalesced") + ", nextFrameGuard=pending, ownedRoots=1";
                if (changed) monitor.Log("AnimalHusbandryProgress product viewer active " + observation + ".");
                return true;
            }
            catch (Exception ex) { Fail("render", ex); return false; }
        }
        internal void Update()
        {
            if (disposed || view == null || selectedAnimal == null) return;
            try
            {
                if (!view.IsAlive) { CloseNativePanel(); return; }
                if (guardPending)
                {
                    guardPending = false;
                    view.Bind(rows, config.ProgressColorHex, false);
                    observation = observation.Replace("nextFrameGuard=pending", "nextFrameGuard=completed");
                }
                if (Time.unscaledTime < refreshAt) return;
                refreshAt = Time.unscaledTime + 0.25f;
                rows = BuildRows(selectedAnimal);
                view.Bind(rows, config.ProgressColorHex, false);
                observation = Describe(rows, "visible") + ", overlayRows=" + rows.Length + ", receiptSequence=" + visibleSequence + ", nextFrameGuard=completed, ownedRoots=1";
            }
            catch (Exception ex) { Fail("refresh", ex); }
        }
        private AnimalProgressRow[] BuildRows(Animal animal)
        {
            var table = DolocConfig.Tables.TbHusbandry.GetOrDefault(animal.protoName);
            if (table == null) return Array.Empty<AnimalProgressRow>();
            Dictionary<string, int> values;
            using (var field = reflection.GetField<Dictionary<string, int>>(animal, "husbandryValues")) values = field.GetValue();
            var candidates = new List<AnimalProgressRow>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var output in table.HusbandryDatas)
            {
                if (output == null || string.IsNullOrWhiteSpace(output.Output) || output.Threshold <= 0 || !seen.Add(output.Output)) continue;
                values.TryGetValue(output.Output, out int current);
                string title = names.TryGetDisplayName(output.Output, out string name) && !string.IsNullOrWhiteSpace(name) ? name : output.Output;
                candidates.Add(new AnimalProgressRow(output.Output, title, current, output.Threshold));
            }
            return AnimalStableRowPlan.CreateRows(candidates, AnimalProgressRow.Compare);
        }
        internal bool TryObserveRows(AnimalFullInfoData data, out string summary)
        {
            summary = observation;
            if (!animals.TryGetValue(data, out Animal animal)) return false;
            try { var current = BuildRows(animal); summary = Describe(current, ReferenceEquals(data, selectedData) ? "visible" : "pending"); return current.Length > 0; }
            catch (Exception ex) { monitor.Log("Animal progress observation failed: " + ex.Message, LogLevel.Warn); return false; }
        }
        internal string GetObservationSummary() => observation;
        internal void CloseNativePanel()
        {
            ResetBoundary("native AnimalPanelUiState.Unregister");
            monitor.Log("AnimalHusbandryProgress native panel closed; product-owned derived rows and clones are zero.");
        }
        internal void ObservePanelClose()
        {
            try { CloseNativePanel(); }
            catch (Exception ex) { monitor.Log("Animal viewer close remains pending: " + ex.Message, LogLevel.Warn); }
        }
        internal void ObserveShowFailure(Exception error) => Fail("native-show", error);
        internal void ResetBoundary(string reason)
        {
            animals = new ConditionalWeakTable<AnimalFullInfoData, Animal>();
            ClearSelection(reason, releaseView: true);
        }
        private void ClearSelection(string reason, bool releaseView = false)
        {
            // Other AnimalFullInfoData rows in the same native panel remain
            // valid when the selected animal is hidden or has no output.
            selectedAnimal = null; selectedData = null; guardPending = false;
            rows = Array.Empty<AnimalProgressRow>();
            if (releaseView) CloseView();
            else if (view != null) view.Bind(rows, config.ProgressColorHex, false);
            observation = "AnimalHusbandryProgress selection cleared reason=" + reason +
                ", state=hidden, rows=0, overlayRows=0, ownedRoots=" + (view == null ? "0" : "1") + ".";
        }
        private void CloseView()
        {
            // Retain the view if native disposal fails, so owner cleanup can retry it.
            view?.Dispose();
            view = null;
        }
        private void Fail(string operation, Exception ex)
        {
            monitor.Log("AnimalHusbandryProgress " + operation + " failed: " + ex.Message, LogLevel.Warn);
            try { ResetBoundary(operation + "-failed"); }
            catch (Exception cleanup) { monitor.Log("Animal viewer cleanup remains pending: " + cleanup.Message, LogLevel.Warn); }
        }
        public void Dispose()
        {
            disposed = true;
            AnimalHusbandryCallbacks.Detach(this);
            ResetBoundary("OwnerDeactivation");
        }
        private static string Describe(AnimalProgressRow[] values, string state)
        {
            string primary = values.Length == 0 ? "none" : values[0].Title + " " + values[0].Text;
            return "state=" + state + ", rows=" + values.Length.ToString(CultureInfo.InvariantCulture) + ", primary=" + primary +
                ", owner=" + AnimalHusbandryHookInstaller.HarmonyOwner + ", moodOverride=False, stateDescriptionOverride=False";
        }
    }
}
