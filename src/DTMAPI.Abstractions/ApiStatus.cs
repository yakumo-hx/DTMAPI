using System;

namespace DTMAPI.Abstractions
{
    public enum DtmApiStatus
    {
        Stable = 3,
        Experimental = 1
    }

    /// <summary>标记 API 或程序集的稳定程度。</summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
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
