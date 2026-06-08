using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
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

        internal bool ApplyOilCoalDropAfterToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null)
                return false;

            string key = BuildOilResourceHitKey(toolCollider, collider);
            pendingOilResourceHits.TryGetValue(key, out PendingOilResourceHit? pending);
            pendingOilResourceHits.Remove(key);

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null)
                resource = pending?.Resource;
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

        private string TryRollOilDropFromCoal(string resourceName, bool removed, string source)
        {
            if (!removed || !IsCoalResourceName(resourceName))
                return string.Empty;

            bool forced = ForceOilDropForSmoke;
            double roll = machineRandom.NextDouble();
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
