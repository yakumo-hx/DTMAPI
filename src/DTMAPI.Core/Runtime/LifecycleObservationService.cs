using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal sealed class LifecycleObservationService
    {
        private const int MaxEntries = 96;
        private readonly object gate = new object();
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<LifecycleObservationEntry> entries = new Queue<LifecycleObservationEntry>();

        public LifecycleObservationEntry Record(
            string phase,
            int threadId,
            int? saveSlot,
            bool? isNewGame,
            int discoveredModCount,
            int loadedModCount,
            int hookStatusCount)
        {
            string normalizedPhase = string.IsNullOrWhiteSpace(phase) ? "Unknown" : phase.Trim();
            lock (gate)
            {
                counts.TryGetValue(normalizedPhase, out int currentCount);
                currentCount++;
                counts[normalizedPhase] = currentCount;

                var entry = new LifecycleObservationEntry(
                    normalizedPhase,
                    currentCount,
                    DateTimeOffset.Now,
                    threadId,
                    saveSlot,
                    isNewGame,
                    discoveredModCount,
                    loadedModCount,
                    hookStatusCount);
                entries.Enqueue(entry);
                while (entries.Count > MaxEntries)
                    entries.Dequeue();
                return entry;
            }
        }

        public LifecycleObservationSnapshot GetSnapshot()
        {
            lock (gate)
            {
                return new LifecycleObservationSnapshot(
                    counts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                    entries.ToArray());
            }
        }

        public string FormatSummary()
        {
            LifecycleObservationSnapshot snapshot = GetSnapshot();
            string countsSummary = snapshot.Counts.Count == 0
                ? "none"
                : string.Join(",", snapshot.Counts.Select(pair => pair.Key + "=" + pair.Value).ToArray());
            LifecycleObservationEntry? last = snapshot.Entries.LastOrDefault();
            string lastSummary = last == null
                ? "none"
                : last.Phase + "#" + last.Count + "@" + last.ObservedAt.ToString("o") +
                    " thread=" + last.ThreadId +
                    " slot=" + (last.SaveSlot.HasValue ? last.SaveSlot.Value.ToString() : "unknown") +
                    " isNewGame=" + (last.IsNewGame.HasValue ? (last.IsNewGame.Value ? "true" : "false") : "unknown") +
                    " discovered=" + last.DiscoveredModCount +
                    " loaded=" + last.LoadedModCount +
                    " hooks=" + last.HookStatusCount;
            return "phases=" + countsSummary + "; entries=" + snapshot.Entries.Count + "; last=" + lastSummary;
        }
    }

    internal sealed class LifecycleObservationSnapshot
    {
        public LifecycleObservationSnapshot(IReadOnlyDictionary<string, int> counts, IReadOnlyList<LifecycleObservationEntry> entries)
        {
            Counts = counts;
            Entries = entries;
        }

        public IReadOnlyDictionary<string, int> Counts { get; }
        public IReadOnlyList<LifecycleObservationEntry> Entries { get; }
    }

    internal sealed class LifecycleObservationEntry
    {
        public LifecycleObservationEntry(
            string phase,
            int count,
            DateTimeOffset observedAt,
            int threadId,
            int? saveSlot,
            bool? isNewGame,
            int discoveredModCount,
            int loadedModCount,
            int hookStatusCount)
        {
            Phase = phase ?? string.Empty;
            Count = count;
            ObservedAt = observedAt;
            ThreadId = threadId;
            SaveSlot = saveSlot;
            IsNewGame = isNewGame;
            DiscoveredModCount = discoveredModCount;
            LoadedModCount = loadedModCount;
            HookStatusCount = hookStatusCount;
        }

        public string Phase { get; }
        public int Count { get; }
        public DateTimeOffset ObservedAt { get; }
        public int ThreadId { get; }
        public int? SaveSlot { get; }
        public bool? IsNewGame { get; }
        public int DiscoveredModCount { get; }
        public int LoadedModCount { get; }
        public int HookStatusCount { get; }
    }
}
