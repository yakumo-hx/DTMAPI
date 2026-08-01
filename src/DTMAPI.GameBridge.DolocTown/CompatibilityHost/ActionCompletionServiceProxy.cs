#pragma warning disable CS0618
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Mandatory thin proxy for the frozen IActionCompletionApi ABI.</summary>
    internal sealed class ActionCompletionService : IActionCompletionApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly object[] constructionArguments;
        private Func<object, object, bool>? applyToolHit;
        private Func<bool>? applyEquipmentFill;

        public ActionCompletionService(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = Array.Empty<object>();
        }

        internal ActionCompletionService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = new object[] { managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent)) };
        }

        private object Backend => broker.GetService("ActionCompletion", constructionArguments);
        internal int OneActionApplicationCount => TryRead("OneActionApplicationCount", 0);
        internal string LastOneActionApplicationSummary => TryRead("LastOneActionApplicationSummary", string.Empty);

        public void Configure(IManifest owner, ActionCompletionOptions options) => CompatibilityHostBroker.Invoke(Backend, "Configure", owner, options);
        public BridgeFeatureStatus GetStatus(string uniqueId) => CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);
        internal void SetActionHooksInstalled(bool installed) => CompatibilityHostBroker.Invoke(Backend, "SetActionHooksInstalled", installed);
        internal int ReconcileManagedProductOwnerBeforeHookInstall() => TryInvoke("ReconcileManagedProductOwnerBeforeHookInstall", 0);
        internal int RemoveOwner(string ownerId, string reason) => TryInvoke("RemoveOwner", 0, ownerId, reason);
        internal int CountOwnerResources(string ownerId) => TryInvoke("CountOwnerResources", 0, ownerId);
        internal bool ApplyOneActionToolHit(object toolCollider, object collider) =>
            (applyToolHit ??= broker.BindIfServiceLoaded<Func<object, object, bool>>("ActionCompletion", "ApplyOneActionToolHit"))?.Invoke(toolCollider, collider) ?? false;
        internal bool ApplyOneActionEquipmentFillAfterInteract() =>
            (applyEquipmentFill ??= broker.BindIfServiceLoaded<Func<bool>>("ActionCompletion", "ApplyOneActionEquipmentFillAfterInteract"))?.Invoke() ?? false;
        internal bool TryFindOneActionPolicyForFixture(object resource, out string ownerId)
        {
            object?[] args = { resource, null };
            bool result = CompatibilityHostBroker.Invoke<bool>(Backend, "TryFindOneActionPolicyForFixture", args);
            ownerId = args[1] as string ?? string.Empty;
            return result;
        }

        private T TryRead<T>(string property, T fallback)
        {
            return broker.TryGetService("ActionCompletion", out object? backend) && backend != null
                ? CompatibilityHostBroker.ReadProperty(backend, property, fallback)
                : fallback;
        }

        private T TryInvoke<T>(string method, T fallback, params object?[] args)
        {
            return broker.TryGetService("ActionCompletion", out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(backend, method, args)
                : fallback;
        }
    }
}
