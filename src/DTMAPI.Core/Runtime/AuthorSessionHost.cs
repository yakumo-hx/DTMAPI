using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;

namespace DTMAPI.Core.Runtime
{
    internal sealed class AuthorSessionHost : IDisposable
    {
        private const int DefaultMaximumPendingRequests = 16;
        private const int DefaultMaximumConcurrentRequests = 8;
        private static readonly TimeSpan DefaultRuntimeResponseTimeout = TimeSpan.FromSeconds(30);
        private readonly object gate = new object();
        private readonly AuthorSessionDescriptor descriptor;
        private readonly string gameRoot;
        private readonly string runtimeVersion;
        private readonly Func<DateTimeOffset> utcNow;
        private readonly Queue<PendingRequest> pending = new Queue<PendingRequest>();
        private readonly Dictionary<string, PendingRequest> activeByUniqueId = new Dictionary<string, PendingRequest>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> replayRequestIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<NamedPipeServerStream> openPipes = new HashSet<NamedPipeServerStream>();
        private readonly List<Thread> listenerThreads = new List<Thread>();
        private readonly CancellationTokenSource listenerCancellation = new CancellationTokenSource();
        private readonly ManualResetEventSlim closeCompleted = new ManualResetEventSlim(false);
        private readonly TimeSpan runtimeResponseTimeout;
        private readonly int runtimeThreadId;
        private int closeOwnerThreadId;
        private int pendingQueuedCount;
        private bool running;
        private bool closed;
        private string closeReason = string.Empty;
        private long totalConnections;
        private long totalParsed;
        private long totalEnqueued;
        private long totalProcessed;
        private long totalRejected;
        private long replayRejected;
        private long concurrentRejected;
        private long queueFullRejected;
        private long handlerFailures;

