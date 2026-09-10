using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.DebugConsole;

namespace DTMAPI.UnitTests
{
    internal static class DebugConsoleSpawnProductTests
    {
        internal static void RunAll()
        {
            var assets = new DebugConsoleSpawnAssets();
            object? previousAssets = DolocAPI.assets;
            object? previousRoom = DolocAPI.CurrentRoom;
            object? previousPosition = DolocAPI.AgentPosition;
            object? previousTables = DolocTown.Config.DolocConfig.Tables;
            DolocAPI.assets = assets;
            DolocAPI.AgentPosition = new DebugConsoleAgentPositionFixture
            {
                x = 12.5f,
                y = -3.25f
            };
            DolocTown.Config.DolocConfig.Tables =
                new DebugConsoleSpawnTables(
                    assets.monsters.TotalProtos,
                    assets.animals.TotalProtos);

            try
            {
                var runtime = new DebugConsoleSpawnRuntime();
                var actions = new DebugConsoleNativeActions(
                    runtime);

                DolocAPI.CurrentRoom = null;
                AnimalCatalogOption[] animals = actions
                    .GetAnimalCatalog()
                    .ToArray();
                Assert(
                    animals.Length == 6 &&
                    animals.Count(option =>
                        option.AnimalId == "slime") == 3 &&
                    animals.Count(option =>
                        option.AnimalId == "hatch") == 3 &&
                    animals.Where(option => option.AnimalId == "slime")
                        .All(option =>
                            !option.IsModSource &&
                            option.SourceId == "__base") &&
                    animals.Where(option => option.AnimalId == "hatch")
                        .All(option =>
                            option.IsModSource &&
                            option.SourceId == "DTMAPI.AnimalPack" &&
                            option.SourceDisplayName == "缺氧动物包"),
                    "The animal catalog must enumerate runtime TbAnimal rows and preserve AnimalPack source attribution for all three state cards.");
                SpawnActionResult customAnimalLookup = actions.SpawnAnimal(
                    null!,
                    "hatch.child",
                    1);
                Assert(
                    !customAnimalLookup.Result.Success &&
                    customAnimalLookup.Result.FailureReason ==
                        "native-host-unavailable",
                    "A custom TbAnimal card must pass catalog/proto lookup and fail only at the missing native room fixture.");

                var oneHost = new DolocTown.DebugConsoleMonsterHost(
                    assets.monsters.SpaceShipBastion);
                DolocAPI.CurrentRoom = oneHost;
                SpawnActionResult one = actions.SpawnMonster(
                    null!,
                    "space_ship",
                    1);
                Assert(
                    one.Result.Success &&
                    one.Result.RequestedCount == 1 &&
                    one.Result.SpawnedCount == 1 &&
                    one.AddedEntityCount == 3 &&
                    oneHost.DM_monster.AllMonsters.Count == 3 &&
                    oneHost.DM_monster.AllMonsters.Count(monster =>
                        monster.proto.Name == "space_ship") == 1 &&
                    oneHost.DM_monster.AllMonsters.Count(monster =>
                        monster.proto.Name == "space_ship_bastion") == 2,
                    "Old City Guardian x1 must validate one returned root plus two official bastions without rollback.");

                var tenHost = new DolocTown.DebugConsoleMonsterHost(
                    assets.monsters.SpaceShipBastion);
                DolocAPI.CurrentRoom = tenHost;
                SpawnActionResult ten = actions.SpawnMonster(
                    null!,
                    "space_ship",
                    10);
                Assert(
                    ten.Result.Success &&
                    ten.Result.RequestedCount == 10 &&
                    ten.Result.SpawnedCount == 10 &&
                    ten.AddedEntityCount == 30 &&
                    tenHost.DM_monster.AllMonsters.Count == 30,
                    "Old City Guardian x10 must remain available and expose ten successful roots plus thirty actual manager additions.");

                var partialHost = new DolocTown.DebugConsoleMonsterHost(
                    assets.monsters.SpaceShipBastion)
                {
                    FailOnRootInvocation = 4
                };
                DolocAPI.CurrentRoom = partialHost;
                SpawnActionResult partial = actions.SpawnMonster(
                    null!,
                    "slime",
                    10);
                Assert(
                    !partial.Result.Success &&
                    partial.Result.RequestedCount == 10 &&
                    partial.Result.SpawnedCount == 3 &&
                    partial.AddedEntityCount == 4 &&
                    partialHost.DM_monster.AllMonsters.Count == 4 &&
                    partial.StopReason.Contains(
                        "injected fourth root failure",
                        StringComparison.Ordinal),
                    "A batch failure with a real fourth-call side effect must stop visibly at three verified roots while retaining all four actual additions.");

                partialHost.FailOnRootInvocation = 0;
                SpawnActionResult retry = actions.SpawnMonster(
                    null!,
                    "slime",
                    1);
                Assert(
                    retry.Result.Success &&
                    retry.Result.SpawnedCount == 1 &&
                    retry.AddedEntityCount == 1 &&
                    partialHost.DM_monster.AllMonsters.Count == 5,
                    "A failed batch must not open a cross-request circuit or delete prior official additions.");
            }
            finally
            {
                DolocAPI.assets = previousAssets;
                DolocAPI.CurrentRoom = previousRoom;
                DolocAPI.AgentPosition = previousPosition;
                DolocTown.Config.DolocConfig.Tables = previousTables;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class DebugConsoleSpawnRuntime :
            IDebugConsoleNativeRuntime
        {
            public IMonitor Monitor => NullMonitor.Instance;
            public string NativeOwnerLabel => "unit-fixture";
            public IReadOnlyList<IContentItemInfo> GetIndexedItems() =>
                Array.Empty<IContentItemInfo>();
            public IContentItemInfo? GetIndexedItem(string itemId) => null;
            public IReadOnlyList<AnimalContentSource>
                GetAnimalContentSources() =>
                new[]
                {
                    new AnimalContentSource
                    {
                        AnimalId = "hatch",
                        SourceId = "DTMAPI.AnimalPack",
                        DisplayName = "缺氧动物包",
                        SourceKind = "DTMAPI",
                        Enabled = true,
                        EnablementKnown = true
                    }
                };
            public void SetCreativeHookDemand(bool enabled)
            {
            }
            public void SetMovementMultiplier(object? player, double multiplier)
            {
            }
            public void Status(
                string id,
                string status,
                string source,
                string details)
            {
            }
            public void Error(string operation, Exception error)
            {
            }
        }
    }

    internal sealed class DebugConsoleSpawnAssets
    {
        internal DebugConsoleSpawnAssets()
        {
            monsters = new DebugConsoleMonsterAssets();
            animals = new DebugConsoleAnimalAssets();
        }

        public DebugConsoleMonsterAssets monsters { get; }
        public DebugConsoleAnimalAssets animals { get; }
    }

    internal sealed class DebugConsoleMonsterAssets
    {
        internal DebugConsoleMonsterAssets()
        {
            Slime = new DolocTown.DebugConsoleMonsterProto("slime");
            SpaceShip = new DolocTown.DebugConsoleMonsterProto("space_ship");
            SpaceShipBastion =
                new DolocTown.DebugConsoleMonsterProto("space_ship_bastion");
            TotalProtos = new[] { Slime, SpaceShip, SpaceShipBastion };
        }

        internal DolocTown.DebugConsoleMonsterProto Slime { get; }
        internal DolocTown.DebugConsoleMonsterProto SpaceShip { get; }
        internal DolocTown.DebugConsoleMonsterProto SpaceShipBastion { get; }
        public DolocTown.DebugConsoleMonsterProto[] TotalProtos { get; }
    }

    internal sealed class DebugConsoleAnimalAssets
    {
        internal DebugConsoleAnimalAssets()
        {
            Slime = new DebugConsoleAnimalProto("slime", "Slime");
            Hatch = new DebugConsoleAnimalProto("hatch", "Hatch");
            TotalProtos = new[] { Slime, Hatch };
        }

        internal DebugConsoleAnimalProto Slime { get; }
        internal DebugConsoleAnimalProto Hatch { get; }
        public DebugConsoleAnimalProto[] TotalProtos { get; }
    }

    internal sealed class DebugConsoleAnimalProto
    {
        internal DebugConsoleAnimalProto(string id, string title)
        {
            Id = id;
            Title = title;
        }

        public string Id { get; }
        public string Title { get; }
        public int Size => 1;
        public int Space => 0;
        public object? UiChildSprite => null;
        public object? UiSprite => null;
    }

    internal sealed class DebugConsoleSpawnTables
    {
        internal DebugConsoleSpawnTables(
            IEnumerable<DolocTown.DebugConsoleMonsterProto> monsters,
            IEnumerable<DebugConsoleAnimalProto> animals)
        {
            TbMonsterDocument =
                new DebugConsoleMonsterDocumentTable(monsters);
            TbAnimal = new DebugConsoleAnimalTable(animals);
        }

        public DebugConsoleMonsterDocumentTable TbMonsterDocument { get; }
        public DebugConsoleAnimalTable TbAnimal { get; }
    }

    internal sealed class DebugConsoleAnimalTable
    {
        internal DebugConsoleAnimalTable(
            IEnumerable<DebugConsoleAnimalProto> protos)
        {
            DataList = protos.ToArray();
        }

        public DebugConsoleAnimalProto[] DataList { get; }
    }

    internal sealed class DebugConsoleMonsterDocumentTable
    {
        private readonly Dictionary<string, DebugConsoleMonsterDocument> rows;

        internal DebugConsoleMonsterDocumentTable(
            IEnumerable<DolocTown.DebugConsoleMonsterProto> protos)
        {
            rows = protos.ToDictionary(
                proto => proto.Name,
                proto => new DebugConsoleMonsterDocument(proto.Name),
                StringComparer.OrdinalIgnoreCase);
        }

        public DebugConsoleMonsterDocument? GetOrDefault(string id) =>
            rows.TryGetValue(id, out DebugConsoleMonsterDocument? row)
                ? row
                : null;
    }

    internal sealed class DebugConsoleMonsterDocument
    {
        internal DebugConsoleMonsterDocument(string id)
        {
            Title = id;
        }

        public string Title { get; }
        public string MonsterType => "monster";
        public object? UiSpriteAsset => null;
    }
}

public static partial class DolocAPI
{
    public static object? assets;
    public static object? CurrentRoom;
    public static object? AgentPosition;
}

namespace DolocTown
{
    internal interface IMonsterHost
    {
        DebugConsoleMonster GenerateMonster(
            DebugConsoleMonsterProto proto,
            UnityEngine.Vector2 position,
            bool run);
    }

