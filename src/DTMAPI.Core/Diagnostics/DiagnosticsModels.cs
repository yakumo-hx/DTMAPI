using System;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Diagnostics
{
    public sealed class DtmErrorInfo : IDtmErrorInfo
    {
        public DtmErrorInfo(string owner, string message, string details)
        {
            Time = DateTimeOffset.Now;
            Owner = owner;
            Message = message;
            Details = details;
        }

        public DateTimeOffset Time { get; }
        public string Owner { get; }
        public string Message { get; }
        public string Details { get; }
    }

    public sealed class DtmWarningInfo : IDtmWarningInfo
    {
        public DtmWarningInfo(string owner, string message, string details)
        {
            Time = DateTimeOffset.Now;
            Owner = owner;
            Message = message;
            Details = details;
        }

        public DateTimeOffset Time { get; }
        public string Owner { get; }
        public string Message { get; }
        public string Details { get; }
    }

    public sealed class HookStatusInfo : IHookStatusInfo
    {
        public HookStatusInfo(string hookId, string status, string source, string details)
        {
            HookId = hookId;
            Status = status;
            Source = source;
            Details = details;
            UpdatedAt = DateTimeOffset.Now;
        }

        public string HookId { get; }
        public string Status { get; }
        public string Source { get; }
        public string Details { get; }
        public DateTimeOffset UpdatedAt { get; }
    }
}
