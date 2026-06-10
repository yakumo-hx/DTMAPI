using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class OilCoalDropService
    {
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, PendingOilResourceHit> pendingOilResourceHits = new Dictionary<string, PendingOilResourceHit>(StringComparer.Ordinal);
        private readonly Random random = new Random();

        public OilCoalDropService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal bool ForceOilDropForSmoke { get; set; }

        internal int OilMiningDropCount { get; private set; }

        internal string LastOilMiningDropSummary { get; private set; } = string.Empty;

        internal void PublishHookStatuses(bool toolColliderRouteReady)
        {
            runtime.SetHookStatus(
                "Resources.OilCoalDrop",
                toolColliderRouteReady ? "experimental" : "pending",
                "Harmony Prefix/Postfix: ToolCollider.HandleTools",
                toolColliderRouteReady
                    ? "Patched pre-hit coal resource capture plus post-hit oil placement; waiting for OilMod coal mining smoke evidence."
                    : "Waiting for the shared ToolCollider.HandleTools Prefix/Postfix route to become patchable.");
        }

        internal void CaptureOilCoalDropBeforeToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null)
                return;

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null)
                return;

            string resourceName = GetResourceName(resource);
            if (!IsCoalResourceName(resourceName))
                return;

            string key = BuildOilResourceHitKey(toolCollider, collider);
            pendingOilResourceHits[key] = new PendingOilResourceHit(resource, resourceName, ReadIntMember(resource, "currentHealth", 0));
        }

        internal void ClearCapturedOilCoalDrop(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null)
                return;
            pendingOilResourceHits.Remove(BuildOilResourceHitKey(toolCollider, collider));
        }

        internal void ClearPendingOilResourceHits(string reason)
        {
            if (pendingOilResourceHits.Count <= 0)
                return;

            int count = pendingOilResourceHits.Count;
            pendingOilResourceHits.Clear();
            runtime.RuntimeMonitor.Log("OilMod pending coal-drop hit cache cleared reason=" + reason + ", count=" + count + ".");
        }

        internal bool ApplyOilCoalDropAfterToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null)
                return false;

            string key = BuildOilResourceHitKey(toolCollider, collider);
            pendingOilResourceHits.TryGetValue(key, out PendingOilResourceHit? pending);
            pendingOilResourceHits.Remove(key);

            object? resource = TryGetDungeonResourceFromCollider(collider) ?? pending?.Resource;
            if (resource == null)
            {
                if (ForceOilDropForSmoke)
                    runtime.RuntimeMonitor.Log("OilMod mining drop smoke probe source=native-tool-hit resourceFound=False collider=" + collider.GetType().FullName + ".");
                return false;
            }

            string resourceName = pending?.ResourceName ?? GetResourceName(resource);
            bool removed = IsResourceRemoved(resource);
            string summary = TryRollOilDropFromCoal(resourceName, removed, "native-tool-hit");
            if (string.IsNullOrWhiteSpace(summary))
            {
                if (ForceOilDropForSmoke)
                    runtime.RuntimeMonitor.Log("OilMod mining drop smoke probe source=native-tool-hit resource=" + resourceName + " removed=" + removed + " coal=" + IsCoalResourceName(resourceName) + ".");
                return false;
            }

            object? currentTool = ReadMember(toolCollider, "currentTool");
            string toolName = currentTool == null ? "unknown" : ReadStringMember(currentTool, "name", currentTool.GetType().Name);
            string toolType = currentTool == null ? "unknown" : (ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown");
            LastOilMiningDropSummary += ", tool=" + toolName + ", toolType=" + toolType;
            if (pending != null)
                LastOilMiningDropSummary += ", healthBefore=" + pending.HealthBefore;
            return summary.IndexOf("oilDrop=crude_oil", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal string TryRollOilDropFromCoal(string resourceName, bool removed, string source)
        {
            if (!removed || !IsCoalResourceName(resourceName))
                return string.Empty;

            bool forced = ForceOilDropForSmoke;
            double roll = random.NextDouble();
            if (!forced && roll > 0.08)
                return string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
            {
                LastOilMiningDropSummary = "source=" + source + ", resource=" + resourceName + ", forced=" + forced + ", roll=" + roll.ToString("0.0000", CultureInfo.InvariantCulture) + ", oilDrop=failed:missing-dolocapi";
                return "oilDrop=failed:missing-dolocapi";
            }
            if (TryPlaceNativeItemInBackpack(dolocApi, "crude_oil", 1, out string message))
            {
                OilMiningDropCount++;
                LastOilMiningDropSummary = "source=" + source + ", resource=" + resourceName + ", forced=" + forced + ", roll=" + roll.ToString("0.0000", CultureInfo.InvariantCulture) + ", oilDrop=crude_oil, count=1, placement={" + message + "}";
                runtime.RuntimeMonitor.Log("OilMod mining drop OK " + LastOilMiningDropSummary);
                runtime.SetHookStatus("OilMod.MiningDrop", "experimental", "ToolCollider.HandleTools Postfix -> DolocAPI.TryPlaceInBackpack", "Coal resource rolled crude_oil x1. " + LastOilMiningDropSummary);
                return "oilDrop=crude_oil";
            }

            LastOilMiningDropSummary = "source=" + source + ", resource=" + resourceName + ", forced=" + forced + ", roll=" + roll.ToString("0.0000", CultureInfo.InvariantCulture) + ", oilDrop=failed:" + message;
            runtime.SetHookStatus("OilMod.MiningDrop", "failed", "ToolCollider.HandleTools Postfix -> DolocAPI.TryPlaceInBackpack", message);
            return "oilDrop=failed:" + message;
        }

        private static bool TryPlaceNativeItemInBackpack(Type dolocApi, string itemId, int count, out string message)
        {
            message = string.Empty;
            if (dolocApi == null)
            {
                message = "DolocAPI is not available.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                message = "Invalid item id or count.";
                return false;
            }

            MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
            object?[] queryArgs = new object?[] { itemId, null };
            if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) || !found || queryArgs[1] == null)
            {
                message = "Item " + itemId + " is not present in DolocConfig.Tables.TbItem.";
                return false;
            }

            MethodInfo? canPlaceItem = dolocApi.GetMethod("CanPlaceItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int) }, null);
            MethodInfo? tryPlaceInBackpack = dolocApi.GetMethod("TryPlaceInBackpack", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            if (canPlaceItem == null || tryPlaceInBackpack == null)
            {
                message = "Native backpack placement methods are not available.";
                return false;
            }

            object? canPlace = canPlaceItem.Invoke(null, new object?[] { itemId, count });
            if (!(canPlace is bool okToPlace) || !okToPlace)
            {
                message = "Backpack cannot place " + itemId + " x" + count + ".";
                return false;
            }

            object? placed = tryPlaceInBackpack.Invoke(null, new object?[] { itemId, count, false });
            if (!(placed is bool ok) || !ok)
            {
                message = "DolocAPI.TryPlaceInBackpack returned false for " + itemId + " x" + count + ".";
                return false;
            }

            message = "Placed " + itemId + " x" + count + " through native backpack placement.";
            return true;
        }

        private static string BuildOilResourceHitKey(object toolCollider, object collider)
        {
            return RuntimeHelpers.GetHashCode(toolCollider).ToString(CultureInfo.InvariantCulture) + ":" +
                RuntimeHelpers.GetHashCode(collider).ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsCoalResourceName(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return false;
            string normalized = resourceName.Trim().ToLowerInvariant();
            return normalized.Equals("coal", StringComparison.Ordinal) ||
                normalized.Equals("coal_ore", StringComparison.Ordinal) ||
                normalized.Contains("coal");
        }

        private sealed class PendingOilResourceHit
        {
            public PendingOilResourceHit(object resource, string resourceName, int healthBefore)
            {
                Resource = resource;
                ResourceName = resourceName;
                HealthBefore = healthBefore;
            }

            public object Resource { get; }

            public string ResourceName { get; }

            public int HealthBefore { get; }
        }
    }
}
