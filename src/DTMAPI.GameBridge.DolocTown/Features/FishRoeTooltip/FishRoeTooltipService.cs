using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishRoeTooltipService : IItemTooltipApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, FishRoeTooltipOptions> fishRoeOptions = new Dictionary<string, FishRoeTooltipOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<string, FishRoeDisplayInfo?>> fishRoeLookups = new Dictionary<string, Func<string, FishRoeDisplayInfo?>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishRoeDisplayInfo> nativeFishRoeLookups = new Dictionary<string, FishRoeDisplayInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishRoeApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedNativeLookupFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool hooksInstalled;

        public FishRoeTooltipService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal void SetHooksInstalled(bool installed)
        {
            hooksInstalled = installed;
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
                ? new BridgeFeatureStatus(hooksInstalled ? "configured-verified-tooltip-hook" : "configured-pending-hook", hooksInstalled ? "Lookup provider accepted and item display hooks are installed; fish roe tooltip evidence is recorded, but the API remains experimental." : "Lookup provider accepted; item tooltip hooks are not yet installed.")
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
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info) &&
                    !TryLookupNativeFishRoe(fishId, out info))
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
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info) &&
                    !TryLookupNativeFishRoe(fishId, out info))
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

        private bool TryLookupNativeFishRoe(string fishId, out FishRoeDisplayInfo info)
        {
            info = null!;
            if (string.IsNullOrWhiteSpace(fishId))
                return false;
            if (nativeFishRoeLookups.TryGetValue(fishId, out FishRoeDisplayInfo cached))
            {
                info = cached;
                return !string.IsNullOrWhiteSpace(cached.FishTitle);
            }

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? queryItemProto = dolocApi?.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                if (queryItemProto == null)
                    return CacheNativeLookupFailure(fishId, "DolocAPI.QueryItemProto was not found.", out info);

                object?[] args = new object?[] { fishId, null };
                bool found = queryItemProto.Invoke(null, args) is bool ok && ok && args[1] != null;
                if (!found)
                    return CacheNativeLookupFailure(fishId, "No native item proto was found for " + fishId + ".", out info);

                string fishTitle = ReadStringMember(args[1]!, "Title", string.Empty);
                if (string.IsNullOrWhiteSpace(fishTitle))
                    return CacheNativeLookupFailure(fishId, "Native item proto for " + fishId + " has no Title.", out info);

                info = new FishRoeDisplayInfo
                {
                    FishId = fishId,
                    FishTitle = fishTitle,
                    RoeTitle = "Fish roe"
                };
                nativeFishRoeLookups[fishId] = info;
                LogOnce(loggedFishRoeApplications, "native:title:" + fishId, "Fish roe native title lookup resolved " + fishId + " -> " + fishTitle + ".");
                return true;
            }
            catch (Exception ex)
            {
                if (loggedNativeLookupFailures.Add(fishId))
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fish roe native title lookup failed for " + fishId + ".", ex.ToString());
                nativeFishRoeLookups[fishId] = new FishRoeDisplayInfo { FishId = fishId };
                return false;
            }
        }

        private bool CacheNativeLookupFailure(string fishId, string reason, out FishRoeDisplayInfo info)
        {
            info = new FishRoeDisplayInfo { FishId = fishId };
            nativeFishRoeLookups[fishId] = info;
            LogOnce(loggedNativeLookupFailures, "native-miss:" + fishId, "Fish roe native title lookup skipped for " + fishId + ": " + reason);
            return false;
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

        private void LogOnce(HashSet<string> keys, string key, string message)
        {
            if (keys.Contains(key))
                return;
            keys.Add(key);
            runtime.RuntimeMonitor.Log(message);
        }
    }
}
