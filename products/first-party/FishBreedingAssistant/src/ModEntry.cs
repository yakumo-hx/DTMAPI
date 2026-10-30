using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private FishBreedingNativeRuntime? nativeRuntime;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            if (helper == null)
                throw new ArgumentNullException(nameof(helper));
            if (nativeRuntime != null)
                throw new InvalidOperationException("FishBreedingAssistant has already entered.");

            IItemDisplayNameApi names = helper.ModRegistry.GetApi<IItemDisplayNameApi>("DTMAPI.GameBridge.DolocTown")
                ?? throw new InvalidOperationException("The shared GameBridge item display-name adapter is required.");
            nativeRuntime = new FishBreedingNativeRuntime(helper.Monitor, names);
            try
            {
                nativeRuntime.InstallHooksAtomically();
                helper.Monitor.Log("FishBreedingAssistant ProductNative runtime ready owner=" + FishBreedingHookInstaller.HarmonyOwner + " patches=" + nativeRuntime.InstalledPatchCount + ".");
            }
            catch (Exception entryFailure)
            {
                try { nativeRuntime.DeactivateOwner(); }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException("FishBreedingAssistant Entry and rollback failed.", entryFailure, cleanupFailure);
                }
                nativeRuntime = null;
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            nativeRuntime?.DeactivateOwner();
            nativeRuntime = null;
            disposed = true;
        }
    }
}
