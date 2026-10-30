namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    public static class FishBreedingCallbacks
    {
        private static FishBreedingNativeRuntime? runtime;

        internal static void Attach(FishBreedingNativeRuntime owner) => runtime = owner;

        internal static void Detach(FishBreedingNativeRuntime owner)
        {
            if (ReferenceEquals(runtime, owner))
                runtime = null;
        }

        public static void ItemTitlePostfix(object __instance, ref string __result)
        {
            FishBreedingNativeRuntime? current = runtime;
            if (current != null)
                __result = current.DecorateTitle(__instance, __result);
        }
    }
}
