using System;

namespace DTMAPI.Abstractions
{
    // Internal MES migration seam. No general configuration-file access is granted.
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    [DtmApiDisposition(DtmApiDisposition.Internal)]
    internal interface IProtectedSourceFiles
    {
        ProtectedSourceResult Capture(string relativeKey);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    [DtmApiDisposition(DtmApiDisposition.Internal)]
    internal interface IProtectedSourceSnapshot : IDisposable
    {
        string RelativeKey { get; }
        byte[] CopyBytes();
        ProtectedSourceResult Revalidate();
        // One attempt, including failure. Never opens with DeleteOnClose.
        ProtectedSourceResult TryDelete();
    }

    internal enum ProtectedSourceStatus
    {
        Captured, Unchanged, DeleteCommitted,
        Missing, Changed, Expired, Rejected, Unsupported, Busy, AccessDenied, IoError
    }

    internal sealed class ProtectedSourceResult
    {
        internal ProtectedSourceResult(ProtectedSourceStatus status, string detail = "", IProtectedSourceSnapshot? snapshot = null)
        { Status = status; Detail = detail; Snapshot = snapshot; }
        public ProtectedSourceStatus Status { get; }
        public string Detail { get; }
        public IProtectedSourceSnapshot? Snapshot { get; }
    }
}
