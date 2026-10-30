using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void VersionedConfigValidatesBeforeCommitAndMigratesOnlyOnce()
        {
            var paths = new RuntimePaths(NewTempGameDir(), NewTempGameDir());
            var config = new ConfigService(paths, new DiagnosticsService(paths));
            var manifest = new ManifestModel { UniqueID = "Author.Config", Name = "Config", Version = "1.0.0", Author = "Test" };
            config.WriteConfig(manifest, new GlobalProbeData { Value = 7 });
            byte[] legacy = File.ReadAllBytes(config.GetConfigPath(manifest));
            var service = config.CreateVersioned(manifest.UniqueID, () => { });
            Assert(service.Read<GlobalProbeData>(1).Status == OwnerDataStatus.Missing, "New config must distinguish missing and never silently import legacy config.");
            Assert(service.Write(new GlobalProbeData { Value = 1 }, 1).Status == OwnerDataStatus.Found, "Initial config must commit.");
            string file = Directory.GetFiles(Path.Combine(paths.ConfigPath, "versioned"), "settings.json", SearchOption.AllDirectories).Single();
            byte[] original = File.ReadAllBytes(file);
            int migrations = 0;
            service.RegisterMigration<GlobalProbeData>(1, value => { migrations++; value.Value += 2; return value; });
            var rejected = service.Read<GlobalProbeData>(2, value => value.Value > 2 ? "Too high" : null);
            Assert(rejected.Status == OwnerDataStatus.ValidationFailed && File.ReadAllBytes(file).SequenceEqual(original), "Rejected migrated candidate must preserve original bytes.");
            var accepted = service.Read<GlobalProbeData>(2, value => null);
            Assert(accepted.Value!.Value == 3 && migrations == 2, "Failed migration candidates may retry; successful migration must persist schema.");
            byte[] committed = File.ReadAllBytes(file);
            var again = service.Read<GlobalProbeData>(2, value => { value.Value = 999; return null; });
            Assert(again.Value!.Value == 3 && migrations == 2 && File.ReadAllBytes(file).SequenceEqual(committed), "Repeated read must not rerun migration or expose validator mutation.");
            Assert(service.Write(new GlobalProbeData { Value = 9 }, 2, _ => throw new Exception("validator")).Status == OwnerDataStatus.ValidationFailed && File.ReadAllBytes(file).SequenceEqual(committed), "Validator exception must preserve committed config.");
            Assert(service.Write(new GlobalProbeData { Value = 0 }, 1).Status == OwnerDataStatus.UnsupportedSchema, "Old author schema must not overwrite future config.");
            service.RegisterMigration<GlobalProbeData>(2, _ => throw new Exception("migration"));
            Assert(service.Read<GlobalProbeData>(3).Status == OwnerDataStatus.MigrationFailed && File.ReadAllBytes(file).SequenceEqual(committed), "Migration exception must preserve original.");
            Assert(File.ReadAllBytes(config.GetConfigPath(manifest)).SequenceEqual(legacy), "Versioned config must not touch legacy config.");
            File.WriteAllText(file, "{broken");
            byte[] corrupt = File.ReadAllBytes(file);
            Assert(service.Read<GlobalProbeData>(2).Status == OwnerDataStatus.Corrupt && service.Write(new GlobalProbeData(), 2).Status == OwnerDataStatus.Corrupt && File.ReadAllBytes(file).SequenceEqual(corrupt), "Corrupt config must remain fail-closed without default overwrite.");
            ((IDisposable)service).Dispose();
            AssertThrows(() => service.Read<GlobalProbeData>(2), "Closed versioned config must reject further reads.");
        }

        private static void InputDiagnosticsDescribeOwnerFocusSuppressionAndPotentialConflicts()
        {
            var input = new InputService(() => true);
            var a = input.CreateOwnerBound("Author.A");
            var b = input.CreateOwnerBound("Author.B");
            using var one = a.RegisterKeybind("one", "Control+F8", DtmInputScope.Gameplay);
            using var two = b.RegisterKeybind("two", "LeftControl+F8", DtmInputScope.SaveLoaded);
            using var other = b.RegisterKeybind("other", "Control+F9", DtmInputScope.Gameplay);
            using var title = b.RegisterKeybind("title", "Control+F8", DtmInputScope.Title);
            var diagnostics = input.CreateDiagnostics("Author.A", () => { });
            Assert(diagnostics.GetRegistrations().Count == 1, "Owner inventory must not return another owner's registrations.");
            var conflicts = diagnostics.FindConflicts(DtmKeybindList.Parse("Control+F8"), DtmInputScope.Gameplay);
            Assert(conflicts.Count == 2 && conflicts.All(v => v.Id != "other" && v.Id != "title"), "Conflict query must match generic modifiers and compatible scopes without treating shared modifier alone as collision.");
            input.GetButtonsToSample(InputAudienceSnapshot.Normal(DtmInputScope.Gameplay));
            a.Suppress("F8");
            Assert(diagnostics.GetRegistrations()[0].Suppressed, "Read-only diagnostic must reflect existing suppression routing.");
            input.ClearFrame();
            input.GetButtonsToSample(new InputAudienceSnapshot(InputAudienceMode.PlatformModal, "", DtmInputScope.SaveLoaded, 1));
            Assert(diagnostics.Snapshot.Audience == DtmInputAudience.PlatformModal && !diagnostics.GetRegistrations()[0].Eligible, "Platform focus must remain ineligible for author input.");
            input.ClearFrame();
            input.GetButtonsToSample(new InputAudienceSnapshot(InputAudienceMode.OwnerModal, "Author.B", DtmInputScope.SaveLoaded, 2));
            Assert(diagnostics.Snapshot.FocusOwnerId == "Author.B" && !diagnostics.GetRegistrations()[0].Eligible, "Another owner's focused UI must not enable this owner.");
            input.ClearTransientState();
            Assert(diagnostics.GetRegistrations()[0].RequiresNeutral, "Title/save boundary must require physical neutral before rearming held keybinds.");
            two.Dispose();
            Assert(diagnostics.FindConflicts(DtmKeybindList.Parse("Control+F8"), DtmInputScope.Gameplay).Count == 1, "Disposed registration must disappear from conflict query.");
        }

        private static void OwnerTranslationsFormatRefreshAndDetachWithScope()
        {
            string? oldLanguage = Environment.GetEnvironmentVariable("DTMAPI_LANGUAGE");
            string? oldUiLanguage = Environment.GetEnvironmentVariable("DTMAPI_UI_LANGUAGE");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_LANGUAGE", "auto");
                Environment.SetEnvironmentVariable("DTMAPI_UI_LANGUAGE", "auto");
                string root = NewTempGameDir();
                string i18n = Path.Combine(root, "i18n"); Directory.CreateDirectory(i18n);
                File.WriteAllText(Path.Combine(i18n, "english.json"), "{\"hello\":\"Hello {name}; {missing}; {{literal}}\",\"fallback\":\"English\"}");
                File.WriteAllText(Path.Combine(i18n, "fr_CA.json"), "{\"hello\":\"Bonjour {name}\"}");
                File.WriteAllText(Path.Combine(i18n, "fr_FR.json"), "{\"hello\":\"Salut {name}\"}");
                var source = new RuntimeLanguageSource(() => "english");
                var warnings = new List<string>();
                var translation = new TranslationService(root, source, warnings.Add);
                var manager = new OwnerServiceManager();
                int thread = Thread.CurrentThread.ManagedThreadId;
                var scope = manager.Create("Author.A", () => { }, () => { if (Thread.CurrentThread.ManagedThreadId != thread) throw new InvalidOperationException("wrong thread"); }, translation: translation);
                var service = scope.GetService<IDtmTranslations>()!;
                var formatted = service.Get("hello", new Dictionary<string, string> { ["name"] = "{missing}" });
                Assert(formatted.Text == "Hello {missing}; {missing}; {literal}" && formatted.MissingParameters.SequenceEqual(new[] { "missing" }), "Interpolation must be single-pass, preserve missing tokens and support literal braces.");
                int changed = 0;
                using var bad = service.SubscribeLanguageChanged(_ => throw new Exception("intentional"));
                using var good = service.SubscribeLanguageChanged(snapshot => { Assert(Thread.CurrentThread.ManagedThreadId == thread, "Language callback must stay on runtime thread."); changed++; });
                source.PollLanguageChanges(); Assert(changed == 0, "Subscribe must not invent an initial change.");
                source.SetNativeLanguageProvider(() => "fr-CA"); source.PollLanguageChanges();
                Assert(changed == 1 && warnings.Count == 1 && service.Get("hello", new Dictionary<string, string> { ["name"] = "A" }).Text == "Bonjour A", "Real language change must refresh catalog despite another callback failure.");
                Assert(service.Get("fallback").Text == "English" && service.Get("absent", fallback: "Visible fallback").Text == "Visible fallback", "Missing keys must retain English and explicit fallback behavior.");
                source.SetNativeLanguageProvider(() => "fr-FR"); source.PollLanguageChanges();
                Assert(changed == 2 && service.Snapshot.ResolvedLanguage == "fr_FR", "Regional language changes must not be collapsed into the base alias.");
                AssertThrows(() => Task.Run(() => service.Get("hello")).GetAwaiter().GetResult(), "Translation facade must reject background native lookup.");
                manager.RemoveOwner("Author.A", ModOwnerCleanupReason.Unload);
                source.SetNativeLanguageProvider(() => "english"); source.PollLanguageChanges();
                Assert(changed == 2, "Owner close must detach all language subscriptions.");
                AssertThrows(() => service.Get("hello"), "Closed translation facade must reject use.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_LANGUAGE", oldLanguage);
                Environment.SetEnvironmentVariable("DTMAPI_UI_LANGUAGE", oldUiLanguage);
            }
        }
    }
}
