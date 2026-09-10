using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleNativeActions
    {
        public IReadOnlyList<SpawnCatalogOption> GetMonsterCatalog()
        {
            try
            {
                EnsureMonsterCatalog();
                bool hostAvailable = NativeInterface(
                    Native.CurrentRoom,
                    "DolocTown.IMonsterHost") != null;
                foreach (SpawnCatalogOption option in monsterCatalog!)
                {
                    option.IsAvailable = hostAvailable;
                    option.UnavailableReason = hostAvailable
                        ? string.Empty
                        : "native-host-unavailable";
                }
                return monsterCatalog;
            }
            catch (Exception error)
            {
                runtime.Error("monster-catalog", error);
                return Array.Empty<SpawnCatalogOption>();
            }
        }

        public IReadOnlyList<AnimalCatalogOption> GetAnimalCatalog()
        {
            try
            {
                EnsureAnimalCatalog();
                bool hostAvailable = NativeInterface(
                    Native.CurrentRoom,
                    "DolocTown.IAnimalHost") != null;
                foreach (AnimalCatalogOption option in animalCatalog!)
                {
                    option.IsAvailable = hostAvailable;
                    option.UnavailableReason = hostAvailable
                        ? string.Empty
                        : "native-host-unavailable";
                }
                return animalCatalog;
            }
            catch (Exception error)
            {
                runtime.Error("animal-catalog", error);
                return Array.Empty<AnimalCatalogOption>();
            }
        }

        private void EnsureMonsterCatalog()
        {
            if (monsterCatalog != null)
                return;
            object? api = Native.Resolve("DolocAPI, Assembly-CSharp");
            object? assets = Native.Read(api, "assets");
            object? monsters = Native.Read(assets, "monsters");
            var options = new List<SpawnCatalogOption>();
            foreach (object proto in Native.Enumerate(Native.Read(monsters, "TotalProtos")))
            {
                string id = Native.TextFirst(
                    proto,
                    "Name",
                    "SpawnId",
                    "Id");
                if (id.Length == 0 || monsterProtos.ContainsKey(id))
                    continue;
                monsterProtos[id] = proto;
                object? document = TableGetOrDefault("TbMonsterDocument", id);
                object? spriteAsset = Native.Read(document, "UiSpriteAsset");
                options.Add(new SpawnCatalogOption
                {
                    Id = id,
                    DisplayName = Native.First(
                        Native.Text(document, "Title"),
                        id),
                    Category = Native.First(
                        Native.Read(document, "MonsterType")?.ToString() ?? string.Empty,
                        "monster"),
                    IconAssetKey = spriteAsset?.ToString() ?? string.Empty,
                    Icon = Native.Read(spriteAsset, "Asset")
                });
            }
            monsterCatalog = options
                .OrderBy(option => option.Id, StringComparer.Ordinal)
                .ToArray();
        }

        private void EnsureAnimalCatalog()
        {
            if (animalCatalog != null)
                return;
            Dictionary<string, AnimalContentSource> contentSources = runtime
                .GetAnimalContentSources()
                .Where(source =>
                    !string.IsNullOrWhiteSpace(source.AnimalId))
                .GroupBy(
                    source => source.AnimalId,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);
            var options = new List<AnimalCatalogOption>();
            foreach (object proto in Native.Enumerate(
                Native.TableList("TbAnimal")))
            {
                string id = Native.Text(proto, "Id").Trim();
                if (id.Length == 0 || animalProtos.ContainsKey(id))
                    continue;
                animalProtos[id] = proto;
                contentSources.TryGetValue(
                    id,
                    out AnimalContentSource? contentSource);
                string title = Native.First(Native.Text(proto, "Title"), id);
                foreach (AnimalSpawnState state in new[]
                {
                    AnimalSpawnState.Child,
                    AnimalSpawnState.Adult,
                    AnimalSpawnState.Ready
                })
                {
                    object? spriteAsset = Native.Read(
                        proto,
                        state == AnimalSpawnState.Child
                            ? "UiChildSprite"
                            : "UiSprite");
                    options.Add(new AnimalCatalogOption
                    {
                        CardId = id + "." + state.ToString().ToLowerInvariant(),
                        AnimalId = id,
                        DisplayName = title,
                        State = state,
                        IconAssetKey = spriteAsset?.ToString() ?? string.Empty,
                        Icon = Native.Read(spriteAsset, "Asset"),
                        SourceId = contentSource?.SourceId ?? "__base",
                        SourceDisplayName = contentSource?.DisplayName ??
                            "Doloc Town",
                        SourceKind = contentSource?.SourceKind ?? "Vanilla",
                        IsModSource = contentSource != null,
                        SourceEnabled = contentSource?.Enabled ?? true,
                        SourceEnablementKnown =
                            contentSource?.EnablementKnown ?? true,
                        WorkshopId = contentSource?.WorkshopId
                    });
                }
            }
            animalCatalog = options.ToArray();
        }

        private object? TableGetOrDefault(string tableName, string id)
        {
            object? table = Native.Table(tableName);
            MethodInfo? get = Native.Method(
                table?.GetType(),
                "GetOrDefault",
                1,
                false);
            return get?.Invoke(table, new object[] { id });
        }

        private static readonly ConcurrentDictionary<NativeInterfaceKey,
            NativeInterfaceResolution> NativeInterfaces =
                new ConcurrentDictionary<NativeInterfaceKey,
                    NativeInterfaceResolution>();

        private static Type? NativeInterface(object? host, string fullName)
        {
            if (host == null)
                return null;
            var key = new NativeInterfaceKey(host.GetType(), fullName);
            if (NativeInterfaces.TryGetValue(
                    key,
                    out NativeInterfaceResolution cached))
            {
                return cached.Interface;
            }
            Type? resolved = null;
            foreach (Type candidate in key.HostType.GetInterfaces())
            {
                if (candidate.FullName?.Equals(
                        fullName,
                        StringComparison.Ordinal) == true)
                {
                    resolved = candidate;
                    break;
                }
            }
            NativeInterfaces.TryAdd(
                key,
                new NativeInterfaceResolution(resolved));
            return resolved;
        }

        private static MethodInfo? HostMethod(
            object? host,
            string interfaceName,
            string methodName,
            int parameterCount) =>
            Native.Method(
                NativeInterface(host, interfaceName),
                methodName,
                parameterCount,
                false);

        private static IReadOnlyList<object> ManagerSnapshot(
            object manager,
            string collectionName) =>
            Native.Enumerate(Native.Read(manager, collectionName)).ToArray();

        private static IReadOnlyList<object> AddedEntities(
            IReadOnlyList<object> before,
            IReadOnlyList<object> after)
        {
            var existing = new HashSet<object>(
                before,
                ReferenceObjectComparer.Instance);
            var added = new List<object>();
            for (int index = 0; index < after.Count; index++)
            {
                object candidate = after[index];
                if (!existing.Contains(candidate))
                    added.Add(candidate);
            }
            return added;
        }

        private static bool ContainsReference(
            IEnumerable<object> values,
            object expected) =>
            values.Any(value => ReferenceEquals(value, expected));

        private static string MonsterEntityId(object entity)
        {
            object? proto = Native.Read(entity, "proto");
            string id = Native.TextFirst(
                proto,
                "Name",
                "SpawnId",
                "Id");
            return string.IsNullOrWhiteSpace(id)
                ? Native.Text(entity, "Name")
                : id;
        }

        private static bool ValidateMonsterAddition(
            object host,
            object expectedProto,
            string requestedId,
            object root,
            IReadOnlyList<object> added,
            out string failure)
        {
            object? rootProto = Native.Read(root, "proto");
            if (rootProto == null ||
                (!ReferenceEquals(rootProto, expectedProto) &&
                 !object.Equals(rootProto, expectedProto)) ||
                !MonsterEntityId(root).Equals(
                    requestedId,
                    StringComparison.OrdinalIgnoreCase) ||
                !ContainsReference(added, root))
            {
                failure = "The returned monster root was not the requested newly registered entity.";
                return false;
            }

            if (added.Any(entity =>
                    !ReferenceEquals(Native.Read(entity, "Host"), host) ||
                    Native.Read(entity, "Controller") == null))
            {
                failure = "A newly registered monster has an invalid Host or Controller.";
                return false;
            }

            if (!requestedId.Equals("space_ship", StringComparison.OrdinalIgnoreCase))
            {
                if (added.Count == 1 && ReferenceEquals(added[0], root))
                {
                    failure = string.Empty;
                    return true;
                }

                failure = "An ordinary monster root did not add exactly one manager entity.";
                return false;
            }

            int rootCount = added.Count(entity =>
                MonsterEntityId(entity).Equals(
                    "space_ship",
                    StringComparison.OrdinalIgnoreCase));
            int bastionCount = added.Count(entity =>
                MonsterEntityId(entity).Equals(
                    "space_ship_bastion",
                    StringComparison.OrdinalIgnoreCase));
            if (added.Count == 3 && rootCount == 1 && bastionCount == 2)
            {
                failure = string.Empty;
                return true;
            }

            failure =
                "Old City Guardian must add one space_ship root and two space_ship_bastion entities.";
            return false;
        }

        private SpawnActionResult SpawnMonsterNative(
            IManifest owner,
            string monsterId,
            int count)
        {
            monsterId = (monsterId ?? string.Empty).Trim();
            count = Math.Max(1, Math.Min(10, count));
            var result = new SpawnDebugResult
            {
                SpawnId = monsterId,
                RequestedCount = count
            };
            try
            {
                EnsureMonsterCatalog();
                SpawnCatalogOption? option = monsterCatalog!.FirstOrDefault(value =>
                    value.Id.Equals(monsterId, StringComparison.OrdinalIgnoreCase));
                object? host = Native.CurrentRoom;
                object? manager = Native.Read(host, "DM_monster");
                if (option == null ||
                    !monsterProtos.TryGetValue(
                        monsterId,
                        out object? proto) ||
                    proto == null)
                    return FailSpawn("spawn-monster", result, "unknown-monster", "Monster is not in the stable native catalog.");
                MethodInfo? generate = HostMethod(
                    host,
                    "DolocTown.IMonsterHost",
                    "GenerateMonster",
                    3);
                if (host == null || manager == null || generate == null)
                    return FailSpawn("spawn-monster", result, "native-host-unavailable", "The current room has no monster host.");
                ParameterInfo[] generateParameters = generate.GetParameters();
                object? position = generateParameters.Length == 3
                    ? Native.Vector2(
                        Native.AgentPosition,
                        generateParameters[1].ParameterType)
                    : null;
                if (position == null)
                {
                    return FailSpawn(
                        "spawn-monster",
                        result,
                        "agent-position-conversion-unavailable",
                        "DolocAPI.AgentPosition could not be converted to the native Vector2 parameter.");
                }
                result.DisplayName = option.DisplayName;
                IReadOnlyList<object> batchBefore =
                    ManagerSnapshot(manager, "AllMonsters");
                var generateArguments =
                    new object?[] { proto, position, true };
                string stopCode = string.Empty;
                string stopReason = string.Empty;
                for (int index = 0; index < count; index++)
                {
                    IReadOnlyList<object> callBefore =
                        ManagerSnapshot(manager, "AllMonsters");
                    try
                    {
                        object? entity = generate.Invoke(
                            host,
                            generateArguments);
                        if (entity == null)
                        {
                            stopCode = "native-create-returned-null";
                            stopReason = "GenerateMonster returned null.";
                            break;
                        }

                        IReadOnlyList<object> callAfter =
                            ManagerSnapshot(manager, "AllMonsters");
                        IReadOnlyList<object> added =
                            AddedEntities(callBefore, callAfter);
                        if (!ValidateMonsterAddition(
                                host,
                                proto,
                                monsterId,
                                entity,
                                added,
                                out stopReason))
                        {
                            stopCode = "monster-postcondition-failed";
                            break;
                        }

                        result.SpawnedCount++;
                    }
                    catch (Exception error)
                    {
                        Exception unwrapped = Unwrap(error);
                        runtime.Error("spawn-monster", unwrapped);
                        stopCode = unwrapped.GetType().Name;
                        stopReason = unwrapped.Message;
                        break;
                    }
                }

                int addedEntityCount = AddedEntities(
                    batchBefore,
                    ManagerSnapshot(manager, "AllMonsters")).Count;
                return FinishSpawn(
                    "spawn-monster",
                    result,
                    addedEntityCount,
                    stopCode,
                    stopReason);
            }
            catch (Exception error)
            {
                runtime.Error("spawn-monster", Unwrap(error));
                return FailSpawn(
                    "spawn-monster",
                    result,
                    Unwrap(error).GetType().Name,
                    Unwrap(error).Message);
            }
        }

        public SpawnActionResult SpawnAnimal(
            IManifest owner,
            string cardId,
            int count)
        {
            cardId = (cardId ?? string.Empty).Trim();
            count = Math.Max(1, Math.Min(10, count));
            var result = new SpawnDebugResult
            {
                SpawnId = cardId,
                RequestedCount = count
            };
            try
            {
                EnsureAnimalCatalog();
                AnimalCatalogOption? option = animalCatalog!.FirstOrDefault(value =>
                    value.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase));
                if (option == null ||
                    !animalProtos.TryGetValue(
                        option.AnimalId,
                        out object? proto) ||
                    proto == null)
                    return FailSpawn("spawn-animal", result, "unknown-animal-card", "Animal state is not present in the runtime TbAnimal catalog.");
                object? host = Native.CurrentRoom;
                object? manager = Native.Read(host, "DM_animal");
                object? system = Native.Read(host, "animalSystem");
                MethodInfo? create = HostMethod(
                    host,
                    "DolocTown.IAnimalHost",
                    "CreateAnimal",
                    3);
                MethodInfo? checkSpace = HostMethod(
                    host,
                    "DolocTown.IAnimalHost",
                    "CheckAnimalSpace",
                    1);
                if (host == null || manager == null || system == null ||
                    create == null || checkSpace == null)
                    return FailSpawn("spawn-animal", result, "native-host-unavailable", "The current room has no animal host.");
                int size = Math.Max(1, Native.Int(proto, "Size", 1));
                int space = Math.Max(0, Native.Int(proto, "Space", 0));
                IReadOnlyList<object> positions = FindAnimalPositions(
                    host,
                    manager,
                    size,
                    count);
                result.DisplayName = option.DisplayName;
                IReadOnlyList<object> batchBefore =
                    ManagerSnapshot(manager, "AllAnimals");
                var checkSpaceArguments = new object?[] { space };
                var createArguments = new object?[] { proto, null, true };
                MethodInfo? containsAnimal = Native.Method(
                    system.GetType(),
                    "ContainsAnimal",
                    1,
                    false);
                var containsAnimalArguments = new object?[1];
                string stopCode = string.Empty;
                string stopReason = string.Empty;
                for (int index = 0; index < count; index++)
                {
                    if (index >= positions.Count)
                    {
                        stopCode = "animal-position-capacity-insufficient";
                        stopReason = "No additional non-overlapping walkable position is available near the player.";
                        break;
                    }

                    try
                    {
                        object? spaceResult = checkSpace.Invoke(
                            host,
                            checkSpaceArguments);
                        if (!(spaceResult is bool hasSpace) || !hasSpace)
                        {
                            stopCode = "animal-capacity-insufficient";
                            stopReason = "The animal host has insufficient capacity for the next animal.";
                            break;
                        }

                        IReadOnlyList<object> callBefore =
                            ManagerSnapshot(manager, "AllAnimals");
                        int beforeSystem = Native.Int(
                            system,
                            "TotalCount",
                            Native.Count(Native.Read(system, "Animals")));
                        createArguments[1] = positions[index];
                        object? entity = create.Invoke(host, createArguments);
                        if (entity == null)
                        {
                            stopCode = "native-create-returned-null";
                            stopReason = "CreateAnimal returned null.";
                            break;
                        }

                        ApplyAnimalState(entity, option);
                        IReadOnlyList<object> callAfter =
                            ManagerSnapshot(manager, "AllAnimals");
                        IReadOnlyList<object> added =
                            AddedEntities(callBefore, callAfter);
                        containsAnimalArguments[0] = entity;
                        bool systemContains =
                            containsAnimal?.Invoke(
                                system,
                                containsAnimalArguments) is bool contained &&
                            contained;
                        bool valid =
                            ReferenceEquals(Native.Read(entity, "proto"), proto) &&
                            Native.First(
                                    Native.Text(entity, "protoName"),
                                    Native.Text(Native.Read(entity, "proto"), "Id"))
                                .Equals(option.AnimalId, StringComparison.OrdinalIgnoreCase) &&
                            added.Count == 1 &&
                            ReferenceEquals(added[0], entity) &&
                            systemContains &&
                            Native.Int(system, "TotalCount") == beforeSystem + 1 &&
                            Native.Bool(entity, "IsAdult") ==
                                (option.State != AnimalSpawnState.Child);
                        if (!valid)
                        {
                            stopCode = "animal-postcondition-failed";
                            stopReason = "Animal postcondition verification failed.";
                            break;
                        }

                        result.SpawnedCount++;
                    }
                    catch (Exception error)
                    {
                        Exception unwrapped = Unwrap(error);
                        runtime.Error("spawn-animal", unwrapped);
                        stopCode = unwrapped.GetType().Name;
                        stopReason = unwrapped.Message;
                        break;
                    }
                }

                int addedEntityCount = AddedEntities(
                    batchBefore,
                    ManagerSnapshot(manager, "AllAnimals")).Count;
                return FinishSpawn(
                    "spawn-animal",
                    result,
                    addedEntityCount,
                    stopCode,
                    stopReason);
            }
            catch (Exception error)
            {
                runtime.Error("spawn-animal", Unwrap(error));
                return FailSpawn(
                    "spawn-animal",
                    result,
                    Unwrap(error).GetType().Name,
                    Unwrap(error).Message);
            }
        }

        private SpawnActionResult FailSpawn(
            string action,
            SpawnDebugResult result,
            string reason,
            string message)
        {
            return FinishSpawn(
                action,
                result,
                0,
                reason,
                message);
        }

        private SpawnActionResult FinishSpawn(
            string action,
            SpawnDebugResult result,
            int addedEntityCount,
            string reason,
            string stopReason)
        {
            reason = (reason ?? string.Empty).Trim();
            stopReason = (stopReason ?? string.Empty).Trim();
            result.Success =
                reason.Length == 0 &&
                result.SpawnedCount == result.RequestedCount;
            if (!result.Success && reason.Length == 0)
                reason = "batch-stopped-before-request-completed";
            result.FailureReason = result.Success ? string.Empty : reason;
            result.Message =
                "Spawn request id=" + result.SpawnId +
                " requestedRoots=" + result.RequestedCount.ToString(CultureInfo.InvariantCulture) +
                " succeededRoots=" + result.SpawnedCount.ToString(CultureInfo.InvariantCulture) +
                " addedEntities=" + addedEntityCount.ToString(CultureInfo.InvariantCulture) +
                " success=" + result.Success.ToString(CultureInfo.InvariantCulture) +
                " reason=" + Native.First(result.FailureReason, "none") +
                (stopReason.Length == 0 ? "." : "; " + stopReason);
            LogMutation(action, result.Success, result.Message);
            return new SpawnActionResult(
                result,
                addedEntityCount,
                stopReason);
        }

        private IReadOnlyList<object> FindAnimalPositions(
            object room,
            object manager,
            int width,
            int count)
        {
            Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
            object? start = Native.Read(api, "AgentRoomCellPosition");
            Type? utils = Native.Resolve("DolocTown.AnimalUtils, Assembly-CSharp");
            MethodInfo? walkable = Native.Method(
                utils,
                "IsPositionWalkable",
                3,
                true);
            if (start == null || walkable == null)
                return Array.Empty<object>();
            int startX = (int)Math.Round(Native.Vector(start, "x"));
            int startY = (int)Math.Round(Native.Vector(start, "y"));
            var occupied = new HashSet<long>();
            foreach (object animal in Native.Enumerate(Native.Read(manager, "AllAnimals")))
            {
                foreach (object cell in Native.Enumerate(Native.Read(animal, "CoverPositions")))
                    occupied.Add(CellKey(cell));
            }
            var positions = new List<object>(count);
            var walkableArguments =
                new object?[] { room, null, (object)width };
            for (int radius = 0; radius <= 64 && positions.Count < count; radius++)
            {
                AddManhattanRing(
                    startX,
                    startY,
                    radius,
                    width,
                    count,
                    walkable,
                    walkableArguments,
                    occupied,
                    positions);
            }
            return positions;
        }

        private static void AddManhattanRing(
            int centerX,
            int centerY,
            int radius,
            int width,
            int count,
            MethodInfo walkable,
            object?[] walkableArguments,
            HashSet<long> occupied,
            List<object> positions)
        {
            if (radius == 0)
            {
                TryAddAnimalPosition(
                    centerX,
                    centerY,
                    width,
                    walkable,
                    walkableArguments,
                    occupied,
                    positions);
                return;
            }
            for (int dy = -radius; dy <= radius; dy++)
            {
                int dx = radius - Math.Abs(dy);
                TryAddAnimalPosition(
                    centerX - dx,
                    centerY + dy,
                    width,
                    walkable,
                    walkableArguments,
                    occupied,
                    positions);
                if (positions.Count >= count)
                    return;
                if (dx == 0)
                    continue;
                TryAddAnimalPosition(
                    centerX + dx,
                    centerY + dy,
                    width,
                    walkable,
                    walkableArguments,
                    occupied,
                    positions);
                if (positions.Count >= count)
                    return;
            }
        }

        private static void TryAddAnimalPosition(
            int x,
            int y,
            int width,
            MethodInfo walkable,
            object?[] walkableArguments,
            HashSet<long> occupied,
            List<object> positions)
        {
            object? position = Native.Vector2Int(x, y);
            if (position == null)
                return;
            walkableArguments[1] = position;
            bool valid = walkable.Invoke(
                    null,
                    walkableArguments) is bool result &&
                result;
            if (!valid)
                return;
            for (int offset = 0; offset < width; offset++)
            {
                if (occupied.Contains(CellKey(x + offset, y)))
                    return;
            }
            for (int offset = 0; offset < width; offset++)
                occupied.Add(CellKey(x + offset, y));
            positions.Add(position);
        }

        private static long CellKey(object value) =>
            CellKey(
                (int)Math.Round(Native.Vector(value, "x")),
                (int)Math.Round(Native.Vector(value, "y")));

        private static long CellKey(int x, int y) =>
            ((long)x << 32) | (uint)y;

        private void ApplyAnimalState(object entity, AnimalCatalogOption option)
        {
            if (option.State == AnimalSpawnState.Child)
                return;
            MethodInfo? setAdult = Native.Method(
                entity.GetType(),
                "DEBUG_SetAdult",
                1,
                false);
            if (setAdult == null)
                throw new MissingMethodException("Animal.DEBUG_SetAdult");
            setAdult.Invoke(entity, BoxedTrueArguments);
            if (!Native.Bool(entity, "IsAdult"))
                throw new InvalidOperationException("Animal did not enter adult state.");
            if (option.State != AnimalSpawnState.Ready)
                return;
            if (!Native.Write(entity, "Metabolism", 100f) ||
                !Native.Write(entity, "Mood", 100) ||
                !Native.Write(entity, "Energy", 100f))
                throw new InvalidOperationException("Animal ready-state properties are not writable.");
            MethodInfo? setHusbandry = Native.Method(
                entity.GetType(),
                "DEBUG_SetHusbandryValue",
                2,
                false);
            object? info = TableGetOrDefault("TbHusbandry", option.AnimalId);
            if (setHusbandry == null || info == null)
                throw new InvalidOperationException("TbHusbandry ready-state owner is unavailable.");
            var husbandryArguments = new object?[2];
            foreach (object data in Native.Enumerate(Native.Read(info, "HusbandryDatas")))
            {
                string output = Native.Text(data, "Output");
                int threshold = Native.Int(data, "Threshold", -1);
                if (output.Length == 0 || threshold < 0)
                    throw new InvalidOperationException("TbHusbandry contains an invalid output threshold.");
                husbandryArguments[0] = output;
                husbandryArguments[1] = threshold;
                setHusbandry.Invoke(entity, husbandryArguments);
                if (!TryReadDictionaryInt(
                        Native.Read(entity, "husbandryValues"),
                        output,
                        out int observed) ||
                    observed < threshold)
                {
                    throw new InvalidOperationException(
                        "Animal husbandry threshold verification failed for " + output + ".");
                }
            }
            if (Math.Abs(Native.Double(entity, "Metabolism") - 100d) > 0.01d ||
                Native.Int(entity, "Mood") != 100 ||
                Math.Abs(Native.Double(entity, "Energy") - 100d) > 0.01d)
                throw new InvalidOperationException("Animal ready-state verification failed.");
        }

        private static bool TryReadDictionaryInt(
            object? dictionary,
            string key,
            out int value)
        {
            value = 0;
            if (!(dictionary is IDictionary values) || !values.Contains(key))
                return false;
            try
            {
                value = Convert.ToInt32(values[key], CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Exception Unwrap(Exception error) =>
            error is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : error;

        private static readonly object?[] BoxedTrueArguments =
            new object?[] { true };

        private sealed class ReferenceObjectComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceObjectComparer Instance =
                new ReferenceObjectComparer();

            public new bool Equals(object? left, object? right) =>
                ReferenceEquals(left, right);

            public int GetHashCode(object value) =>
                RuntimeHelpers.GetHashCode(value);
        }

        private readonly struct NativeInterfaceKey :
            IEquatable<NativeInterfaceKey>
        {
            internal NativeInterfaceKey(Type hostType, string fullName)
            {
                HostType = hostType;
                FullName = fullName;
            }

            internal Type HostType { get; }
            private string FullName { get; }

            public bool Equals(NativeInterfaceKey other) =>
                ReferenceEquals(HostType, other.HostType) &&
                string.Equals(
                    FullName,
                    other.FullName,
                    StringComparison.Ordinal);

            public override bool Equals(object? obj) =>
                obj is NativeInterfaceKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (HostType.GetHashCode() * 397) ^
                        StringComparer.Ordinal.GetHashCode(FullName);
                }
            }
        }

        private readonly struct NativeInterfaceResolution
        {
            internal NativeInterfaceResolution(Type? value)
            {
                Interface = value;
            }

            internal Type? Interface { get; }
        }
    }
}
