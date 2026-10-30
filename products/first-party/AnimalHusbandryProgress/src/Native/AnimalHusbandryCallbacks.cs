using System;
using DolocTown;
using DolocTown.UI;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    public static class AnimalHusbandryCallbacks
    {
        private static AnimalHusbandryNativeRuntime? runtime;
        internal static void Attach(AnimalHusbandryNativeRuntime owner)
        {
            if (runtime != null && !ReferenceEquals(runtime, owner)) throw new InvalidOperationException("Animal viewer owner already attached.");
            runtime = owner;
        }
        internal static void Detach(AnimalHusbandryNativeRuntime owner) { if (ReferenceEquals(runtime, owner)) runtime = null; }
        internal static bool IsAttached => runtime != null;
        public static void AnimalFullInfoDataCtorPostfix(AnimalFullInfoData __instance, Animal __0)
            => runtime?.DecorateAnimalFullInfoData(__instance, __0);
        public static void AnimalViewerShowPostfix(AnimalViewer __instance, AnimalFullInfoData __0)
            => runtime?.RenderAfterShow(__instance, __0);
        public static void AnimalViewerShowFinalizer(Exception? __exception)
        {
            if (__exception != null) runtime?.ObserveShowFailure(__exception);
        }
        public static void AnimalPanelUiStateUnregisterPostfix() => runtime?.ObservePanelClose();
        public static bool TryObserveRows(object data, out string summary)
        {
            summary = runtime?.GetObservationSummary() ?? "AnimalHusbandryProgress product callback is detached.";
            return data is AnimalFullInfoData info && runtime?.TryObserveRows(info, out summary) == true;
        }
        public static string GetObservationSummary() => runtime?.GetObservationSummary() ?? "AnimalHusbandryProgress product callback is detached.";
    }
}
