using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AudioReplacementHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly AudioReplacementService service;
        private bool hasPublishedPhysicalState;
        private bool publishedInternalPostSoundEventPatched;
        private bool publishedPaperBoxInteractPatched;
        private bool publishedAnimalPlayAnimalSoundPrefixPatched;
        private bool publishedAnimalPlayAnimalSoundPostfixPatched;

        public AudioReplacementHookBridge(DtmApiRuntime runtime, AudioReplacementService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool InternalPostSoundEventPatched { get; private set; }
        internal bool PaperBoxInteractPatched { get; private set; }
        internal bool AnimalPlayAnimalSoundPrefixPatched { get; private set; }
        internal bool AnimalPlayAnimalSoundPostfixPatched { get; private set; }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus(
                "Audio.SoundEventReplacement",
                "contract",
                "IAudioReplacementApi/content-pack JSON -> WwiseSoundManager.InternalPostSoundEvent Prefix",
                "Experimental native sound-event replacement contract. Public API uses string event names; content packs may declare reviewed short SFX replacements; GameBridge owns Wwise/Unity reflection and must fail open when replacement audio is not playable.");
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

            if (service.HasEnabledPaperBoxDiagnosticDefinitions && !PaperBoxInteractPatched)
            {
                PaperBoxInteractPatched = patcher.TryPatchPostfix(
                    "DolocTown.DungeonResourceModelPaperBox, Assembly-CSharp",
                    "OnInteract",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DungeonResourceModelPaperBoxOnInteractPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            if (service.HasEnabledAnimalVoiceDefinitions && !AnimalPlayAnimalSoundPrefixPatched)
            {
                AnimalPlayAnimalSoundPrefixPatched = patcher.TryPatchPrefix(
                    "DolocTown.Animal, Assembly-CSharp",
                    "PlayAnimalSound",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalPlayAnimalSoundPrefix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            if (service.HasEnabledAnimalVoiceDefinitions && !AnimalPlayAnimalSoundPostfixPatched)
            {
                AnimalPlayAnimalSoundPostfixPatched = patcher.TryPatchPostfix(
                    "DolocTown.Animal, Assembly-CSharp",
                    "PlayAnimalSound",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalPlayAnimalSoundPostfix), BindingFlags.Public | BindingFlags.Static),
                    0);
            }

            PublishPhysicalHookStateIfChanged(
                InternalPostSoundEventPatched,
                PaperBoxInteractPatched,
                AnimalPlayAnimalSoundPrefixPatched,
                AnimalPlayAnimalSoundPostfixPatched);
        }

        internal bool PublishPhysicalHookStateIfChanged(
            bool internalPostSoundEventPatched,
            bool paperBoxInteractPatched,
            bool animalPlayAnimalSoundPrefixPatched,
            bool animalPlayAnimalSoundPostfixPatched)
        {
            if (hasPublishedPhysicalState &&
                publishedInternalPostSoundEventPatched == internalPostSoundEventPatched &&
                publishedPaperBoxInteractPatched == paperBoxInteractPatched &&
                publishedAnimalPlayAnimalSoundPrefixPatched == animalPlayAnimalSoundPrefixPatched &&
                publishedAnimalPlayAnimalSoundPostfixPatched == animalPlayAnimalSoundPostfixPatched)
            {
                return false;
            }

            service.SetHookInstalled(internalPostSoundEventPatched);
            runtime.SetHookStatus(
                "Audio.SoundEventReplacement",
                internalPostSoundEventPatched ? "experimental" : "pending",
                "Harmony Prefix: WwiseSoundManager.InternalPostSoundEvent; Animal.PlayAnimalSound context; diagnostic Postfix: DungeonResourceModelPaperBox.OnInteract",
                internalPostSoundEventPatched
                    ? "Patched the native Wwise event bridge. Replacement playback suppresses native audio only after local audio is ready and a replacement backend starts successfully. Paper-box native owner diagnostic patched=" + paperBoxInteractPatched + "; animalVoiceContextPrefix=" + animalPlayAnimalSoundPrefixPatched + "; animalVoiceContextPostfix=" + animalPlayAnimalSoundPostfixPatched + "."
                    : "Waiting for WwiseSoundManager.InternalPostSoundEvent to become patchable. Paper-box native owner diagnostic patched=" + paperBoxInteractPatched + "; animalVoiceContextPrefix=" + animalPlayAnimalSoundPrefixPatched + "; animalVoiceContextPostfix=" + animalPlayAnimalSoundPostfixPatched + ".");

            hasPublishedPhysicalState = true;
            publishedInternalPostSoundEventPatched = internalPostSoundEventPatched;
            publishedPaperBoxInteractPatched = paperBoxInteractPatched;
            publishedAnimalPlayAnimalSoundPrefixPatched = animalPlayAnimalSoundPrefixPatched;
            publishedAnimalPlayAnimalSoundPostfixPatched = animalPlayAnimalSoundPostfixPatched;
            return true;
        }
    }
}

