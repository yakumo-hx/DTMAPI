using System;
using DolocTown;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    internal sealed class FishBreedingNativeRuntime
    {
        private readonly IMonitor monitor;
        private readonly IItemDisplayNameApi itemDisplayNames;
        private readonly FishBreedingHookInstaller hooks;

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

        internal string DecorateTitle(object item, string current)
        {
            string result = current ?? string.Empty;
            if (!(item is ItemFishRoe roe))
                return result;
            try
            {
                string fishId = roe.fishName;
                if (string.IsNullOrWhiteSpace(fishId) ||
                    !itemDisplayNames.TryGetDisplayName(fishId, out string fishTitle) ||
                    string.IsNullOrWhiteSpace(fishTitle))
                    return result;
                string marker = " (" + fishTitle + ")";
                return result.EndsWith(marker, StringComparison.Ordinal) ? result : result + marker;
            }
            catch (Exception ex)
            {
                monitor.Log("FishBreedingAssistant native title fallback failed closed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return result;
            }
        }

        internal void DeactivateOwner()
        {
            FishBreedingCallbacks.Detach(this);
            hooks.UnpatchOwnedHooks();
        }

    }
}