        private AuthorSessionHost(
            AuthorSessionDescriptor descriptor,
            string gameRoot,
            string runtimeVersion,
            Func<DateTimeOffset> utcNow,
            int maximumPendingRequests,
            int maximumConcurrentRequests,
            TimeSpan runtimeResponseTimeout)
        {
            this.descriptor = descriptor;
            this.gameRoot = AuthorSessionProtocol.NormalizePath(gameRoot);
            this.runtimeVersion = runtimeVersion;
            this.utcNow = utcNow;
            MaximumPendingRequests = Math.Max(1, maximumPendingRequests);
            MaximumConcurrentRequests = Math.Max(1, maximumConcurrentRequests);
            this.runtimeResponseTimeout = runtimeResponseTimeout <= TimeSpan.Zero ? DefaultRuntimeResponseTimeout : runtimeResponseTimeout;
            runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public int MaximumPendingRequests { get; }

        public int MaximumConcurrentRequests { get; }

        public string SessionId => descriptor.SessionId;

        public string PipeName => descriptor.PipeName;

        public DateTimeOffset ExpiresAtUtc => descriptor.ExpiresAt;

        public static bool TryCreate(
            AuthorSessionDescriptor descriptor,
            string gameRoot,
            string runtimeVersion,
            out AuthorSessionHost? host,
            out AuthorSessionValidationResult validation)
        {
            return TryCreate(
                descriptor,
                gameRoot,
                runtimeVersion,
                out host,
                out validation,
                () => DateTimeOffset.UtcNow,
                DefaultMaximumPendingRequests,
                DefaultMaximumConcurrentRequests,
                DefaultRuntimeResponseTimeout);
        }

        internal static bool TryCreate(
            AuthorSessionDescriptor descriptor,
            string gameRoot,
            string runtimeVersion,
            out AuthorSessionHost? host,
            out AuthorSessionValidationResult validation,
            Func<DateTimeOffset> utcNow,
            int maximumPendingRequests,
            int maximumConcurrentRequests,
            TimeSpan runtimeResponseTimeout)
        {
            host = null;
            if (utcNow == null)
            {
                validation = AuthorSessionValidationResult.Reject("clock-missing", "The author-session UTC clock is unavailable.");
                return false;
            }

            DateTimeOffset now;
            try
            {
                now = utcNow();
            }
            catch (Exception ex)
            {
                validation = AuthorSessionValidationResult.Reject("clock-failed", "The author-session UTC clock failed: " + ex.GetType().Name + ".");
                return false;
            }

            validation = AuthorSessionDescriptorStore.ValidateDescriptor(descriptor, gameRoot, runtimeVersion, now);
            if (!validation.Accepted)
                return false;

            host = new AuthorSessionHost(
                descriptor,
                gameRoot,
                runtimeVersion,
                utcNow,
                maximumPendingRequests,
                maximumConcurrentRequests,
                runtimeResponseTimeout);
            return true;
        }

        public AuthorSessionValidationResult Start()
        {
            lock (gate)
            {
                if (closed)
                    return AuthorSessionValidationResult.Reject("session-closed", "The author session is already closed.");
                if (running)
                    return AuthorSessionValidationResult.Reject("session-already-started", "The author session is already running.");
                if (Thread.CurrentThread.ManagedThreadId != runtimeThreadId)
                    return AuthorSessionValidationResult.Reject("start-wrong-thread", "The author session must start on its creating Runtime thread.");
                if (utcNow() >= descriptor.ExpiresAt)
                    return AuthorSessionValidationResult.Reject("session-expired", "The author session expired before startup.");

                running = true;
                int listenerCount = Math.Max(2, Math.Min(MaximumConcurrentRequests + 1, 8));
                for (int index = 0; index < listenerCount; index++)
                {
                    var thread = new Thread(ListenerLoop)
                    {
                        IsBackground = true,
                        Name = "DTMAPI Author Session " + (index + 1).ToString(CultureInfo.InvariantCulture)
                    };
                    listenerThreads.Add(thread);
                    thread.Start();
                }
            }

            return AuthorSessionValidationResult.Accept("session-started", "The explicit author session is listening.");
        }

        public AuthorSessionProcessResult ProcessPending(
            Func<AuthorSessionRequest, AuthorSessionOperationResult> handler,
            int maximumRequests = 8)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (Thread.CurrentThread.ManagedThreadId != runtimeThreadId)
            {
                return new AuthorSessionProcessResult(
                    false,
                    "process-wrong-thread",
                    "Author-session requests may only be processed on the Runtime thread.",
                    0,
                    0);
            }

            int limit = Math.Max(1, Math.Min(maximumRequests, 64));
            int handled = 0;
            int failed = 0;
            while (handled < limit)
            {
                PendingRequest? item = null;
                lock (gate)
                {
                    if (!running || closed)
                        break;

                    while (pending.Count > 0)
                    {
                        PendingRequest candidate = pending.Dequeue();
                        if (candidate.State != PendingRequestState.Queued)
                            continue;
                        pendingQueuedCount--;
                        candidate.State = PendingRequestState.Processing;
                        item = candidate;
                        break;
                    }
                }

                if (item == null)
                    break;

                AuthorSessionOperationResult operationResult;
                if (utcNow() >= descriptor.ExpiresAt)
                {
                    operationResult = AuthorSessionOperationResult.Rejected("session-expired", "The author session expired before Runtime processing.");
                }
                else
                {
                    try
                    {
                        operationResult = handler(item.Request) ??
                            AuthorSessionOperationResult.Error("handler-result-null", "The Runtime handler returned no result.");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        lock (gate)
                            handlerFailures++;
                        operationResult = AuthorSessionOperationResult.Error(
                            "handler-exception",
                            "The Runtime handler failed: " + ex.GetType().Name + ".");
                    }
                }

                Complete(item, CreateOperationResponse(item.Request, operationResult), PendingRequestState.Completed);
                handled++;
                lock (gate)
                    totalProcessed++;
            }

            return new AuthorSessionProcessResult(
                true,
                "process-complete",
                "Pending author-session requests were processed on the Runtime thread.",
                handled,
                failed);
        }

        public AuthorSessionHostSnapshot GetSnapshot()
        {
            lock (gate)
            {
                return new AuthorSessionHostSnapshot(
                    running,
                    closed,
                    closeReason,
                    runtimeThreadId,
                    MaximumPendingRequests,
                    MaximumConcurrentRequests,
                    pendingQueuedCount,
                    activeByUniqueId.Count,
                    replayRequestIds.Count,
                    totalConnections,
                    totalParsed,
                    totalEnqueued,
                    totalProcessed,
                    totalRejected,
                    replayRejected,
                    concurrentRejected,
                    queueFullRejected,
                    handlerFailures);
            }
        }

