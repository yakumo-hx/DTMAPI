using System;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void ModuleScopesRetainFailedChildrenAndIsolateSiblings()
        {
            var owners = new OwnerDependencyStore();
            var a = owners.CreateContribution("Mixed.Owner", "voice", () => { });
            var b = owners.CreateContribution("Mixed.Owner", "definitions", () => { });
            ((IRuntimeContribution)a).UseProvider("Builtin.Audio");
            bool fail = true; int childReleases = 0, parentReleases = 0;
            a.Own(new ProbePackActivation(() => { }, () => parentReleases++));
            a.Own(new ProbePackActivation(() => { }, () => { childReleases++; if (fail) throw new IOException("voice callback pending"); }));
            Assert(!owners.PrepareContributionsExit("Builtin.Audio", out _) && childReleases == 1 && parentReleases == 0,
                "Failed child retains both its Provider edge and the parent resource needed for retry.");
            Assert(owners.CountContributions("Builtin.Audio") == 1, "A pending voice still pins its Provider.");
            AssertThrows<ObjectDisposedException>(() => a.Guard(), "Closing a contribution rejects new operations immediately.");
            b.Guard();
            fail = false;
            Assert(owners.PrepareContributionsExit("Builtin.Audio", out _) && parentReleases == 1 && owners.CountContributions("Builtin.Audio") == 0,
                "Successful retry releases the child before the parent and only then removes its edge.");
            b.Guard();
            Assert(owners.CountContributions("Mixed.Owner") == 1, "Provider failure cannot close the sibling definition contribution.");
            var replacement = owners.CreateContribution("Mixed.Owner", "voice", () => { });
            Assert(!ReferenceEquals(a.Generation, replacement.Generation), "Reusing a local contribution name creates a distinct lifetime.");
            replacement.Guard();
            int activated = 0, disposed = 0;
            var late = owners.CreateContribution("Late.Owner", "content", () => { });
            AssertThrows<ObjectDisposedException>(() => late.Activate(() =>
            {
                Assert(!owners.PrepareContributionsExit("Late.Owner", out _), "Exit waits for the running preparation callback.");
                return new ProbePackActivation(() => activated++, () => disposed++);
            }, value => value.Activate()), "A callback result cannot activate after its scope closes.");
            Assert(owners.PrepareContributionsExit("Late.Owner", out _) && activated == 0 && disposed == 1,
                "The late result remains owned until compensation completes.");
        }

        private static void BuiltinProviderOriginSurvivesUnavailableComponents()
        {
            var registry = new ModRegistryService();
            registry.ReserveRuntimeProvider("Builtin.Capability");
            Assert(registry.IsRuntimeProvider("Builtin.Capability") && !registry.IsLoaded("Builtin.Capability"),
                "Reserved origin and available API are distinct facts.");
            var metadata = new ManifestModel { UniqueID = "Builtin.Capability", Version = "1.0.0" };
            AssertThrows<InvalidOperationException>(() => registry.AddLoaded(metadata), "A package cannot impersonate an unavailable built-in Provider.");
            registry.RegisterRuntimeApiFactory<IUnitProbeApi>(metadata.UniqueID, "1.0.0", (_, _) => new ContentScopeProbe());
            Assert(registry.IsLoaded(metadata.UniqueID), "Publishing an API makes the built-in capability available.");
            Assert(registry.Get(metadata.UniqueID) == null && registry.GetAll().Count == 0 && registry.TryGetVersion(metadata.UniqueID, out string version) && version == "1.0.0", "A built-in Provider has a version and API without a fabricated package manifest.");
            int roots = registry.TotalRootCount;
            Assert(registry.GetContributionApi<IUnitProbeApi>(new ManifestModel { UniqueID = "Consumer" }, metadata.UniqueID, () => { }) != null && registry.TotalRootCount == roots, "Internal domain lookup does not create an unowned facade cache root.");
            registry.RemoveOwner(metadata.UniqueID);
            Assert(registry.IsRuntimeProvider(metadata.UniqueID) && !registry.IsLoaded(metadata.UniqueID) && registry.TotalRootCount == 0,
                "Closing a Provider removes active roots but retains its origin reservation.");
            AssertThrows<InvalidOperationException>(() => registry.AddLoaded(metadata), "Failed or closed modules cannot become ordinary Mods.");
        }
    }
}
