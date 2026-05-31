using System;
using System.Linq;
using System.Reflection;
using DolocTown.SMAPI;
using DolocTown.SMAPI.Experimental;
using UnityEngine;

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Dlk.DolocAnimalHusbandryProgress
{
    public sealed class AnimalHusbandryProgressMod : IDolocMod, IDolocModLifecycle
    {
        public const string PluginGuid = "Yuuka.AnimalHusbandryProgress";
        public const string PluginName = "DolocTownAnimalHusbandryProgress";
        public const string PluginVersion = "1.0.0";

        private IModHelper helper;
        private AnimalHusbandryProgressConfig config;
        private bool active;
        private bool subscribed;

        public void Entry(IModHelper helper)
        {
            this.helper = helper;
            config = helper.Config.ReadConfig<AnimalHusbandryProgressConfig>();
            helper.Diagnostics.ReportFeature("native-smapi", "SMAPI native entry", DiagnosticFeatureStatus.Available, "Loaded through IDolocMod and uses helper.Experimental.Animals.");
            helper.Diagnostics.ReportFeature("animal-husbandry-progress", "Animal special-produce progress bar", DiagnosticFeatureStatus.Experimental, "Adds one Runtime-managed progress bar to the animal bell viewer.");
            StartRuntime();
            helper.Monitor.Info(PluginName + " " + PluginVersion + " loaded as a native DolocTown SMAPI mod.");
        }

        public void OnEnabled()
        {
            StartRuntime();
        }

        public void OnDisabled()
        {
            StopRuntime();
            if (helper != null)
            {
                if (config != null)
                {
                    helper.Config.WriteConfig(config);
                }

                helper.Monitor.Warn(PluginName + " disabled. Animal viewer events were unsubscribed; restart the game to unload the DLL completely.");
            }
        }

        private void StartRuntime()
        {
            active = true;
            if (helper == null || subscribed)
            {
                return;
            }

            helper.Experimental.Animals.ViewerRendering += OnAnimalViewerRendering;
            subscribed = true;
        }

        private void StopRuntime()
        {
            active = false;
            if (helper == null || !subscribed)
            {
                return;
            }

            helper.Experimental.Animals.ViewerRendering -= OnAnimalViewerRendering;
            subscribed = false;
        }

        private void OnAnimalViewerRendering(object sender, AnimalViewerRenderingEventArgs e)
        {
            if (!active || config == null || !config.ShowSpecialProduceProgress || e == null || e.HusbandryProgress == null)
            {
                return;
            }

            AnimalHusbandryProgress best = e.HusbandryProgress
                .Where(p => p != null && p.Threshold > 0)
                .OrderByDescending(p => p.Progress)
                .ThenByDescending(p => p.Current)
                .FirstOrDefault();

            if (best == null)
            {
                return;
            }

            e.AddProgressBar(config.ProgressLabel, best.Progress, FormatProgressText(best), config.ProgressColor);
        }

        private string FormatProgressText(AnimalHusbandryProgress progress)
        {
            if (progress == null || progress.Threshold <= 0)
            {
                return string.Empty;
            }

            return progress.Current + "/" + progress.Threshold;
        }
    }

    public sealed class AnimalHusbandryProgressConfig
    {
        public bool ShowSpecialProduceProgress = true;
        public string ProgressLabel = "\u7279\u6b8a\u4ea7\u7269";
        public Color ProgressColor = new Color(1f, 0.58f, 0.18f, 1f);
    }
}