        public void Close(string reason)
        {
            List<NamedPipeServerStream> pipes = new List<NamedPipeServerStream>();
            List<Thread> threads = new List<Thread>();
            List<PendingRequest> active = new List<PendingRequest>();
            int callerThreadId = Thread.CurrentThread.ManagedThreadId;
            bool ownsClose = false;
            lock (gate)
            {
                if (!closed)
                {
                    ownsClose = true;
                    closeOwnerThreadId = callerThreadId;
                    closed = true;
                    running = false;
                    closeReason = AuthorSessionProtocol.BoundedSingleLine(reason, 128);
                    pipes = openPipes.ToList();
                    openPipes.Clear();
                    threads = listenerThreads.ToList();
                    active = activeByUniqueId.Values.Distinct().ToList();
                    activeByUniqueId.Clear();
                    pending.Clear();
                    pendingQueuedCount = 0;
                    replayRequestIds.Clear();

                    foreach (PendingRequest item in active)
                    {
                        if (item.State == PendingRequestState.Completed || item.State == PendingRequestState.Canceled)
                            continue;
                        item.State = PendingRequestState.Canceled;
                        item.Response = CreateTransportResponse(
                            item.Request,
                            "rejected",
                            "session-closed",
                            "The author session closed before the request completed.");
                    }
                }
            }

            if (!ownsClose)
            {
                if (callerThreadId != closeOwnerThreadId)
                    closeCompleted.Wait(TimeSpan.FromSeconds(3));
                return;
            }

            try
            {
                try
                {
                    listenerCancellation.Cancel();
                }
                catch
                {
                    // Pipe disposal below remains a second deterministic listener-stop signal.
                }

                foreach (PendingRequest item in active)
                    item.ResponseReady.Set();

                foreach (NamedPipeServerStream pipe in pipes)
                {
                    try
                    {
                        pipe.Dispose();
                    }
                    catch
                    {
                        // Closing a pipe is best-effort after cancellation has already stopped acceptance.
                    }
                }

                DateTimeOffset joinDeadline = DateTimeOffset.UtcNow.AddSeconds(2);
                foreach (Thread thread in threads)
                {
                    if (thread == Thread.CurrentThread)
                        continue;
                    TimeSpan remaining = joinDeadline - DateTimeOffset.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;
                    try
                    {
                        thread.Join(remaining);
                    }
                    catch
                    {
                        // Threads are background-only and observe cancellation or the closed flag at their next boundary.
                    }
                }
            }
            finally
            {
                closeCompleted.Set();
            }
        }

        public void Dispose()
        {
            Close("dispose");
        }

        private void ListenerLoop()
        {
            while (true)
            {
                NamedPipeServerStream? pipe = null;
                try
                {
                    lock (gate)
                    {
                        if (!running || closed)
                            return;
                    }

                    pipe = new NamedPipeServerStream(
                        descriptor.PipeName,
                        PipeDirection.InOut,
                        Math.Max(2, Math.Min(MaximumConcurrentRequests + 1, 8)),
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        4096,
                        4096);

                    lock (gate)
                    {
                        if (!running || closed)
                            return;
                        openPipes.Add(pipe);
                    }

                    pipe.WaitForConnectionAsync(listenerCancellation.Token).GetAwaiter().GetResult();
                    lock (gate)
                        totalConnections++;
                    HandleConnection(pipe);
                }
                catch (OperationCanceledException)
                {
                    lock (gate)
                    {
                        if (closed)
                            return;
                        totalRejected++;
                    }
                }
                catch (ObjectDisposedException)
                {
                    lock (gate)
                    {
                        if (closed)
                            return;
                    }
                }
                catch (IOException)
                {
                    lock (gate)
                    {
                        if (closed)
                            return;
                        totalRejected++;
                    }
                }
                catch
                {
                    lock (gate)
                    {
                        if (closed)
                            return;
                        totalRejected++;
                    }
                }
                finally
                {
                    if (pipe != null)
                    {
                        lock (gate)
                            openPipes.Remove(pipe);
                        try
                        {
                            pipe.Dispose();
                        }
                        catch
                        {
                            // The next listener iteration creates a fresh pipe instance.
                        }
                    }
                }
            }
        }

