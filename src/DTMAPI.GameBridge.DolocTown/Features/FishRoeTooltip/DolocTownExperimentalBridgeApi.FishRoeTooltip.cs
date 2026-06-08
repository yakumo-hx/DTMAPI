using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
        internal void SetFishRoeHooksInstalled(bool installed)
        {
            fishRoeHooksInstalled = installed;
        }

        public void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            fishRoeOptions[owner.UniqueID] = options ?? new FishRoeTooltipOptions();
            if (lookup != null)
                fishRoeLookups[owner.UniqueID] = lookup;
            runtime.RuntimeMonitor.Log("Fish roe tooltip bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IItemTooltipApi.GetStatus(string uniqueId)
        {
            return fishRoeOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(fishRoeHooksInstalled ? "configured-verified-tooltip-hook" : "configured-pending-hook", fishRoeHooksInstalled ? "Lookup provider accepted and item display hooks are installed; fish roe tooltip evidence is recorded, but the API remains experimental." : "Lookup provider accepted; item tooltip hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No fish roe tooltip provider was registered for this mod.");
        }

        internal string DecorateFishRoeTitle(object item, string current)
        {
            if (!TryGetFishRoeId(item, out string fishId))
                return current ?? string.Empty;

            string result = current ?? string.Empty;
            foreach (KeyValuePair<string, FishRoeTooltipOptions> entry in fishRoeOptions)
            {
                FishRoeTooltipOptions options = entry.Value ?? new FishRoeTooltipOptions();
                if (!options.Enabled || !options.LabelFishRoeTitle)
                    continue;
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info))
                    continue;

                string fishTitle = FirstText(info.FishTitle, info.FishId, fishId);
                string marker = " (" + fishTitle + ")";
                if (result.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    continue;
                if (string.IsNullOrWhiteSpace(result))
                    result = FirstText(info.RoeTitle, "Fish roe");
                result += marker;
                LogOnce(loggedFishRoeApplications, entry.Key + ":title:" + fishId, "Fish roe title hook applied by " + entry.Key + " for " + fishId + ".");
            }
            return result;
        }

        internal string DecorateFishRoeDetail(object item, string current)
        {
            if (!TryGetFishRoeId(item, out string fishId))
                return current ?? string.Empty;

            string result = current ?? string.Empty;
            foreach (KeyValuePair<string, FishRoeTooltipOptions> entry in fishRoeOptions)
            {
                FishRoeTooltipOptions options = entry.Value ?? new FishRoeTooltipOptions();
                if (!options.Enabled || !options.LabelFishRoeDetails)
                    continue;
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info))
                    continue;

                string fishTitle = FirstText(info.FishTitle, info.FishId, fishId);
                string marker = "Hatches: " + fishTitle;
                if (result.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    continue;

                var parts = new List<string> { marker };
                if (!string.IsNullOrWhiteSpace(info.IncubateText))
                    parts.Add("Incubate: " + info.IncubateText);
                if (!string.IsNullOrWhiteSpace(info.GrowText))
                    parts.Add("Grow: " + info.GrowText);
                if (!string.IsNullOrWhiteSpace(info.ParentSummary))
                    parts.Add(info.ParentSummary);

                string extra = string.Join("; ", parts.ToArray());
                result = string.IsNullOrWhiteSpace(result) ? extra : result.TrimEnd() + Environment.NewLine + extra;
                LogOnce(loggedFishRoeApplications, entry.Key + ":detail:" + fishId, "Fish roe detail hook applied by " + entry.Key + " for " + fishId + ".");
            }
            return result;
        }

        private bool TryLookupFishRoe(string ownerId, string fishId, out FishRoeDisplayInfo info)
        {
            info = null!;
            if (!fishRoeLookups.TryGetValue(ownerId ?? string.Empty, out Func<string, FishRoeDisplayInfo?> lookup) || string.IsNullOrWhiteSpace(fishId))
                return false;
            try
            {
                FishRoeDisplayInfo? lookedUp = lookup(fishId);
                if (lookedUp == null)
                    return false;
                info = lookedUp;
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fish roe provider failed for " + ownerId + "/" + fishId + ".", ex.ToString());
                return false;
            }
        }

        private static bool TryGetFishRoeId(object item, out string fishId)
        {
            fishId = string.Empty;
            if (item == null)
                return false;
            Type type = item.GetType();
            if (!IsTypeOrBase(type, "DolocTown.ItemFishRoe"))
                return false;
            object? value = type.GetProperty("fishName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item);
            fishId = value as string ?? string.Empty;
            return !string.IsNullOrWhiteSpace(fishId);
        }
    }
}
