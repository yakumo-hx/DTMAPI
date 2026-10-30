using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                IsModItem = i % 3 == 0,
                SourceId = i % 3 == 0 ? "mod-a" : "__base",
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
                    (source.Length == 0 || (source == "__base" ? !item.IsModItem :
                        source == "__mods" ? item.IsModItem : item.SourceId.Equals(source, StringComparison.OrdinalIgnoreCase))))
                    .ToArray();
                int resolvedSize = size == 0 ? 12 : Math.Min(50, size);
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
            var runtime = new CatalogQueryRuntime { FailRead = true };
            var actions = new DebugConsoleNativeActions(runtime);
            Assert(actions.GetSources().Count == 0 && runtime.Errors == 1,
                "Source discovery errors must remain contained as an empty source list.");
            runtime.FailRead = false;
            actions.GetSources();
            Assert(runtime.Reads == 2 && runtime.Errors == 1,
                "Failed source discovery must retry once the content service recovers.");

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
            internal bool FailRead;
            internal int Reads, Errors;
            public IMonitor Monitor => NullMonitor.Instance;
            public string NativeOwnerLabel => "catalog-query-test";
            public IReadOnlyList<IContentItemInfo> GetIndexedItems()
            {
                Reads++;
                if (FailRead) throw new InvalidOperationException("Source read failure.");
                return Array.Empty<IContentItemInfo>();
            }
            public IContentItemInfo? GetIndexedItem(string id) => null;
            public IReadOnlyList<AnimalContentSource> GetAnimalContentSources() => Array.Empty<AnimalContentSource>();
            public void SetMovementMultiplier(object? player, double multiplier) { }
            public void Status(string id, string status, string source, string details) { }
            public void Error(string operation, Exception error) => Errors++;
        }
    }
}
