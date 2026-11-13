using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Services;
using DTMAPI.ModConfigMenu;
using DTMAPI.Abstractions;
using DTMAPI.DebugConsole;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void DebugConsoleCatalogPagingPreservesQueriesAndSnapshots()
        {
            var actions = new DebugConsoleNativeActions(new CatalogQueryRuntime());
            InventoryDebugItem[] items = Enumerable.Range(0, 137).Select(i => new InventoryDebugItem
            {
                Id = "item-" + i,
                SearchText = "entry-" + i,
                Category = i % 2 == 0 ? "tool" : "material",
                IsModItem = i % 3 != 1,
                SourceId = i % 3 == 0 ? "mod-a" : i % 3 == 1 ? "__base" : "__mods",
                SourceKind = i % 3 == 1 ? "Vanilla" : "Mod",
                CanGive = i % 5 != 0
            }).ToArray();
            SetPrivateField(actions, "inventoryCatalog", items);
            SetPrivateField(actions, "inventorySourceGroups", Array.Empty<InventoryDebugSourceGroup>());
            IReadOnlyList<InventoryDebugItem> first = actions.GetItems(new InventoryDebugQuery
            {
                IncludeUnavailable = true, PageSize = 12
            }).Items;
            foreach (string source in new[] { "", "__base", "__mods", "MOD-A", "missing" })
            foreach (string category in new[] { "", "TOOL" })
            foreach (string search in new[] { "", " ENTRY-1 ", "absent" })
            foreach (bool unavailable in new[] { false, true })
            foreach (int size in new[] { 0, 1, 12, 35, 50, 200 })
            foreach (int requested in new[] { -1, 0, 1, 4, int.MaxValue })
            {
                InventoryDebugItem[] expected = items.Where(item =>
                    (unavailable || item.CanGive) &&
                    (search.Trim().Length == 0 || item.SearchText.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (category.Length == 0 || item.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) &&
                    (source.Length == 0 || (source == "__base" ? item.SourceKind == "Vanilla" :
                        source == "__mods" ? item.IsModItem : item.SourceId.Equals(source, StringComparison.OrdinalIgnoreCase))))
                    .ToArray();
                int resolvedSize = size == 0 ? 12 : size;
                int pages = Math.Max(1, (expected.Length + resolvedSize - 1) / resolvedSize);
                int page = Math.Clamp(requested, 0, pages - 1);
                InventoryDebugPage actual = actions.GetItems(new InventoryDebugQuery
                {
                    SearchText = search, Category = category, SourceId = source,
                    IncludeUnavailable = unavailable, Page = requested, PageSize = size
                });
                Assert(actual.Status == "ok" && actual.Page == page && actual.PageSize == resolvedSize &&
                    actual.TotalItems == expected.Length && actual.TotalPages == pages &&
                    actual.Items.SequenceEqual(expected.Skip(page * resolvedSize).Take(resolvedSize)),
                    "Catalog queries must preserve order, availability/source/search filters and clamped last pages, including empty and overflowing requests.");
            }
            actions.InvalidateCatalogs();
            Assert(first.SequenceEqual(items.Take(12)), "Previously returned pages must remain detached from subsequent queries and catalog invalidation.");

            foreach (int total in new[] { 0, 1, 12, 13, 24, 25, 137 })
            foreach (int requested in new[] { 0, 1, 2, int.MaxValue })
            {
                var slice = new CatalogPageAccumulator<SpawnCatalogOption>(requested, 12);
                var entries = Enumerable.Range(0, total).Select(i => new SpawnCatalogOption { Id = i.ToString() }).ToArray();
                foreach (var entry in entries) slice.Add(entry);
                IReadOnlyList<SpawnCatalogOption> page = slice.Complete();
                int pages = Math.Max(1, (total + 11) / 12);
                int expectedPage = Math.Min(requested, pages - 1);
                Assert(slice.TotalItems == total && slice.TotalPages == pages && slice.Page == expectedPage &&
                    page.SequenceEqual(entries.Skip(expectedPage * 12).Take(12)),
                    "Virtual catalogs must keep full and partial tail pages in native order after shared pagination.");
            }
        }

        private static void DebugConsoleCatalogSourcesAvoidItemTraversalAndRetry()
        {
            var runtime = new CatalogQueryRuntime();
            var actions = new DebugConsoleNativeActions(runtime);
            object? previousTables = DolocTown.Config.DolocConfig.Tables;
            try
            {
                var rows = new[]
                {
                    new { Id = "CaseItem", Title = "Current native title", EnglishTitle = "Native English", Overlay = 7,
                        MainType = new { Id = "tool", Title = "Tools" }, SubType = "current_subtype",
                        SubType_Ref = new { Title = "Native subtype" }, UiSpriteAsset = "native_icon" },
                    new { Id = "caseitem", Title = "Second native row", EnglishTitle = "Second English", Overlay = 0,
                        MainType = new { Id = "material", Title = "Materials" }, SubType = "other_subtype",
                        SubType_Ref = new { Title = "Other subtype" }, UiSpriteAsset = "other_icon" }
                };
                DolocTown.Config.DolocConfig.Tables = null;
                Assert(actions.GetSources().Count == 0 && runtime.Errors == 1,
                    "An unavailable native item table must report failure without caching an empty catalog.");
                DolocTown.Config.DolocConfig.Tables = new { TbItem = new { DataList = new CatalogFailOnceRows(rows) } };
                Assert(actions.GetSources().Count == 0 && runtime.Errors == 2,
                    "A failed native item enumeration must not publish partial rows or sources.");
                InventoryDebugPage page = actions.GetItems(new InventoryDebugQuery());
                Assert(page.Status == "ok" && page.Items.Select(item => item.Id).SequenceEqual(new[] { "CaseItem", "caseitem" }) &&
                    runtime.Errors == 2,
                    "Retry must return both case-distinct native rows in runtime order.");
                InventoryDebugItem first = page.Items[0];
                Assert(first.DisplayName == "Current native title" && first.EnglishName == "Native English" &&
                    first.Category == "tool" && first.SubCategory == "Native subtype" && first.MaxStack == 7 &&
                    first.IconAssetKey == "native_icon" && first.CanGive && !page.Items[1].CanGive &&
                    page.Items.All(item => item.RuntimeLoaded && item.IsModItem && item.SourceKind == "Mod" &&
                        item.SourceId == "__mods" && !item.SourceEnablementKnown && !item.WorkshopId.HasValue),
                    "An item absent from the base configuration must count as Mod without inventing a package identity.");
                Assert(page.Sources.Single(source => source.Id == "").Count == 2 &&
                    page.Sources.Single(source => source.Id == "__base").Count == 0 &&
                    page.Sources.Single(source => source.Id == "__mods").Count == 2 && page.Sources.Count == 3 &&
                    actions.GetItems(new InventoryDebugQuery { SourceId = "__base" }).TotalItems == 0 &&
                    actions.GetItems(new InventoryDebugQuery { SourceId = "__mods" }).TotalItems == 2,
                    "Unattributed mod items must contribute to Mods exactly once, with no separate package row.");
                DebugConsoleSourceLabelsAndUnifiedCounts(page);
                runtime.Sources["CaseItem"] = new InventoryDebugSourceGroup { Id = "__base", SourceKind = "Vanilla", Enabled = true, EnablementKnown = true };
                runtime.Sources["caseitem"] = new InventoryDebugSourceGroup { Id = "pack", DisplayName = "Pack", SourceKind = "Mod", IsModSource = true, Enabled = true, EnablementKnown = true };
                actions.InvalidateCatalogs();
                page = actions.GetItems(new InventoryDebugQuery());
                Assert(page.Items[0].SourceKind == "Vanilla" && page.Items[1].SourceId == "pack" && page.Items[1].IsModItem &&
                    page.Sources.Single(source => source.Id == "__base").Count == 1 &&
                    page.Sources.Single(source => source.Id == "__mods").Count == 1 &&
                    page.Sources.Single(source => source.Id == "pack").Count == 1 && page.Sources.Count == 4,
                    "Confirmed native provenance must populate item labels and source filters without changing native values or case-distinct IDs.");
                runtime.SourcesAvailable = false;
                actions.InvalidateCatalogs();
                page = actions.GetItems(new InventoryDebugQuery());
                Assert(page.Status == "ok" && page.TotalItems == 2 && page.Sources.Count == 1 &&
                    page.Sources[0].Id.Length == 0 && page.Sources[0].Count == 2 &&
                    page.Items.All(item => item.SourceId.Length == 0),
                    "A failed source read must keep the native catalog usable with only All, unlike a successfully read empty base table.");
                DebugConsoleUnavailableSourcesKeepOnlyAll(page);
            }
            finally
            {
                DolocTown.Config.DolocConfig.Tables = previousTables;
            }

            var items = new CountingCatalog(Enumerable.Range(0, 137).Select(i =>
                new InventoryDebugItem { Id = "item-" + i, CanGive = true }).ToArray());
            var sources = new[] { new InventoryDebugSourceGroup { Id = "__base", Count = 137 } };
            SetPrivateField(actions, "inventoryCatalog", items);
            SetPrivateField(actions, "inventorySourceGroups", sources);
            InventoryDebugPage oldVirtualQuery = actions.GetItems(new InventoryDebugQuery
            {
                IncludeUnavailable = true, PageSize = 35
            });
            Assert(items.Reads == 137 && ReferenceEquals(oldVirtualQuery.Sources, sources),
                "The former virtual-page query traverses the cached item catalog to return its source list.");
            items.Reads = 0;
            for (int i = 0; i < 10; i++)
                Assert(ReferenceEquals(actions.GetSources(), sources), "Direct source reads must reuse the same cached source snapshot.");
            Assert(items.Reads == 0, "Warmed virtual catalog source reads must not traverse any item entries.");
            Console.WriteLine("Y catalog work: old virtual GetItems = 137 item visits; 10 direct GetSources calls = 0 item visits.");
            DebugConsoleAnimalSourcesResolvePackageOwnership();
        }

        private static void DebugConsoleSourceLabelsAndUnifiedCounts(InventoryDebugPage page)
        {
            var host = new FakeHost(NewTempGameDir());
            var runtime = new DtmApiRuntime(host, new ConfigMenuRegistry());
            var ui = new DebugConsoleUi(new DebugConsoleTestRuntime(runtime));
            Assert(GetPrivateField<string>(ui, "sourceFilter") == string.Empty,
                "The initial item browser must show all runtime items, including unattributed mod items.");
            MethodInfo tooltip = typeof(DebugConsoleUi).GetMethod("FormatItemTooltip", BindingFlags.NonPublic | BindingFlags.Instance)!;
            string text = (string)tooltip.Invoke(ui, new object[] { page.Items[0] })!;
            Assert(text.Contains("Source: Mods", StringComparison.Ordinal) &&
                !text.Contains("Source: Base", StringComparison.Ordinal),
                "An unattributed mod item must use the translated Mods label in its tooltip.");
            MethodInfo sourceMatch = typeof(DebugConsoleUi).GetMethod("CatalogSourceMatches", BindingFlags.NonPublic | BindingFlags.Static)!;
            Assert(!(bool)sourceMatch.Invoke(null, new object[] { "__base", "__mods", true })! &&
                (bool)sourceMatch.Invoke(null, new object[] { "__mods", "__mods", true })!,
                "Unattributed animal cards must match Mods, never Base.");
            var monsters = new[] { new SpawnCatalogOption { Id = "native-monster", SourceId = "__base", IsModSource = false } };
            var animals = new[]
            {
                new AnimalCatalogOption { AnimalId = "proven-base-animal", SourceId = "__base", IsModSource = false, SourceKind = "Vanilla", SourceEnablementKnown = true },
                new AnimalCatalogOption { AnimalId = "declared-animal", SourceId = "Author.Animals", IsModSource = true, SourceKind = "Mod" },
                new AnimalCatalogOption { AnimalId = "ambiguous-animal", SourceId = "__mods", IsModSource = true, SourceKind = "Mod" }
            };
            MethodInfo unified = typeof(DebugConsoleUi).GetMethod("BuildUnifiedCatalogSources", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var sources = (InventoryDebugSourceGroup[])unified.Invoke(ui, new object[] { page.Sources, monsters, animals })!;
            Assert(sources.Single(source => source.Id == "").Count == 6 &&
                sources.Single(source => source.Id == "__base").Count == 2 &&
                sources.Single(source => source.Id == "__mods").Count == 4 &&
                sources.Single(source => source.Id == "Author.Animals").Count == 1 && sources.Length == 4,
                "Base plus Mods must equal All across items, animals and monsters, without counting the generic Mods group a second time.");
            MethodInfo label = typeof(DebugConsoleUi).GetMethod("FormatSourceButtonLabel", BindingFlags.NonPublic | BindingFlags.Instance)!;
            Assert(((string)label.Invoke(ui, new object[] { sources.Single(source => source.Id == "__mods") })!) == "Mods 4",
                "The source selector must translate the Mods aggregate without exposing its internal ID.");
        }

        private static void DebugConsoleUnavailableSourcesKeepOnlyAll(InventoryDebugPage page)
        {
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var ui = new DebugConsoleUi(new DebugConsoleTestRuntime(runtime));
            MethodInfo unified = typeof(DebugConsoleUi).GetMethod("BuildUnifiedCatalogSources", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var sources = (InventoryDebugSourceGroup[])unified.Invoke(ui, new object[]
            {
                page.Sources,
                new[] { new SpawnCatalogOption { Id = "mod-monster", SourceId = "Author.Monsters" } },
                new[] { new AnimalCatalogOption { AnimalId = "mod-animal", SourceId = "Author.Animals" } }
            })!;
            Assert(sources.Length == 1 && sources[0].Id.Length == 0 && sources[0].Count == 4,
                "Unified sources must not recreate Base, Mods or package rows after the source reader falls back to All.");
            MethodInfo tooltip = typeof(DebugConsoleUi).GetMethod("FormatItemTooltip", BindingFlags.NonPublic | BindingFlags.Instance)!;
            string text = (string)tooltip.Invoke(ui, new object[] { page.Items[0] })!;
            Assert(!text.Contains("Source:", StringComparison.Ordinal),
                "An unavailable source must not produce a guessed source label in the item tooltip.");
        }

        private static void DebugConsoleAnimalSourcesResolvePackageOwnership()
        {
            MethodInfo addNativeSource = typeof(DebugConsoleContentSources).GetMethod("AddModSource", BindingFlags.NonPublic | BindingFlags.Static)!;
            var baseSource = new InventoryDebugSourceGroup { Id = "__base", SourceKind = "Vanilla" };
            var sourceA = new InventoryDebugSourceGroup { Id = "Author.AnimalsA", SourceKind = "Mod", IsModSource = true };
            var sourceB = new InventoryDebugSourceGroup { Id = "Author.AnimalsB", SourceKind = "Mod", IsModSource = true };
            foreach (string table in new[] { "item_tbitem", "animal_tbanimal", "monster_tbmonster" })
            {
                var nativeSources = new Dictionary<string, InventoryDebugSourceGroup>(StringComparer.Ordinal) { ["base"] = baseSource };
                addNativeSource.Invoke(null, new object[] { nativeSources, table, "base", sourceA });
                Assert(ReferenceEquals(nativeSources["base"], baseSource),
                    "A native Mod declaration must never change base membership in any source table.");
            }
            var animalSources = new Dictionary<string, InventoryDebugSourceGroup>(StringComparer.Ordinal);
            foreach (var source in new[] { sourceA, sourceB, sourceA })
                addNativeSource.Invoke(null, new object[] { animalSources, "animal_tbanimal", "shared", source });
            foreach (var source in new[] { sourceA, sourceA })
                addNativeSource.Invoke(null, new object[] { animalSources, "animal_tbanimal", "solo", source });
            Assert(animalSources["shared"].Id == "__mods" && animalSources["shared"].IsModSource &&
                animalSources["solo"].Id == sourceA.Id,
                "Native Mod packages declaring one species must retain ambiguity after later declarations, while one repeated owner stays named.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var packs = new List<DiscoveredMod>();
                foreach (string id in new[] { "Author.AnimalsA", "Author.AnimalsB" })
                {
                    string path = Path.Combine(gameDir, id);
                    Directory.CreateDirectory(Path.Combine(path, "PackData"));
                    File.WriteAllText(Path.Combine(path, "PackData", "content.json"),
                        id.EndsWith("A", StringComparison.Ordinal)
                            ? "{\"schemaVersion\":1,\"animals\":{\"definitions\":[{\"speciesId\":\"shared\"},{\"speciesId\":\"solo\"},{\"speciesId\":\"solo\"}]}}"
                            : "{\"schemaVersion\":1,\"animals\":{\"definitions\":[{\"speciesId\":\"shared\"}]}}");
                    var manifest = new ManifestModel { UniqueID = id, Name = id, Author = "Fixture", Version = "1.0.0" };
                    packs.Add(new DiscoveredMod(manifest, path, Path.Combine(path, "manifest.json"), "Local", null, true, false, id, true, string.Empty));
                }
                runtime.Content.SetOwners(packs.ToArray());
                var owner = new ManifestModel { UniqueID = "Fixture.Console", Name = "Console", Author = "Fixture", Version = "1.0.0" };
                var monitor = new SourceReadMonitor();
                var helper = new DtmHelper(owner, monitor, null!, runtime.Config, runtime.ModRegistry, runtime.Workshop,
                    runtime.UI.CreateOwnerBound(owner.UniqueID, () => { }, () => { }), runtime.Diagnostics, runtime.Content, runtime.Input, null!);
                var adapter = new ProductRuntimeAdapter(helper);
                IReadOnlyList<AnimalContentSource> sources = adapter.GetAnimalContentSources();
                AnimalContentSource shared = sources.Single(source => source.AnimalId == "shared");
                AnimalContentSource solo = sources.Single(source => source.AnimalId == "solo");
                Assert(sources.Count == 2 && shared.SourceId == "__mods" && shared.SourceKind == "Mod" &&
                    shared.DisplayName.Length == 0 && !shared.EnablementKnown &&
                    solo.SourceId == "Author.AnimalsA" && solo.SourceKind == "Mod" && solo.EnablementKnown,
                    "Multiple packages declaring a species must leave it in Mods; repeated declarations by one package must retain that package.");
                Assert(DebugConsoleContentSources.Read(helper, "missing_source_fixture") == null &&
                    DebugConsoleContentSources.Read(helper, "another_missing_source_fixture") == null && monitor.Warnings == 1,
                    "Unavailable native sources must return failure, with one warning instead of one per table or catalog entry.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private sealed class SourceReadMonitor : IMonitor
        {
            private readonly HashSet<string> once = new HashSet<string>(StringComparer.Ordinal);
            internal int Warnings;
            public void Log(string message, LogLevel level = LogLevel.Info) { if (level == LogLevel.Warn) Warnings++; }
            public void LogOnce(string key, string message, LogLevel level = LogLevel.Info) { if (once.Add(key)) Log(message, level); }
            public void LogException(Exception exception, string message) { }
        }

        private sealed class CountingCatalog : IReadOnlyList<InventoryDebugItem>
        {
            private readonly InventoryDebugItem[] items;
            internal CountingCatalog(InventoryDebugItem[] items) => this.items = items;
            internal int Reads;
            public int Count => items.Length;
            public InventoryDebugItem this[int index] { get { Reads++; return items[index]; } }
            public IEnumerator<InventoryDebugItem> GetEnumerator() => ((IEnumerable<InventoryDebugItem>)items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class CatalogQueryRuntime : IDebugConsoleNativeRuntime
        {
            internal readonly Dictionary<string, InventoryDebugSourceGroup> Sources = new Dictionary<string, InventoryDebugSourceGroup>(StringComparer.Ordinal);
            internal bool SourcesAvailable = true;
            internal int Errors;
            public IMonitor Monitor => NullMonitor.Instance;
            public string NativeOwnerLabel => "catalog-query-test";
            public IReadOnlyDictionary<string, InventoryDebugSourceGroup>? GetContentSources(string table) => SourcesAvailable ? Sources : null;
            public IReadOnlyList<AnimalContentSource> GetAnimalContentSources() => Array.Empty<AnimalContentSource>();
            public void SetMovementMultiplier(object? player, double multiplier) { }
            public void Status(string id, string status, string source, string details) { }
            public void Error(string operation, Exception error) => Errors++;
        }
    }
}
