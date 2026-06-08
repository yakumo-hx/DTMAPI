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

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
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
}
