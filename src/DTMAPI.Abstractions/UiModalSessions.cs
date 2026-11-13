using System;

namespace DTMAPI.Abstractions
{
    /// <summary>Optional capability of an owner-bound <see cref="IUiHelper"/>. Runtime thread only.</summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5", Notes = "Candidate owner modal arbitration only; the consumer owns rendering, focus and native input isolation.")]
    public interface IUiModalSessions
    {
        /// <summary>Acquires a modal or returns the same live session for this owner/menu. Synchronous UI events may invalidate it before return.</summary>
        DtmModalOpenResult OpenModal(string menuId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public enum DtmModalOpenStatus
    {
        Opened,
        AlreadyOpen,
        Busy,
        Interrupted
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public sealed class DtmModalOpenResult
    {
        public DtmModalOpenResult(DtmModalOpenStatus status, IUiModalSession? session)
        {
            if ((status == DtmModalOpenStatus.Opened || status == DtmModalOpenStatus.AlreadyOpen) != (session != null))
                throw new ArgumentException("Only an acquired modal result can carry a session.", nameof(session));
            Status = status;
            Session = session;
        }

        public DtmModalOpenStatus Status { get; }
        public IUiModalSession? Session { get; }
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    public interface IUiModalSession : IDisposable
    {
        string OwnerId { get; }
        string MenuId { get; }
        long Sequence { get; }
        /// <summary>Checks the exact session on the runtime thread. Remains safe after owner deactivation.</summary>
        bool IsOpen { get; }
        /// <summary>Releases this exact session. Returns false when stale; never closes a successor. Event exceptions do not roll back the released occupancy.</summary>
        bool Close();
    }
}
