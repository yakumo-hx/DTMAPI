#pragma warning disable CS0618 // This file intentionally implements the retained Lamp compatibility shell.
using System;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Stateless compatibility provider for the retired Lamp API. It is deliberately not an
    /// IGameBridgeFeature, so it has no hook, update, save, title, or native-game lifecycle path.
    /// </summary>
    internal sealed class RetiredLampControlApi : ILampControlApi
    {
        internal const string StatusCode = "retired-disabled";
        internal const string StatusMessage = "Lamp control is a retired compatibility shell in DTMAPI 0.5.5. No hooks are installed, no lights are changed, and no state is persisted.";

        internal static readonly RetiredLampControlApi Instance = new RetiredLampControlApi();

        private RetiredLampControlApi()
        {
        }

        public LampManualToggleRegisterResult RegisterManualToggle(IManifest owner, LampManualToggleOptions options)
        {
            return new LampManualToggleRegisterResult
            {
                Success = false,
                OwnerId = owner?.UniqueID ?? string.Empty,
                Enabled = false,
                RegisteredEquipmentIds = Array.Empty<string>(),
                HookInstalled = false,
                FailureReason = StatusCode,
                Message = StatusMessage,
                SessionOverrideCount = 0,
                LastTouchedEquipmentId = string.Empty,
                LastToggledEquipmentId = string.Empty
            };
        }

        public LampManualToggleState GetState(string uniqueId)
        {
            return new LampManualToggleState
            {
                OwnerId = uniqueId ?? string.Empty,
                IsConfigured = false,
                Enabled = false,
                RegisteredEquipmentIds = Array.Empty<string>(),
                HookInstalled = false,
                Status = StatusCode,
                FailureReason = StatusCode,
                LastMessage = StatusMessage,
                SessionOverrideCount = 0,
                LastTouchedEquipmentId = string.Empty,
                LastToggledEquipmentId = string.Empty,
                LastToggledValue = false
            };
        }

        public BridgeFeatureStatus GetStatus(string uniqueId)
        {
            return new BridgeFeatureStatus(StatusCode, StatusMessage);
        }
    }
}
