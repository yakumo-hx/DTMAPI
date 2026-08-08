using System;
using System.Collections.Generic;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// The single reflected entry point for the dormant-shipped compatibility component.
    /// Construction creates no Harmony patch, demand, callback, or owner root.
    /// </summary>
    public static class CompatibilityHostFactory
    {
        public static object Create(DtmApiRuntime runtime)
        {
            return new CompatibilityHostBackend(runtime ?? throw new ArgumentNullException(nameof(runtime)));
        }
    }

    public sealed class CompatibilityHostBackend
    {
        private readonly object sync = new object();
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, object> services = new Dictionary<string, object>(StringComparer.Ordinal);
        private DTMAPI.DebugConsole.CompatibilityDebugActionService?
            debugActions;

        internal CompatibilityHostBackend(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public object GetService(string serviceId, object[] constructionArguments)
        {
            serviceId ??= string.Empty;
            constructionArguments ??= Array.Empty<object>();
            lock (sync)
            {
                if (services.TryGetValue(serviceId, out object service))
                    return service;

                service = CreateService(serviceId, constructionArguments);
                services.Add(serviceId, service);
                return service;
            }
        }

        private object CreateService(string serviceId, object[] arguments)
        {
            Func<bool>? ownerProbe = arguments.Length == 0 ? null : arguments[0] as Func<bool>;
            Func<bool>? compatibilityOwnerClaim =
                arguments.Length < 2
                    ? null
                    : arguments[1] as Func<bool>;
            switch (serviceId)
            {
                case "ActionCompletion":
                    return ownerProbe == null ? new ActionCompletionService(runtime) : new ActionCompletionService(runtime, ownerProbe);
                case "ActionSpeed":
                    return ownerProbe == null ? new ActionSpeedService(runtime) : new ActionSpeedService(runtime, ownerProbe);
                case "AnimalViewer":
                    return ownerProbe == null ? new AnimalViewerService(runtime) : new AnimalViewerService(runtime, ownerProbe);
                case "ChestLocatorEnhancer":
                    return ownerProbe == null
                        ? new ChestLocatorEnhancerCompatibilityService(runtime)
                        : new ChestLocatorEnhancerCompatibilityService(runtime, ownerProbe);
                case "Camera":
                    return new CameraCompatibilityBackend(
                        runtime,
                        ownerProbe,
                        compatibilityOwnerClaim);
                case "FishRoeTooltip":
                    return ownerProbe == null ? new FishRoeTooltipService(runtime) : new FishRoeTooltipService(runtime, ownerProbe);
                case "FishingAutomation":
                    return new LegacyFishingAutomationService(runtime);
                case "DebugConsole":
                    return new DTMAPI.DebugConsole.DebugConsoleCompatibilityService(
                        runtime,
                        GetDebugActions());
                case "DebugActions":
                    return GetDebugActions();
                case "EquipmentSlots":
                    return new EquipmentSlotsCompatibilityService(runtime);
                case "SaveSlots":
                    return new SaveSlotsCompatibilityService(
                        runtime,
                        arguments.Length > 0 ? arguments[0] as Func<object?> : null,
                        arguments.Length > 1 ? arguments[1] as Func<object, int> : null,
                        arguments.Length > 2 ? arguments[2] as Func<object, int, bool> : null);
                default:
                    throw new InvalidOperationException("Unknown frozen compatibility service '" + serviceId + "'.");
            }
        }

        private DTMAPI.DebugConsole.CompatibilityDebugActionService
            GetDebugActions() =>
            debugActions ??=
                new DTMAPI.DebugConsole.CompatibilityDebugActionService(
                    runtime);
    }
}
