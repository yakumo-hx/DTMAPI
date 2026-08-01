using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Mandatory thin provider for the exact retained IDebugConsoleApi ABI.
    /// It performs no UI/input/frame work until a real legacy consumer binds.
    /// </summary>
    internal sealed class DebugConsoleCompatibilityProxy :
        IDebugConsoleApi,
        IOwnerBoundApiHost
    {
        private readonly CompatibilityHostBroker broker;

        internal DebugConsoleCompatibilityProxy(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(
                runtime ??
                throw new ArgumentNullException(nameof(runtime)));
        }

        private object Backend =>
            broker.GetService("DebugConsole", Array.Empty<object>());

        public bool IsOpen => TryRead("IsOpen", false);

        public void Bind(
            IManifest owner,
            IInventoryDebugApi? inventoryApi,
            IWeatherDebugApi? weatherApi,
            ITeleportDebugApi? teleportApi,
            ITimeDebugApi? timeApi,
            IMovementDebugApi? movementApi,
            IInstantSaveDebugApi? instantSaveApi = null) =>
            CompatibilityHostBroker.Invoke(
                Backend,
                "Bind",
                owner,
                inventoryApi,
                weatherApi,
                teleportApi,
                timeApi,
                movementApi,
                instantSaveApi);

        public void BindAdvanced(
            IManifest owner,
            IAdvancedDebugApi? advancedDebugApi) =>
            CompatibilityHostBroker.Invoke(
                Backend,
                "BindAdvanced",
                owner,
                advancedDebugApi);

        public void SetLanguage(IManifest owner, string language) =>
            CompatibilityHostBroker.Invoke(
                Backend,
                "SetLanguage",
                owner,
                language);

        public void Open(IManifest owner, string reason) =>
            CompatibilityHostBroker.Invoke(
                Backend,
                "Open",
                owner,
                reason);

        public void Close(IManifest owner, string reason)
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(backend!, "Close", owner, reason);
        }

        public void Toggle(IManifest owner, string reason) =>
            CompatibilityHostBroker.Invoke(
                Backend,
                "Toggle",
                owner,
                reason);

        public BridgeFeatureStatus GetStatus(string uniqueId) =>
            CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(
                Backend,
                "GetStatus",
                uniqueId);

        public int CountOwnerResources(string ownerId) =>
            TryInvoke("CountOwnerResources", 0, ownerId);

        public int RemoveOwner(string ownerId, string reason) =>
            TryInvoke("RemoveOwner", 0, ownerId, reason);

        internal void UpdateIfLoaded()
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(backend!, "Update");
        }

        internal void ResetForSaveBoundaryIfLoaded(
            int? saveSlot,
            bool isNewGame)
        {
            if (TryGet(out object? backend))
            {
                CompatibilityHostBroker.Invoke(
                    backend!,
                    "ResetForSaveBoundary",
                    saveSlot,
                    isNewGame);
            }
        }

        internal void ResetForTitleBoundaryIfLoaded()
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(backend!, "ResetForTitleBoundary");
        }

        internal void ShutdownIfLoaded(string reason)
        {
            if (TryGet(out object? backend))
                CompatibilityHostBroker.Invoke(backend!, "Shutdown", reason);
        }

        internal bool ConsumedInputThisFrame =>
            TryRead("ConsumedInputThisFrame", false);

        internal string GetLifecycleSummary() =>
            TryInvoke("GetLifecycleSummary", "resident-dormant");

        internal string GetOwnerObjectGraphSummary() =>
            TryInvoke(
                "GetOwnerObjectGraphSummary",
                "DTMAPI.DebugConsole={Canvas=0; EventSystem=0; Button=0; InputField=0; ScrollRect=0; UnityEventListeners=0; DynamicBinders=0; rootAlive=0}");

        private bool TryGet(out object? backend) =>
            broker.TryGetService("DebugConsole", out backend);

        private T TryInvoke<T>(
            string method,
            T fallback,
            params object?[] arguments) =>
            TryGet(out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(backend, method, arguments)
                : fallback;

        private T TryRead<T>(string property, T fallback) =>
            TryGet(out object? backend) && backend != null
                ? CompatibilityHostBroker.ReadProperty(backend, property, fallback)
                : fallback;
    }
}
