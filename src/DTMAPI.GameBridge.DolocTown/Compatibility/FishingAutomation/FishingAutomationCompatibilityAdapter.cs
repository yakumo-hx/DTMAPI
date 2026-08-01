#pragma warning disable CS0618 // Frozen IFishingAutomationApi compatibility implementation.
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Frozen public-API facade only. Native execution and lifecycle state stay
    /// behind <see cref="FishingAutomationCompatibilityFeature"/>.
    /// </summary>
    internal sealed class FishingAutomationCompatibilityAdapter : IFishingAutomationApi
    {
        private readonly FishingAutomationCompatibilityFeature feature;

        internal FishingAutomationCompatibilityAdapter(FishingAutomationCompatibilityFeature feature)
        {
            this.feature = feature;
        }

        public void Configure(IManifest owner, FishingAutomationOptions options)
        {
            feature.ConfigureCompatibility(owner, options);
        }

        public void SetEnabled(IManifest owner, bool enabled, string reason)
        {
            feature.SetCompatibilityEnabled(owner, enabled, reason);
        }

        public FishingAutomationState GetState(string uniqueId)
        {
            return feature.GetCompatibilityState(uniqueId);
        }

        public BridgeFeatureStatus GetStatus(string uniqueId)
        {
            return feature.GetCompatibilityStatus(uniqueId);
        }
    }
}
