using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static string HostPackManifest(string owner = "Author.PackA", string schema = "demo/1") =>
            "{\"UniqueID\":\"" + owner + "\",\"Name\":\"Pack\",\"Author\":\"Test\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\",\"MinimumDTMApiVersion\":\"0.7.3\",\"DependencyContractVersion\":1," +
            "\"Dependencies\":[{\"UniqueID\":\"Author.Host\",\"Required\":true,\"VersionRange\":{\"minimumInclusive\":\"1.0.0\",\"maximumExclusive\":\"2.0.0\",\"includePrerelease\":false}}]," +
            "\"ContentPackFor\":{\"HostId\":\"Author.Host\",\"Schema\":\"" + schema + "\",\"ContractVersionRange\":{\"minimumInclusive\":\"1.0.0\",\"maximumExclusive\":\"2.0.0\",\"includePrerelease\":false}}}";

        private static (ManifestModel Manifest, ContentPackBinding Binding, string Root) NewHostPack(string owner = "Author.PackA", string schema = "demo/1")
        {
            string root = NewTempGameDir(), text = HostPackManifest(owner, schema);
            Directory.CreateDirectory(Path.Combine(root, "PackData"));
            File.WriteAllText(Path.Combine(root, "manifest.json"), text);
            return (new ManifestReader().Read(Path.Combine(root, "manifest.json")), ContentPackBinding.Read(Encoding.UTF8.GetBytes(text))!, root);
        }

        private static void ModResourcesUseBoundedIndependentSnapshotsAndOwnerPaths()
        {
            string root = NewTempGameDir();
            File.WriteAllBytes(Path.Combine(root, "image.png"), new byte[] { 1, 2, 3 });
            bool alive = true;
            var manager = new OwnerServiceManager();
            var scope = manager.Create("Author.A", () => { if (!alive) throw new ObjectDisposedException("A"); }, () => { }, root);
            var resources = scope.GetService<IModResourceHelper>()!;
            var bounded = new ModResourceService(root, () => { if (!alive) throw new ObjectDisposedException("A"); }).Reference("image.png");
            Assert(bounded.GetLength().Status == OwnerDataStatus.Found && bounded.GetLength().Value == 3, "Opaque references expose length without returning a physical path or media array.");
            Assert(bounded.ReadBytes(2).Status == OwnerDataStatus.IoError, "A caller's smaller budget is enforced before allocation.");
            AssertThrows<ArgumentOutOfRangeException>(() => bounded.ReadBytes(ModResourceService.MaxBytes + 1), "Internal audio reads cannot raise the global byte ceiling.");
            byte[] original = resources.ReadBytes("image.png").Value!;
            original[0] = 9;
            Assert(resources.ReadBytes("image.png").Value![0] == 1, "Caller mutation must not change a later snapshot.");
            File.WriteAllBytes(Path.Combine(root, "image.png"), new byte[] { 4 });
            Assert(resources.ReadBytes("image.png").Value!.SequenceEqual(new byte[] { 4 }) && original[1] == 2, "New requests see changed bytes; retained snapshots stay caller-owned.");
            Assert(resources.ReadBytes("missing").Status == OwnerDataStatus.Missing, "Missing resources remain distinct.");
            AssertThrows<ArgumentException>(() => resources.ReadBytes("../outside"), "Traversal must fail.");
            AssertThrows<ArgumentException>(() => resources.ReadBytes("Image.png"), "Wrong case must fail on Windows too.");
            using (var stream = File.Create(Path.Combine(root, "large"))) stream.SetLength(ModResourceService.MaxBytes + 1);
            Assert(resources.ReadBytes("large").Status == OwnerDataStatus.IoError, "Oversize resources cannot allocate unbounded snapshots.");
            using (var stream = new FileStream(Path.Combine(root, "large"), FileMode.Open, FileAccess.Write)) stream.SetLength(ModResourceService.MaxBytes);
            Assert(resources.ReadBytes("large").Value!.Length == ModResourceService.MaxBytes, "Exact byte limit remains readable.");
            manager.RemoveOwner("Author.A", ModOwnerCleanupReason.Unload);
            AssertThrows<ObjectDisposedException>(() => resources.ReadBytes("image.png"), "Retained reader closes with owner scope.");
            alive = false;
            AssertThrows<ObjectDisposedException>(() => bounded.GetLength(), "Retained metadata references close with the Owner/Host guard.");
            AssertThrows<ObjectDisposedException>(() => bounded.ReadBytes(2), "Retained bounded readers close with the Owner/Host guard.");

            var registry = new ModRegistryService();
            var provider = new ManifestModel { UniqueID = "Scoped.Provider" };
            var consumer = new ManifestModel { UniqueID = "Scoped.Pack" };
            registry.RegisterApiForOwner<IUnitProbeApi>(provider, new ContentScopeProbe());
            bool aOpen = true, bOpen = true, hostOpen = true;
            var aRegistry = registry.CreateContentBoundRegistry(consumer, () => { if (!aOpen || !hostOpen) throw new ObjectDisposedException("UnitA/Host"); });
            var bRegistry = registry.CreateContentBoundRegistry(consumer, () => { if (!bOpen || !hostOpen) throw new ObjectDisposedException("UnitB/Host"); });
            var aApi = aRegistry.GetApi<IUnitProbeApi>(provider.UniqueID)!;
            var bApi = bRegistry.GetApi<IUnitProbeApi>(provider.UniqueID)!;
            Assert(!ReferenceEquals(aApi, bApi) && ReferenceEquals(aApi, aRegistry.GetApi<IUnitProbeApi>(provider.UniqueID)), "Content facades cache within the exact unit scope, never across sibling guards.");
            aOpen = false;
            AssertThrows<ObjectDisposedException>(() => { _ = aApi.Owner; }, "A retained facade cannot borrow its sibling's active package authority.");
            Assert(bApi.Owner == consumer.UniqueID, "Closing A retains B's distinct Pack/Host guard.");
            ((IDisposable)aRegistry).Dispose();
            Assert(registry.CountOwner(consumer.UniqueID) == 1, "Successful unit cleanup releases only its cached facade roots.");
            hostOpen = false;
            AssertThrows<ObjectDisposedException>(() => { _ = bApi.Owner; }, "The retained sibling facade closes when its real Host guard closes.");
            bOpen = false; ((IDisposable)bRegistry).Dispose();
            Assert(registry.CountOwner(consumer.UniqueID) == 0, "Closing both content scopes leaves no cached facade roots.");
        }

        private sealed class ContentScopeProbe : IUnitProbeApi, IOwnerBoundApiFactory
        {
            private readonly Action guard;
            private readonly string owner;
            internal ContentScopeProbe(Action? guard = null, string owner = "root") { this.guard = guard ?? (() => { }); this.owner = owner; }
            public string Owner { get { guard(); return owner; } }
            public object CreateOwnerBoundApi(Type apiType, IManifest consumer, Action ensureOwnerActive) => new ContentScopeProbe(ensureOwnerActive, consumer.UniqueID);
        }

        private static void ContentHostBindingsRequireExplicitVersionedDependencyAndSafeLayout()
        {
            string text = HostPackManifest();
            var binding = ContentPackBinding.Read(Encoding.UTF8.GetBytes(text))!;
            Assert(binding.HostId == "Author.Host" && binding.ContractRange.Contains(PackageSemanticVersion.Parse("1.2.0")), "Binding preserves Host and independent contract SemVer.");
            Assert(ContentPackBinding.Read(Encoding.UTF8.GetBytes("{\"Type\":\"ContentPack\"}")) == null, "Unbound legacy packages retain their reader.");
            foreach (string invalid in new[] {
                text + "{}", text.Insert(text.Length - 1, ","), text.Replace("\"DependencyContractVersion\":1", "\"DependencyContractVersion\":01"),
                text.Replace("ContentPackFor", "contentpackfor"), text.Replace("\"ContentPack\"", "\"CodeMod\""),
                text.Replace("\"Schema\":\"demo/1\"", "\"Schema\":\"\""),
                text.Replace("\"HostId\":\"Author.Host\"", "\"HostId\":\"Author.Host\",\"hostId\":\"Author.Other\""),
                text.Replace("\"minimumInclusive\":\"1.0.0\"", "\"minimumInclusive\":\"01.0.0\"") })
                AssertThrows<InvalidDataException>(() => ContentPackBinding.Read(Encoding.UTF8.GetBytes(invalid)), "Malformed binding must fail without legacy fallback: " + invalid);
            foreach (string historicalValue in new[] { "2", "999", "\"damaged\"", "null", "{}" })
                Assert(ContentPackBinding.Read(Encoding.UTF8.GetBytes(text.Replace("\"DependencyContractVersion\":1", "\"DependencyContractVersion\":" + historicalValue))) != null,
                    "Historical tool selectors do not change the actual Host binding.");
            Assert(ContentPackBinding.Read(Encoding.UTF8.GetBytes(text.Replace("0.7.3", "0.7.0"))) != null,
                "The installed reader can accept a binding without an artificial minimum Runtime declaration.");
            var implicitHost = PackageDependencyContract.ReadManifest(Encoding.UTF8.GetBytes(text.Replace("\"Required\":true", "\"Required\":false")))!;
            Assert(implicitHost.Dependencies.Single().Required, "Selecting a Host establishes the required relationship without redundant author declarations.");
            var pack = NewHostPack();
            Directory.CreateDirectory(Path.Combine(pack.Root, "Content/DTMAPI"));
            File.WriteAllText(Path.Combine(pack.Root, "Content/DTMAPI/manifest.json"), text);
            File.WriteAllBytes(Path.Combine(pack.Root, "PackData/image.png"), new byte[] { 1 });
            ContentPackBinding.ValidatePackage(pack.Root);
            File.WriteAllText(Path.Combine(pack.Root, "Content/DTMAPI/stone.json"), "{}");
            ContentPackBinding.ValidatePackage(pack.Root);
            File.Delete(Path.Combine(pack.Root, "Content/DTMAPI/stone.json"));
            File.WriteAllBytes(Path.Combine(pack.Root, "PackData/hidden.dll"), new byte[] { 1 });
            ContentPackBinding.ValidatePackage(pack.Root);
        }

        private static void ContentHostsIsolatePacksAndRetainFailedActivationCleanup()
        {
            var a = NewHostPack();
            var b = NewHostPack("Author.PackB");
            var bad = NewHostPack("Author.Bad", "bad/9");
            File.WriteAllText(Path.Combine(a.Root, "PackData/same.txt"), "A");
            File.WriteAllText(Path.Combine(b.Root, "PackData/same.txt"), "B");
            var calls = new List<string>();
            var contexts = new Dictionary<string, IContentPack>();
            var manager = new ContentHostManager(() => { });
            var registry = new ModRegistryService();
            bool failDispose = true;
            var provider = new ManifestModel { UniqueID = "Author.Host", Version = "1.3.0" };
            registry.AddLoaded(provider);
            manager.CreateService("Author.Host", () => { }).Register("1.1.0", pack => {
                if (pack.Schema != "demo/1") throw new InvalidDataException("unsupported-schema");
                contexts.Add(pack.Manifest.UniqueID, pack);
                Assert(pack.GetRequiredDependency("Author.Host")?.Version == "1.3.0" && pack.GetRequiredDependency("Unknown.Host") == null, "Pack only sees declared loaded required providers.");
                return new ProbePackActivation(() => {
                    calls.Add("activate:" + pack.Files.ReadText("same.txt").Value);
                    if (pack.Manifest.UniqueID == "Author.PackA") throw new InvalidOperationException("partial activation");
                }, () => {
                    calls.Add("dispose:" + pack.Manifest.UniqueID);
                    if (pack.Manifest.UniqueID == "Author.PackA" && failDispose) throw new IOException("retry cleanup");
                });
            });
            AssertThrows<InvalidOperationException>(() => manager.Activate(a.Manifest, a.Binding, a.Root, () => { }, registry, new TranslationService(a.Root, "en")), "Activation failure remains associated with Pack.");
            var retained = manager.RemoveOwner(a.Manifest.UniqueID, ModOwnerCleanupReason.EntryFailed);
            Assert(retained.RemainingResources == 1 && retained.FailureCount == 1, "Failed compensation must retain its activation and report a failure.");
            Assert(manager.PendingCleanupOwners().SequenceEqual(new[] { a.Manifest.UniqueID }), "Refresh can find a failed Pack after it leaves the loaded list.");
            AssertThrows<ObjectDisposedException>(() => contexts[a.Manifest.UniqueID].GetRequiredDependency("Author.Host"), "Dependency lookup closes with Pack execution.");
            AssertThrows<ObjectDisposedException>(() => contexts[a.Manifest.UniqueID].Files.ReadText("same.txt"), "Failed activation closes retained Pack reader immediately.");
            manager.Activate(b.Manifest, b.Binding, b.Root, () => { }, registry, new TranslationService(b.Root, "en"));
            AssertThrows<InvalidDataException>(() => manager.Activate(bad.Manifest, bad.Binding, bad.Root, () => { }, registry, new TranslationService(bad.Root, "en")), "Bad schema does not activate.");
            Assert(manager.RemoveOwner(bad.Manifest.UniqueID, ModOwnerCleanupReason.EntryFailed).RemainingResources == 0, "Prepare failure with no effects has no lease.");
            Assert(contexts[b.Manifest.UniqueID].Files.ReadText("same.txt").Value == "B", "Another Pack reads its own file after sibling failure.");
            failDispose = false;
            Assert(manager.RemoveOwner(a.Manifest.UniqueID, ModOwnerCleanupReason.Unload).RemainingResources == 0, "Second cleanup releases retained partial activation.");
            int disposed = calls.Count;
            manager.RemoveOwner(a.Manifest.UniqueID, ModOwnerCleanupReason.Unload);
            Assert(calls.Count == disposed, "Repeated Pack cleanup is idempotent.");
            manager.RemoveOwner("Author.Host", ModOwnerCleanupReason.Unload);
            AssertThrows<ObjectDisposedException>(() => contexts[b.Manifest.UniqueID].Resources.ReadBytes("same.txt"), "Host removal closes remaining Pack resources.");
            Assert(calls.SequenceEqual(new[] { "activate:A", "dispose:Author.PackA", "activate:B", "dispose:Author.PackA", "dispose:Author.PackB" }), "Failed disposal retries exactly the retained Pack; no sibling compensation.");
        }

        private static void ContentHostsRejectMissingVersionAndHandleReentrantClosure()
        {
            var pack = NewHostPack();
            var manager = new ContentHostManager(() => { });
            var registry = new ModRegistryService();
            void Activate() => manager.Activate(pack.Manifest, pack.Binding, pack.Root, () => { }, registry, new TranslationService(pack.Root, "en"));
            AssertThrows<InvalidOperationException>(Activate, "Missing handler rejects Pack.");
            bool ran = false;
            manager.CreateService("Author.Host", () => { }).Register("2.0.0", _ => { ran = true; return new ProbePackActivation(() => { }, () => { }); });
            AssertThrows<InvalidOperationException>(Activate, "Contract mismatch rejects before calling Host.");
            Assert(!ran, "Wrong contract may not prepare.");
            manager.RemoveOwner("Author.Host", ModOwnerCleanupReason.Unload);
            int dispose = 0;
            manager.CreateService("Author.Host", () => { }).Register("1.0.0", _ => {
                var result = manager.RemoveOwner(pack.Manifest.UniqueID, ModOwnerCleanupReason.Unload);
                Assert(result.RemainingResources == 1, "Reentrant close defers until callback returns.");
                return new ProbePackActivation(() => { ran = true; }, () => dispose++);
            });
            AssertThrows<ObjectDisposedException>(Activate, "A late prepared activation cannot revive a closed Pack.");
            Assert(!ran && dispose == 0, "Late result is retained before cleanup, never activated.");
            Assert(manager.RemoveOwner(pack.Manifest.UniqueID, ModOwnerCleanupReason.Unload).RemainingResources == 0 && dispose == 1, "Reentrant late result is disposed on retry.");
        }

        private sealed class ProbePackActivation : IContentPackActivation
        {
            private readonly Action activate, dispose;
            internal ProbePackActivation(Action activate, Action dispose) { this.activate = activate; this.dispose = dispose; }
            public void Activate() => activate();
            public void Dispose() => dispose();
        }
    }
}
