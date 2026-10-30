using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{

    internal sealed class EquipmentSlotsRecoveryResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int RecoveredCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
