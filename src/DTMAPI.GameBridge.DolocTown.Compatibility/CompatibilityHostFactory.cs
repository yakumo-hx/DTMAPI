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
            if (serviceId == "EquipmentSlots") return new EquipmentSlotsCompatibilityService(runtime);
            throw new InvalidOperationException("Unknown recovery service '" + serviceId + "'.");
        }
    }
}
