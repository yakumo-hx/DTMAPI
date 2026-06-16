using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AudioReplacementHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly AudioReplacementService service;

        public AudioReplacementHookBridge(DtmApiRuntime runtime, AudioReplacementService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool InternalPostSoundEventPatched { get; private set; }
        internal bool PaperBoxInteractPatched { get; private set; }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Audio.SoundEventReplacement",
                "contract",
                "IAudioReplacementApi -> WwiseSoundManager.InternalPostSoundEvent Prefix",
                "Experimental native sound-event replacement contract. Public API uses string event names; GameBridge owns Wwise/Unity reflection and must fail open when replacement audio is not playable.");
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!InternalPostSoundEventPatched)
            {
                var exactEventCallbackSignature = HarmonyTargetSignature.Exact(
                    "DolocTown.WwiseSoundManager",
                    "System.Boolean",
                    "System.String",
                    "UnityEngine.GameObject",
                    "EventCallback",
                    "System.Boolean");
                var exactNestedCallbackSignature = HarmonyTargetSignature.Exact(
                    "DolocTown.WwiseSoundManager",
                    "System.Boolean",
                    "System.String",
                    "UnityEngine.GameObject",
                    "AkCallbackManager+EventCallback",
                    "System.Boolean");
                InternalPostSoundEventPatched = patcher.TryPatchPrefix(
                    "DolocTown.WwiseSoundManager, Assembly-CSharp",
                    "InternalPostSoundEvent",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.WwiseInternalPostSoundEventPrefix), BindingFlags.Public | BindingFlags.Static),
                    exactEventCallbackSignature) ||
                    patcher.TryPatchPrefix(
                        "DolocTown.WwiseSoundManager, Assembly-CSharp",
                        "InternalPostSoundEvent",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.WwiseInternalPostSoundEventPrefix), BindingFlags.Public | BindingFlags.Static),
                        exactNestedCallbackSignature);
            }

            if (!PaperBoxInteractPatched)
            {
                PaperBoxInteractPatched = patcher.TryPatchPostfix(
                    "DolocTown.DungeonResourceModelPaperBox, Assembly-CSharp",
                    "OnInteract",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DungeonResourceModelPaperBoxOnInteractPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            service.SetHookInstalled(InternalPostSoundEventPatched);
            runtime.SetHookStatus(
                "Audio.SoundEventReplacement",
                InternalPostSoundEventPatched ? "experimental" : "pending",
                "Harmony Prefix: WwiseSoundManager.InternalPostSoundEvent; diagnostic Postfix: DungeonResourceModelPaperBox.OnInteract",
                InternalPostSoundEventPatched
                    ? "Patched the native Wwise event bridge. Replacement playback suppresses native audio only after local audio is ready and a replacement backend starts successfully. Paper-box native owner diagnostic patched=" + PaperBoxInteractPatched + "."
                    : "Waiting for WwiseSoundManager.InternalPostSoundEvent to become patchable. Paper-box native owner diagnostic patched=" + PaperBoxInteractPatched + ".");
        }
    }
}

