using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private AnimalHusbandryConfig config = new AnimalHusbandryConfig();
        private AnimalHusbandryNativeRuntime? runtime;

        public override void Entry(IDtmHelper helper)
        {
            if (runtime != null) throw new InvalidOperationException("AnimalHusbandryProgress has already entered.");
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            var names = helper.ModRegistry.GetApi<IItemDisplayNameApi>("DTMAPI.GameBridge.DolocTown")
                ?? throw new InvalidOperationException("Item display-name service is required.");
            var resources = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            var hooks = new AnimalHusbandryHookInstaller(helper.Monitor);
            resources.Register(hooks);
            runtime = new AnimalHusbandryNativeRuntime(helper.Monitor, names, helper.GetReflection(), hooks);
            resources.Register(runtime);
            config = helper.ReadConfig<AnimalHusbandryConfig>();
            config.Normalize();
            runtime.Configure(config);
            runtime.InstallHooksAtomically();
            RegisterConfigMenu();
            helper.Events.GameLoop.UpdateTicked += (_, __) => runtime.Update();
            helper.Events.Save.SaveLoaded += (_, __) => runtime.ResetBoundary("SaveLoaded");
            helper.Events.GameLoop.ReturnedToTitle += (_, __) => runtime.ResetBoundary("ReturnedToTitle");
            helper.Monitor.Log("AnimalHusbandryProgress ready; owner=" + AnimalHusbandryHookInstaller.HarmonyOwner + "; patches=" + hooks.InstalledPatchCount + ".");
        }

        private void RegisterConfigMenu()
        {
            var menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu")
                ?? throw new InvalidOperationException("Configuration menu is required.");
            menu.Register(helper.ModManifest, () => config = new AnimalHusbandryConfig(), () =>
            {
                config.Normalize();
                helper.WriteConfig(config);
                runtime!.Configure(config);
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "Animal produce progress"));
            menu.AddColorPresetOption(helper.ModManifest, () => T("config.colorPreset.name", "Color preset"), () => string.Empty,
                () => config.ColorPreset, value => config.ColorPreset = value, new[]
                {
                    new DtmColorPreset("Orange", T("config.color.orange", "Orange"), "FF942E"),
                    new DtmColorPreset("Green", T("config.color.green", "Green"), "70C978"),
                    new DtmColorPreset("Blue", T("config.color.blue", "Blue"), "65A6FF"),
                    new DtmColorPreset("Pink", T("config.color.pink", "Pink"), "FF7AC8"),
                    new DtmColorPreset("White", T("config.color.white", "White"), "F0F0F0"),
                    new DtmColorPreset("Custom", T("config.color.custom", "Custom"), config.ProgressColorHex)
                });
            Func<bool> custom = () => string.Equals(config.ColorPreset, "Custom", StringComparison.OrdinalIgnoreCase);
            menu.AddTextOption(helper.ModManifest, () => T("config.fillColor.name", "Fill color"), () => string.Empty,
                () => config.ProgressColorHex, value => config.ProgressColorHex = value, custom, custom);
        }
        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);
    }
}
