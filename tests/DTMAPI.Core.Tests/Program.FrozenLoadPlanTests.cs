using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void ColdFixedPlansIgnoreDisabledOptionalSources()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string game = NewTempGameDir();
                runtime = new DtmApiRuntime(new FakeHost(game));
                var orderMods = typeof(DtmApiRuntime).GetMethod("OrderMods", BindingFlags.Instance | BindingFlags.NonPublic)!;
                foreach (string scenario in new[] { "independent", "disabled-optional", "required-control" })
                foreach (bool reversed in new[] { false, true })
                {
                    DiscoveredMod Node(string id, int priority, bool enabled = true)
                    {
                        var manifest = new ManifestModel { UniqueID = id, Name = id, Author = "Test", Version = "1.0.0", Type = "ContentPack" };
                        string root = Path.Combine(game, id);
                        return new DiscoveredMod(manifest, root, Path.Combine(root, "manifest.json"), "Local", null,
                            enabled, false, "Local." + id, true, "", officialPriority: priority);
                    }
                    var a = Node("A", 10); var b = Node("B", 20); var c = Node("C", 30, scenario != "disabled-optional");
                    if (scenario != "independent")
                        a.Manifest.DependencyModels.Add(new ManifestDependencyModel { UniqueID = "C", Required = scenario == "required-control" });
                    DiscoveredMod[] discovered = reversed ? new[] { c, b, a } : new[] { a, b, c };
                    object?[] arguments = { discovered, null };
                    var legacyOrder = (IReadOnlyList<DiscoveredMod>)orderMods.Invoke(runtime, arguments)!;
                    Assert(((HashSet<string>)arguments[1]!).Count == 0, "These cold-plan cases contain no cycles.");
                    using var loader = new DependencyAssemblyLoader(discovered, Array.Empty<KeyValuePair<string, string>>(),
                        _ => { }, game);
                    var legacyPlan = new FrozenModLoadPlan(legacyOrder.Where(mod => mod.OfficialEnabled), _ => { });
                    var packagePlan = new FrozenModLoadPlan(loader.Plan.LoadOrder.Select(id => discovered.Single(mod => mod.Manifest.UniqueID == id)), _ => { });
                    string[] expected = scenario == "independent" ? new[] { "A", "B", "C" } :
                        scenario == "required-control" ? new[] { "B", "C", "A" } : new[] { "A", "B" };
                    string label = scenario + ", reversed=" + reversed;
                    Assert(loader.Plan.Blocked.Count == 0 && legacyPlan.Entries.Select(row => row.Owner).SequenceEqual(expected) &&
                        packagePlan.Entries.Select(row => row.Owner).SequenceEqual(expected), "Both cold plans must have the specified order: " + label);
                    for (int index = 0; index < expected.Length; index++)
                    {
                        DiscoveredMod owner = discovered.Single(mod => mod.Manifest.UniqueID == expected[index]);
                        Assert(legacyPlan.Resolve(owner) == index && packagePlan.Resolve(owner) == index, "Both frozen LoadIndex values must match: " + label + ", " + owner.Manifest.UniqueID);
                    }
                    if (!c.OfficialEnabled)
                        Assert(legacyPlan.Resolve(c) == null && packagePlan.Resolve(c) == null, "Disabled C has no frozen index in either path.");
                }
            }
            finally { runtime?.NotifyRuntimeShutdown("cold-fixed-plan-test"); RestorePersistentRoot(previous); }
        }

        private static void LegacyColdPlanKeepsRequiredDependencyRejection()
        {
            foreach (bool missing in new[] { false, true })
            {
                string? previous = UseTempPersistentRoot();
                DtmApiRuntime? runtime = null;
                try
                {
                    string persistent = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")!;
                    string game = NewTempGameDir();
                    foreach (string id in missing ? new[] { "A", "B" } : new[] { "A", "B", "C" })
                    {
                        string root = Path.Combine(persistent, "MODS", id);
                        Directory.CreateDirectory(root);
                        File.WriteAllText(Path.Combine(root, "manifest.json"), "{\"UniqueID\":\"" + id + "\",\"Name\":\"Plan\",\"Author\":\"Test\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\",\"Dependencies\":[" +
                            (id == "A" ? "{\"UniqueID\":\"C\",\"Required\":true}" : "") + "]}");
                    }
                    WriteOfficialModInfos(persistent, new OfficialModInfoTestEntry("Local.A", true, "Local", 10),
                        new OfficialModInfoTestEntry("Local.B", true, "Local", 20), new OfficialModInfoTestEntry("Local.C", false, "Local", 30));
                    var host = new FakeHost(game);
                    runtime = new DtmApiRuntime(host); runtime.Start();
                    Assert(runtime.LoadedMods.Select(mod => mod.Manifest.UniqueID).SequenceEqual(new[] { "B" }) &&
                        runtime.CreateSnapshot().Errors.Any(error => error.Owner == "A" && error.Details.Contains("C")), "Missing/disabled required C still rejects A at the actual legacy load gate.");
                    Assert(runtime.ResolveOwnerLoadIndex(runtime.LoadedMods.Single().Manifest) == 0, "The unified preflight plan gives the first admitted owner index zero.");
                    using var loader = new DependencyAssemblyLoader(runtime.DiscoveredMods, Array.Empty<KeyValuePair<string, string>>(),
                        _ => { }, game);
                    Assert(loader.Plan.Blocked.ContainsKey("A") && loader.Plan.LoadOrder.SequenceEqual(new[] { "B" }),
                        "The package path still rejects missing/disabled required C during preflight.");
                }
                finally { runtime?.NotifyRuntimeShutdown("cold-required-plan-test"); RestorePersistentRoot(previous); }
            }
        }

        private static void PackagePlanSelectsOfficialReadyCandidates()
        {
            PackageModDependency Dep(string id, bool required = true) => new PackageModDependency(id, required, new PackageVersionRange("1.0.0", null, false));
            PackagePlanInput Node(string id, int? order, params PackageModDependency[] deps) => new PackagePlanInput
                { Id = id, OfficialPriority = order, Version = "1.0.0", Dependencies = deps };
            PackageDependencyPlan Plan(params PackagePlanInput[] nodes) => PackageDependencyPlanner.Plan(nodes);
            var a = Node("A", 10, Dep("C")); var b = Node("B", 20); var c = Node("C", 30);
            Assert(Plan(a, b, c).LoadOrder.SequenceEqual(new[] { "B", "C", "A" }) &&
                Plan(c, b, a).LoadOrder.SequenceEqual(new[] { "B", "C", "A" }), "Select the earliest READY candidate, not sorted DFS C,A,B.");
            a.Dependencies = Array.Empty<PackageModDependency>(); a.OfficialPriority = 30; b.OfficialPriority = 10; c.OfficialPriority = 20;
            Assert(Plan(a, b, c).LoadOrder.SequenceEqual(new[] { "B", "C", "A" }), "Independent owners follow official low-to-high order.");
            a.OfficialPriority = c.OfficialPriority = 10; b.OfficialPriority = null; var d = Node("D", null);
            Assert(Plan(d, c, b, a).LoadOrder.SequenceEqual(new[] { "B", "D", "A", "C" }), "Missing precedes present; ties use canonical IDs, not discovery order.");
            a.OfficialPriority = c.OfficialPriority = null;
            Assert(Plan(d, c, b, a).LoadOrder.SequenceEqual(new[] { "A", "B", "C", "D" }), "Offline SDK default remains ID deterministic.");
            a.Dependencies = new[] { Dep("B", false) }; b.Dependencies = new[] { Dep("A", false) }; b.OfficialPriority = 0; a.OfficialPriority = 10;
            var soft = Plan(a, b);
            Assert(soft.Blocked.Count == 0 && soft.LoadOrder.SequenceEqual(new[] { "B", "A" }) && soft.Warnings.Single().Contains("optional-order-cycle-broken: B"), "Package soft cycles break at the earliest official ready-required candidate.");
            a.Dependencies = new[] { Dep("B") }; b.Dependencies = new[] { Dep("A") }; c.Dependencies = new[] { Dep("A") };
            var hard = Plan(a, b, c, d, Node("Missing", 1, Dep("Absent")), Node("WrongVersion", 2, new PackageModDependency("D", true, new PackageVersionRange("2.0.0", null, false))));
            Assert(hard.LoadOrder.SequenceEqual(new[] { "D" }) && hard.Blocked.Count == 5, "Required cycles, missing/version-invalid dependencies and their consumers stay blocked.");
            var provider = Node("Z.Platform", null); provider.IsProcessProvider = true;
            Assert(Plan(Node("A.Owner", null), provider).LoadOrder.First() == provider.Id, "Process providers are explicit existing platform nodes, not insertion-time order.");
            a = Node("A", 20); b = Node("B", 10);
            var binding = Plan(b, a);
            Assert(binding.LoadOrder.First() == "B", "Official ordering does not preselect a DLL provider.");
        }

        private static void LegacyFixedPlanUsesOfficialSourcesAndKeepsLifetime()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string persistent = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")!;
                string game = NewTempGameDir();
                string Root(string id) => Path.Combine(persistent, "MODS", id);
                void Pack(string folder, string id, string version = "1.0.0", string dependency = "", string api = "0.7.3")
                {
                    Directory.CreateDirectory(Root(folder));
                    File.WriteAllText(Path.Combine(Root(folder), "manifest.json"), "{\"UniqueID\":\"" + id + "\",\"Name\":\"Plan\",\"Author\":\"Test\",\"Version\":\"" + version + "\",\"Type\":\"ContentPack\",\"MinimumDTMApiVersion\":\"" + api + "\",\"Dependencies\":[" +
                        (dependency.Length == 0 ? "" : "{\"UniqueID\":\"" + dependency + "\",\"Required\":true}") + "]}");
                }
                Pack("C", "Plan.C"); Pack("B", "Plan.B"); Pack("A", "Plan.A", dependency: "Plan.C"); Pack("Failure", "Plan.Failure", api: "99.0.0");
                void Enable(int a, int b, int c, bool enableB = true, string cFolder = "C", bool extra = false) => WriteOfficialModInfos(persistent,
                    new OfficialModInfoTestEntry("Local.C", cFolder == "C", "Local", c),
                    new OfficialModInfoTestEntry("Local.C2", cFolder == "C2", "Local", c),
                    new OfficialModInfoTestEntry("Local.B", enableB, "Local", b),
                    new OfficialModInfoTestEntry("Local.A", true, "Local", a),
                    new OfficialModInfoTestEntry("Local.Failure", true, "Local", 25),
                    new OfficialModInfoTestEntry("Local.New", extra, "Local", 99));
                Enable(10, 20, 30);
                var host = new FakeHost(game);
                runtime = new DtmApiRuntime(host); runtime.Start();
                Assert(runtime.LoadedMods.Select(mod => mod.Manifest.UniqueID).SequenceEqual(new[] { "Plan.B", "Plan.C", "Plan.A" }), "Actual legacy Runtime path selects B,C,A after official source discovery.");
                IManifest Owner(string id) => runtime.LoadedMods.Single(mod => mod.Manifest.UniqueID == id).Manifest;
                IManifest aOwner = Owner("Plan.A"), bOwner = Owner("Plan.B"), cOwner = Owner("Plan.C");
                Assert(runtime.ResolveOfficialOwnerOrder(aOwner) == 0 && runtime.ResolveOfficialOwnerOrder(Owner("Plan.B")) == 1 && runtime.ResolveOfficialOwnerOrder(cOwner) == 3,
                    "Audio conflicts retain official A,B,Failure,C order even though managed dependencies start B,C,A.");
                Assert(runtime.ResolveOwnerLoadIndex(Owner("Plan.B")) == 0 && runtime.ResolveOwnerLoadIndex(cOwner) == 2 && runtime.ResolveOwnerLoadIndex(aOwner) == 3,
                    "Rejected owner leaves its frozen index hole; surviving owners are never renumbered.");
                Assert(runtime.ResolveOwnerLoadIndex(new ManifestModel { UniqueID = "Plan.A", Version = "1.0.0" }) == null, "Same-ID author object cannot impersonate the canonical manifest.");
                runtime.NotifySaveLoaded(false); runtime.NotifyReturnedToTitle();
                Assert(runtime.ResolveOwnerLoadIndex(aOwner) == 3, "Title/save generations do not end the process order plan.");
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Select(mod => mod.Manifest.UniqueID).SequenceEqual(new[] { "Plan.B", "Plan.C", "Plan.A" }) &&
                    ReferenceEquals(runtime.ModRegistry.Get("Plan.A"), aOwner) && !runtime.ModRegistry.IsLoaded("Plan.Failure"),
                    "An unchanged selection with a rejected Mod preserves the loaded instances and rejected state.");
                Enable(60, 50, 40); runtime.NotifyWorkshopModListChanged();
                Assert(runtime.ResolveOwnerLoadIndex(aOwner) == 3 && runtime.ResolveOwnerLoadIndex(cOwner) == 2,
                    "Official list changes keep the running owners and frozen indices active.");
                Enable(60, 50, 40, enableB: false); runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == "Plan.B").OfficialEnabled &&
                    runtime.ModRegistry.IsLoaded("Plan.B") && ReferenceEquals(runtime.ModRegistry.Get("Plan.B"), bOwner) &&
                    runtime.ResolveOwnerLoadIndex(bOwner) == 0 && runtime.ResolveOwnerLoadIndex(aOwner) == 3,
                    "Disabling a source updates discovery but keeps the actual loaded owner, canonical manifest and frozen order until restart.");
                Pack("New", "Plan.New"); Enable(60, 50, 40, enableB: false, extra: true); runtime.NotifyWorkshopModListChanged();
                IManifest newSource = runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == "Plan.New").Manifest;
                Assert(runtime.ModRegistry.IsLoaded("Plan.New") && runtime.ResolveOwnerLoadIndex(newSource) == 4,
                    "A newly selected owner loads and appends a rank to the running process plan.");
                Pack("C2", "Plan.C"); Enable(60, 50, 40, enableB: false, cFolder: "C2", extra: true); runtime.NotifyWorkshopModListChanged();
                DiscoveredMod newCSource = runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == "Plan.C");
                Assert(PathsEqual(newCSource.RootPath, Root("C2")) && ReferenceEquals(Owner("Plan.C"), cOwner) &&
                    runtime.ResolveOwnerLoadIndex(cOwner) == 2 && runtime.ResolveOwnerLoadIndex(newCSource.Manifest) == null,
                    "A source change remains pending while the original owner stays active; the discovered replacement cannot borrow its frozen rank.");
                Pack("A", "Plan.A", "2.0.0", "Plan.C"); runtime.NotifyWorkshopModListChanged();
                IManifest newAVersion = runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == "Plan.A").Manifest;
                Assert(newAVersion.Version == "2.0.0" && ReferenceEquals(Owner("Plan.A"), aOwner) && aOwner.Version == "1.0.0" &&
                    runtime.ResolveOwnerLoadIndex(aOwner) == 3 && runtime.ResolveOwnerLoadIndex(newAVersion) == null,
                    "A changed version does not replace the running manifest or reuse its frozen plan identity.");
                runtime.DeactivateOwner("Plan.B", ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(!runtime.ModRegistry.IsLoaded("Plan.B") && runtime.ResolveOwnerLoadIndex(aOwner) == 3 && runtime.ResolveOwnerLoadIndex(cOwner) == 2,
                    "Explicit owner cleanup remains available and does not compact the surviving process plan.");
            }
            finally { runtime?.NotifyRuntimeShutdown("fixed-plan-test"); RestorePersistentRoot(previous); }
        }

        private static void LegacyLoadPlanKeepsOptionalCycleAdmission()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string game = NewTempGameDir();
                foreach (string id in new[] { "A", "B", "C" })
                    WriteManifest(game, id, "{\"UniqueID\":\"" + id + "\",\"Name\":\"Cycle\",\"Author\":\"Test\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\",\"Dependencies\":[" +
                        (id == "C" ? "" : "{\"UniqueID\":\"" + (id == "A" ? "B" : "A") + "\",\"Required\":false}") + "]}");
                runtime = new DtmApiRuntime(new FakeHost(game)); runtime.Start();
                Assert(runtime.LoadedMods.Select(mod => mod.Manifest.UniqueID).OrderBy(id => id).SequenceEqual(new[] { "A", "B", "C" }), "Optional ordering cycles are broken deterministically without rejecting usable owners.");
            }
            finally { runtime?.NotifyRuntimeShutdown("fixed-plan-cycle-test"); RestorePersistentRoot(previous); }
        }
    }
}
