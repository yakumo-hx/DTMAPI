using System;
using System.IO;

namespace DTMAPI.Abstractions.Internal
{
    // Internal optional-host protocol. Only Core can issue these capabilities;
    // an author path, callback or implementation cannot manufacture authority.
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    [DtmApiDisposition(DtmApiDisposition.Internal, Since = "0.7.5", Notes = "Core-issued optional-host resource metadata; not an author service.")]
    public sealed class PackageResourceMetadata
    {
        internal PackageResourceMetadata(string source, string key, long length, long modified)
        { Source = source; Key = key; Length = length; ModifiedUtcTicks = modified; }
        public string Source { get; }
        public string Key { get; }
        public long Length { get; }
        public long ModifiedUtcTicks { get; }
        public bool Matches(PackageResourceMetadata other) => Source == other.Source && Key == other.Key && Length == other.Length && ModifiedUtcTicks == other.ModifiedUtcTicks;
    }
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    [DtmApiDisposition(DtmApiDisposition.Internal, Since = "0.7.5", Notes = "Only Core can issue this owner/source/Host capability.")]
    public sealed class PackageResourceReference
    {
        private readonly Func<PackageResourceMetadata> inspect;
        private readonly Func<PackageResourceMetadata, long, Stream> open;
        internal PackageResourceReference(Func<PackageResourceMetadata> inspect, Func<PackageResourceMetadata, long, Stream> open)
        { this.inspect = inspect; this.open = open; }
        // Both operations, including revalidation after worker completion, run
        // on the runtime thread and invoke the exact Pack/Host/source guard.
        public PackageResourceMetadata Inspect() => inspect();
        public void Validate(PackageResourceMetadata expected)
        { if (!Inspect().Matches(expected)) throw new IOException("PackageResourceChanged: acquire a new source generation."); }
        public PackageResourceReadLease Acquire(PackageResourceMetadata expected, long maximumBytes)
            => new PackageResourceReadLease(open(expected, maximumBytes));
    }
    // One consumer transfers this read-only handle from main to worker. It has
    // no package API, arbitrary path, native object or authority to publish.
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.7.5")]
    [DtmApiDisposition(DtmApiDisposition.Internal, Since = "0.7.5", Notes = "Transferred read-only worker handle; publishing remains main-thread guarded.")]
    public sealed class PackageResourceReadLease : Stream
    {
        private readonly Stream input;
        internal PackageResourceReadLease(Stream input) { this.input = input; }
        public override int Read(byte[] buffer, int offset, int count)
        { if (count > 65536) throw new ArgumentOutOfRangeException(nameof(count), "Package reads are bounded to 64 KiB."); return input.Read(buffer, offset, count); }
        public override long Seek(long offset, SeekOrigin origin) => input.Seek(offset, origin);
        public override bool CanRead => input.CanRead;
        public override bool CanSeek => input.CanSeek;
        public override bool CanWrite => false;
        public override long Length => input.Length;
        public override long Position { get => input.Position; set => input.Position = value; }
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) input.Dispose(); base.Dispose(disposing); }
    }
}
