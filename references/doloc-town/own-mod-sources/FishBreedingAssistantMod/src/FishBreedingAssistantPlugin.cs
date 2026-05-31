using System;
using System.Reflection;
using DolocTown.SMAPI;
using DolocTown.SMAPI.Experimental;

[assembly: AssemblyVersion("1.1.3.0")]
[assembly: AssemblyFileVersion("1.1.3.0")]

namespace Dlk.DolocFishBreedingAssistant
{
    public sealed class FishBreedingAssistantMod : IDolocMod, IDolocModLifecycle
    {
        public const string PluginGuid = "Yuuka.FishBreedingAssistant";
        public const string PluginName = "DolocTownFishBreedingAssistant";
        public const string PluginVersion = "1.1.3";

        private IModHelper helper;
        private FishBreedingAssistantConfig config;
        private bool active;
        private bool subscribed;

        public void Entry(IModHelper helper)
        {
            this.helper = helper;
            config = helper.Config.ReadConfig<FishBreedingAssistantConfig>();
            helper.Diagnostics.ReportFeature("native-smapi", "SMAPI native entry", DiagnosticFeatureStatus.Available, "Loaded through IDolocMod and uses helper.Experimental.Items instead of patching item title/description/detail methods directly.");
            helper.Diagnostics.ReportFeature("fish-roe-display", "Fish roe title/description display", DiagnosticFeatureStatus.Experimental, "Reads ItemFishRoe identity, then appends hatch fish, hatch time, and growth time.");
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

                helper.Monitor.Warn(PluginName + " disabled. Item display events were unsubscribed; restart the game to unload the DLL completely.");
            }
        }

        private void StartRuntime()
        {
            active = true;
            if (helper == null || subscribed)
            {
                return;
            }

            helper.Experimental.Items.TitleRendering += OnTitleRendering;
            helper.Experimental.Items.DescriptionRendering += OnDescriptionRendering;
            helper.Experimental.Items.DetailRendering += OnDetailRendering;
            subscribed = true;
        }

        private void StopRuntime()
        {
            active = false;
            if (helper == null || !subscribed)
            {
                return;
            }

            helper.Experimental.Items.TitleRendering -= OnTitleRendering;
            helper.Experimental.Items.DescriptionRendering -= OnDescriptionRendering;
            helper.Experimental.Items.DetailRendering -= OnDetailRendering;
            subscribed = false;
        }

        private void OnTitleRendering(object sender, ItemDisplayEventArgs e)
        {
            if (!active || config == null || !config.LabelFishRoeTitle)
            {
                return;
            }

            FishRoeInfo info;
            if (!TryGetInfo(e.Item, out info))
            {
                return;
            }

            string baseTitle = string.IsNullOrEmpty(e.Text) ? info.RoeTitle : e.Text;
            string marker = "（" + info.FishTitle + "）";
            if (baseTitle.IndexOf(marker, StringComparison.Ordinal) >= 0)
            {
                return;
            }

            e.Text = baseTitle + marker;
        }

        private void OnDescriptionRendering(object sender, ItemDisplayEventArgs e)
        {
            if (!active || config == null || !config.LabelFishRoeDetails)
            {
                return;
            }

            FishRoeInfo info;
            if (!TryGetInfo(e.Item, out info))
            {
                return;
            }

            e.Text = AppendDetail(e.Text, info);
        }

        private void OnDetailRendering(object sender, ItemDisplayEventArgs e)
        {
            if (!active || config == null || !config.LabelFishRoeDetails)
            {
                return;
            }

            FishRoeInfo info;
            if (!TryGetInfo(e.Item, out info))
            {
                return;
            }

            e.Text = AppendDetail(e.Text, info);
        }

        private bool TryGetInfo(object item, out FishRoeInfo info)
        {
            info = null;
            if (helper == null)
            {
                return false;
            }

            FishRoeIdentity roe;
            return helper.Experimental.Items.TryGetFishRoeIdentity(item, out roe)
                   && FishBreedingLookup.TryGet(roe.FishId, out info);
        }

        private string AppendDetail(string current, FishRoeInfo info)
        {
            string prefix = string.IsNullOrEmpty(current) ? string.Empty : current.Trim();
            string extra = "孵化：" + info.FishTitle + "；孵化时间：" + info.IncubateText + "；成长时间：" + info.GrowText;
            if (prefix.Length == 0)
            {
                return extra;
            }

            if (prefix.IndexOf("孵化：" + info.FishTitle, StringComparison.Ordinal) >= 0)
            {
                return current;
            }

            return prefix + "\n" + extra;
        }
    }

    public sealed class FishBreedingAssistantConfig
    {
        public bool LabelFishRoeTitle = true;
        public bool LabelFishRoeDetails = true;
    }
}
