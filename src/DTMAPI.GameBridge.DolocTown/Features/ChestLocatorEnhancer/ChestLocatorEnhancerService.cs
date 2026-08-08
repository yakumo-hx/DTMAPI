#pragma warning disable CS0618 // Frozen IChestLocatorEnhancerApi proxy.
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Mandatory thin proxy for the frozen IChestLocatorEnhancerApi ABI.</summary>
    internal sealed class ChestLocatorEnhancerService : IChestLocatorEnhancerApi
    {
        private readonly CompatibilityHostBroker broker;
        private readonly object[] constructionArguments;
        private Func<object, object, object, bool, Array, Array>? extendAvailableInventories;

        internal ChestLocatorEnhancerService(DtmApiRuntime runtime)
        {
            broker = CompatibilityHostBroker.For(runtime ?? throw new ArgumentNullException(nameof(runtime)));
            constructionArguments = Array.Empty<object>();
        }

        internal ChestLocatorEnhancerService(DtmApiRuntime runtime, Func<bool> managedProductOwnerPresent)
        {
            broker = CompatibilityHostBroker.For(runtime ?? throw new ArgumentNullException(nameof(runtime)));
            constructionArguments = new object[]
            {
                managedProductOwnerPresent ?? throw new ArgumentNullException(nameof(managedProductOwnerPresent))
            };
        }

        private object Backend => broker.GetService("ChestLocatorEnhancer", constructionArguments);

        internal string LastChestLocatorEnhancerSummary =>
            TryRead("LastChestLocatorEnhancerSummary", string.Empty);

        internal int ChestLocatorEnhancerExtensionApplications =>
            TryRead("ChestLocatorEnhancerExtensionApplications", 0);

        public ChestLocatorEnhancerRegisterResult Register(IManifest owner, ChestLocatorEnhancerOptions options) =>
            CompatibilityHostBroker.Invoke<ChestLocatorEnhancerRegisterResult>(
                Backend,
                "Register",
                owner,
                NormalizeChestLocatorEnhancerOptions(options));

        public ChestLocatorEnhancerState GetState(string uniqueId) =>
            CompatibilityHostBroker.Invoke<ChestLocatorEnhancerState>(Backend, "GetState", uniqueId);

        public BridgeFeatureStatus GetStatus(string uniqueId) =>
            CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);

        internal void SetInventoryHookInstalled(bool installed)
        {
            if (broker.TryGetService("ChestLocatorEnhancer", out object? backend) &&
                backend != null)
            {
                CompatibilityHostBroker.Invoke(
                    backend,
                    "SetInventoryHookInstalled",
                    installed);
            }
        }

        internal int ReconcileManagedProductOwnerBeforeHookInstall() =>
            TryInvoke("ReconcileManagedProductOwnerBeforeHookInstall", 0);

        internal int RemoveOwner(string ownerId, string reason) =>
            TryInvoke("RemoveOwner", 0, ownerId, reason);

        internal int CountOwnerResources(string ownerId) =>
            TryInvoke("CountOwnerResources", 0, ownerId);

        internal Array ExtendAvailableInventoriesForChestLocator(
            object archive,
            object anchor,
            object area,
            bool useBox,
            Array nativeResult)
        {
            Func<object, object, object, bool, Array, Array>? callback =
                extendAvailableInventories ??=
                    broker.BindIfServiceLoaded<Func<object, object, object, bool, Array, Array>>(
                        "ChestLocatorEnhancer",
                        "ExtendAvailableInventoriesForChestLocator");
            return callback?.Invoke(archive, anchor, area, useBox, nativeResult) ?? nativeResult;
        }

        internal static ChestLocatorEnhancerOptions NormalizeChestLocatorEnhancerOptions(
            ChestLocatorEnhancerOptions? options)
        {
            options ??= new ChestLocatorEnhancerOptions();
            return new ChestLocatorEnhancerOptions
            {
                Enabled = options.Enabled,
                IncludeSharedCases = options.IncludeSharedCases,
                IncludeSharedStorageShelfBoxes = options.IncludeSharedStorageShelfBoxes,
                RespectNativeAutoUseBoxSetting = options.RespectNativeAutoUseBoxSetting,
                VerboseLogging = options.VerboseLogging
            };
        }

        private T TryRead<T>(string property, T fallback) =>
            broker.TryGetService("ChestLocatorEnhancer", out object? backend) && backend != null
                ? CompatibilityHostBroker.ReadProperty(backend, property, fallback)
                : fallback;

        private T TryInvoke<T>(string method, T fallback, params object?[] arguments) =>
            broker.TryGetService("ChestLocatorEnhancer", out object? backend) && backend != null
                ? CompatibilityHostBroker.Invoke<T>(backend, method, arguments)
                : fallback;
    }
}
