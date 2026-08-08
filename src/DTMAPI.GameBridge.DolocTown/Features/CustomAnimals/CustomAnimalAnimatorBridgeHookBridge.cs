using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CustomAnimalAnimatorBridgeHookBridge
    {
        private static readonly string[] RuntimeAnimatorControllerTypeNames =
        {
            "UnityEngine.RuntimeAnimatorController, UnityEngine.AnimationModule",
            "UnityEngine.RuntimeAnimatorController, UnityEngine",
            "UnityEngine.RuntimeAnimatorController, UnityEngine.CoreModule"
        };

        private readonly DtmApiRuntime runtime;
        private readonly CustomAnimalAnimatorBridgeService service;

        public CustomAnimalAnimatorBridgeHookBridge(DtmApiRuntime runtime, CustomAnimalAnimatorBridgeService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool AnimatorAssetTryLoadPatched { get; private set; }

        internal bool AnimalAIDefaultAnyStatePatched { get; private set; }

        internal bool AnimalOnRenderPatched { get; private set; }

        internal bool AnimalDebugSetAdultPatched { get; private set; }

        internal bool AnimalRendererOnRecyclePatched { get; private set; }

        internal bool SpriteOverrideTryGetModOverrideSpritePatched { get; private set; }

        internal bool AnimalSleepPatched { get; private set; }

        internal bool AnimalWakeUpPatched { get; private set; }

        internal bool AnimalCallToRoomPatched { get; private set; }

        internal bool AnimalRendererOnFellPrefixPatched { get; private set; }

        internal bool AnimalRendererOnFellPostfixPatched { get; private set; }

        internal bool AnimalRendererPlayAnimationPatched { get; private set; }

        internal bool AnimalRendererFixedUpdatePatched { get; private set; }

        internal bool AnimalAIMakeDecisionFreeTimePatched { get; private set; }

        internal bool AnimalControllerOnUpdatePatched { get; private set; }

        internal bool SleepWakeDiagnosticsPatched =>
            AnimalOnRenderPatched &&
            AnimalSleepPatched &&
            AnimalWakeUpPatched &&
            AnimalCallToRoomPatched &&
            AnimalRendererOnFellPrefixPatched &&
            AnimalRendererOnFellPostfixPatched &&
            AnimalRendererPlayAnimationPatched &&
            AnimalRendererFixedUpdatePatched;

        internal bool SleepTaskBoundaryPatched =>
            AnimalAIMakeDecisionFreeTimePatched &&
            AnimalControllerOnUpdatePatched;

        internal bool HooksReady =>
            (service.RegisteredKeyCount <= 0 || AnimatorAssetTryLoadPatched) &&
            (service.RegisteredAiTemplateCount <= 0 || AnimalAIDefaultAnyStatePatched) &&
            (service.RegisteredPngSpriteOverrideSpeciesCount <= 0 ||
                (AnimalOnRenderPatched && AnimalDebugSetAdultPatched && AnimalRendererOnRecyclePatched && SpriteOverrideTryGetModOverrideSpritePatched)) &&
            (service.RegisteredDiagnosticSpeciesCount <= 0 || SleepWakeDiagnosticsPatched) &&
            (service.RegisteredCustomAnimalSpeciesCount <= 0 || SleepTaskBoundaryPatched);

        public void PublishHookStatuses()
        {
            service.PublishHookStatuses(
                AnimatorAssetTryLoadPatched,
                AnimalAIDefaultAnyStatePatched,
                AnimalOnRenderPatched,
                AnimalDebugSetAdultPatched,
                AnimalRendererOnRecyclePatched,
                SpriteOverrideTryGetModOverrideSpritePatched,
                SleepWakeDiagnosticsPatched,
                SleepTaskBoundaryPatched);
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (service.RegisteredKeyCount > 0 && !AnimatorAssetTryLoadPatched)
            {
                foreach (string typeName in RuntimeAnimatorControllerTypeNames)
                {
                    AnimatorAssetTryLoadPatched = patcher.TryPatchAnimatorAssetTryLoadAssetPrefix(
                        "DolocTown.AnimatorAsset, Assembly-CSharp",
                        "TryLoadAsset",
                        typeName,
                        service.TryResolveRuntimeAnimatorController);
                    if (AnimatorAssetTryLoadPatched)
                        break;
                }
            }

            if (service.RegisteredAiTemplateCount > 0 && !AnimalAIDefaultAnyStatePatched)
            {
                AnimalAIDefaultAnyStatePatched = patcher.TryPatchAnimalAIDefaultAnyStatePostfix(
                    "DolocTown.AnimalAI, Assembly-CSharp",
                    "GetDefaultAnyState",
                    service.TryResolveDefaultAnimalAIStateType);
            }

            if (service.RegisteredPngSpriteOverrideSpeciesCount > 0)
            {
                if (!AnimalOnRenderPatched)
                {
                    AnimalOnRenderPatched = patcher.TryPatchPostfix(
                        "DolocTown.Animal, Assembly-CSharp",
                        "OnRender",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalOnRenderPostfix)),
                        parameterCount: 0);
                }

                if (!AnimalDebugSetAdultPatched)
                {
                    AnimalDebugSetAdultPatched = patcher.TryPatchPostfix(
                        "DolocTown.Animal, Assembly-CSharp",
                        "DEBUG_SetAdult",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalDebugSetAdultPostfix)),
                        parameterCount: 1);
                }

                if (!AnimalRendererOnRecyclePatched)
                {
                    AnimalRendererOnRecyclePatched = patcher.TryPatchPostfix(
                        "DolocTown.AnimalRenderer, Assembly-CSharp",
                        "OnRecycle",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalRendererOnRecyclePostfix)),
                        parameterCount: 0);
                }

                if (!SpriteOverrideTryGetModOverrideSpritePatched)
                {
                    SpriteOverrideTryGetModOverrideSpritePatched = patcher.TryPatchSpriteOverrideTryGetModOverrideSpritePostfix(
                        "DolocTown.SpriteOverrideHandler, Assembly-CSharp",
                        "TryGetModOverrideSprite",
                        "UnityEngine.Sprite, UnityEngine.CoreModule",
                        service.TryResolvePngSpriteOverride);
                    if (!SpriteOverrideTryGetModOverrideSpritePatched)
                    {
                        SpriteOverrideTryGetModOverrideSpritePatched = patcher.TryPatchSpriteOverrideTryGetModOverrideSpritePostfix(
                            "DolocTown.SpriteOverrideHandler, Assembly-CSharp",
                            "TryGetModOverrideSprite",
                            "UnityEngine.Sprite, UnityEngine",
                            service.TryResolvePngSpriteOverride);
                    }
                }
            }

            if (service.RegisteredDiagnosticSpeciesCount > 0)
            {
                if (!AnimalOnRenderPatched)
                {
                    AnimalOnRenderPatched = patcher.TryPatchPostfix(
                        "DolocTown.Animal, Assembly-CSharp",
                        "OnRender",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalOnRenderPostfix)),
                        parameterCount: 0);
                }

                if (!AnimalSleepPatched)
                {
                    AnimalSleepPatched = patcher.TryPatchPostfix(
                        "DolocTown.Animal, Assembly-CSharp",
                        "Sleep",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalSleepPostfix)),
                        parameterCount: 0);
                }

                if (!AnimalWakeUpPatched)
                {
                    AnimalWakeUpPatched = patcher.TryPatchPostfix(
                        "DolocTown.Animal, Assembly-CSharp",
                        "WakeUp",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalWakeUpPostfix)),
                        parameterCount: 0);
                }

                if (!AnimalCallToRoomPatched)
                {
                    AnimalCallToRoomPatched = patcher.TryPatchPostfix(
                        "DolocTown.Animal, Assembly-CSharp",
                        "CallToRoom",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalCallToRoomPostfix)),
                        parameterCount: 2);
                }

                if (!AnimalRendererOnFellPrefixPatched)
                {
                    AnimalRendererOnFellPrefixPatched = patcher.TryPatchPrefix(
                        "DolocTown.AnimalRenderer, Assembly-CSharp",
                        "OnFell",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalRendererOnFellPrefix)),
                        parameterCount: 2);
                }

                if (!AnimalRendererOnFellPostfixPatched)
                {
                    AnimalRendererOnFellPostfixPatched = patcher.TryPatchPostfix(
                        "DolocTown.AnimalRenderer, Assembly-CSharp",
                        "OnFell",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalRendererOnFellPostfix)),
                        parameterCount: 2);
                }

                if (!AnimalRendererPlayAnimationPatched)
                {
                    AnimalRendererPlayAnimationPatched = patcher.TryPatchPostfix(
                        "DolocTown.AnimalRenderer, Assembly-CSharp",
                        "PlayAnimation",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalRendererPlayAnimationPostfix)),
                        parameterCount: 3);
                }

                if (!AnimalRendererFixedUpdatePatched)
                {
                    AnimalRendererFixedUpdatePatched = patcher.TryPatchPostfix(
                        "DolocTown.AnimalRenderer, Assembly-CSharp",
                        "FixedUpdate",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalRendererFixedUpdatePostfix)),
                        parameterCount: 0);
                }
            }

            if (service.RegisteredCustomAnimalSpeciesCount > 0)
            {
                if (!AnimalAIMakeDecisionFreeTimePatched)
                {
                    AnimalAIMakeDecisionFreeTimePatched = patcher.TryPatchAnimalAIMakeDecisionFreeTimePrefix(
                        "DolocTown.AnimalAI+AnimalAIState, Assembly-CSharp",
                        "MakeDecision_FreeTime",
                        service.TryPreventSleepingFreeTimeDecision);
                }

                if (!AnimalControllerOnUpdatePatched)
                {
                    AnimalControllerOnUpdatePatched = patcher.TryPatchPostfix(
                        "DolocTown.AnimalController, Assembly-CSharp",
                        "OnUpdate",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalControllerOnUpdatePostfix)),
                        parameterCount: 1);
                }
            }

            service.PublishHookStatuses(
                AnimatorAssetTryLoadPatched,
                AnimalAIDefaultAnyStatePatched,
                AnimalOnRenderPatched,
                AnimalDebugSetAdultPatched,
                AnimalRendererOnRecyclePatched,
                SpriteOverrideTryGetModOverrideSpritePatched,
                SleepWakeDiagnosticsPatched,
                SleepTaskBoundaryPatched);
            runtime.RuntimeMonitor.Log(
                "CustomAnimals.AnimatorBridge hook install animatorAssetTryLoad=" + AnimatorAssetTryLoadPatched +
                " animalAIDefaultAnyState=" + AnimalAIDefaultAnyStatePatched +
                " animalOnRender=" + AnimalOnRenderPatched +
                " animalDebugSetAdult=" + AnimalDebugSetAdultPatched +
                " animalRendererOnRecycle=" + AnimalRendererOnRecyclePatched +
                " spriteOverrideTryGetModOverrideSprite=" + SpriteOverrideTryGetModOverrideSpritePatched +
                " animalSleep=" + AnimalSleepPatched +
                " animalWakeUp=" + AnimalWakeUpPatched +
                " animalCallToRoom=" + AnimalCallToRoomPatched +
                " animalRendererOnFellPrefix=" + AnimalRendererOnFellPrefixPatched +
                " animalRendererOnFellPostfix=" + AnimalRendererOnFellPostfixPatched +
                " animalRendererPlayAnimation=" + AnimalRendererPlayAnimationPatched +
                " animalRendererFixedUpdate=" + AnimalRendererFixedUpdatePatched +
                " animalAIMakeDecisionFreeTime=" + AnimalAIMakeDecisionFreeTimePatched +
                " animalControllerOnUpdate=" + AnimalControllerOnUpdatePatched +
                " registeredKeys=" + service.RegisteredKeyCount +
                " aiTemplates=" + service.RegisteredAiTemplateSummary +
                " pngSpriteOverrides=" + service.RegisteredPngSpriteOverrideSummary +
                " diagnosticSpecies=" + service.RegisteredDiagnosticSpeciesSummary +
                " customSpecies=" + service.RegisteredCustomAnimalSpeciesSummary + ".");
        }
    }
}
