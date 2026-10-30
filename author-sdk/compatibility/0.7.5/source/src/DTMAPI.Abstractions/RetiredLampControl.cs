#pragma warning disable CS0618 // The retained compatibility declarations necessarily reference one another.
using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Disabled, Since = "0.5.5", Notes = "Retired compatibility shell. Every operation fails closed; no Lamp hooks or gameplay behavior are available.")]
    [Obsolete("ILampControlApi is a retired compatibility shell. Lamp gameplay is disabled; remove this dependency and do not expect hooks, light mutation, or persisted state.", false)]
    public interface ILampControlApi
    {
        LampManualToggleRegisterResult RegisterManualToggle(IManifest owner, LampManualToggleOptions options);
        LampManualToggleState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Disabled, Since = "0.5.5", Notes = "Retained only for binary compatibility with the retired Lamp API.")]
    [Obsolete("LampManualToggleOptions is retained only for ILampControlApi binary compatibility. Lamp gameplay is retired and disabled.", false)]
    public sealed class LampManualToggleOptions
    {
        public bool Enabled { get; set; } = true;
        public IReadOnlyList<string> EquipmentIds { get; set; } = Array.Empty<string>();
        public bool VerboseLogging { get; set; }
    }

    [DtmApiStatus(DtmApiStatus.Disabled, Since = "0.5.5", Notes = "Retained only for binary compatibility; results always report retired-disabled.")]
    [Obsolete("LampManualToggleRegisterResult is retained only for ILampControlApi binary compatibility. Results always report retired-disabled.", false)]
    public sealed class LampManualToggleRegisterResult
    {
        public bool Success { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public IReadOnlyList<string> RegisteredEquipmentIds { get; set; } = Array.Empty<string>();
        public bool HookInstalled { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int SessionOverrideCount { get; set; }
        public string LastTouchedEquipmentId { get; set; } = string.Empty;
        public string LastToggledEquipmentId { get; set; } = string.Empty;
    }

    [DtmApiStatus(DtmApiStatus.Disabled, Since = "0.5.5", Notes = "Retained only for binary compatibility; state always reports retired-disabled.")]
    [Obsolete("LampManualToggleState is retained only for ILampControlApi binary compatibility. State always reports retired-disabled.", false)]
    public sealed class LampManualToggleState
    {
        public string OwnerId { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public bool Enabled { get; set; }
        public IReadOnlyList<string> RegisteredEquipmentIds { get; set; } = Array.Empty<string>();
        public bool HookInstalled { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public int SessionOverrideCount { get; set; }
        public string LastTouchedEquipmentId { get; set; } = string.Empty;
        public string LastToggledEquipmentId { get; set; } = string.Empty;
        public bool LastToggledValue { get; set; }
    }
}
