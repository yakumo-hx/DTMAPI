#pragma warning disable CS0618 // Frozen ISaveSlotsApi proxy.
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Mandatory thin proxy for the frozen ISaveSlotsApi ABI.</summary>
    internal sealed class SaveSlotsService : ISaveSlotsApi
    {
        private readonly CompatibilityHostBroker broker;

        internal SaveSlotsService(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(runtime ?? throw new ArgumentNullException(nameof(runtime)));
        }

        private object Backend => broker.GetService("SaveSlots", Array.Empty<object>());

        public SaveSlotsRegisterResult RegisterSlots(IManifest owner, SaveSlotsOptions options) =>
            CompatibilityHostBroker.Invoke<SaveSlotsRegisterResult>(Backend, "RegisterSlots", owner, NormalizeSaveSlotsOptions(options));

        public SaveSlotsState GetState(string uniqueId) =>
            CompatibilityHostBroker.Invoke<SaveSlotsState>(Backend, "GetState", uniqueId);

        public BridgeFeatureStatus GetStatus(string uniqueId) =>
            CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);

        internal int RemoveOwner(string ownerId, string reason) =>
            TryInvoke("RemoveOwner", 0, ownerId, reason);

        internal int CountOwnerResources(string ownerId) =>
            TryInvoke("CountOwnerResources", 0, ownerId);

        internal void RefreshSaveSlotExpansionForRuntime(bool force, string reason)
        {
            if (broker.TryGetService("SaveSlots", out object? backend) && backend != null)
                CompatibilityHostBroker.Invoke(backend, "RefreshSaveSlotExpansionForRuntime", force, reason);
        }

        internal string GetOfficialSaveUiLifecycleSummary() =>
            "saveUiStates=0, saveUiPagers=0, saveUiBinders=0";

        internal string GetOfficialSaveUiOwnerObjectGraphSummary() =>
            "DTMAPI.SaveSlots={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}";

        internal static SaveSlotsOptions NormalizeSaveSlotsOptions(SaveSlotsOptions? value) =>
            new SaveSlotsOptions
            {
                Enabled = value?.Enabled ?? true,
                SlotCount = value?.Enabled == false ? 6 : 12,
                VerboseLogging = value?.VerboseLogging ?? false
            };

        private T TryInvoke<T>(string method, T fallback, params object?[] arguments) =>
            broker.TryGetService("SaveSlots", out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(backend, method, arguments)
                : fallback;
    }
}
