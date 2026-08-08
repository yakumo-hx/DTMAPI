namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    public static class AnimalHusbandryCallbacks
    {
        private static AnimalHusbandryNativeRuntime? runtime;

        internal static void Attach(AnimalHusbandryNativeRuntime owner) => runtime = owner;

        internal static void Detach(AnimalHusbandryNativeRuntime owner)
        {
            if (ReferenceEquals(runtime, owner))
                runtime = null;
        }

        internal static bool IsAttached => runtime != null;

        public static void AnimalFullInfoDataCtorPostfix(object __instance, object __0)
            => runtime?.DecorateAnimalFullInfoData(__instance, __0);

        public static void AnimalViewerShowPrefix(object __instance, object __0)
            => runtime?.PrepareBeforeShow(__instance, __0);

        public static void AnimalViewerShowPostfix(object __instance, object __0)
            => runtime?.RenderAfterShow(__instance, __0);

        public static void AnimalPanelUiStateUnregisterPostfix()
            => runtime?.CloseNativePanel();

        public static bool TryObserveRows(object data, out string summary)
        {
            AnimalHusbandryNativeRuntime? current = runtime;
            if (current == null)
            {
                summary = "AnimalHusbandryProgress product callback is detached.";
                return false;
            }
            return current.TryObserveRows(data, out summary);
        }

        public static string GetObservationSummary()
            => runtime?.GetObservationSummary() ?? "AnimalHusbandryProgress product callback is detached.";
    }
}