        private void HandleConnection(NamedPipeServerStream pipe)
        {
            AuthorSessionWireReadResult wire = AuthorSessionWire.ReadRequest(pipe);
            if (!wire.Accepted || wire.Request == null)
            {
                lock (gate)
                    totalRejected++;
                TryWrite(pipe, CreateTransportResponse(null, "rejected", wire.Code, wire.Message));
                return;
            }

            AuthorSessionRequest request = wire.Request;
            lock (gate)
                totalParsed++;

            AuthorSessionValidationResult validation = ValidateRequest(request);
            if (!validation.Accepted)
            {
                lock (gate)
                    totalRejected++;
                TryWrite(pipe, CreateTransportResponse(request, "rejected", validation.Code, validation.Message));
                if (string.Equals(validation.Code, "session-expired", StringComparison.Ordinal))
                    Close("descriptor-expired");
                return;
            }

            request.NormalizeAuthenticatedValues();
            request.Token = string.Empty;
            if (!TryEnqueue(request, out PendingRequest? item, out AuthorSessionResponse rejection) || item == null)
            {
                TryWrite(pipe, rejection);
                return;
            }

            TimeSpan remaining = descriptor.ExpiresAt - utcNow();
            TimeSpan wait = remaining > TimeSpan.Zero && remaining < runtimeResponseTimeout ? remaining : runtimeResponseTimeout;
            if (!item.ResponseReady.WaitOne(wait))
            {
                bool canceled = CancelQueued(
                    item,
                    CreateTransportResponse(request, "rejected", "runtime-timeout", "The Runtime thread did not process the request before its deadline."));
                if (!canceled)
                    item.ResponseReady.WaitOne();
            }

            AuthorSessionResponse response = item.Response ??
                CreateTransportResponse(request, "error", "response-missing", "The Runtime completed without a response.");
            TryWrite(pipe, response);
            item.ResponseReady.Dispose();
        }

        private AuthorSessionValidationResult ValidateRequest(AuthorSessionRequest request)
        {
            if (!string.Equals(request.Protocol, AuthorSessionProtocol.ProtocolVersion, StringComparison.Ordinal))
                return AuthorSessionValidationResult.Reject("protocol-unsupported", "The request protocol is unsupported.");
            if (utcNow() >= descriptor.ExpiresAt)
                return AuthorSessionValidationResult.Reject("session-expired", "The author session has expired.");

            bool sessionMatches = string.Equals(request.Session, descriptor.SessionId, StringComparison.Ordinal);
            bool tokenMatches = AuthorSessionProtocol.FixedTimeEquals(request.Token, descriptor.Token);
            if (!sessionMatches || !tokenMatches)
                return AuthorSessionValidationResult.Reject("authentication-failed", "The request session or token is invalid.");

            if (!string.Equals(request.Runtime, runtimeVersion, StringComparison.Ordinal))
                return AuthorSessionValidationResult.Reject("runtime-mismatch", "The request Runtime version does not match the running Runtime.");
            if (!AuthorSessionProtocol.PathsEqual(request.GameRoot, gameRoot))
                return AuthorSessionValidationResult.Reject("game-root-mismatch", "The request gameRoot does not match this Runtime installation.");
            if (!AuthorSessionProtocol.IsValidRequestId(request.RequestId))
                return AuthorSessionValidationResult.Reject("request-id-invalid", "The requestId must be a non-empty GUID in N format.");
            if (!AuthorSessionProtocol.IsSafeIdentifier(request.UniqueId, 200))
                return AuthorSessionValidationResult.Reject("unique-id-invalid", "The request UniqueID is invalid.");
            if (!AuthorSessionProtocol.IsSupportedOperation(request.Operation))
            {
                return AuthorSessionValidationResult.Reject(
                    "operation-unsupported",
                    "Only get-source-snapshot and reload-content are supported; DLL and native-table reload are restart-only.");
            }
            if (string.IsNullOrWhiteSpace(request.SelectedRoot) || !Path.IsPathRooted(request.SelectedRoot))
                return AuthorSessionValidationResult.Reject("selected-root-invalid", "The selectedRoot must be an absolute path.");
            try
            {
                AuthorSessionProtocol.NormalizePath(request.SelectedRoot);
            }
            catch
            {
                return AuthorSessionValidationResult.Reject("selected-root-invalid", "The selectedRoot path is invalid.");
            }
            if (!AuthorSessionProtocol.IsSha256(request.ExpectedTreeSha256))
                return AuthorSessionValidationResult.Reject("tree-hash-invalid", "The expectedTreeSha256 must be a SHA-256 hex digest.");

            return AuthorSessionValidationResult.Accept("request-authenticated", "The request passed transport authentication.");
        }

