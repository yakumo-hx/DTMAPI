using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ItemDisplayNameService : IItemDisplayNameApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly Func<string, string?> resolveDisplayName;
        private readonly Func<bool> isEnvironmentResetHookReady;
        private readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool cacheLifecycleDemanded;
        private bool cacheReadyPublished;

        internal ItemDisplayNameService(DtmApiRuntime runtime, Func<bool> isEnvironmentResetHookReady)
            : this(runtime, ResolveNativeDisplayName, isEnvironmentResetHookReady)
        {
        }

        internal ItemDisplayNameService(
            DtmApiRuntime runtime,
            Func<string, string?> resolveDisplayName,
            Func<bool> isEnvironmentResetHookReady)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.resolveDisplayName = resolveDisplayName ?? throw new ArgumentNullException(nameof(resolveDisplayName));
            this.isEnvironmentResetHookReady = isEnvironmentResetHookReady ?? throw new ArgumentNullException(nameof(isEnvironmentResetHookReady));
        }

        public bool TryGetDisplayName(string itemId, out string displayName)
        {
            if (!runtime.IsRuntimeThread)
                throw new InvalidOperationException("IItemDisplayNameApi may only be called on the DTMAPI Runtime/main thread.");

            displayName = string.Empty;
            itemId = (itemId ?? string.Empty).Trim();
            if (itemId.Length == 0)
                return false;
            if (cache.TryGetValue(itemId, out displayName))
                return true;
            try
            {
                displayName = resolveDisplayName(itemId) ?? string.Empty;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Shared item display-name lookup failed for " + itemId + ".", ex.ToString());
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = string.Empty;
                return false;
            }

            if (!cacheLifecycleDemanded)
            {
                GameBridgeDemandRoutes.SetOwnerDemand(
                    runtime,
                    GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset,
                    GameBridgeDemandRoutes.OperationOwner,
                    RuntimeDemandSourceType.CapabilityOperation,
                    RuntimeDemandLifetime.Operation,
                    "cache-lifecycle",
                    true,
                    "A successful shared item display-name lookup requires the native environment-reset lifecycle Hook before caching.");
                cacheLifecycleDemanded = true;
            }
            bool hookReady = false;
            try
            {
                hookReady = isEnvironmentResetHookReady();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Shared item display-name cache readiness check failed.", ex.ToString());
            }
            if (hookReady)
            {
                cache[itemId] = displayName;
                if (!cacheReadyPublished)
                {
                    cacheReadyPublished = true;
                    runtime.SetHookStatus(
                        "SharedNative.ItemDisplayNameCache",
                        "active",
                        "IItemDisplayNameApi.TryGetDisplayName",
                        "cache=" + cache.Count + "; demand=true; hookReady=true; successfulNonEmptyOnly=true; negativeCache=false");
                }
            }
            return true;
        }

        internal void ObserveOwnerLookup(string ownerId, string itemId)
        {
            runtime.RuntimeMonitor.Log(
                "Shared item display-name consumer query succeeded owner=" + (ownerId ?? string.Empty) +
                " itemId=" + (itemId ?? string.Empty) + "; demand=true.");
        }

        internal void Clear(string reason)
        {
            int count = cache.Count;
            bool lifecycleDemanded = cacheLifecycleDemanded;
            cache.Clear();
            if (cacheLifecycleDemanded)
            {
                GameBridgeDemandRoutes.SetOwnerDemand(
                    runtime,
                    GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset,
                    GameBridgeDemandRoutes.OperationOwner,
                    RuntimeDemandSourceType.CapabilityOperation,
                    RuntimeDemandLifetime.Operation,
                    "cache-lifecycle",
                    false,
                    "Shared item display-name cache lifecycle cleared: " + (reason ?? string.Empty));
                cacheLifecycleDemanded = false;
            }
            cacheReadyPublished = false;
            if (count == 0 && !lifecycleDemanded)
                return;
            string actualReason = reason ?? string.Empty;
            runtime.RuntimeMonitor.Log("Shared item display-name cache cleared count=" + count + " reason=" + actualReason + "; demandReleased=true.");
            runtime.SetHookStatus(
                "SharedNative.ItemDisplayNameCache",
                "cleared",
                actualReason,
                "cacheBefore=" + count + "; cacheAfter=0; demandReleased=true; negativeCache=false");
            if (actualReason.IndexOf("EnvironmentReset", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                runtime.SetHookStatus(
                    "SharedNative.ItemDisplayNameEnvironmentReset",
                    "cleared",
                    actualReason,
                    "cacheBefore=" + count + "; cacheAfter=0; demandReleased=true; causalBoundary=DolocAPI.SetEnvCamera");
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
