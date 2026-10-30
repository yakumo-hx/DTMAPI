using System;

namespace DTMAPI.Abstractions
{
    public enum DtmApiStatus
    {
        Proposed,
        Experimental,
        Verified,
        Stable,
        Disabled,
        StableCandidate
    }

    /// <summary>
    /// Describes whether an API surface is open for adoption independently of its stability level.
    /// A declaration without this attribute is treated as <see cref="DtmApiDisposition.Open"/>.
    /// </summary>
    public enum DtmApiDisposition
    {
        Open,
        Frozen,
        Diagnostic,
        Internal,
        Disabled
    }

    /// <summary>Describes contract stability independently of adoption disposition.</summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public sealed class DtmApiStatusAttribute : Attribute
    {
        public DtmApiStatusAttribute(DtmApiStatus status)
        {
            Status = status;
        }

        public DtmApiStatus Status { get; }
        public string? Since { get; set; }
        public string? Notes { get; set; }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public sealed class DtmApiDispositionAttribute : Attribute
    {
        public DtmApiDispositionAttribute(DtmApiDisposition disposition)
        {
            Disposition = disposition;
        }

        public DtmApiDisposition Disposition { get; }
        public string? Since { get; set; }
        public string? Notes { get; set; }
    }
}