        private bool TryEnqueue(
            AuthorSessionRequest request,
            out PendingRequest? item,
            out AuthorSessionResponse rejection)
        {
            item = null;
            rejection = CreateTransportResponse(request, "error", "enqueue-unavailable", "The request could not be enqueued.");
            lock (gate)
            {
                if (!running || closed)
                {
                    totalRejected++;
                    rejection = CreateTransportResponse(request, "rejected", "session-closed", "The author session is closed.");
                    return false;
                }
                if (replayRequestIds.Contains(request.RequestId))
                {
                    totalRejected++;
                    replayRejected++;
                    rejection = CreateTransportResponse(request, "rejected", "request-replay", "The requestId was already observed in this session.");
                    return false;
                }
                if (replayRequestIds.Count >= AuthorSessionProtocol.MaximumReplayEntries)
                {
                    totalRejected++;
                    replayRejected++;
                    rejection = CreateTransportResponse(request, "rejected", "replay-window-full", "The bounded request replay window is full.");
                    return false;
                }

                replayRequestIds.Add(request.RequestId);
                if (activeByUniqueId.ContainsKey(request.UniqueId))
                {
                    totalRejected++;
                    concurrentRejected++;
                    rejection = CreateTransportResponse(request, "rejected", "owner-request-concurrent", "Another request for this UniqueID is already in flight.");
                    return false;
                }
                if (activeByUniqueId.Count >= MaximumConcurrentRequests)
                {
                    totalRejected++;
                    concurrentRejected++;
                    rejection = CreateTransportResponse(request, "rejected", "request-concurrency-limit", "The bounded concurrent request limit was reached.");
                    return false;
                }
                if (pendingQueuedCount >= MaximumPendingRequests)
                {
                    totalRejected++;
                    queueFullRejected++;
                    rejection = CreateTransportResponse(request, "rejected", "request-queue-full", "The bounded Runtime request queue is full.");
                    return false;
                }

                item = new PendingRequest(request);
                pending.Enqueue(item);
                pendingQueuedCount++;
                activeByUniqueId.Add(request.UniqueId, item);
                totalEnqueued++;
                return true;
            }
        }

        private bool CancelQueued(PendingRequest item, AuthorSessionResponse response)
        {
            bool canceled = false;
            lock (gate)
            {
                if (item.State == PendingRequestState.Queued)
                {
                    item.State = PendingRequestState.Canceled;
                    item.Response = response;
                    RemoveQueuedItemNoLock(item);
                    pendingQueuedCount--;
                    activeByUniqueId.Remove(item.Request.UniqueId);
                    totalRejected++;
                    canceled = true;
                }
            }

            if (canceled)
                item.ResponseReady.Set();
            return canceled;
        }

        private void RemoveQueuedItemNoLock(PendingRequest item)
        {
            int retainedCount = pending.Count;
            for (int index = 0; index < retainedCount; index++)
            {
                PendingRequest candidate = pending.Dequeue();
                if (!ReferenceEquals(candidate, item))
                    pending.Enqueue(candidate);
            }
        }

        private void Complete(PendingRequest item, AuthorSessionResponse response, PendingRequestState finalState)
        {
            bool signal = false;
            lock (gate)
            {
                if (item.State == PendingRequestState.Completed || item.State == PendingRequestState.Canceled)
                    return;
                item.Response = response;
                item.State = finalState;
                activeByUniqueId.Remove(item.Request.UniqueId);
                signal = true;
            }

            if (signal)
                item.ResponseReady.Set();
        }

        private AuthorSessionResponse CreateOperationResponse(AuthorSessionRequest request, AuthorSessionOperationResult result)
        {
            AuthorSessionResponse response = CreateTransportResponse(request, result.Status, result.Code, result.Message);
            response.Values = result.Values
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new AuthorSessionResponseValue(value.Key, value.Value))
                .ToList();
            return response;
        }

        private AuthorSessionResponse CreateTransportResponse(
            AuthorSessionRequest? request,
            string status,
            string code,
            string message)
        {
            return new AuthorSessionResponse
            {
                Protocol = AuthorSessionProtocol.ProtocolVersion,
                Runtime = runtimeVersion,
                Session = descriptor.SessionId,
                RequestId = request?.RequestId ?? string.Empty,
                Operation = request?.Operation ?? string.Empty,
                UniqueId = request?.UniqueId ?? string.Empty,
                Status = status,
                Code = AuthorSessionProtocol.BoundedSingleLine(code, 96),
                Message = AuthorSessionProtocol.BoundedSingleLine(message, 512),
                Values = new List<AuthorSessionResponseValue>()
            };
        }

