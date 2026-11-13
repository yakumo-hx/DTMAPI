#pragma warning disable CS0618 // The frozen compatibility APIs still require owner-bound facades.
using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Creates compile-time owner-bound facades without changing the raw runtime API objects.</summary>
    internal static class OwnerBoundGameBridgeApis
    {

        internal static IOwnerBoundApiFactory ForItemDisplayName(IItemDisplayNameApi api) =>
            Factory<IItemDisplayNameApi>((owner, active) => new ItemDisplayNameFacade(api, owner, active));

        internal static IOwnerBoundApiFactory ForCropHarvesting(ICropHarvestingApi api) =>
            Factory<ICropHarvestingApi>((owner, active) => new CropHarvestingFacade(api, owner, active));

        private static IOwnerBoundApiFactory Factory<TApi>(Func<IManifest, Action, TApi> create) where TApi : class =>
            new TypedFactory<TApi>(create);

        private sealed class TypedFactory<TApi> : IOwnerBoundApiFactory where TApi : class
        {
            private readonly Func<IManifest, Action, TApi> create;

            public TypedFactory(Func<IManifest, Action, TApi> create)
            {
                this.create = create;
            }

            public object CreateOwnerBoundApi(Type apiType, IManifest consumer, Action ensureOwnerActive)
            {
                if (apiType != typeof(TApi))
                    throw new InvalidOperationException("Owner-bound GameBridge factory expected contract '" + (typeof(TApi).FullName ?? typeof(TApi).Name) + "', not '" + (apiType.FullName ?? apiType.Name) + "'.");
                return create(consumer, ensureOwnerActive);
            }
        }

        private abstract class FacadeBase
        {
            private readonly Action ensureOwnerActive;

            protected FacadeBase(IManifest owner, Action ensureOwnerActive)
            {
                Owner = owner;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            protected IManifest Owner { get; }

            protected void EnsureActive()
            {
                ensureOwnerActive();
            }

            protected void EnsureOwner(IManifest supplied)
            {
                ensureOwnerActive();
                if (supplied == null || !string.Equals(supplied.UniqueID, Owner.UniqueID, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("GameBridge API for owner '" + Owner.UniqueID + "' can't mutate resources for another owner.");
            }
        }

        private sealed class ItemDisplayNameFacade : FacadeBase, IItemDisplayNameApi
        {
            private readonly IItemDisplayNameApi inner;
            public ItemDisplayNameFacade(IItemDisplayNameApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public bool TryGetDisplayName(string itemId, out string displayName)
            {
                EnsureActive();
                return inner.TryGetDisplayName(itemId, out displayName);
            }
        }

        private sealed class CropHarvestingFacade : FacadeBase, ICropHarvestingApi
        {
            private readonly ICropHarvestingApi inner;
            public CropHarvestingFacade(ICropHarvestingApi inner, IManifest owner, Action active) : base(owner, active) { this.inner = inner; }
            public CropHarvestResult ScanMatureCrops(IManifest owner, CropHarvestRequest request) { EnsureOwner(owner); return inner.ScanMatureCrops(Owner, request); }
            public CropHarvestResult HarvestMatureCrops(IManifest owner, CropHarvestRequest request) { EnsureOwner(owner); return inner.HarvestMatureCrops(Owner, request); }
            public BridgeFeatureStatus GetStatus(string uniqueId) { EnsureActive(); return inner.GetStatus(uniqueId); }
        }
    }
}
