using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AnimalContent;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.RuntimeModules.Animals;
using DTMAPI.RuntimeModules.Content;

namespace DTMAPI.UnitTests
{
    internal static class AnimalModuleTests
    {
        internal static void Run()
        {
            using var host = new Fixture(); var a = host.Owner("A"); var b = host.Owner("B");
            var api = a.GetApi<IAnimalContentApi>(AnimalModule.ProviderId)!;
            var other = b.GetApi<IAnimalContentApi>(AnimalModule.ProviderId)!;
            Check(host.Created == 0, "A queried API must remain cold.");
            var bad = Png("missing"); bad.Definitions[0].FrameManifest = "missing.json";
            Reject(() => api.Register(bad));
            bad = Png("missing-prefix"); bad.Definitions[0].TemplateSpritePrefix = "";
            Reject(() => api.Register(bad));
            bad = Png("bad-mode"); bad.Definitions[0].AnimatorMode = "unknown";
            Reject(() => api.Register(bad));
            Check(host.Created == 0, "Invalid input must not acquire native demand.");

            var input = Png("hatch"); var first = api.Register(input);
            input.Definitions[0].SpeciesId = "mutated";
            Check(host.Native.Active.Single().Rows[0].SpeciesId == "hatch", "Accepted definitions are immutable copies.");
            Reject(() => api.Register(Png("hatch")));
            Reject(() => api.Register(new AnimalDefinitionBatch { Definitions = new[] { Png("free").Definitions[0], Png("hatch").Definitions[0] } }));
            Check(host.Native.Active.Count() == 1, "A conflicting batch cannot partially commit.");
            var sibling = api.Register(Png("sibling"));
            var independent = other.Register(Png("other"));
            first.Dispose();
            Check(host.Scopes.CountContributions("A") == 1 && host.Native.Active.Count() == 2, "Single release must clear its scope and preserve sibling and other Mod.");
            var replacement = api.Register(Png("hatch")); first.Dispose();
            Check(replacement.GetSnapshot().State == AnimalRegistrationState.Ready, "An old released handle cannot revoke a reused identity.");
            Check(host.Scopes.PrepareContributionsExit("A", out _), "Mod exit releases all its accepted definitions.");
            Check(host.Native.Active.Count() == 1 && independent.GetSnapshot().State == AnimalRegistrationState.Ready, "Mod exit cannot release another Mod.");
            host.Native.OnRegister = () => host.Scopes.PrepareContributionsExit("A", out _);
            Reject(() => api.Register(Png("reentrant"))); host.Native.OnRegister = null;
            Check(host.Native.Active.Count() == 1 && host.Scopes.CountContributions("A") == 0, "Exit inside native activation rolls back the late result and provider use.");
            using var reused = api.Register(Png("reentrant"));
            Check(host.Created == 1, "Successful drain allows the same backend and IDs to be reused.");
            Check(AnimalNativeRules.MapSprite("anim_animal_chicken", "anim_animal_hatch", "anim_animal_chicken_child_walk_2") == "anim_animal_hatch_young_walk_2", "Child mapping preserves the frame convention.");
            Check(AnimalNativeRules.MapSprite("anim_animal_goat", "anim_animal_drecko", "anim_animal_goat_adult_jump_ready_2") == "anim_animal_drecko_adult_jump_0", "Jump preparation uses the established frame mapping.");
            Check(AnimalNativeRules.MapSprite("a", "b", "prefix_a_frame") == "", "An unrelated sprite name falls through.");
            BundleDefinitions(host, api, other);
            FileSource(host);
        }
        private static AnimalDefinitionBatch Png(string species) => new AnimalDefinitionBatch
        {
            Definitions = new[] { new AnimalDefinition
            {
                SpeciesId = species, AnimatorMode = "pngSpriteOverride",
                TemplateSpritePrefix = "anim_animal_chicken", CustomSpritePrefix = "anim_animal_" + species
            } }
        };
        private static void BundleDefinitions(Fixture host, IAnimalContentApi api, IAnimalContentApi other)
        {
            File.WriteAllBytes(Path.Combine(host.Root, "animals.bundle"), new byte[] { 0 });
            AnimalDefinitionBatch Bundle(string species, string key) => new AnimalDefinitionBatch
            {
                Definitions = new[] { new AnimalDefinition
                {
                    SpeciesId = species, AnimatorMode = "assetBundle", TemplateSpeciesId = "goat",
                    AnimatorBundle = "animals.bundle", AdultAnimatorKey = key, AdultAnimatorAsset = "Adult"
                } }
            };
            var malformed = Bundle("unpaired", "unpaired_anim"); malformed.Definitions[0].AdultAnimatorAsset = "";
            Reject(() => api.Register(malformed));
            malformed = Bundle("no-key", ""); malformed.Definitions[0].AdultAnimatorAsset = "";
            Reject(() => api.Register(malformed));
            using var bundle = api.Register(Bundle("bundle", "bundle_anim"));
            Reject(() => other.Register(Bundle("other-bundle", "bundle_anim")));
            Check(bundle.GetSnapshot().State == AnimalRegistrationState.Ready, "A bundle key conflict keeps the original registration.");
            host.Native.Fail = true;
            Reject(() => other.Register(Bundle("failed", "failed_anim")));
            host.Native.Fail = false;
            using var recovered = other.Register(Bundle("failed", "failed_anim"));
            Check(recovered.GetSnapshot().State == AnimalRegistrationState.Ready, "Failed native admission releases species and animator reservations.");
        }
        private static void FileSource(Fixture host)
        {
            var owner = host.Owner("File"); var api = owner.GetApi<IAnimalContentApi>(AnimalModule.ProviderId)!;
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string path = Path.Combine(host.Root, "PackData", "content.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            void Write(AnimalDefinitionBatch body) => File.WriteAllText(path, JsonSerializer.Serialize(new { schemaVersion = 1, animals = body, audio = new { invalid = true } }, options));
            var malformed = Png("file"); malformed.Definitions[0].CustomSpritePrefix = ""; Write(malformed);
            using (var invalid = RuntimeContent.Prepare(owner)) invalid.Activate();
            Check(!host.Native.Active.Any(r => r.Rows[0].SpeciesId == "file"), "An initial file with missing PNG prefixes is rejected.");
            malformed = Png("file"); malformed.Definitions[0].AnimatorMode = "unknown"; Write(malformed);
            using (var invalid = RuntimeContent.Prepare(owner)) invalid.Activate();
            Check(!host.Native.Active.Any(r => r.Rows[0].SpeciesId == "file"), "An initial file with an unknown mode is rejected.");
            Write(Png("file"));
            using var source = RuntimeContent.Prepare(owner); source.Activate();
            Check(host.Native.Active.Any(r => r.Rows[0].SpeciesId == "file"), "Invalid sound cannot discard a valid animal body.");
            Reject(() => api.Register(Png("file")));
            source.Dispose(); using var reopened = api.Register(Png("file"));
            Check(reopened.GetSnapshot().State == AnimalRegistrationState.Ready, "File withdrawal releases exclusivity for the direct API.");
            Check(host.Created == 1, "Non-audio animal files create no Audio backend.");
        }
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Reject(Action action) { try { action(); } catch { return; } throw new Exception("Expected rejected candidate."); }
        private sealed class Fixture : IRuntimeModuleContext, IDisposable
        {
            private readonly AnimalService service = new AnimalService();
            internal readonly OwnerDependencyStore Scopes = new OwnerDependencyStore();
            internal readonly Backend Native = new Backend(); internal int Created;
            internal readonly string Root = Path.Combine(Path.GetTempPath(), "dtmapi-animal-module-" + Guid.NewGuid().ToString("N"));
            private Func<IRuntimeModuleOwnerContext, object> factory = null!;
            internal Fixture() { Directory.CreateDirectory(Root); service.Register(this); }
            internal OwnerContext Owner(string id) => new OwnerContext(this, id);
            public string ProviderId => AnimalModule.ProviderId;
            public IMonitor Monitor { get; } = new MonitorStub();
            public IDtmRuntimeContext Runtime { get; } = new Context();
            public IDtmScheduler Scheduler => throw new NotSupportedException();
            public string ComponentDirectory => Root; public string CacheDirectory => Root;
            public void AuthorizePatches(Action install) => install();
            public T ReadConfig<T>() where T : new() => new T();
            public void RegisterApi<T>(Func<IRuntimeModuleOwnerContext, T> create) where T : class => factory = owner => create(owner);
            public T CreateBackend<T>(string component, params object[] arguments) where T : class { Created++; return (T)(object)Native; }
            public void PublishStatus(string status, string detail) { }
            public void Dispose() { Check(Scopes.PrepareContributionsExit(ProviderId, out _), "Scope cleanup"); Check(service.TryClose().Released, "Module cleanup"); Directory.Delete(Root, true); }
            internal sealed class OwnerContext : IRuntimeModuleOwnerContext
            {
                private readonly Fixture host;
                internal OwnerContext(Fixture host, string id) { this.host = host; Manifest = new ManifestModel { UniqueID = id }; Resources = new ModResourceService(host.Root, Guard); Files = new OwnerFileService(host.Root, Guard); }
                public IManifest Manifest { get; }
                public int LoadOrder => 1; public int OfficialOrder => 1;
                public IModResourceHelper Resources { get; } public IOwnerFileHelper Files { get; }
                public void Guard() { }
                public PackageResourceReference Reference(string key) => ((ModResourceService)Resources).BackgroundReference(key, Manifest.UniqueID);
                public T? GetApi<T>(string provider) where T : class => provider == AnimalModule.ProviderId ? (T)host.factory(this) : null;
                public bool HasRequiredDependency(string provider) => false;
                public void Report(string section, string message) { }
                public IRuntimeContribution CreateContribution(string id) => host.Scopes.CreateContribution(Manifest.UniqueID, id, Guard);
            }
        }
        private sealed class Backend : IAnimalBackend
        {
            private readonly List<Registration> all = new List<Registration>();
            internal IEnumerable<Registration> Active => all.Where(r => !r.Closed);
            internal Action? OnRegister; internal bool Fail;
            public IAnimalRegistration Register(AnimalDefinition[] rows, Func<string, PackageResourceReference> resource, Action guard)
            { if (Fail) throw new InvalidOperationException("native admission failure"); var value = new Registration(rows); all.Add(value); OnRegister?.Invoke(); return value; }
            public IDisposable Register(string owner, int order, AnimalVoiceBinding[] voices, Action guard) => throw new NotSupportedException();
            public void Dispose() { foreach (var row in all) row.Dispose(); }
        }
        private sealed class Registration : IAnimalRegistration
        {
            internal readonly AnimalDefinition[] Rows; internal bool Closed;
            internal Registration(AnimalDefinition[] rows) { Rows = rows; }
            public AnimalRegistrationSnapshot GetSnapshot() => new AnimalRegistrationSnapshot(Closed ? AnimalRegistrationState.Released : AnimalRegistrationState.Ready);
            public void Dispose() { Closed = true; }
        }
        private sealed class MonitorStub : IMonitor
        {
            public void Log(string message, LogLevel level = LogLevel.Info) { }
            public void LogOnce(string key, string message, LogLevel level = LogLevel.Info) { }
            public void LogException(Exception exception, string message) { }
        }
        private sealed class Context : IDtmRuntimeContext
        {
            public bool IsMainThread => true;
            public DtmRuntimeContextSnapshot Snapshot => new DtmRuntimeContextSnapshot(Guid.Empty, 1, DtmRuntimePhase.WorldReady, 1, 1);
            public IDisposable Subscribe(Action<DtmRuntimeContextSnapshot> changed) => throw new NotSupportedException();
        }
    }
}
