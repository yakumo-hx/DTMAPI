using System;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    internal sealed class FishBreedingNativeRuntime
    {
        private readonly IMonitor monitor;
        private readonly IItemDisplayNameApi itemDisplayNames;
        private readonly FishBreedingHookInstaller hooks;
        private FishBreedingConfig config = new FishBreedingConfig();

        internal FishBreedingNativeRuntime(IMonitor monitor, IItemDisplayNameApi itemDisplayNames)
        {
            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.itemDisplayNames = itemDisplayNames ?? throw new ArgumentNullException(nameof(itemDisplayNames));
            hooks = new FishBreedingHookInstaller(monitor);
        }

        internal int InstalledPatchCount => hooks.InstalledPatchCount;

        internal void InstallHooksAtomically()
        {
            hooks.InstallAtomically();
            try
            {
                FishBreedingCallbacks.Attach(this);
            }
            catch
            {
                hooks.UnpatchOwnedHooks();
                throw;
            }
        }

        internal void Configure(FishBreedingConfig value)
        {
            config = (value ?? new FishBreedingConfig()).Copy();
            config.Normalize();
        }

        internal string DecorateTitle(object item, string current)
        {
            string result = current ?? string.Empty;
            if (!config.Enabled || !config.LabelFishRoeTitle || !IsFishRoe(item))
                return result;
            try
            {
                string fishId = item.GetType().GetProperty("fishName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item) as string ?? string.Empty;
                if (!itemDisplayNames.TryGetDisplayName(fishId, out string fishTitle))
                    return result;
                string marker = " (" + fishTitle + ")";
                return result.IndexOf(marker, StringComparison.Ordinal) >= 0 ? result : result + marker;
            }
            catch (Exception ex)
            {
                monitor.Log("FishBreedingAssistant native title fallback failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return result;
            }
        }

        internal void ResetBoundary(string reason)
        {
            monitor.Log("FishBreedingAssistant product boundary reset reason=" + (reason ?? string.Empty) + "; shared item-name cache remains platform-owned.");
        }

        internal void DeactivateOwner(string reason)
        {
            var failures = new List<Exception>();
            TryCleanup(() => ResetBoundary(reason), failures);
            TryCleanup(() => FishBreedingCallbacks.Detach(this), failures);
            TryCleanup(hooks.UnpatchOwnedHooks, failures);
            if (failures.Count == 1)
                throw failures[0];
            if (failures.Count > 1)
                throw new AggregateException("FishBreedingAssistant state cleanup, callback detach and exact-owner Harmony cleanup failed.", failures);
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures)
        {
            try { cleanup(); }
            catch (Exception ex) { failures.Add(ex); }
        }

        private static bool IsFishRoe(object item)
        {
            if (item == null)
                return false;
            for (Type? type = item.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName == "DolocTown.ItemFishRoe")
                    return true;
            }
            return false;
        }

    }
}
