using System;
using DTMAPI.Abstractions;
using Yuuka.DTMAPI.FishBreedingAssistant;

namespace DTMAPI.UnitTests
{
    internal static class FishBreedingProductTests
    {
        internal static void RunAll()
        {
            var names = new Names();
            var runtime = new FishBreedingNativeRuntime(NullMonitor.Instance, names);
            var roe = new DolocTown.ItemFishRoe { fishName = "carp" };
            Equal("鱼卵 (鲤鱼)", runtime.DecorateTitle(roe, "鱼卵"));
            Equal("鱼卵 (鲤鱼)", runtime.DecorateTitle(roe, "鱼卵 (鲤鱼)"));
            roe.fishName = "eel";
            Equal("鱼卵 (鳗鱼)", runtime.DecorateTitle(roe, "鱼卵"));
            names.English = true;
            Equal("Roe (Eel)", runtime.DecorateTitle(roe, "Roe"));

            int queries = names.Queries;
            Equal("木材", runtime.DecorateTitle(new object(), "木材"));
            roe.fishName = " ";
            Equal("鱼卵", runtime.DecorateTitle(roe, "鱼卵"));
            Check(names.Queries == queries, "Ordinary items and empty fish IDs must not query names.");
            roe.fishName = "unknown";
            Equal("鱼卵", runtime.DecorateTitle(roe, "鱼卵"));
            roe.fishName = "empty";
            Equal("鱼卵", runtime.DecorateTitle(roe, "鱼卵"));
            roe.fishName = "throw";
            Equal("鱼卵", runtime.DecorateTitle(roe, "鱼卵"));

            roe.fishName = "carp";
            runtime.InstallHooksAtomically();
            string title = "Roe";
            FishBreedingCallbacks.ItemTitlePostfix(roe, ref title);
            Equal("Roe (Carp)", title);
            FishBreedingHookInstaller.FailCleanup = true;
            try { runtime.DeactivateOwner(); throw new Exception("Cleanup failure was hidden."); }
            catch (InvalidOperationException) { }
            title = "Roe";
            FishBreedingCallbacks.ItemTitlePostfix(roe, ref title);
            Equal("Roe", title);
            FishBreedingHookInstaller.FailCleanup = false;
            runtime.DeactivateOwner();
            runtime.DeactivateOwner();
            Check(runtime.InstalledPatchCount == 0, "Cleanup retries must finish.");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private static void Equal(string expected, string actual) => Check(expected == actual, "Expected '" + expected + "', got '" + actual + "'.");

        private sealed class Names : IItemDisplayNameApi
        {
            internal bool English;
            internal int Queries;
            public bool TryGetDisplayName(string itemId, out string displayName)
            {
                Queries++;
                if (itemId == "throw") throw new InvalidOperationException("Native query failed.");
                displayName = itemId == "carp" ? (English ? "Carp" : "鲤鱼") : itemId == "eel" ? (English ? "Eel" : "鳗鱼") : "";
                return itemId != "unknown";
            }
        }
    }
}

// Shape-only fixtures exercise product formatting and callback cleanup. Actual
// fish-to-fry identity and Harmony ownership belong to the native acceptance.
namespace DolocTown
{
    internal class ItemFishRoe { public string fishName { get; set; } = ""; }
}
namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    internal sealed class FishBreedingHookInstaller
    {
        internal static bool FailCleanup;
        internal FishBreedingHookInstaller(IMonitor monitor) { }
        internal int InstalledPatchCount { get; private set; }
        internal void InstallAtomically() => InstalledPatchCount = 1;
        internal void UnpatchOwnedHooks()
        {
            if (FailCleanup) throw new InvalidOperationException("Unpatch rejected.");
            InstalledPatchCount = 0;
        }
    }
}
