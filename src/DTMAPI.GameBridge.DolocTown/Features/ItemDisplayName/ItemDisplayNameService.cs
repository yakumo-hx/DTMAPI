using System;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ItemDisplayNameService : IItemDisplayNameApi
    {
        private readonly DtmApiRuntime runtime;

        internal ItemDisplayNameService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public bool TryGetDisplayName(string itemId, out string displayName)
        {
            if (!runtime.IsRuntimeThread)
                throw new InvalidOperationException("IItemDisplayNameApi may only be called on the DTMAPI Runtime/main thread.");

            displayName = string.Empty;
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            try
            {
                string? title = ResolveNativeDisplayName(itemId);
                if (string.IsNullOrWhiteSpace(title))
                    return false;
                displayName = title!;
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Native item title lookup failed for " + itemId + ".", ex.ToString());
                return false;
            }
        }

        private static string? ResolveNativeDisplayName(string itemId)
        {
            Type? dolocApi = GameBridgeNativeHelpers.ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? query = dolocApi?.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
            object?[] args = { itemId, null };
            if (query == null || !(query.Invoke(null, args) is bool found) || !found || args[1] == null)
                return null;
            using (var scope = new ReflectionScope())
                return scope.TryProperty<string>(args[1]!, "Title", out var title) ? title!.GetValue() : null;
        }
    }
}
