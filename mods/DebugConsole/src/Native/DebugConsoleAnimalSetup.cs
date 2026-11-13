using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal static class DebugConsoleAnimalSetup
    {
        internal static IReadOnlyList<object> FindPositions(
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
            object? dimensions = Native.Read(Native.Read(room, "DM_terrain"), "Size");
            int roomWidth = (int)Native.Vector(dimensions, "x");
            int roomHeight = (int)Native.Vector(dimensions, "y");
            int furthest = Math.Max(startX, roomWidth - 1 - startX) + Math.Max(startY, roomHeight - 1 - startY);
            for (int radius = 0; radius <= furthest && positions.Count < count; radius++)
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

        internal static void ApplyState(object entity, AnimalCatalogOption option, object home)
        {
            Native.Method(entity.GetType(), "SetHomeRoom", 1, false)!.Invoke(entity, new[] { home });
            if (Native.Read(entity, "homeRoom") == null)
                throw new InvalidOperationException("Animal home was not assigned.");
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
            object? table = Native.Table("TbHusbandry");
            object? info = Native.Method(table?.GetType(), "GetOrDefault", 1, false)?.Invoke(table, new object[] { option.AnimalId });
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
            value = Convert.ToInt32(values[key], CultureInfo.InvariantCulture);
            return true;
        }

        private static readonly object?[] BoxedTrueArguments = { true };
    }
}
