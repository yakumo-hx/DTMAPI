using System;
using System.Linq;
using DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.Internal.StoryS2
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private StoryModule? module;
        private Acceptance? acceptance;
        public override void Entry(IDtmHelper helper)
        {
            var config = helper.ReadConfig<ExperimentConfig>();
            if (!config.Enabled)
            {
                helper.Monitor.Log("StoryS2 disabled; module=" + (StoryModule.Active != null) +
                    "; hooks=" + Harmony.GetAllPatchedMethods().Count(m => Harmony.GetPatchInfo(m).Owners.Contains(StoryModule.Owner)) +
                    "; unity=" + PreparedProject.ObserveUnityObjects() + "; subscriptions=0; candidate=unread");
                return;
            }
            try
            {
                module = new StoryModule(s => helper.Monitor.Log("StoryS2 " + s));
                module.Start();
                if (config.Acceptance)
                {
                    acceptance = new Acceptance(module, config, helper);
                    acceptance.Start();
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }
        public void Dispose()
        {
            acceptance?.Dispose(); acceptance = null;
            module?.Dispose(); module = null;
        }
    }

    public sealed class ExperimentConfig
    {
        public bool Enabled { get; set; }
        public bool Acceptance { get; set; }
        public int SaveSlot { get; set; } = 2;
        public bool QuitAfterAcceptance { get; set; } = true;
    }
}
