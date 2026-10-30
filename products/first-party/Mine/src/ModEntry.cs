using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.Mine
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private MineConfig config = new MineConfig();
        private MineNativeRuntime runtime = null!;
        private bool runtimeCreated;
        private bool updateSubscribed;
        private bool saveSubscribed;
        private bool titleSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            runtime = new MineNativeRuntime(helper.Monitor);
            runtimeCreated = true;
            try
            {
                config =
                    helper.ReadConfig<MineConfig>() ??
                    new MineConfig();
                config.Normalize();
                runtime.InitializeAtEntry(
                    config,
                    helper.ModManifest,
                    IsOilAvailable);
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle +=
                    OnReturnedToTitle;
                titleSubscribed = true;
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "DTMAPI Mine loaded; official JSON owns the Mine item/equipment/recipe and ProductNative production starts after a save loads.") +
                    " owner=" +
                    MineProductContract.HarmonyOwner +
                    " scheduler=session-derived; runtime={" +
                    runtime.BuildStatusSummary() + "}.");
            }
            catch (Exception entryFailure)
            {
                var failures =
                    new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                if (runtimeCreated)
                {
                    TryCleanup(
                        () => runtime.DeactivateOwner(
                            "entry-failed"),
                        failures);
                }
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "Mine Entry failed and exact rollback encountered cleanup failures.",
                    failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            var failures = new List<Exception>();
            updateSubscribed = false;
            saveSubscribed = false;
            titleSubscribed = false;
            if (runtimeCreated)
            {
                TryCleanup(
                    () => runtime.DeactivateOwner(
                        "OwnerDeactivation"),
                    failures);
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Mine owner deactivation could not prove exact ProductNative cleanup.",
                    failures);
            }
            disposed = true;
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu =
                helper.ModRegistry.GetApi<IDtmConfigMenuApi>(
                    "DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(
                helper.ModManifest,
                ResetConfig,
                SaveConfig);
            menu.SetDisplayName(
                helper.ModManifest,
                () => T("mod.name", "DTMAPI Mine"));
            menu.AddSectionTitle(
                helper.ModManifest,
                () => T(
                    "config.section.machine",
                    "Mine production"));
            menu.AddParagraph(
                helper.ModManifest,
                () => runtime.BuildStatusSummary());
            menu.AddParagraph(
                helper.ModManifest,
                () => T(
                    "config.contract",
                    "Power is fixed at the native 10-point appliance threshold. Cycle progress is session-derived and restarts after loading a save."));
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.enabled.name",
                    "Runtime production enabled"),
                () => T(
                    "config.enabled.tooltip",
                    "Enable ProductNative Mine production. Disabling restores recipe, tech and visual originals immediately; enabling after a cold-disabled start requires restart."),
                () => config.Enabled,
                value => config.Enabled = value);
            menu.AddNumberOption(
                helper.ModManifest,
                () => T(
                    "config.cycle.name",
                    "Cycle minutes"),
                () => T(
                    "config.cycle.tooltip",
                    "Game minutes per session-derived production cycle."),
                () => config.CycleMinutes,
                value =>
                    config.CycleMinutes =
                        (int)Math.Round(value),
                5d,
                720d,
                5d);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.oilRecipe.name",
                    "Use Oil recipe"),
                () => T(
                    "config.oilRecipe.tooltip",
                    "When DTMAPI Oil is loaded, temporarily use the Oil recipe. The exact original recipe is restored on disable, title or unload."),
                () => config.UseOilRecipeReplacement,
                value =>
                    config.UseOilRecipeReplacement = value);
            menu.AddSectionTitle(
                helper.ModManifest,
                () => T(
                    "config.section.outputs",
                    "Output weights"));
            AddWeightOption(
                menu,
                "config.weight.coal",
                "Coal weight",
                () => config.CoalWeight,
                value => config.CoalWeight = value);
            AddWeightOption(
                menu,
                "config.weight.copper",
                "Copper ore weight",
                () => config.CopperOreWeight,
                value => config.CopperOreWeight = value);
            AddWeightOption(
                menu,
                "config.weight.iron",
                "Iron ore weight",
                () => config.IronOreWeight,
                value => config.IronOreWeight = value);
            AddWeightOption(
                menu,
                "config.weight.oil",
                "Oil weight",
                () => config.OilWeight,
                value => config.OilWeight = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.verbose.name",
                    "Verbose logs"),
                () => T(
                    "config.verbose.tooltip",
                    "Write bounded Mine lifecycle diagnostics."),
                () => config.VerboseLogging,
                value => config.VerboseLogging = value);
        }

        private void AddWeightOption(
            IDtmConfigMenuApi menu,
            string key,
            string fallback,
            Func<double> getValue,
            Action<double> setValue)
        {
            menu.AddNumberOption(
                helper.ModManifest,
                () => T(key, fallback),
                () => T(
                    "config.weight.tooltip",
                    "Relative probability weight; zero disables this output."),
                getValue,
                setValue,
                0d,
                100d,
                0.5d);
        }

        private void SaveConfig()
        {
            config.Normalize();
            helper.WriteConfig(config);
            runtime.Configure(
                config,
                helper.ModManifest,
                IsOilAvailable,
                "config saved");
            SetUpdateSubscription(runtime.IsActive);
        }

        private void ResetConfig()
        {
            config = new MineConfig();
            config.Normalize();
        }

        private void OnUpdateTicked(
            object sender,
            UpdateTickedEventArgs e) =>
            runtime.Update();

        private void OnSaveLoaded(
            object sender,
            SaveLoadedEventArgs e)
        {
            runtime.SaveLoaded(e.SaveSlot);
            SetUpdateSubscription(runtime.IsActive);
        }

        private void OnReturnedToTitle(
            object sender,
            EventArgs e)
        {
            SetUpdateSubscription(false);
            runtime.ReturnedToTitle("ReturnedToTitle");
        }

        private bool IsOilAvailable() =>
            helper.ModRegistry.IsLoaded("DTMAPI.OilMod");

        private void SetUpdateSubscription(bool enabled)
        {
            if (enabled == updateSubscribed)
                return;
            if (enabled)
            {
                helper.Events.GameLoop.UpdateTicked +=
                    OnUpdateTicked;
            }
            else
            {
                helper.Events.GameLoop.UpdateTicked -=
                    OnUpdateTicked;
            }
            updateSubscribed = enabled;
        }

        private void UnsubscribeEvents()
        {
            if (updateSubscribed)
            {
                helper.Events.GameLoop.UpdateTicked -=
                    OnUpdateTicked;
                updateSubscribed = false;
            }
            if (saveSubscribed)
            {
                helper.Events.Save.SaveLoaded -= OnSaveLoaded;
                saveSubscribed = false;
            }
            if (titleSubscribed)
            {
                helper.Events.GameLoop.ReturnedToTitle -=
                    OnReturnedToTitle;
                titleSubscribed = false;
            }
        }

        private string T(string key, string fallback) =>
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
