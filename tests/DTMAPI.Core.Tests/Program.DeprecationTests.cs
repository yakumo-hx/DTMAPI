using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
#pragma warning disable CS0618 // Deliberately exercise the retained ABI through old declarations.
        private static void DeprecatedApiWarningsAreOwnerBoundAndDoNotChangeLookup()
        {
            int warnings = 0;
            string lastOwner = "";
            var registry = new ModRegistryService(reportDeprecatedApi: (owner, contract, message) =>
            {
                warnings++;
                lastOwner = owner;
                Assert(contract == typeof(ILampControlApi).FullName && message.Length > 0, "Warning must identify the exact known old API.");
            });
            var first = new ManifestModel { UniqueID = "Linden.First", Name = "First", Author = "Linden", Version = "1.0.0" };
            var second = new ManifestModel { UniqueID = "Linden.Second", Name = "Second", Author = "Linden", Version = "1.0.0" };
            bool active = true;
            var helper = registry.CreateOwnerBoundRegistry(first, () => { if (!active) throw new InvalidOperationException("closed"); });
            Assert(helper.GetApi<ILampControlApi>("DTMAPI") == null, "Missing provider must retain its null lookup result.");
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10000; index++) helper.GetApi<ILampControlApi>("DTMAPI");
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            helper.GetApi<IDtmConfigMenuApi>("DTMAPI");
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10000; index++) helper.GetApi<IDtmConfigMenuApi>("DTMAPI");
            long ordinaryAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert(warnings == 1 && allocated <= ordinaryAllocated, "Repeated old API queries must emit one warning and add no allocation beyond ordinary registry lookup. old=" + allocated + "; ordinary=" + ordinaryAllocated);
            Assert(warnings == 1, "Current API lookup must not inherit a warning from its owner's other requests.");
            registry.CreateOwnerBoundRegistry(second, () => { }).GetApi<ILampControlApi>("DTMAPI");
            Assert(warnings == 2 && lastOwner == second.UniqueID, "Each consumer owns its own warning.");
            active = false;
            AssertThrows(() => helper.GetApi<ILampControlApi>("DTMAPI"), "Closed owner must be rejected before diagnostics.");
            registry.RemoveOwner(first.UniqueID);
            registry.CreateOwnerBoundRegistry(first, () => { }).GetApi<ILampControlApi>("DTMAPI");
            Assert(warnings == 3, "Owner cleanup must discard its diagnostic state for a later activation.");
            var failingSink = new ModRegistryService(reportDeprecatedApi: (_, __, ___) => throw new InvalidOperationException("diagnostic sink"));
            Assert(failingSink.CreateOwnerBoundRegistry(first, () => { }).GetApi<ILampControlApi>("DTMAPI") == null, "A failing diagnostic sink must not change old API behavior.");
        }
#pragma warning restore CS0618
    }
}
