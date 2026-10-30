using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.OneActionComplete
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private OneActionConfig config = new OneActionConfig();
        private OneActionNativeRuntime? nativeRuntime;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ModEntry));
            if (nativeRuntime != null) throw new InvalidOperationException("OneActionComplete has already entered.");
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            config = helper.ReadConfig<OneActionConfig>();
            nativeRuntime = new OneActionNativeRuntime(helper.Monitor);
            try
            {
                nativeRuntime.Configure(config);
                nativeRuntime.InstallHooksAtomically();
                RegisterConfigMenu();
                helper.Monitor.Log("OneActionComplete ProductNative ready owner=" + OneActionHookInstaller.HarmonyOwner + "; patches=" + nativeRuntime.InstalledPatchCount + ".");
            }
            catch (Exception entryFailure)
            {
                try { nativeRuntime.DeactivateOwner(); }
                catch (Exception cleanupFailure) { throw new AggregateException("OneActionComplete Entry and rollback failed.", entryFailure, cleanupFailure); }
                nativeRuntime = null;
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            nativeRuntime?.DeactivateOwner();
            nativeRuntime = null;
            disposed = true;
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu")
                ?? throw new InvalidOperationException("The DTMAPI configuration menu is required.");
            menu.Register(helper.ModManifest, () => config = new OneActionConfig(), () =>
            {
                helper.WriteConfig(config);
                nativeRuntime!.Configure(config);
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddBoolOption(helper.ModManifest, () => T("config.trees.name", "Trees"), () => string.Empty, () => config.CompleteTrees, value => config.CompleteTrees = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.ores.name", "Ores"), () => string.Empty, () => config.CompleteOres, value => config.CompleteOres = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.garbage.name", "Garbage"), () => string.Empty, () => config.CompleteGarbage, value => config.CompleteGarbage = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.weeds.name", "Weeds"), () => string.Empty, () => config.CompleteWeeds, value => config.CompleteWeeds = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.fuel.name", "Fuel machines"), () => string.Empty, () => config.CompleteMachineFuel, value => config.CompleteMachineFuel = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.feeders.name", "Feeders"), () => string.Empty, () => config.CompleteFeeder, value => config.CompleteFeeder = value);
        }
        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);
    }
}
