using System;
using System.IO;
using System.Linq;
using System.Text;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Internal.Authoring;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void RuntimeExplicitEntryIgnoresUnresolvableTypesAndReleasesAfterStop()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string game = NewTempGameDir();
                string package = CreateEnabledFixtureModDirectory(game, "ExplicitEntry");
                Directory.CreateDirectory(Path.Combine(package, "bin"));
                const string assemblyName = "DTMAPI.DemandEntryFixture";
                File.Copy(Path.Combine(AppContext.BaseDirectory, "demand-fixtures", assemblyName + ".dll"), Path.Combine(package, "bin", "Entry.dll"));
                File.WriteAllText(Path.Combine(package, "Entry.dll"), "unused duplicate basename");
                File.WriteAllText(Path.Combine(package, "manifest.json"),
                    "{\"UniqueID\":\"Mechanism.Explicit\",\"Version\":\"October preview\",\"Type\":\"CodeMod\",\"EntryDll\":\"./bin/../bin/Entry.dll\",\"EntryType\":\"DTMAPI.DemandEntryFixture.ExplicitMod\"}");
                var runtime = new DtmApiRuntime(new FakeHost(game));
                runtime.Start();
                Assert(runtime.ModRegistry.IsLoaded("Mechanism.Explicit"), "Explicit entry loads with an absent optional AssemblyRef, an unresolved adjacent type, a duplicate basename and a display-only version. " + string.Join(" | ", runtime.Diagnostics.GetErrors().Select(e => e.Details)));
                var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == assemblyName);
                var type = assembly.GetType("DTMAPI.DemandEntryFixture.ExplicitMod")!;
                AssertThrows<System.Reflection.ReflectionTypeLoadException>(() => assembly.GetTypes(), "The fixture really has an unresolved type; the successful entry must not depend on full enumeration.");
                runtime.Update();
                Assert((int)type.GetField("UpdateCount")!.GetValue(null)! > 0, "The actual Mod callback executes.");
                runtime.DeactivateOwner("Mechanism.Explicit", ModOwnerCleanupReason.EntryFailed, false);
                Assert((int)type.GetField("DisposeCount")!.GetValue(null)! == 1 && CountCoreOwnerRootsForTest(runtime, "Mechanism.Explicit") == 0,
                    "Dispose can unsubscribe and release input after admission for new work has stopped.");
                runtime.NotifyRuntimeShutdown("common-boundary");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeDeclaredProviderCanExportItsRenamedEntryAssembly()
        {
            var own = new OptionalPackageFixture(); var provider = new OptionalPackageFixture();
            const string payload = "DTMAPI.DemandPayloadFixture";
            File.Copy(Path.Combine(AppContext.BaseDirectory, "demand-fixtures", payload + ".dll"), provider.PathFor("Provider.dll"));
            var providerManifest = provider.Manifest; providerManifest.UniqueID = "Boundary.Provider"; providerManifest.EntryDll = "Provider.dll";
            var ownManifest = own.Manifest;
            ownManifest.DependencyContract = PackageDependencyContract.ReadManifest(Encoding.UTF8.GetBytes(
                "{\"UniqueID\":\"Optional.Package\",\"Dependencies\":[{\"UniqueID\":\"Boundary.Provider\"}]}"));
            providerManifest.Version = "author preview";
            DiscoveredMod Mod(ManifestModel manifest, OptionalPackageFixture fixture) => new DiscoveredMod(manifest, fixture.Root, fixture.ManifestPath, "Local", null, true, false, "Local." + manifest.UniqueID, true, "");
            using var loader = new DependencyAssemblyLoader(new[] { Mod(ownManifest, own), Mod(providerManifest, provider) },
                Array.Empty<System.Collections.Generic.KeyValuePair<string, string>>(), _ => { }, own.Root);
            var exported = loader.LoadEntry(providerManifest.UniqueID, provider.PathFor("Provider.dll"));
            var consumer = loader.LoadEntry(ownManifest.UniqueID, own.EntryPath);
            var bound = loader.ResolveRequest(new System.Reflection.AssemblyName(payload), consumer);
            Assert(loader.GetBindings()[bound!].Contains(providerManifest.UniqueID) && loader.GetBindings()[bound!].Contains(ownManifest.UniqueID),
                "The actual provider and consumer binding both retain ownership of the shared assembly.");
            Assert(ReferenceEquals(bound, exported) && (string)bound!.GetType("DTMAPI.DemandPayloadFixture.Payload")!.GetMethod("Read")!.Invoke(null, null)! == "loaded-on-demand",
                "A declared provider's actual entry assembly is available even when its filename differs, without parsing an unconstrained display version.");
        }

        private static void RuntimeDynamicRequestSelectsOnlyDemandedDll()
        {
            var fixture = new OptionalPackageFixture();
            const string entry = "DTMAPI.DemandEntryFixture";
            const string payload = "DTMAPI.DemandPayloadFixture";
            File.Copy(Path.Combine(AppContext.BaseDirectory, "demand-fixtures", entry + ".dll"), fixture.PathFor(entry + ".dll"));
            Directory.CreateDirectory(fixture.PathFor("lib/private"));
            File.Copy(Path.Combine(AppContext.BaseDirectory, "demand-fixtures", payload + ".dll"), fixture.PathFor("lib/private/" + payload + ".dll"));
            File.WriteAllText(fixture.PathFor("lib/private/Unused.dll"), "invalid unused DLL");
            var manifest = fixture.Manifest; manifest.EntryDll = entry + ".dll";
            var mod = new DiscoveredMod(manifest, fixture.Root, fixture.ManifestPath, "Local", null, true, false, "Local.Demand", true, "");
            using var loader = new DependencyAssemblyLoader(new[] { mod }, Array.Empty<System.Collections.Generic.KeyValuePair<string, string>>(), _ => { }, fixture.Root);
            var code = loader.LoadEntry(manifest.UniqueID, fixture.PathFor(entry + ".dll"));
            Assert(loader.GetBindings().Count == 1, "Entry binding does not eagerly load other DLLs.");
            Assert(loader.ResolveRequest(new System.Reflection.AssemblyName(payload), null) == null,
                "An unattributed request must not select a candidate from an unrelated package.");
            var resolveWithoutRequester = new Func<System.Reflection.Assembly>(() => loader.ResolveRequest(new System.Reflection.AssemblyName(payload), null)!);
            string monoResult = (string)code.GetType("DTMAPI.DemandEntryFixture.Entry")!.GetMethod("RunWithoutRequester")!.Invoke(null, new object[] { resolveWithoutRequester })!;
            Assert(monoResult == "loaded-on-demand" && loader.GetBindings().Count == 2, "Mono requests without RequestingAssembly are attributed to the actual package caller.");
            string result = (string)code.GetType("DTMAPI.DemandEntryFixture.Entry")!.GetMethod("Run")!.Invoke(null, null)!;
            Assert(result == "loaded-on-demand" && loader.GetBindings().Keys.Any(a => a.GetName().Name == payload), "A later Assembly.Load request binds the actual payload without changing the Mod dependency plan.");
            var loadMemory = new Func<System.Reflection.Assembly>(() => System.Reflection.Assembly.Load(File.ReadAllBytes(fixture.PathFor("lib/private/" + payload + ".dll"))));
            code.GetType("DTMAPI.DemandEntryFixture.Entry")!.GetMethod("RunWithoutRequester")!.Invoke(null, new object[] { loadMemory });
            Assert(loader.GetBindings().Any(pair => pair.Key.Location.Length == 0 && pair.Value.Contains(manifest.UniqueID)),
                "Memory assemblies are attributed to the actual caller without requiring Location or a disk digest.");
        }

        private static void RuntimeDeferredModulesBindOnlyOnActivationAndDetectResidentConflict()
        {
            const string library = "DTMAPI.LegacyNativeHelperFixture";
            var fixture = new OptionalPackageFixture();
            string libraryPath = fixture.PathFor("lib/private/" + library + ".dll");
            Directory.CreateDirectory(Path.GetDirectoryName(libraryPath)!);
            File.Copy(Path.Combine(AppContext.BaseDirectory, library + ".dll"), libraryPath);
            var manifest = fixture.Manifest;
            var mod = new DiscoveredMod(manifest, fixture.Root, fixture.ManifestPath, "Local", null, true, false, "Local.Test", true, "");
            using var loader = new DependencyAssemblyLoader(new[] { mod }, Array.Empty<System.Collections.Generic.KeyValuePair<string, string>>(), _ => { }, fixture.Root);
            Assert(loader.GetBindings().Count == 0, "Candidate discovery leaves deferred implementation uninspected and unloaded.");
            var implementation = loader.LoadDeclaredAssembly(manifest.UniqueID, library);
            string result = (string)implementation.GetType("DTMAPI.LegacyNativeHelperFixture.LegacyNativeHelper")!.GetMethod("Describe")!.Invoke(null, new object[] { "activation" })!;
            Assert(result == "legacy-helper-ready:activation" && loader.GetBindings().Count == 1, "Module activation binds exactly the requested implementation.");
            var replacement = new OptionalPackageFixture();
            string replacementPath = replacement.PathFor("lib/private/" + library + ".dll");
            Directory.CreateDirectory(Path.GetDirectoryName(replacementPath)!);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "conflicting-fixture", library + ".dll"), replacementPath);
            var second = new DiscoveredMod(replacement.Manifest, replacement.Root, replacement.ManifestPath, "Local", null, true, false, "Local.Test", true, "");
            using var next = new DependencyAssemblyLoader(new[] { second }, Array.Empty<System.Collections.Generic.KeyValuePair<string, string>>(), _ => { }, replacement.Root);
            bool conflict = false;
            try
            {
                var resident = next.LoadDeclaredAssembly(manifest.UniqueID, library);
                Assert(ReferenceEquals(resident, implementation), "The host's actual resident binding is recorded.");
                var requestedMethod = resident.GetType("DTMAPI.LegacyNativeHelperFixture.LegacyNativeHelper")!
                    .GetMethod("ReplacementOnly") ?? throw new MissingMethodException("ReplacementOnly");
                requestedMethod.Invoke(null, null);
            }
            catch (Exception error) when (error is FileLoadException || error is MissingMethodException) { conflict = true; }
            Assert(conflict, "An actually requested method absent from the non-replaceable resident implementation needs a restart; an unused candidate does not.");
        }

        private static void RuntimeCandidatesIgnoreUnusedDllsAndBrokenProofs()
        {
            var fixture = new OptionalPackageFixture();
            Directory.CreateDirectory(fixture.PathFor("lib/private"));
            File.WriteAllText(fixture.PathFor("lib/private/Unused.dll"), "not a PE");
            File.WriteAllText(fixture.PathFor("dtmapi-dependencies.json"), "{broken old proof");
            File.WriteAllText(fixture.PathFor("dtmapi-native-build.json"), "{broken old proof");
            using var transaction = new StartupPackageReadTransaction();
            RuntimePackage package = transaction.Read(fixture.Manifest, fixture.Root, fixture.ManifestPath);
            Assert(package.HasCandidate("Unused"), "Discovery records paths without inspecting or hashing candidate DLLs.");
            var entry = package.Select(fixture.EntryName);
            Assert(entry.Role == "entry", "Only the selected entry participates in the plan.");
            File.WriteAllText(fixture.PathFor("lib/private/Unused.dll"), "a completely different unused file");
            Assert(ReferenceEquals(package, transaction.Read(fixture.Manifest, fixture.Root, fixture.ManifestPath)), "Unrelated edits do not revoke startup admission.");
            bool failed = false;
            try { package.ReadCandidate(package.Select("Unused")); } catch (Exception ex) when (ex is BadImageFormatException || ex is InvalidDataException || ex is IOException) { failed = true; }
            Assert(failed, "The invalid DLL fails only when selected; it never becomes a selected assembly.");
        }

        private static void RuntimeSelectedBytesStillProtectClrBinding()
        {
            var fixture = new OptionalPackageFixture();
            RuntimePackage package = new StartupPackageReadTransaction().Read(fixture.Manifest, fixture.Root, fixture.ManifestPath);
            var entry = package.Select(fixture.EntryName);
            File.WriteAllBytes(fixture.EntryPath, File.ReadAllBytes(fixture.EntryPath).Concat(new byte[] { 0 }).ToArray());
            var bound = System.Reflection.Assembly.LoadFrom(fixture.EntryPath);
            Assert(bound.GetType(typeof(AtomicOwnerProbeMod).FullName!) != null, "An appended byte does not change the compiled implementation or invalidate a prior candidate observation.");
        }

        private static void RuntimeCandidatePriorityUsesVisiblePathsAndReportsOnlyRequestedAmbiguity()
        {
            foreach (bool reverse in new[] { false, true })
            {
                var fixture = new OptionalPackageFixture();
                var manifest = fixture.Manifest;
                Directory.CreateDirectory(fixture.PathFor("bin"));
                File.Copy(fixture.EntryPath, fixture.PathFor("bin/Entry.dll"));
                manifest.EntryDll = "bin/Entry.dll";
                manifest.AssemblyModels = new System.Collections.Generic.List<ManifestAssemblyModel> { new ManifestAssemblyModel { Path = "extra/Choice.dll", Role = "shared-contract" } };
                string[] paths = { "bin/Choice.dll", "Choice.dll", "lib/private/a/Choice.dll", "lib/private/b/Choice.dll", "lib/shared/Choice.dll", "extra/Choice.dll" };
                foreach (string path in reverse ? paths.Reverse() : paths)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fixture.PathFor(path))!);
                    File.Copy(fixture.EntryPath, fixture.PathFor(path));
                }
                RuntimePackage Package() => new RuntimePackage(manifest, fixture.Root);
                var package = Package();
                Assert(package.Select("Choice", requesterPath: fixture.PathFor("lib/private/b/Caller.dll")).Path == "lib/private/b/Choice.dll",
                    "The actual requester's directory takes precedence regardless of discovery order.");
                Assert(Package().Select("Choice").Path == "bin/Choice.dll", "Entry directory precedes the package root.");
                Assert(Package().Select("Choice", sharedOnly: true, requesterPath: fixture.PathFor("lib/private/b/Caller.dll")).Path == "lib/shared/Choice.dll",
                    "Visibility filters out private files before applying nearby path priority.");
                File.Delete(fixture.PathFor("bin/Choice.dll"));
                Assert(Package().Select("Choice").Path == "Choice.dll", "Package root precedes library subtrees.");
                File.Delete(fixture.PathFor("Choice.dll"));
                package = Package();
                package.SelectEntry();
                try { package.Select("Choice"); throw new Exception("Ambiguous request selected an arbitrary file."); }
                catch (InvalidDataException ex)
                {
                    Assert(ex.Message.Contains("dependency-candidate-ambiguous") && ex.Message.Contains("lib/private/a/Choice.dll") && ex.Message.Contains("lib/private/b/Choice.dll"),
                        "Only the requested same-level ambiguity is reported with both paths.");
                }
                File.Delete(fixture.PathFor("lib/private/b/Choice.dll"));
                manifest.AssemblyModels.Add(new ManifestAssemblyModel { Path = "lib/private/a/../a/Choice.dll", Role = "private-managed" });
                Assert(Package().Select("Choice").Path == "lib/private/a/Choice.dll", "Normalized path aliases do not create extra candidates; private precedes shared.");
                File.Delete(fixture.PathFor("lib/private/a/Choice.dll"));
                manifest.AssemblyModels.RemoveAt(manifest.AssemblyModels.Count - 1);
                Assert(Package().Select("Choice").Path == "lib/shared/Choice.dll", "Shared subtree precedes other declared paths.");
                File.Delete(fixture.PathFor("lib/shared/Choice.dll"));
                Assert(Package().Select("Choice").Path == "extra/Choice.dll", "An extra declared location remains available when earlier layers are empty.");
            }
        }

        private static void RuntimeProviderVisibilityAndAmbiguityStayRequestScoped()
        {
            foreach (bool reverse in new[] { false, true })
            {
                var consumer = new OptionalPackageFixture();
                var first = new OptionalPackageFixture(); var second = new OptionalPackageFixture();
                var a = first.Manifest; a.UniqueID = "Visible.First";
                var b = second.Manifest; b.UniqueID = "Visible.Second";
                var own = consumer.Manifest;
                own.DependencyContract = PackageDependencyContract.ReadManifest(Encoding.UTF8.GetBytes(
                    "{\"UniqueID\":\"Optional.Package\",\"Dependencies\":[{\"UniqueID\":\"" + (reverse ? b.UniqueID : a.UniqueID) + "\"},{\"UniqueID\":\"" + (reverse ? a.UniqueID : b.UniqueID) + "\"}]}"));
                foreach (var fixture in new[] { first, second })
                {
                    Directory.CreateDirectory(fixture.PathFor("lib/shared"));
                    Directory.CreateDirectory(fixture.PathFor("lib/private"));
                    File.WriteAllText(fixture.PathFor("lib/shared/Ambiguous.dll"), "Never read: multiple providers are ambiguous.");
                    File.WriteAllText(fixture.PathFor("lib/private/Hidden.dll"), "Not visible to other Mods.");
                }
                DiscoveredMod Mod(ManifestModel value, OptionalPackageFixture fixture) => new DiscoveredMod(value, fixture.Root, fixture.ManifestPath, "Local", null, true, false, "Local." + value.UniqueID, true, "");
                var mods = new[] { Mod(own, consumer), Mod(a, first), Mod(b, second) };
                using var loader = new DependencyAssemblyLoader(reverse ? mods.Reverse() : mods, Array.Empty<System.Collections.Generic.KeyValuePair<string, string>>(), _ => { }, consumer.Root);
                var entry = loader.LoadEntry(own.UniqueID, consumer.EntryPath);
                Assert(loader.ResolveRequest(new System.Reflection.AssemblyName("Hidden"), entry) == null, "Declaring a provider does not expose its private subtree.");
                try { loader.ResolveRequest(new System.Reflection.AssemblyName("Ambiguous"), entry); throw new Exception("Multiple providers were silently ordered."); }
                catch (InvalidDataException ex) { Assert(ex.Message.Contains("dependency-provider-ambiguous") && ex.Message.Contains(a.UniqueID) && ex.Message.Contains(b.UniqueID), "Provider ambiguity does not depend on dependency/discovery order."); }
                Assert(loader.GetBindings().Count == 1 && loader.Plan.Blocked.Count == 0, "A failed actual request does not bind candidates or block unrelated planned Owners.");
            }
        }

        private static void RuntimeMissingRequesterRetainsNestedCallerDirectory()
        {
            const string caller = "DTMAPI.DemandCallerFixture", payload = "DTMAPI.DemandPayloadFixture";
            var fixture = new OptionalPackageFixture();
            Directory.CreateDirectory(fixture.PathFor("lib/private/nested"));
            foreach (string name in new[] { caller, payload })
                File.Copy(Path.Combine(AppContext.BaseDirectory, "demand-fixtures", name + ".dll"), fixture.PathFor("lib/private/nested/" + name + ".dll"));
            File.WriteAllText(fixture.PathFor(payload + ".dll"), "The entry-directory candidate must not shadow the actual nested caller.");
            var manifest = fixture.Manifest;
            var mod = new DiscoveredMod(manifest, fixture.Root, fixture.ManifestPath, "Local", null, true, false, "Local.NestedCaller", true, "");
            using var loader = new DependencyAssemblyLoader(new[] { mod }, Array.Empty<System.Collections.Generic.KeyValuePair<string, string>>(), _ => { }, fixture.Root);
            var bridge = loader.LoadDeclaredAssembly(manifest.UniqueID, caller);
            var resolve = new Func<System.Reflection.Assembly>(() => loader.ResolveRequest(new System.Reflection.AssemblyName(payload), null)!);
            string result = (string)bridge.GetType(caller + ".Caller")!.GetMethod("ReadWithoutRequester")!.Invoke(null, new object[] { resolve })!;
            Assert(result == "loaded-on-demand", "Missing RequestingAssembly retains the actual calling assembly, not only its Owner.");
        }

        private static void RuntimeManifestUsesInfoAndOptionalDependencies()
        {
            var fixture = new OptionalPackageFixture();
            File.WriteAllText(fixture.ManifestPath, "{\"ManifestVersion\":2,\"UniqueID\":\"Optional.Package\",\"Type\":\"CodeMod\",\"EntryDll\":\"" + Path.GetFileName(fixture.EntryPath) + "\",\"Version\":\"9.9.9\",\"Name\":\"obsolete\"}");
            File.WriteAllText(fixture.PathFor("info.json"), "{\"name\":\"Actual name\",\"author\":\"Actual author\",\"version\":\"2.3.4\"}");
            var manifest = fixture.Manifest;
            Assert(manifest.Version == "2.3.4" && manifest.Name == "Actual name" && manifest.DependencyContract!.Dependencies.Count == 0,
                "V2 reads presentation/version from info.json and supports an absent Dependencies field.");
            File.Delete(fixture.PathFor("info.json"));
            Assert(fixture.Manifest.Version == "" && fixture.Manifest.Name == "Optional.Package", "Missing display data does not invent a version or fail code admission.");
            foreach (int format in new[] { 0, 2 })
                foreach (string selector in new[] { "2", "999", "\"damaged\"", "null", "{}" })
                {
                    File.WriteAllText(fixture.ManifestPath, "{\"ManifestVersion\":" + format + ",\"UniqueID\":\"Optional.Package\",\"Type\":\"CodeMod\",\"EntryDll\":\"" + Path.GetFileName(fixture.EntryPath) + "\",\"DependencyContractVersion\":" + selector + "}");
                    Assert(fixture.Manifest.DependencyContract!.Dependencies.Count == 0, "Historical selector values do not gate either supported manifest format.");
                }
        }

        private static void RuntimeCandidateTransactionEndsWithoutPersistentCertificate()
        {
            var fixture = new OptionalPackageFixture();
            var transaction = new StartupPackageReadTransaction();
            transaction.Read(fixture.Manifest, fixture.Root, fixture.ManifestPath);
            transaction.Dispose(); transaction.Dispose();
            bool disposed = false;
            try { transaction.Read(fixture.Manifest, fixture.Root, fixture.ManifestPath); } catch (ObjectDisposedException) { disposed = true; }
            Assert(disposed, "Disposed startup candidate state cannot be reused.");
            File.WriteAllText(fixture.PathFor("Later.dll"), "later optional module");
            using var next = new StartupPackageReadTransaction();
            Assert(next.Read(fixture.Manifest, fixture.Root, fixture.ManifestPath).HasCandidate("Later"), "A later startup discovers current files without any receipt renewal.");
        }

        private sealed class OptionalPackageFixture
        {
            internal string Root { get; } = Path.Combine(DtmApiTestSession.Current.RootPath, "optional-package-" + Guid.NewGuid().ToString("N"));
            internal string EntryName => Path.GetFileNameWithoutExtension(typeof(AtomicOwnerProbeMod).Assembly.Location);
            internal string EntryPath => PathFor(Path.GetFileName(typeof(AtomicOwnerProbeMod).Assembly.Location));
            internal string ManifestPath => PathFor("manifest.json");
            internal ManifestModel Manifest => new ManifestReader().Read(ManifestPath);
            internal string PathFor(string relative) => Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
            internal OptionalPackageFixture()
            {
                Directory.CreateDirectory(Root);
                File.Copy(typeof(AtomicOwnerProbeMod).Assembly.Location, EntryPath);
                File.WriteAllText(ManifestPath, "{\"UniqueID\":\"Optional.Package\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"CodeModKind\":\"Advanced\",\"NativeContractVersion\":1,\"DependencyContractVersion\":1,\"EntryDll\":\"" + Path.GetFileName(EntryPath) + "\"}");
            }
        }
    }
}
