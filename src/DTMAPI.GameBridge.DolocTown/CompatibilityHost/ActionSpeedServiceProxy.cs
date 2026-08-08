#pragma warning disable CS0618
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Mandatory thin proxy for the frozen IActionSpeedApi ABI.</summary>
    internal sealed class ActionSpeedService : IActionSpeedApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly object[] constructionArguments;
        private Action? update;
        private Func<object, bool>? applyToolEnter;
        private Func<object, bool>? applyInteractEnter;
        private Action<object>? markAnimalInteract;
        private Func<object, bool>? applyEatEnter;
        private RefFloat? adjustUseItemDelta;
        private RefFloat? adjustInteractDelta;
        private Action<string>? restore;

        public ActionSpeedService(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = Array.Empty<object>();
        }

        internal ActionSpeedService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = new object[] { managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent)) };
        }

        private object Backend => broker.GetService("ActionSpeed", constructionArguments);
        internal int ActionSpeedApplicationCount => TryRead("ActionSpeedApplicationCount", 0);
        internal string LastActionSpeedApplicationSummary => TryRead("LastActionSpeedApplicationSummary", string.Empty);
        internal int ActionSpeedContinuousUseApplicationCount => TryRead("ActionSpeedContinuousUseApplicationCount", 0);
        internal string LastActionSpeedContinuousUseSummary => TryRead("LastActionSpeedContinuousUseSummary", string.Empty);
        internal string LastActionSpeedAutoFillSummary => TryRead("LastActionSpeedAutoFillSummary", string.Empty);
        internal int ActionSpeedAutoFillApplicationCount => TryRead("ActionSpeedAutoFillApplicationCount", 0);
        internal bool RequiresToolHooks => TryRead("RequiresToolHooks", false);
        internal bool RequiresInteractionHooks => TryRead("RequiresInteractionHooks", false);
        internal bool RequiresInteractExit => TryRead("RequiresInteractExit", false);
        internal bool RequiresBaseExit => TryRead("RequiresBaseExit", false);

        public void Configure(IManifest owner, ActionSpeedOptions options) => CompatibilityHostBroker.Invoke(Backend, "Configure", owner, options);
        public BridgeFeatureStatus GetStatus(string uniqueId) => CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);
        internal string GetActionSpeedLifecycleSummary() => TryInvoke("GetActionSpeedLifecycleSummary", "actionAnimators=0, actionAutoFillApplications=0, actionPendingAnimalInteract=false");
        internal void SetActionSpeedToolHooksInstalled(bool installed) => CompatibilityHostBroker.Invoke(Backend, "SetActionSpeedToolHooksInstalled", installed);
        internal void SetActionSpeedInteractionHooksInstalled(bool installed) => CompatibilityHostBroker.Invoke(Backend, "SetActionSpeedInteractionHooksInstalled", installed);
        internal void Update() => (update ??= broker.BindIfServiceLoaded<Action>("ActionSpeed", "Update"))?.Invoke();
        internal int ReconcileManagedProductOwnerBeforeHookInstall() => TryInvoke("ReconcileManagedProductOwnerBeforeHookInstall", 0);
        internal int RemoveOwner(string ownerId, string reason) => TryInvoke("RemoveOwner", 0, ownerId, reason);
        internal int CountOwnerResources(string ownerId) => TryInvoke("CountOwnerResources", 0, ownerId);
        internal bool TryGetConfiguredActionSpeedOwner(out string ownerId) => TryOwner("TryGetConfiguredActionSpeedOwner", out ownerId);
        internal bool TryGetConfiguredActionSpeedInteractionOwner(out string ownerId) => TryOwner("TryGetConfiguredActionSpeedInteractionOwner", out ownerId);
        internal bool ApplyActionSpeedToolEnter(object state) => (applyToolEnter ??= broker.BindIfServiceLoaded<Func<object, bool>>("ActionSpeed", "ApplyActionSpeedToolEnter"))?.Invoke(state) ?? false;
        internal bool ApplyActionSpeedInteractEnter(object state) => (applyInteractEnter ??= broker.BindIfServiceLoaded<Func<object, bool>>("ActionSpeed", "ApplyActionSpeedInteractEnter"))?.Invoke(state) ?? false;
        internal void MarkNativeAnimalInteract(object animalRenderer) => (markAnimalInteract ??= broker.BindIfServiceLoaded<Action<object>>("ActionSpeed", "MarkNativeAnimalInteract"))?.Invoke(animalRenderer);
        internal bool ApplyActionSpeedEatEnter(object state) => (applyEatEnter ??= broker.BindIfServiceLoaded<Func<object, bool>>("ActionSpeed", "ApplyActionSpeedEatEnter"))?.Invoke(state) ?? false;
        internal bool AdjustActionSpeedUseItemContinuesDelta(ref float dt) => TryRefFloat("AdjustActionSpeedUseItemContinuesDelta", ref dt);
        internal bool AdjustActionSpeedInteractContinuesDelta(ref float dt) => TryRefFloat("AdjustActionSpeedInteractContinuesDelta", ref dt);
        internal void RestoreActionSpeed(string reason) => (restore ??= broker.BindIfServiceLoaded<Action<string>>("ActionSpeed", "RestoreActionSpeed"))?.Invoke(reason);

        private bool TryOwner(string method, out string ownerId)
        {
            ownerId = string.Empty;
            if (!broker.TryGetService("ActionSpeed", out object? backend) || backend == null)
                return false;
            object?[] args = { null };
            bool result = CompatibilityHostBroker.Invoke<bool>(backend, method, args);
            ownerId = args[0] as string ?? string.Empty;
            return result;
        }

        private bool TryRefFloat(string method, ref float value)
        {
            RefFloat? callback = method == "AdjustActionSpeedUseItemContinuesDelta"
                ? adjustUseItemDelta ??= broker.BindIfServiceLoaded<RefFloat>("ActionSpeed", method)
                : adjustInteractDelta ??= broker.BindIfServiceLoaded<RefFloat>("ActionSpeed", method);
            return callback?.Invoke(ref value) ?? false;
        }

        private T TryRead<T>(string property, T fallback) => broker.TryGetService("ActionSpeed", out object? backend) && backend != null ? CompatibilityHostBroker.ReadProperty(backend, property, fallback) : fallback;
        private T TryInvoke<T>(string method, T fallback, params object?[] args) => broker.TryGetService("ActionSpeed", out object? backend) && backend != null ? CompatibilityHostBroker.Invoke<T>(backend, method, args) : fallback;
        private void TryInvokeVoid(string method, params object?[] args) { if (broker.TryGetService("ActionSpeed", out object? backend) && backend != null) CompatibilityHostBroker.Invoke(backend, method, args); }
        private delegate bool RefFloat(ref float value);
    }
}