        private static void TryWrite(Stream pipe, AuthorSessionResponse response)
        {
            try
            {
                AuthorSessionWire.WriteResponse(pipe, response);
            }
            catch
            {
                // A disconnected SDK client cannot change Runtime state or keep a request active.
            }
        }

        private sealed class PendingRequest
        {
            public PendingRequest(AuthorSessionRequest request)
            {
                Request = request;
                ResponseReady = new ManualResetEvent(false);
                State = PendingRequestState.Queued;
            }

            public AuthorSessionRequest Request { get; }

            public ManualResetEvent ResponseReady { get; }

            public PendingRequestState State { get; set; }

            public AuthorSessionResponse? Response { get; set; }
        }

        private enum PendingRequestState
        {
            Queued,
            Processing,
            Completed,
            Canceled
        }
    }

    internal sealed class AuthorSessionProcessResult
    {
        public AuthorSessionProcessResult(bool success, string code, string message, int handled, int failed)
        {
            Success = success;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Handled = handled;
            Failed = failed;
        }

        public bool Success { get; }

        public string Code { get; }

        public string Message { get; }

        public int Handled { get; }

        public int Failed { get; }
    }

    internal sealed class AuthorSessionHostSnapshot
    {
        public AuthorSessionHostSnapshot(
            bool running,
            bool closed,
            string closeReason,
            int runtimeThreadId,
            int maximumPending,
            int maximumConcurrent,
            int pending,
            int inFlight,
            int replayEntries,
            long connections,
            long parsed,
            long enqueued,
            long processed,
            long rejected,
            long replayRejected,
            long concurrentRejected,
            long queueFullRejected,
            long handlerFailures)
        {
            Running = running;
            Closed = closed;
            CloseReason = closeReason ?? string.Empty;
            RuntimeThreadId = runtimeThreadId;
            MaximumPending = maximumPending;
            MaximumConcurrent = maximumConcurrent;
            Pending = pending;
            InFlight = inFlight;
            ReplayEntries = replayEntries;
            Connections = connections;
            Parsed = parsed;
            Enqueued = enqueued;
            Processed = processed;
            Rejected = rejected;
            ReplayRejected = replayRejected;
            ConcurrentRejected = concurrentRejected;
            QueueFullRejected = queueFullRejected;
            HandlerFailures = handlerFailures;
        }

        public bool Running { get; }

        public bool Closed { get; }

        public string CloseReason { get; }

        public int RuntimeThreadId { get; }

        public int MaximumPending { get; }

        public int MaximumConcurrent { get; }

        public int Pending { get; }

        public int InFlight { get; }

        public int ReplayEntries { get; }

        public long Connections { get; }

        public long Parsed { get; }

        public long Enqueued { get; }

        public long Processed { get; }

        public long Rejected { get; }

        public long ReplayRejected { get; }

        public long ConcurrentRejected { get; }

        public long QueueFullRejected { get; }

        public long HandlerFailures { get; }

        public string FormatSummary()
        {
            return "running=" + Running.ToString(CultureInfo.InvariantCulture) +
                "; closed=" + Closed.ToString(CultureInfo.InvariantCulture) +
                "; pending=" + Pending.ToString(CultureInfo.InvariantCulture) +
                "; inFlight=" + InFlight.ToString(CultureInfo.InvariantCulture) +
                "; replayEntries=" + ReplayEntries.ToString(CultureInfo.InvariantCulture) +
                "; connections=" + Connections.ToString(CultureInfo.InvariantCulture) +
                "; parsed=" + Parsed.ToString(CultureInfo.InvariantCulture) +
                "; enqueued=" + Enqueued.ToString(CultureInfo.InvariantCulture) +
                "; processed=" + Processed.ToString(CultureInfo.InvariantCulture) +
                "; rejected=" + Rejected.ToString(CultureInfo.InvariantCulture) +
                "; replayRejected=" + ReplayRejected.ToString(CultureInfo.InvariantCulture) +
                "; concurrentRejected=" + ConcurrentRejected.ToString(CultureInfo.InvariantCulture) +
                "; queueFullRejected=" + QueueFullRejected.ToString(CultureInfo.InvariantCulture) +
                "; handlerFailures=" + HandlerFailures.ToString(CultureInfo.InvariantCulture) +
                "; closeReason=" + (string.IsNullOrWhiteSpace(CloseReason) ? "none" : CloseReason);
        }
    }
}
