#pragma warning disable CS0618 // Mandatory proxy for the frozen camera ABI.
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraZoomCompatibilityService :
        ICameraZoomApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly object[] constructionArguments;

        internal CameraZoomCompatibilityService(
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

        public CameraZoomRegisterResult Register(
            IManifest owner,
            CameraZoomOptions options) =>
            CompatibilityHostBroker.Invoke<CameraZoomRegisterResult>(
                Backend,
                "ZoomRegister",
                owner,
                options);

        public CameraZoomResult SetViewScale(
            IManifest owner,
            double viewScale,
            string reason) =>
            CompatibilityHostBroker.Invoke<CameraZoomResult>(
                Backend,
                "ZoomSetViewScale",
                owner,
                viewScale,
                reason);

        public CameraZoomResult StepViewScale(
            IManifest owner,
            int direction,
            string reason) =>
            CompatibilityHostBroker.Invoke<CameraZoomResult>(
                Backend,
                "ZoomStepViewScale",
                owner,
                direction,
                reason);

        public CameraZoomResult ResetViewScale(
            IManifest owner,
            string reason) =>
            CompatibilityHostBroker.Invoke<CameraZoomResult>(
                Backend,
                "ZoomResetViewScale",
                owner,
                reason);

        public CameraZoomState GetState(string uniqueId) =>
            CompatibilityHostBroker.Invoke<CameraZoomState>(
                Backend,
                "ZoomGetState",
                uniqueId);

        public CameraZoomState GetSnapshot(string uniqueId) =>
            CompatibilityHostBroker.Invoke<CameraZoomState>(
                Backend,
                "ZoomGetSnapshot",
                uniqueId);

        public BridgeFeatureStatus GetStatus(string uniqueId) =>
            CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(
                Backend,
                "ZoomGetStatus",
                uniqueId);

        internal int CountOwnerResources(string ownerId) =>
            broker.TryGetService(
                "Camera",
                out object? backend) &&
            backend != null
                ? CompatibilityHostBroker.Invoke<int>(
                    backend,
                    "CountOwnerResources",
                    ownerId)
                : 0;
    }
}

#pragma warning restore CS0618
