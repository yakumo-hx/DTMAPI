using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal sealed class SaveLoadRequestCoordinatorService
    {
        private const int MaxRequests = 64;
        private const int MaxTimeline = 128;
        private readonly object gate = new object();
        private readonly List<MutableSaveLoadRequest> requests = new List<MutableSaveLoadRequest>();
        private readonly Queue<SaveLoadRequestTimelineEntry> timeline = new Queue<SaveLoadRequestTimelineEntry>();
        private int nextRequestNumber = 1;
        private int duplicateRequests;
        private int suppressedDuplicateRequests;
        private int nativeEnterCount;
        private int nativeReturnCount;
        private int saveLoadedDispatchCount;
        private int timeoutCount;

        public SaveLoadRequestDecision TryBeginDtmapiRequest(int slot, string owner, string source, int threadId, string phase)
        {
            lock (gate)
            {
                MutableSaveLoadRequest? active = FindActiveNoLock();
                if (active != null && active.Slot == slot)
                {
                    duplicateRequests++;
                    suppressedDuplicateRequests++;
                    AddTimelineNoLock(
                        "SuppressedDuplicate",
                        active.RequestId,
                        slot,
                        owner,
                        source,
                        threadId,
                        phase,
                        "Suppressed duplicate DTMAPI/smoke LoadGame request while request " + active.RequestId + " is active.");
                    return new SaveLoadRequestDecision(true, active.RequestId, BuildUpdateNoLock());
                }

                MutableSaveLoadRequest request = CreateRequestNoLock(slot, owner, source, threadId, phase, dtmapiOrigin: true);
                AddTimelineNoLock("DtmapiRequest", request.RequestId, slot, owner, source, threadId, phase, "DTMAPI/smoke LoadGame request accepted.");
                return new SaveLoadRequestDecision(false, request.RequestId, BuildUpdateNoLock());
            }
        }

        public SaveLoadRequestUpdate RecordNativeEnter(int slot, string owner, string source, int threadId, string phase)
        {
            lock (gate)
            {
                nativeEnterCount++;
                MutableSaveLoadRequest? active = FindActiveNoLock();
                MutableSaveLoadRequest request;
                if (active != null && active.Slot == slot && active.DtmapiOrigin && !active.NativeEntered)
                {
                    request = active;
                }
                else
                {
                    if (active != null && active.Slot == slot && !active.Completed)
                        duplicateRequests++;
                    request = CreateRequestNoLock(slot, owner, source, threadId, phase, dtmapiOrigin: false);
                }

                request.NativeEntered = true;
                request.NativeEnterAt = DateTimeOffset.Now;
                request.NativeEnterThreadId = threadId;
                request.NativeEnterPhase = phase ?? string.Empty;
                request.Status = SaveLoadRequestStatus.NativeEntered;
                AddTimelineNoLock("NativeEnter", request.RequestId, slot, owner, source, threadId, phase ?? string.Empty, "Native LoadGame entered.");
                return BuildUpdateNoLock();
            }
        }

        public SaveLoadRequestUpdate RecordNativeReturn(int slot, bool result, string owner, string source, int threadId, string phase)
        {
            lock (gate)
            {
                nativeReturnCount++;
                MutableSaveLoadRequest? request = FindLatestForSlotWithoutNativeReturnNoLock(slot)
                    ?? FindLatestOpenForSlotNoLock(slot)
                    ?? FindActiveNoLock();
                if (request == null)
                    request = CreateRequestNoLock(slot, owner, source, threadId, phase, dtmapiOrigin: false);

                request.NativeReturned = true;
                request.NativeReturnAt = DateTimeOffset.Now;
                request.NativeReturnThreadId = threadId;
                request.NativeReturnPhase = phase ?? string.Empty;
                request.NativeReturnResult = result;
                if (!request.Completed && !request.TimedOut)
                {
                    request.Status = result ? SaveLoadRequestStatus.NativeReturned : SaveLoadRequestStatus.NativeFailed;
                    request.Completed = !result;
                }
                AddTimelineNoLock("NativeReturn", request.RequestId, slot, owner, source, threadId, phase ?? string.Empty, "Native LoadGame returned result=" + (result ? "true" : "false") + ".");
                return BuildUpdateNoLock();
            }
        }

        public SaveLoadRequestUpdate RecordSaveLoaded(int? slot, bool isNewGame, string owner, string source, int threadId, string phase)
        {
            lock (gate)
            {
                saveLoadedDispatchCount++;
                MutableSaveLoadRequest? request = slot.HasValue
                    ? FindLatestOpenForSlotNoLock(slot.Value)
                    : FindActiveNoLock();
                if (request == null)
                    request = CreateRequestNoLock(slot, owner, source, threadId, phase, dtmapiOrigin: false);

                request.SaveLoadedDispatched = true;
                request.SaveLoadedAt = DateTimeOffset.Now;
                request.SaveLoadedThreadId = threadId;
                request.SaveLoadedPhase = phase ?? string.Empty;
                request.SaveLoadedIsNewGame = isNewGame;
                request.Completed = true;
                request.Status = SaveLoadRequestStatus.SaveLoaded;
                AddTimelineNoLock("SaveLoaded", request.RequestId, slot, owner, source, threadId, phase ?? string.Empty, "SaveLoaded dispatched isNewGame=" + (isNewGame ? "true" : "false") + ".");
                return BuildUpdateNoLock();
            }
        }

        public SaveLoadRequestUpdate RecordTimeout(string owner, string source, string details, int threadId, string phase)
        {
            lock (gate)
            {
                timeoutCount++;
                MutableSaveLoadRequest? request = FindActiveNoLock();
                if (request != null)
                {
                    request.TimedOut = true;
                    request.Status = SaveLoadRequestStatus.Timeout;
                }
                AddTimelineNoLock("Timeout", request?.RequestId ?? string.Empty, request?.Slot, owner, source, threadId, phase, details);
                return BuildUpdateNoLock();
            }
        }

        public SaveLoadRequestSnapshot GetSnapshot()
        {
            lock (gate)
            {
                return BuildSnapshotNoLock();
            }
        }

        public string GetCurrentRequestIdForDiagnostics()
        {
            lock (gate)
            {
                MutableSaveLoadRequest? active = FindActiveNoLock();
                if (active != null)
                    return active.RequestId;

                for (int i = requests.Count - 1; i >= 0; i--)
                {
                    if (!string.IsNullOrWhiteSpace(requests[i].RequestId))
                        return requests[i].RequestId;
                }

                return string.Empty;
            }
        }

        public SaveLoadRequestDiagnosticCounts GetDiagnosticCounts()
        {
            lock (gate)
            {
                return new SaveLoadRequestDiagnosticCounts(
                    nativeEnterCount,
                    nativeReturnCount,
                    saveLoadedDispatchCount);
            }
        }

        private MutableSaveLoadRequest CreateRequestNoLock(int? slot, string owner, string source, int threadId, string phase, bool dtmapiOrigin)
        {
            string requestId = "SL-" + nextRequestNumber.ToString("0000", CultureInfo.InvariantCulture);
            nextRequestNumber++;
            var request = new MutableSaveLoadRequest(
                requestId,
                slot,
                owner ?? string.Empty,
                source ?? string.Empty,
                threadId,
                phase ?? string.Empty,
                DateTimeOffset.Now,
                dtmapiOrigin);
            requests.Add(request);
            while (requests.Count > MaxRequests)
                requests.RemoveAt(0);
            return request;
        }

        private MutableSaveLoadRequest? FindActiveNoLock()
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                MutableSaveLoadRequest request = requests[i];
                if (!request.Completed && !request.TimedOut)
                    return request;
            }
            return null;
        }

        private MutableSaveLoadRequest? FindLatestOpenForSlotNoLock(int slot)
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                MutableSaveLoadRequest request = requests[i];
                if (request.Slot == slot && !request.Completed && !request.TimedOut)
                    return request;
            }
            return null;
        }

        private MutableSaveLoadRequest? FindLatestForSlotWithoutNativeReturnNoLock(int slot)
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                MutableSaveLoadRequest request = requests[i];
                if (request.Slot == slot && !request.NativeReturned)
                    return request;
            }
            return null;
        }

        private void AddTimelineNoLock(string kind, string requestId, int? slot, string owner, string source, int threadId, string phase, string details)
        {
            timeline.Enqueue(new SaveLoadRequestTimelineEntry(
                kind ?? string.Empty,
                requestId ?? string.Empty,
                slot,
                owner ?? string.Empty,
                source ?? string.Empty,
                threadId,
                phase ?? string.Empty,
                details ?? string.Empty,
                DateTimeOffset.Now));
            while (timeline.Count > MaxTimeline)
                timeline.Dequeue();
        }

        private SaveLoadRequestUpdate BuildUpdateNoLock()
        {
            return new SaveLoadRequestUpdate(BuildSnapshotNoLock());
        }

        private SaveLoadRequestSnapshot BuildSnapshotNoLock()
        {
            MutableSaveLoadRequest? active = FindActiveNoLock();
            SaveLoadRequestTimelineEntry? last = timeline.Count == 0 ? null : timeline.Last();
            return new SaveLoadRequestSnapshot(
                requests.Select(request => request.ToRequest()).ToArray(),
                timeline.ToArray(),
                active?.RequestId ?? string.Empty,
                active?.Slot,
                duplicateRequests,
                suppressedDuplicateRequests,
                nativeEnterCount,
                nativeReturnCount,
                saveLoadedDispatchCount,
                timeoutCount,
                last);
        }

        private sealed class MutableSaveLoadRequest
        {
            public MutableSaveLoadRequest(string requestId, int? slot, string owner, string source, int threadId, string phase, DateTimeOffset requestedAt, bool dtmapiOrigin)
            {
                RequestId = requestId;
                Slot = slot;
                Owner = owner ?? string.Empty;
                Source = source ?? string.Empty;
                ThreadId = threadId;
                Phase = phase ?? string.Empty;
                RequestedAt = requestedAt;
                DtmapiOrigin = dtmapiOrigin;
                Status = SaveLoadRequestStatus.Requested;
            }

            public string RequestId { get; }
            public int? Slot { get; }
            public string Owner { get; }
            public string Source { get; }
            public int ThreadId { get; }
            public string Phase { get; }
            public DateTimeOffset RequestedAt { get; }
            public bool DtmapiOrigin { get; }
            public string Status { get; set; }
            public bool NativeEntered { get; set; }
            public bool NativeReturned { get; set; }
            public bool NativeReturnResult { get; set; }
            public bool SaveLoadedDispatched { get; set; }
            public bool SaveLoadedIsNewGame { get; set; }
            public bool Completed { get; set; }
            public bool TimedOut { get; set; }
            public int NativeEnterThreadId { get; set; }
            public int NativeReturnThreadId { get; set; }
            public int SaveLoadedThreadId { get; set; }
            public string NativeEnterPhase { get; set; } = string.Empty;
            public string NativeReturnPhase { get; set; } = string.Empty;
            public string SaveLoadedPhase { get; set; } = string.Empty;
            public DateTimeOffset? NativeEnterAt { get; set; }
            public DateTimeOffset? NativeReturnAt { get; set; }
            public DateTimeOffset? SaveLoadedAt { get; set; }

            public SaveLoadRequestEntry ToRequest()
            {
                return new SaveLoadRequestEntry(
                    RequestId,
                    Slot,
                    Owner,
                    Source,
                    ThreadId,
                    Phase,
                    RequestedAt,
                    DtmapiOrigin,
                    Status,
                    NativeEntered,
                    NativeReturned,
                    NativeReturnResult,
                    SaveLoadedDispatched,
                    SaveLoadedIsNewGame,
                    Completed,
                    TimedOut,
                    NativeEnterThreadId,
                    NativeReturnThreadId,
                    SaveLoadedThreadId,
                    NativeEnterPhase,
                    NativeReturnPhase,
                    SaveLoadedPhase,
                    NativeEnterAt,
                    NativeReturnAt,
                    SaveLoadedAt);
            }
        }
    }

    internal static class SaveLoadRequestStatus
    {
        public const string Requested = "requested";
        public const string NativeEntered = "native-entered";
        public const string NativeReturned = "native-returned";
        public const string NativeFailed = "native-failed";
        public const string SaveLoaded = "save-loaded";
        public const string Timeout = "timeout";
    }

    internal sealed class SaveLoadRequestDecision
    {
        public SaveLoadRequestDecision(bool suppressed, string requestId, SaveLoadRequestUpdate update)
        {
            Suppressed = suppressed;
            RequestId = requestId ?? string.Empty;
            Update = update;
        }

        public bool Suppressed { get; }
        public string RequestId { get; }
        public SaveLoadRequestUpdate Update { get; }
    }

    internal readonly struct SaveLoadRequestDiagnosticCounts
    {
        public SaveLoadRequestDiagnosticCounts(int nativeEnterCount, int nativeReturnCount, int saveLoadedDispatchCount)
        {
            NativeEnterCount = nativeEnterCount;
            NativeReturnCount = nativeReturnCount;
            SaveLoadedDispatchCount = saveLoadedDispatchCount;
        }

        public int NativeEnterCount { get; }

        public int NativeReturnCount { get; }

        public int SaveLoadedDispatchCount { get; }
    }

    internal sealed class SaveLoadRequestUpdate
    {
        public SaveLoadRequestUpdate(SaveLoadRequestSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public SaveLoadRequestSnapshot Snapshot { get; }
    }

    internal sealed class SaveLoadRequestSnapshot
    {
        public SaveLoadRequestSnapshot(
            IReadOnlyList<SaveLoadRequestEntry> requests,
            IReadOnlyList<SaveLoadRequestTimelineEntry> timeline,
            string activeRequestId,
            int? activeSlot,
            int duplicateRequests,
            int suppressedDuplicateRequests,
            int nativeEnterCount,
            int nativeReturnCount,
            int saveLoadedDispatchCount,
            int timeoutCount,
            SaveLoadRequestTimelineEntry? lastEntry)
        {
            Requests = requests ?? Array.Empty<SaveLoadRequestEntry>();
            Timeline = timeline ?? Array.Empty<SaveLoadRequestTimelineEntry>();
            ActiveRequestId = activeRequestId ?? string.Empty;
            ActiveSlot = activeSlot;
            DuplicateRequests = duplicateRequests;
            SuppressedDuplicateRequests = suppressedDuplicateRequests;
            NativeEnterCount = nativeEnterCount;
            NativeReturnCount = nativeReturnCount;
            SaveLoadedDispatchCount = saveLoadedDispatchCount;
            TimeoutCount = timeoutCount;
            LastEntry = lastEntry;
        }

        public IReadOnlyList<SaveLoadRequestEntry> Requests { get; }
        public IReadOnlyList<SaveLoadRequestTimelineEntry> Timeline { get; }
        public string ActiveRequestId { get; }
        public int? ActiveSlot { get; }
        public int DuplicateRequests { get; }
        public int SuppressedDuplicateRequests { get; }
        public int NativeEnterCount { get; }
        public int NativeReturnCount { get; }
        public int SaveLoadedDispatchCount { get; }
        public int TimeoutCount { get; }
        public SaveLoadRequestTimelineEntry? LastEntry { get; }
        public bool HasActiveRequest => !string.IsNullOrWhiteSpace(ActiveRequestId);
        public bool HasUnsuppressedDuplicates => DuplicateRequests > SuppressedDuplicateRequests;
        public string Status => TimeoutCount > 0 || HasUnsuppressedDuplicates ? "warning" : "ok";
        public SaveLoadRequestEntry? ActiveRequest => Requests.LastOrDefault(request => request.RequestId.Equals(ActiveRequestId, StringComparison.Ordinal));
        public SaveLoadRequestEntry? LatestRequest => Requests.LastOrDefault();
        public string BoundaryStatus
        {
            get
            {
                SaveLoadRequestEntry? active = ActiveRequest;
                if (active != null)
                {
                    if (!active.SaveLoadedDispatched)
                        return "loading";
                    return "closed";
                }

                SaveLoadRequestEntry? latest = LatestRequest;
                if (latest == null)
                    return "idle";
                if (latest.TimedOut)
                    return "timeout";
                if (latest.SaveLoadedDispatched || latest.Completed || latest.NativeReturned)
                    return "closed";
                if (latest.NativeEntered)
                    return "loading";
                return "idle";
            }
        }

        public string FormatSummary()
        {
            SaveLoadRequestEntry? active = ActiveRequest;
            string activeSummary = active == null
                ? "none"
                : active.RequestId +
                  ":slot=" + (active.Slot.HasValue ? active.Slot.Value.ToString(CultureInfo.InvariantCulture) : "unknown") +
                  ";owner=" + SingleLine(active.Owner) +
                  ";source=" + SingleLine(active.Source) +
                  ";status=" + SingleLine(active.Status) +
                  ";nativeEnter=" + (active.NativeEntered ? "true" : "false") +
                  ";nativeReturn=" + (active.NativeReturned ? "true" : "false") +
                  ";saveLoaded=" + (active.SaveLoadedDispatched ? "true" : "false");
            string lastSummary = LastEntry == null
                ? "none"
                : LastEntry.Kind + ":" + (string.IsNullOrWhiteSpace(LastEntry.RequestId) ? "none" : LastEntry.RequestId) +
                  ":slot=" + (LastEntry.Slot.HasValue ? LastEntry.Slot.Value.ToString(CultureInfo.InvariantCulture) : "unknown") +
                  ":phase=" + SingleLine(LastEntry.Phase) +
                  ":source=" + SingleLine(LastEntry.Source);
            return "status=" + Status +
                "; requests=" + Requests.Count.ToString(CultureInfo.InvariantCulture) +
                "; active=" + activeSummary +
                "; duplicateRequests=" + DuplicateRequests.ToString(CultureInfo.InvariantCulture) +
                "; suppressedDuplicates=" + SuppressedDuplicateRequests.ToString(CultureInfo.InvariantCulture) +
                "; nativeEnter=" + NativeEnterCount.ToString(CultureInfo.InvariantCulture) +
                "; nativeReturn=" + NativeReturnCount.ToString(CultureInfo.InvariantCulture) +
                "; saveLoaded=" + SaveLoadedDispatchCount.ToString(CultureInfo.InvariantCulture) +
                "; timeouts=" + TimeoutCount.ToString(CultureInfo.InvariantCulture) +
                "; last=" + lastSummary;
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace(";", ",").Trim();
        }
    }

    internal sealed class SaveLoadRequestEntry
    {
        public SaveLoadRequestEntry(
            string requestId,
            int? slot,
            string owner,
            string source,
            int threadId,
            string phase,
            DateTimeOffset requestedAt,
            bool dtmapiOrigin,
            string status,
            bool nativeEntered,
            bool nativeReturned,
            bool nativeReturnResult,
            bool saveLoadedDispatched,
            bool saveLoadedIsNewGame,
            bool completed,
            bool timedOut,
            int nativeEnterThreadId,
            int nativeReturnThreadId,
            int saveLoadedThreadId,
            string nativeEnterPhase,
            string nativeReturnPhase,
            string saveLoadedPhase,
            DateTimeOffset? nativeEnterAt,
            DateTimeOffset? nativeReturnAt,
            DateTimeOffset? saveLoadedAt)
        {
            RequestId = requestId ?? string.Empty;
            Slot = slot;
            Owner = owner ?? string.Empty;
            Source = source ?? string.Empty;
            ThreadId = threadId;
            Phase = phase ?? string.Empty;
            RequestedAt = requestedAt;
            DtmapiOrigin = dtmapiOrigin;
            Status = status ?? string.Empty;
            NativeEntered = nativeEntered;
            NativeReturned = nativeReturned;
            NativeReturnResult = nativeReturnResult;
            SaveLoadedDispatched = saveLoadedDispatched;
            SaveLoadedIsNewGame = saveLoadedIsNewGame;
            Completed = completed;
            TimedOut = timedOut;
            NativeEnterThreadId = nativeEnterThreadId;
            NativeReturnThreadId = nativeReturnThreadId;
            SaveLoadedThreadId = saveLoadedThreadId;
            NativeEnterPhase = nativeEnterPhase ?? string.Empty;
            NativeReturnPhase = nativeReturnPhase ?? string.Empty;
            SaveLoadedPhase = saveLoadedPhase ?? string.Empty;
            NativeEnterAt = nativeEnterAt;
            NativeReturnAt = nativeReturnAt;
            SaveLoadedAt = saveLoadedAt;
        }

        public string RequestId { get; }
        public int? Slot { get; }
        public string Owner { get; }
        public string Source { get; }
        public int ThreadId { get; }
        public string Phase { get; }
        public DateTimeOffset RequestedAt { get; }
        public bool DtmapiOrigin { get; }
        public string Status { get; }
        public bool NativeEntered { get; }
        public bool NativeReturned { get; }
        public bool NativeReturnResult { get; }
        public bool SaveLoadedDispatched { get; }
        public bool SaveLoadedIsNewGame { get; }
        public bool Completed { get; }
        public bool TimedOut { get; }
        public int NativeEnterThreadId { get; }
        public int NativeReturnThreadId { get; }
        public int SaveLoadedThreadId { get; }
        public string NativeEnterPhase { get; }
        public string NativeReturnPhase { get; }
        public string SaveLoadedPhase { get; }
        public DateTimeOffset? NativeEnterAt { get; }
        public DateTimeOffset? NativeReturnAt { get; }
        public DateTimeOffset? SaveLoadedAt { get; }
    }

    internal sealed class SaveLoadRequestTimelineEntry
    {
        public SaveLoadRequestTimelineEntry(string kind, string requestId, int? slot, string owner, string source, int threadId, string phase, string details, DateTimeOffset timestamp)
        {
            Kind = kind ?? string.Empty;
            RequestId = requestId ?? string.Empty;
            Slot = slot;
            Owner = owner ?? string.Empty;
            Source = source ?? string.Empty;
            ThreadId = threadId;
            Phase = phase ?? string.Empty;
            Details = details ?? string.Empty;
            Timestamp = timestamp;
        }

        public string Kind { get; }
        public string RequestId { get; }
        public int? Slot { get; }
        public string Owner { get; }
        public string Source { get; }
        public int ThreadId { get; }
        public string Phase { get; }
        public string Details { get; }
        public DateTimeOffset Timestamp { get; }
    }
}
