#pragma warning disable CS0618
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Mandatory thin proxy for the frozen IAnimalViewerApi ABI.</summary>
    internal sealed class AnimalViewerService : IAnimalViewerApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly object[] constructionArguments;
        private Action<object, object>? decorateFullInfo;
        private Func<object, object, bool>? renderOverlay;
        private Action? panelUnregistered;
        private Action<object, object>? prepareOverlay;
        private Action<bool>? refreshOverlay;

        public AnimalViewerService(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = Array.Empty<object>();
        }

        internal AnimalViewerService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            broker = CompatibilityHostBroker.For(runtime);
            constructionArguments = new object[] { managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent)) };
        }

        private object Backend => broker.GetService("AnimalViewer", constructionArguments);
        public void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options) => CompatibilityHostBroker.Invoke(Backend, "ConfigureSpecialProduceProgress", owner, options);
        public BridgeFeatureStatus GetStatus(string uniqueId) => CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);
        internal void SetAnimalViewerHookInstalled(bool installed) => CompatibilityHostBroker.Invoke(Backend, "SetAnimalViewerHookInstalled", installed);
        internal string GetAnimalViewerLifecycleSummary() => TryInvoke("GetAnimalViewerLifecycleSummary", "nativeData=0, overlayObjects=0, overlayRows=0");
        internal int ReconcileManagedProductOwnerBeforeHookInstall() => TryInvoke("ReconcileManagedProductOwnerBeforeHookInstall", 0);
        internal int RemoveOwner(string ownerId, string reason) => TryInvoke("RemoveOwner", 0, ownerId, reason);
        internal int CountOwnerResources(string ownerId) => TryInvoke("CountOwnerResources", 0, ownerId);
        internal void DecorateAnimalFullInfoData(object data, object animal) => (decorateFullInfo ??= broker.BindIfServiceLoaded<Action<object, object>>("AnimalViewer", "DecorateAnimalFullInfoData"))?.Invoke(data, animal);
        internal bool RenderAnimalProgressOverlay(object viewer, object data) => (renderOverlay ??= broker.BindIfServiceLoaded<Func<object, object, bool>>("AnimalViewer", "RenderAnimalProgressOverlay"))?.Invoke(viewer, data) ?? false;
        internal void ResetAnimalViewerRuntimeState(string reason) => TryInvokeVoid("ResetAnimalViewerRuntimeState", reason);
        internal void NotifyAnimalPanelUnregistered() => (panelUnregistered ??= broker.BindIfServiceLoaded<Action>("AnimalViewer", "NotifyAnimalPanelUnregistered"))?.Invoke();
        internal void NotifyAnimalViewerEnvironmentReset(string reason) => TryInvokeVoid("NotifyAnimalViewerEnvironmentReset", reason);
        internal void PrepareAnimalProgressOverlayBeforeShow(object viewer, object data) => (prepareOverlay ??= broker.BindIfServiceLoaded<Action<object, object>>("AnimalViewer", "PrepareAnimalProgressOverlayBeforeShow"))?.Invoke(viewer, data);
        internal bool HasAnimalProgressRowsForObservation(object? data, out string summary)
        {
            summary = string.Empty;
            if (!broker.TryGetService("AnimalViewer", out object? backend) || backend == null)
                return false;
            object?[] args = { data, null };
            bool result = CompatibilityHostBroker.Invoke<bool>(backend, "HasAnimalProgressRowsForObservation", args);
            summary = args[1] as string ?? string.Empty;
            return result;
        }
        internal void RefreshAnimalProgressOverlayTexts(bool force) => (refreshOverlay ??= broker.BindIfServiceLoaded<Action<bool>>("AnimalViewer", "RefreshAnimalProgressOverlayTexts"))?.Invoke(force);

        private T TryInvoke<T>(string method, T fallback, params object?[] args)
        {
            return broker.TryGetService("AnimalViewer", out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(backend, method, args)
                : fallback;
        }

        private void TryInvokeVoid(string method, params object?[] args)
        {
            if (broker.TryGetService("AnimalViewer", out object? backend) && backend != null)
                CompatibilityHostBroker.Invoke(backend, method, args);
        }
    }
}
