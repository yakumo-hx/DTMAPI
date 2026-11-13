using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal enum ProviderOrigin { Package, Runtime }
    internal sealed class ProviderRegistration
    {
        internal ProviderRegistration(string id, ProviderOrigin origin, IManifest? metadata = null)
        { Id = id; Origin = origin; Metadata = metadata; Version = metadata?.Version ?? ""; Available = metadata != null; }
        internal string Id { get; }
        internal ProviderOrigin Origin { get; }
        // Registry metadata is a projection, never authority for package origin.
        internal IManifest? Metadata { get; set; }
        private string runtimeVersion = "";
        internal string Version { get => Metadata?.Version ?? runtimeVersion; set => runtimeVersion = value; }
        internal bool Available { get; set; }
    }
}
