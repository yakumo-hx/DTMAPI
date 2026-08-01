using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen three-target Hook set for old IItemTooltipApi consumers.</summary>
    internal sealed class FishRoeTooltipHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly FishRoeTooltipService service;

        public FishRoeTooltipHookBridge(DtmApiRuntime runtime, FishRoeTooltipService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool TitlePatched { get; private set; }

        internal bool DescriptionPatched { get; private set; }

        internal bool DetailPatched { get; private set; }

        internal bool AllPatched => TitlePatched && DescriptionPatched && DetailPatched;

        public void PublishHookStatuses()
        {
            PublishStatus();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!TitlePatched)
            {
                TitlePatched = patcher.TryPatchPostfix(
                    "DolocTown.Item, Assembly-CSharp",
                    "get_title",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemTitlePostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            if (!DescriptionPatched)
            {
                DescriptionPatched = patcher.TryPatchPostfix(
                    "DolocTown.Item, Assembly-CSharp",
                    "get_description",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemDescriptionPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            if (!DetailPatched)
            {
                DetailPatched = patcher.TryPatchPostfix(
                    "DolocTown.Item, Assembly-CSharp",
                    "GetDetailInfo",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemDetailInfoPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            service.SetHooksInstalled(AllPatched);
            PublishStatus();
        }

        private void PublishStatus()
        {
            runtime.SetHookStatus(
                "Items.FishRoeTooltip",
                AllPatched ? "verified" : "pending",
                "Harmony Postfix: Item.title/description/GetDetailInfo",
                AllPatched
                    ? "Patched item display paths for fish roe providers; verified by FISHROE-001."
                    : "Waiting for item display targets to become patchable.");
        }
    }
}
