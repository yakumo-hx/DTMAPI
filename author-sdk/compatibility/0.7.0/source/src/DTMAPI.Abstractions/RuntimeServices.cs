using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public enum DtmRuntimePhase { Starting, Title, LoadingSave, LoadingWorld, WorldReady, ReturningToTitle, ShuttingDown }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public enum DtmWorkScope { ModOwner, CurrentSaveSession, CurrentWorld }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public sealed class DtmRuntimeContextSnapshot
    {
        public DtmRuntimeContextSnapshot(Guid runtimeInstanceId, long revision, DtmRuntimePhase phase,
            long? saveSessionEpoch, long? worldEpoch)
        {
            if ((phase == DtmRuntimePhase.WorldReady) != worldEpoch.HasValue || (worldEpoch.HasValue && !saveSessionEpoch.HasValue))
                throw new ArgumentException("WorldReady requires a save and world epoch; other phases cannot expose a world epoch.");
            RuntimeInstanceId = runtimeInstanceId; Revision = revision; Phase = phase;
            SaveSessionEpoch = saveSessionEpoch; WorldEpoch = worldEpoch;
        }
        public Guid RuntimeInstanceId { get; }
        public long Revision { get; }
        public DtmRuntimePhase Phase { get; }
        public long? SaveSessionEpoch { get; }
        public long? WorldEpoch { get; }
        public bool IsWorldReady => Phase == DtmRuntimePhase.WorldReady;
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Acquire on the Runtime thread. Snapshot reads and IsMainThread are safe on any thread. Change callbacks run on the Runtime thread after publication; Dispose only unsubscribes.")]
    public interface IDtmRuntimeContext
    {
        DtmRuntimeContextSnapshot Snapshot { get; }
        bool IsMainThread { get; }
        IDisposable Subscribe(Action<DtmRuntimeContextSnapshot> changed);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public enum DtmWorkStatus { Queued, Running, Succeeded, Failed, Cancelled, Rejected, Expired }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public sealed class DtmWorkResult
    {
        public DtmWorkResult(DtmWorkStatus status, string errorCode = "", string message = "")
        { Status = status; ErrorCode = errorCode; Message = message; }
        public DtmWorkStatus Status { get; }
        public string ErrorCode { get; }
        public string Message { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public interface IDtmScheduledWork
    {
        DtmWorkStatus Status { get; }
        bool TryCancel();
        Task<DtmWorkResult> Completion { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Acquire on the Runtime thread, then submit from any thread. Synchronous Action only; callbacks are never inline. Completion continuations are asynchronous and not guaranteed on the Runtime thread. Never block that thread on Completion. Cancellation/queueTimeout affect unstarted work only.")]
    public interface IDtmScheduler
    {
        IDtmScheduledWork Post(Action action, DtmWorkScope scope = DtmWorkScope.ModOwner,
            CancellationToken cancellationToken = default, TimeSpan? queueTimeout = null);
        IDtmScheduledWork NextTick(Action action, DtmWorkScope scope = DtmWorkScope.ModOwner,
            CancellationToken cancellationToken = default, TimeSpan? queueTimeout = null);
        IDtmScheduledWork Delay(Action action, TimeSpan delay, DtmWorkScope scope = DtmWorkScope.ModOwner,
            CancellationToken cancellationToken = default, TimeSpan? queueTimeout = null);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public enum DtmResourceStatus { Registered, DisposeQueued, Disposing, Disposed, Unregistered, Failed }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Unregister releases platform ownership without disposing. Dispose requests Runtime-thread disposal; failed disposal may be retried. Neither operation reactivates a closed registration.")]
    public interface IDtmOwnedResource : IDisposable
    {
        DtmResourceStatus Status { get; }
        bool Unregister();
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public interface IDtmOwnedResources
    {
        IDtmOwnedResource Register(IDisposable resource, DtmWorkScope scope = DtmWorkScope.ModOwner);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public sealed class DtmCommandInfo
    {
        public DtmCommandInfo(string name, string ownerId, string description, string parameters, DtmWorkScope scope)
        { Name = name; OwnerId = ownerId; Description = description; Parameters = parameters; Scope = scope; }
        public string Name { get; }
        public string OwnerId { get; }
        public string Description { get; }
        public string Parameters { get; }
        public DtmWorkScope Scope { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public interface IDtmCommandContext
    {
        string RequestId { get; }
        string OwnerId { get; }
        IReadOnlyList<string> Arguments { get; }
        bool WriteLine(string text);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2")]
    public sealed class DtmCommandResult
    {
        public DtmCommandResult(string requestId, string ownerId, DtmWorkResult work, IEnumerable<string> output)
        { RequestId = requestId; OwnerId = ownerId; Work = work; Output = new List<string>(output).AsReadOnly(); }
        public string RequestId { get; }
        public string OwnerId { get; }
        public DtmWorkResult Work { get; }
        public IReadOnlyList<string> Output { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.6.2", Notes = "Registration is Runtime-thread only. All frontends execute through the shared scheduler. Canonical names are UniqueID/name; alias conflicts reject the entire registration. Input is data, never shell code.")]
    public interface IDtmCommands
    {
        IDisposable Register(string name, Action<IDtmCommandContext> handler, DtmWorkScope scope = DtmWorkScope.ModOwner,
            string description = "", string parameters = "", string? alias = null);
        IReadOnlyList<DtmCommandInfo> GetHelp();
        Task<DtmCommandResult> Execute(string commandLine, CancellationToken cancellationToken = default);
    }
}
