#pragma warning disable CS0618 // Mandatory proxy for the frozen camera ABI.
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraViewService : ICameraViewApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly object[] constructionArguments;

        internal CameraViewService(
            DtmApiRuntime runtime,
            Func<bool> managedProductOwnerPresent)
        {
            broker = CompatibilityHostBroker.For(
                runtime ?? throw new ArgumentNullException(nameof(runtime)));
            constructionArguments = new object[]
            {
                managedProductOwnerPresent ??
                    throw new ArgumentNullException(
                        nameof(managedProductOwnerPresent)),
                null!
            };
        }

        internal void ConfigureCompatibilityOwnerClaim(
            Func<bool> ownerClaim) =>
            constructionArguments[1] =
                ownerClaim ??
                throw new ArgumentNullException(nameof(ownerClaim));

        private object Backend =>
            broker.GetService("Camera", constructionArguments);

        internal bool HasEnvironmentResetDemand =>
            TryRead("HasEnvironmentResetDemand", false);

        public ICameraViewLease AcquireLease(
            IManifest owner,
            CameraViewRequest request) =>
            CompatibilityHostBroker.Invoke<ICameraViewLease>(
                Backend,
                "ViewAcquireLease",
                owner,
                request);

        public CameraViewState GetState(string uniqueId) =>
            CompatibilityHostBroker.Invoke<CameraViewState>(
                Backend,
                "ViewGetState",
                uniqueId);

        public CameraViewState GetSnapshot(string uniqueId) =>
            CompatibilityHostBroker.Invoke<CameraViewState>(
                Backend,
                "ViewGetSnapshot",
                uniqueId);

        public BridgeFeatureStatus GetStatus(string uniqueId) =>
            CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(
                Backend,
                "ViewGetStatus",
                uniqueId);

        internal void PublishHookStatuses() =>
            TryInvokeIfLoaded("PublishHookStatuses");

        internal void SetEnvironmentLifecyclePatched(bool patched) =>
            TryInvokeIfLoaded(
                "SetEnvironmentLifecyclePatched",
                patched);

        internal void RefreshForRuntime() =>
            TryInvokeIfLoaded("Update");

        internal void ResetForLifecycleBoundary(string reason) =>
            TryInvokeIfLoaded(
                "ResetForLifecycleBoundary",
                reason);

        internal void NotifyEnvironmentReset(string reason) =>
            TryInvokeIfLoaded(
                "NotifyEnvironmentReset",
                reason);

        internal int ReconcileManagedProductOwnerBeforeHookInstall() =>
            TryInvokeOrFallback(
                "ReconcileManagedProductOwnerBeforeHookInstall",
                0);

        internal int RemoveOwner(string ownerId, string reason) =>
            TryInvokeOrFallback(
                "RemoveOwner",
                0,
                ownerId,
                reason);

        internal int CountOwnerResources(string ownerId) =>
            TryInvokeOrFallback(
                "CountOwnerResources",
                0,
                ownerId);

        internal void ConfigureNativeAccessForTests(
            Func<double?>? reader,
            Func<double, bool>? writer) =>
            CompatibilityHostBroker.Invoke(
                Backend,
                "ConfigureNativeAccessForTests",
                reader,
                writer);

        private T TryRead<T>(string property, T fallback) =>
            broker.TryGetService("Camera", out object? backend) &&
            backend != null
                ? CompatibilityHostBroker.ReadProperty(
                    backend,
                    property,
                    fallback)
                : fallback;

        private void TryInvokeIfLoaded(
            string method,
            params object?[] arguments)
        {
            if (broker.TryGetService(
                    "Camera",
                    out object? backend) &&
                backend != null)
            {
                CompatibilityHostBroker.Invoke(
                    backend,
                    method,
                    arguments);
            }
        }

        private T TryInvokeOrFallback<T>(
            string method,
            T fallback,
            params object?[] arguments) =>
            broker.TryGetService(
                "Camera",
                out object? backend) &&
            backend != null
                ? CompatibilityHostBroker.Invoke<T>(
                    backend,
                    method,
                    arguments)
                : fallback;
    }
}

#pragma warning restore CS0618
