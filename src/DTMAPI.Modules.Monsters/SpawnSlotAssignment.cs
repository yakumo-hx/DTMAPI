using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Modules.Monsters
{
    // Input is a fresh native allocation, before landing/movement. Never infer an
    // allocation from the current position of an already running or hidden monster.
    internal sealed class SpawnSlotAssignment
    {
        internal readonly string[] Slots;
        internal readonly string? Diagnostic;
        internal SpawnSlotAssignment(string[] slots, string? diagnostic = null) { Slots = slots; Diagnostic = diagnostic; }

        internal static SpawnSlotAssignment Resolve(IEnumerable<(string Region, float X, float Y, int Index, int Capacity)> points,
            float x, float y, float offsetX, float offsetY, int volume)
        {
            var matches = points.Where(p => Math.Abs(p.X + offsetX - x) <= .001f && Math.Abs(p.Y + offsetY - y) <= .001f).ToArray();
            if (matches.Length != 1 || volume <= 0 || volume > matches[0].Capacity - matches[0].Index)
                return new SpawnSlotAssignment(Array.Empty<string>(), "NativeSpawnAssignmentUncertain: allocation does not identify one valid region/slot range.");
            var match = matches[0];
            return new SpawnSlotAssignment(Enumerable.Range(match.Index, volume).Select(i => match.Region + ":" + i).ToArray());
        }
    }
}
