using System;
using System.Reflection;
using DolocTown;
using DolocTown.UI;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal sealed class AnimalHusbandryHookInstaller : IDisposable
    {
        internal const string HarmonyOwner = "Yuuka.DTMAPI.AnimalHusbandryProgress.Native";
        private readonly IMonitor monitor;
        private Harmony? harmony;
        internal AnimalHusbandryHookInstaller(IMonitor monitor) => this.monitor = monitor;
        internal int InstalledPatchCount { get; private set; }
        internal void InstallAtomically()
        {
            if (InstalledPatchCount == 4) return;
            var ctor = typeof(AnimalFullInfoData).GetConstructor(new[] { typeof(Animal) })
                ?? throw new MissingMethodException("AnimalFullInfoData(Animal)");
            var show = typeof(AnimalViewer).GetMethod("Show", new[] { typeof(AnimalFullInfoData) })
                ?? throw new MissingMethodException("AnimalViewer.Show(AnimalFullInfoData)");
            var close = typeof(AnimalPanelUiState).GetMethod("Unregister", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
                ?? throw new MissingMethodException("AnimalPanelUiState.Unregister()");
            foreach (MethodBase target in new MethodBase[] { ctor, show, close })
                if (Harmony.GetPatchInfo(target)?.Owners.Contains("dtmapi.gamebridge.doloctown") == true)
                    throw new InvalidOperationException("Frozen animal-viewer compatibility already owns the native viewer.");
            var dataReady = Callback(nameof(AnimalHusbandryCallbacks.AnimalFullInfoDataCtorPostfix));
            var shown = Callback(nameof(AnimalHusbandryCallbacks.AnimalViewerShowPostfix));
            var showFailed = Callback(nameof(AnimalHusbandryCallbacks.AnimalViewerShowFinalizer));
            var closed = Callback(nameof(AnimalHusbandryCallbacks.AnimalPanelUiStateUnregisterPostfix));
            harmony = new Harmony(HarmonyOwner);
            try
            {
                harmony.Patch(ctor, postfix: dataReady);
                harmony.Patch(show, postfix: shown, finalizer: showFailed);
                harmony.Patch(close, postfix: closed);
                InstalledPatchCount = 4;
                monitor.Log("AnimalHusbandryProgress product-owned patches installed owner=" + HarmonyOwner + "; targets=3; patches=4.");
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            harmony?.UnpatchSelf();
            InstalledPatchCount = 0;
        }
        private static HarmonyMethod Callback(string name) => new HarmonyMethod(typeof(AnimalHusbandryCallbacks).GetMethod(name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(AnimalHusbandryCallbacks).FullName, name));
    }
}
