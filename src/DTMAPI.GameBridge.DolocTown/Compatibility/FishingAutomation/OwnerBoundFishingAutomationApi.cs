#pragma warning disable CS0618 // This file is part of the frozen compatibility island.
using System;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class OwnerBoundFishingAutomationApi
    {
        internal static IOwnerBoundApiFactory For(IFishingAutomationApi api) => new Factory(api);

        private sealed class Factory : IOwnerBoundApiFactory
        {
            private readonly IFishingAutomationApi inner;
            public Factory(IFishingAutomationApi inner) { this.inner = inner; }

            public object CreateOwnerBoundApi(Type apiType, IManifest consumer, Action ensureOwnerActive)
            {
                if (apiType != typeof(IFishingAutomationApi))
                    throw new InvalidOperationException("Owner-bound fishing compatibility factory received an unexpected contract.");
                return new Facade(inner, consumer, ensureOwnerActive);
            }
        }

        private sealed class Facade : IFishingAutomationApi
        {
            private readonly IFishingAutomationApi inner;
            private readonly IManifest owner;
            private readonly Action ensureOwnerActive;

            public Facade(IFishingAutomationApi inner, IManifest owner, Action ensureOwnerActive)
            {
                this.inner = inner;
                this.owner = owner;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public void Configure(IManifest supplied, FishingAutomationOptions options)
            {
                EnsureOwner(supplied);
                inner.Configure(owner, options);
            }

            public void SetEnabled(IManifest supplied, bool enabled, string reason)
            {
                EnsureOwner(supplied);
                inner.SetEnabled(owner, enabled, reason);
            }

            public FishingAutomationState GetState(string uniqueId)
            {
                ensureOwnerActive();
                return inner.GetState(uniqueId);
            }

            public BridgeFeatureStatus GetStatus(string uniqueId)
            {
                ensureOwnerActive();
                return inner.GetStatus(uniqueId);
            }

            private void EnsureOwner(IManifest supplied)
            {
                ensureOwnerActive();
                if (supplied == null || !string.Equals(supplied.UniqueID, owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Fishing compatibility API for owner '" + owner.UniqueID + "' can't mutate resources for another owner.");
            }
        }
    }
}
