using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.DebugConsole;

namespace DTMAPI.UnitTests
{
    internal static class DebugConsoleInventoryProductTests
    {
        internal static void RunAll()
        {
            DebugConsoleInventoryFixture? previous = DolocAPI.InventoryFixture;
            try
            {
                var runtime = new InventoryRuntime();
                var actions = new DebugConsoleNativeActions(runtime);
                var fixture = Reset("HTL_dj", 1);
                InventoryGiveResult result = actions.GiveItem(null!, "HTL_dj", 1);
                Check(result.Success && result.GivenCount == 1 && fixture.Count("HTL_dj") == 1,
                    "A registered uppercase item must be given without changing its ID.");

                fixture = Reset("HTL_djex", 2);
                fixture.Protos.Add("htl_djex", new DebugConsoleInventoryProto("htl_djex", 2));
                result = actions.GiveItem(null!, "HTL_djex", 5);
                Check(result.Success && result.AfterCount == 5 && fixture.Count("HTL_djex") == 5 &&
                    fixture.Count("htl_djex") == 0 && fixture.PlacedChunks.SequenceEqual(new[] { 2, 2, 1 }),
                    "Case-distinct IDs must remain distinct, including multi-stack requests.");
                Check(fixture.StringPlacementCalls == 0 && !fixture.EmailRequested,
                    "Giving must avoid the case-normalizing overload and overflow email.");

                fixture = Reset("wood", 4);
                result = actions.GiveItem(null!, "wood", 10);
                Check(result.Success && result.BeforeCount == 0 && result.AfterCount == 10 &&
                    fixture.Count("wood") == 10, "Ordinary lowercase give-10 must remain correct.");

                fixture = Reset("HTL_dj", 1);
                fixture.Capacity = 0;
                result = actions.GiveItem(null!, "HTL_dj", 1);
                Check(!result.Success && result.GivenCount == 0 && fixture.PlacedChunks.Count == 0,
                    "A full backpack must not receive an item.");

                fixture = Reset("HTL_djex", 2);
                fixture.Capacity = 2;
                result = actions.GiveItem(null!, "HTL_djex", 3);
                Check(!result.Success && result.GivenCount == 2 && result.AfterCount == 2 &&
                    result.FailureReason == "partial-inventory-full" && !fixture.EmailRequested,
                    "Partial capacity must retain exactly the items already given without emailing overflow.");

                fixture = Reset("HTL_dj", 1);
                fixture.ReturnNull = true;
                result = actions.GiveItem(null!, "HTL_dj", 1);
                Check(!result.Success && result.GivenCount == 0 &&
                    result.FailureReason == "native-item-generation-failed" && fixture.PlacedChunks.Count == 0,
                    "An empty native generation result must be reported before backpack placement.");

                fixture = Reset("HTL_dj", 1);
                fixture.ThrowOnGenerate = true;
                result = actions.GiveItem(null!, "HTL_dj", 1);
                Check(!result.Success && result.FailureReason == nameof(InvalidOperationException) &&
                    runtime.LastError is InvalidOperationException && fixture.Count("HTL_dj") == 0,
                    "Reflection errors must retain the native cause and must not create phantom inventory.");
            }
            finally
            {
                DolocAPI.InventoryFixture = previous;
            }
        }

        private static DebugConsoleInventoryFixture Reset(string id, int stack)
        {
            var fixture = new DebugConsoleInventoryFixture();
            fixture.Protos.Add(id, new DebugConsoleInventoryProto(id, stack));
            DolocAPI.InventoryFixture = fixture;
            return fixture;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class InventoryRuntime : IDebugConsoleNativeRuntime
        {
            public IMonitor Monitor => NullMonitor.Instance;
            public string NativeOwnerLabel => "inventory-fixture";
            public Exception? LastError { get; private set; }
            public IReadOnlyList<IContentItemInfo> GetIndexedItems() => Array.Empty<IContentItemInfo>();
            public IContentItemInfo? GetIndexedItem(string itemId) => null;
            public IReadOnlyList<AnimalContentSource> GetAnimalContentSources() => Array.Empty<AnimalContentSource>();
            public void SetCreativeHookDemand(bool enabled) { }
            public void SetMovementMultiplier(object? player, double multiplier) { }
            public void Status(string id, string status, string source, string details) { }
            public void Error(string operation, Exception error) => LastError = error;
        }
    }
}

public sealed class DebugConsoleInventoryProto
{
    public DebugConsoleInventoryProto(string id, int stack) { Id = id; Overlay = stack; }
    public string Id { get; }
    public string Title => Id;
    public int Overlay { get; }
}

public sealed class DebugConsoleInventoryItem
{
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class DebugConsoleInventoryFixture
{
    public Dictionary<string, DebugConsoleInventoryProto> Protos { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> Inventory { get; } = new(StringComparer.Ordinal);
    public List<int> PlacedChunks { get; } = new();
    public int Capacity { get; set; } = 100;
    public bool ReturnNull { get; set; }
    public bool ThrowOnGenerate { get; set; }
    public bool EmailRequested { get; set; }
    public int StringPlacementCalls { get; set; }
    public int Count(string id) => Inventory.TryGetValue(id, out int value) ? value : 0;
}

public static partial class DolocAPI
{
    public static DebugConsoleInventoryFixture? InventoryFixture;
    public static bool QueryItemProto(string id, out DebugConsoleInventoryProto? proto) =>
        InventoryFixture!.Protos.TryGetValue(id, out proto);
    public static int CountItem(string id, bool checkBox) => InventoryFixture!.Count(id);
    public static DebugConsoleInventoryItem? GenerateItem(string id, int count)
    {
        DebugConsoleInventoryFixture fixture = InventoryFixture!;
        if (fixture.ThrowOnGenerate)
            throw new InvalidOperationException("Native item generation fixture failure.");
        return fixture.ReturnNull || !fixture.Protos.TryGetValue(id, out DebugConsoleInventoryProto? proto)
            ? null : new DebugConsoleInventoryItem { Id = id, Count = Math.Min(count, proto.Overlay) };
    }
    public static bool CanPlaceItem(string id, int count) => CanPlaceItem(GenerateItem(id, count)!);
    public static bool CanPlaceItem(DebugConsoleInventoryItem item) =>
        item != null && InventoryFixture!.Inventory.Values.Sum() + item.Count <= InventoryFixture.Capacity;
    // Model the observed native string-overload hazard; production must use the object overload.
    public static bool TryPlaceInBackpack(string id, int count, bool sendEmailOnOverflow)
    {
        InventoryFixture!.StringPlacementCalls++;
        return TryPlaceInBackpack(GenerateItem(id.ToLowerInvariant(), count)!, sendEmailOnOverflow);
    }
    public static bool TryPlaceInBackpack(DebugConsoleInventoryItem item, bool sendEmailOnOverflow)
    {
        if (item == null)
            throw new NullReferenceException("Native string lookup produced no item.");
        DebugConsoleInventoryFixture fixture = InventoryFixture!;
        fixture.EmailRequested |= sendEmailOnOverflow;
        if (!CanPlaceItem(item))
            return false;
        fixture.Inventory[item.Id] = fixture.Count(item.Id) + item.Count;
        fixture.PlacedChunks.Add(item.Count);
        return true;
    }
}
