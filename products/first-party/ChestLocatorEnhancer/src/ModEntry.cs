using System;
using System.Collections.Generic;
using System.Globalization;
using global::DTMAPI.Abstractions;

namespace DTMAPI.ChestLocatorEnhancer
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private ChestLocatorEnhancerNativeRuntime nativeRuntime = null!;
        private bool entryStarted;
        private bool runtimeCreated;
        private bool saveEventSubscribed;
        private bool titleEventSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper =
                helper ?? throw new ArgumentNullException(nameof(helper));
            entryStarted = true;
            nativeRuntime =
                new ChestLocatorEnhancerNativeRuntime(helper.Monitor);
            runtimeCreated = true;
            try
            {
                nativeRuntime.Activate();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveEventSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                titleEventSubscribed = true;
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "DTMAPI Chest Locator Enhancer loaded; locator-marked containers can contribute materials across farm buildings.") +
                    " owner=" +
                    ChestLocatorEnhancerHookInstaller.HarmonyOwner +
                    " patches=" +
                    nativeRuntime.InstalledPatchCount +
                    ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                if (runtimeCreated)
                {
                    TryCleanup(
                        () => nativeRuntime.DeactivateOwner("entry-failed"),
                        failures);
                }

                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "ChestLocatorEnhancer Entry failed and rollback encountered additional cleanup failures.",
                    failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            var failures = new List<Exception>();
            if (entryStarted)
            {
                // Core owns platform event-root removal once this owner is
                // Deactivating; owner-bound event proxies reject mutations here.
                saveEventSubscribed = false;
                titleEventSubscribed = false;
            }
            if (runtimeCreated)
            {
                TryCleanup(
                    () => nativeRuntime.DeactivateOwner("OwnerDeactivation"),
                    failures);
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "ChestLocatorEnhancer owner deactivation could not prove complete product-private cleanup.",
                    failures);
            }

            disposed = true;
        }

        private void OnSaveLoaded(
            object sender,
            SaveLoadedEventArgs e)
        {
            nativeRuntime.ResetBoundary("SaveLoaded");
            helper.Monitor.Log(
                "ChestLocatorEnhancer SaveLoaded boundary reset slot=" +
                (e.SaveSlot?.ToString(
                    CultureInfo.InvariantCulture) ??
                 "unknown") +
                ".");
        }

        private void OnReturnedToTitle(
            object sender,
            EventArgs e)
        {
            nativeRuntime.ResetBoundary("ReturnedToTitle");
            helper.Monitor.Log(
                "ChestLocatorEnhancer ReturnedToTitle boundary reset.");
        }

        private void UnsubscribeEvents()
        {
            if (saveEventSubscribed)
            {
                helper.Events.Save.SaveLoaded -= OnSaveLoaded;
                saveEventSubscribed = false;
            }
            if (titleEventSubscribed)
            {
                helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle;
                titleEventSubscribed = false;
            }
        }

        private string T(
            string key,
            string fallback) =>
            helper.Translation.Get(key, fallback);

        private static void TryCleanup(
            Action cleanup,
            ICollection<Exception> failures)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }
}
