using System;
using System.Collections;
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
                return monsterCatalog.Concat(ModuleMonsters()).ToArray();
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
            var protos = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            object rows = Native.Read(monsters, "TotalProtos")
                ?? throw new InvalidOperationException("Native monster catalog is not ready.");
            var sources = runtime.GetContentSources("monster_tbmonster");
            foreach (object proto in Native.Enumerate(rows))
            {
                string id = Native.TextFirst(
                    proto,
                    "Name",
                    "SpawnId",
                    "Id");
                if (id.Length == 0 || id.StartsWith("dtm_mc_", StringComparison.Ordinal) || protos.ContainsKey(id))
                    continue;
                protos[id] = proto;
                object? document = TableGetOrDefault("TbMonsterDocument", id);
                object? spriteAsset = Native.Read(document, "UiSpriteAsset");
                InventoryDebugSourceGroup? source = DebugConsoleContentSources.Resolve(sources, id);
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
                    Icon = Native.Read(spriteAsset, "Asset"),
                    SourceId = source?.Id ?? string.Empty,
                    SourceDisplayName = source?.DisplayName ?? string.Empty,
                    IsModSource = source?.IsModSource ?? false
                });
            }
            SpawnCatalogOption[] completed = options
                .OrderBy(option => option.Id, StringComparer.Ordinal)
                .ToArray();
            monsterProtos = protos;
            monsterCatalog = completed;
        }

        private void EnsureAnimalCatalog()
        {
            if (animalCatalog != null)
                return;
            Dictionary<string, AnimalContentSource> contentSources = runtime
                .GetAnimalContentSources()
                .ToDictionary(
                    source => source.AnimalId,
                    StringComparer.OrdinalIgnoreCase);
            var options = new List<AnimalCatalogOption>();
            var protos = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            object rows = Native.TableList("TbAnimal")
                ?? throw new InvalidOperationException("Native animal catalog is not ready.");
            var nativeSources = runtime.GetContentSources("animal_tbanimal");
            foreach (object proto in Native.Enumerate(rows))
            {
                string id = Native.Text(proto, "Id").Trim();
                if (id.Length == 0 || id.StartsWith("dtm_mc_", StringComparison.Ordinal) || protos.ContainsKey(id))
                    continue;
                protos[id] = proto;
                InventoryDebugSourceGroup? source = DebugConsoleContentSources.Resolve(nativeSources, id);
                AnimalContentSource? contentSource = null;
                if (source?.IsModSource == true)
                    contentSources.TryGetValue(id, out contentSource);
                if (contentSource != null && nativeSources!.ContainsKey(id) &&
                    !source!.Id.Equals(contentSource.SourceId, StringComparison.OrdinalIgnoreCase))
                {
                    contentSource = new AnimalContentSource
                    {
                        AnimalId = id, SourceId = "__mods", SourceKind = "Mod", EnablementKnown = false
                    };
                }
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
                        SourceId = contentSource?.SourceId ?? source?.Id ?? string.Empty,
                        SourceDisplayName = contentSource?.DisplayName ?? source?.DisplayName ?? string.Empty,
                        SourceKind = source?.SourceKind ?? string.Empty,
                        IsModSource = source?.IsModSource ?? false,
                        SourceEnabled = contentSource?.Enabled ?? source?.Enabled ?? true,
                        SourceEnablementKnown =
                            contentSource?.EnablementKnown ?? source?.EnablementKnown ?? false,
                        WorkshopId = contentSource?.SourceId == "__mods" ? null : contentSource?.WorkshopId ?? source?.WorkshopId
                    });
                }
            }
            AnimalCatalogOption[] completed = options.ToArray();
            animalProtos = protos;
            animalCatalog = completed;
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

        private static readonly Dictionary<NativeInterfaceKey,
            NativeInterfaceResolution> NativeInterfaces =
                new Dictionary<NativeInterfaceKey,
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
            NativeInterfaces.Add(
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
            count = Math.Max(1, Math.Min(100, count));
            var result = new SpawnDebugResult
            {
                SpawnId = monsterId,
                RequestedCount = count
            };
            try
            {
                EnsureMonsterCatalog();
                var moduleOption = ModuleMonsters().FirstOrDefault(value => value.Id.Equals(monsterId, StringComparison.Ordinal));
                if (moduleOption != null) return SpawnModuleMonster(moduleOption, result);
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
            count = Math.Max(1, Math.Min(100, count));
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
                object? home = Native.Text(host, "Type") == "Farm" ? host : Native.Read(Native.Archive, "MainFarm");
                if (home == null)
                    return FailSpawn("spawn-animal", result, "native-animal-home-unavailable", "The loaded save has no animal home.");
                IReadOnlyList<object> positions = DebugConsoleAnimalSetup.FindPositions(
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

                        DebugConsoleAnimalSetup.ApplyState(entity, option, home);
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

        private static Exception Unwrap(Exception error) =>
            error is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : error;

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
