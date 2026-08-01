#pragma warning disable CS0618
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Mandatory thin proxy for the frozen IItemTooltipApi ABI.</summary>
    internal sealed class FishRoeTooltipService : IItemTooltipApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly object[] constructionArguments;
        private Func<object, string, string>? decorateTitle;
        private Func<object, string, string>? decorateDetail;

        public FishRoeTooltipService(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = Array.Empty<object>();
        }

        internal FishRoeTooltipService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = new object[] { managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent)) };
        }

        private object Backend => broker.GetService("FishRoeTooltip", constructionArguments);
        public void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup) => CompatibilityHostBroker.Invoke(Backend, "ConfigureFishRoeProvider", owner, options, lookup);
        public BridgeFeatureStatus GetStatus(string uniqueId) => CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);
        internal void SetHooksInstalled(bool installed) => CompatibilityHostBroker.Invoke(Backend, "SetHooksInstalled", installed);
        internal int ReconcileManagedProductOwnerBeforeHookInstall() => TryInvoke("ReconcileManagedProductOwnerBeforeHookInstall", 0);
        internal int RemoveOwner(string ownerId, string reason) => TryInvoke("RemoveOwner", 0, ownerId, reason);
        internal int CountOwnerResources(string ownerId) => TryInvoke("CountOwnerResources", 0, ownerId);
        internal string DecorateFishRoeTitle(object item, string current) =>
            (decorateTitle ??= broker.BindIfServiceLoaded<Func<object, string, string>>("FishRoeTooltip", "DecorateFishRoeTitle"))?.Invoke(item, current) ?? current ?? string.Empty;
        internal string DecorateFishRoeDetail(object item, string current) =>
            (decorateDetail ??= broker.BindIfServiceLoaded<Func<object, string, string>>("FishRoeTooltip", "DecorateFishRoeDetail"))?.Invoke(item, current) ?? current ?? string.Empty;

        private T TryInvoke<T>(string method, T fallback, params object?[] args)
        {
            return broker.TryGetService("FishRoeTooltip", out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(backend, method, args)
                : fallback;
        }
    }
}