    internal sealed class DebugConsoleMonsterHost : IMonsterHost
    {
        private readonly DebugConsoleMonsterProto bastionProto;
        private int rootInvocations;

        internal DebugConsoleMonsterHost(
            DebugConsoleMonsterProto bastionProto)
        {
            this.bastionProto = bastionProto;
        }

        public DebugConsoleMonsterManager DM_monster { get; } =
            new DebugConsoleMonsterManager();

        internal int FailOnRootInvocation { get; set; }

        public DebugConsoleMonster GenerateMonster(
            DebugConsoleMonsterProto proto,
            UnityEngine.Vector2 position,
            bool run)
        {
            rootInvocations++;
            DebugConsoleMonster root = Create(proto);
            if (FailOnRootInvocation > 0 &&
                rootInvocations == FailOnRootInvocation)
            {
                throw new InvalidOperationException(
                    "injected fourth root failure after manager addition");
            }

            if (proto.Name.Equals(
                    "space_ship",
                    StringComparison.OrdinalIgnoreCase))
            {
                Create(bastionProto);
                Create(bastionProto);
            }

            return root;
        }

        private DebugConsoleMonster Create(DebugConsoleMonsterProto proto)
        {
            var monster = new DebugConsoleMonster(proto, this);
            DM_monster.AllMonsters.Add(monster);
            return monster;
        }
    }

    internal sealed class DebugConsoleMonsterManager
    {
        public List<DebugConsoleMonster> AllMonsters { get; } =
            new List<DebugConsoleMonster>();
    }

    internal sealed class DebugConsoleMonster
    {
        internal DebugConsoleMonster(
            DebugConsoleMonsterProto proto,
            object host)
        {
            this.proto = proto;
            Host = host;
        }

        public DebugConsoleMonsterProto proto { get; }
        public object Host { get; }
        public object Controller { get; } = new object();
    }

    internal sealed class DebugConsoleMonsterProto
    {
        internal DebugConsoleMonsterProto(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
