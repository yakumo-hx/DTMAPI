#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void CustomAnimalAnimatorMetadataParsesAssetBundleRegistrations()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService")
                ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService type should exist.");
            MethodInfo buildSummaries = serviceType.GetMethod("BuildRegistrationSummariesForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal animator metadata test helper should exist.");

            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", "ShellCrabPack");
            string bundlePath = Path.Combine(root, "Content", "DTMAPI", "assets", "shell-crab", "shell_crab_animators.bundle");
            string json =
                "[" +
                "{ \"speciesId\": \"shell_crab\", \"templateSpeciesId\": \"goat\", \"animatorMode\": \"assetBundle\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_animal_shell_crab\", \"childAnimatorKey\": \"dtmapi_anim_animal_shell_crab_child\", " +
                "\"animatorBundle\": \"Content/DTMAPI/assets/shell-crab/shell_crab_animators.bundle\", " +
                "\"adultAnimatorAsset\": \"shell_crab_adult\", \"childAnimatorAsset\": \"shell_crab_young\" }," +
                "{ \"speciesId\": \"ignored\", \"templateSpeciesId\": \"goat\", \"animatorMode\": \"runtimeOverrideController\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_ignored\", \"animatorBundle\": \"ignored.bundle\", \"adultAnimatorAsset\": \"ignored\" }" +
                "]";

            string[] summaries = ((IEnumerable<string>)(buildSummaries.Invoke(null, new object[] { "Local.DTMAPI_ShellCrab", root, json })
                ?? throw new InvalidOperationException("BuildRegistrationSummariesForTest returned null."))).ToArray();

            Assert(summaries.Length == 2, "AssetBundle metadata should register exactly adult and child animator keys.");
            Assert(summaries.Any(s => s == "dtmapi_anim_animal_shell_crab|adult|" + bundlePath + "|shell_crab_adult|game_anim_animal_goat"), "Adult shell crab animator registration should point at the bundle controller and goat template fallback.");
            Assert(summaries.Any(s => s == "dtmapi_anim_animal_shell_crab_child|child|" + bundlePath + "|shell_crab_young|game_anim_animal_goat_child"), "Child shell crab animator registration should point at the bundle controller and goat child template fallback.");
            Assert(!summaries.Any(s => s.Contains("dtmapi_anim_ignored", StringComparison.OrdinalIgnoreCase)), "Non-assetBundle custom animal entries should not register animator bridge keys.");
        }

        private static void CustomAnimalAnimatorMetadataParsesPngSpriteOverrideRegistrations()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService")
                ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService type should exist.");
            MethodInfo buildSummaries = serviceType.GetMethod("BuildRegistrationSummariesForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal animator metadata test helper should exist.");
            MethodInfo mapSprite = serviceType.GetMethod("MapPngSpriteNameForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal PNG sprite mapping test helper should exist.");
            MethodInfo tryResolvePng = serviceType.GetMethod("TryResolvePngSpriteOverride", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("TryResolvePngSpriteOverride should exist.");

            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", "HatchPack");
            string json =
                "[" +
                "{ \"speciesId\": \"hatch\", \"templateSpeciesId\": \"chicken\", \"aiTemplate\": \"chicken\", \"animatorMode\": \"pngSpriteOverride\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_animal_hatch\", \"childAnimatorKey\": \"dtmapi_anim_animal_hatch_child\", " +
                "\"frameManifest\": \"Content/Sprites/hatch_frame_manifest.json\", " +
                "\"templateSpritePrefix\": \"anim_animal_chicken\", \"customSpritePrefix\": \"anim_animal_hatch\" }," +
                "{ \"speciesId\": \"ignored\", \"templateSpeciesId\": \"chicken\", \"animatorMode\": \"runtimeOverrideController\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_ignored\", \"templateSpritePrefix\": \"anim_animal_chicken\", \"customSpritePrefix\": \"anim_animal_ignored\" }" +
                "]";

            string[] summaries = ((IEnumerable<string>)(buildSummaries.Invoke(null, new object[] { "DTMAPI.HatchAssets", root, json })
                ?? throw new InvalidOperationException("BuildRegistrationSummariesForTest returned null."))).ToArray();

            Assert(summaries.Length == 2, "pngSpriteOverride metadata should register exactly adult and child Hatch animator keys.");
            Assert(summaries.Any(s => s == "dtmapi_anim_animal_hatch|adult|||game_anim_animal_chicken"), "Adult Hatch animator registration should return the chicken template controller.");
            Assert(summaries.Any(s => s == "dtmapi_anim_animal_hatch_child|child|||game_anim_animal_chicken_child"), "Child Hatch animator registration should return the chicken child template controller.");
            Assert(!summaries.Any(s => s.Contains("dtmapi_anim_ignored", StringComparison.OrdinalIgnoreCase)), "Unknown custom animal animator modes should not register animator bridge keys.");

            Assert((string)(mapSprite.Invoke(null, new object[] { "anim_animal_chicken", "anim_animal_hatch", "anim_animal_chicken_adult_idle_2" }) ?? string.Empty) == "anim_animal_hatch_adult_idle_2", "PNG sprite override should map chicken adult idle frames to Hatch adult idle frames.");
            Assert((string)(mapSprite.Invoke(null, new object[] { "anim_animal_chicken", "anim_animal_hatch", "anim_animal_chicken_adult_jump_ready_0" }) ?? string.Empty) == "anim_animal_hatch_adult_jump_0", "PNG sprite override should map chicken jump_ready to Hatch jump_0.");
            Assert((string)(mapSprite.Invoke(null, new object[] { "anim_animal_chicken", "anim_animal_hatch", "anim_animal_chicken_child_idle_0" }) ?? string.Empty) == "anim_animal_hatch_young_idle_0", "PNG sprite override should tolerate child stage sprite names by mapping them to Hatch young frames.");
            Assert((string)(mapSprite.Invoke(null, new object[] { "anim_animal_chicken", "anim_animal_hatch", "anim_animal_goat_adult_idle_0" }) ?? string.Empty) == string.Empty, "PNG sprite override should not map non-template sprite names.");

            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            object service = Activator.CreateInstance(
                serviceType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { runtime },
                culture: null) ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService should be constructable.");
            object unregisteredResult = tryResolvePng.Invoke(service, new object?[] { new object(), "anim_animal_chicken_adult_idle_0", null })
                ?? throw new InvalidOperationException("TryResolvePngSpriteOverride should return a result struct.");
            bool handled = (bool)(unregisteredResult.GetType().GetProperty("Handled")?.GetValue(unregisteredResult)
                ?? throw new InvalidOperationException("PNG sprite override result should expose Handled."));
            Assert(!handled, "Unregistered SpriteOverrideHandler instances must not replace sprites.");
        }

        private static void CustomAnimalAiTemplateMetadataParsesTemplateMappings()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService")
                ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService type should exist.");
            MethodInfo buildSummaries = serviceType.GetMethod("BuildAiTemplateSummariesForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal AI template metadata test helper should exist.");

            string json =
                "[" +
                "{ \"speciesId\": \"shell_crab\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\" }," +
                "{ \"speciesId\": \"fallback_crab\", \"templateSpeciesId\": \"chicken\", \"animatorMode\": \"assetBundle\" }," +
                "{ \"speciesId\": \"goat\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\" }," +
                "{ \"speciesId\": \"\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\" }" +
                "]";

            string[] summaries = ((IEnumerable<string>)(buildSummaries.Invoke(null, new object[] { "Local.DTMAPI_ShellCrab", json })
                ?? throw new InvalidOperationException("BuildAiTemplateSummariesForTest returned null."))).ToArray();

            Assert(summaries.Length == 2, "AI template metadata should register only custom species mapped to a different native template.");
            Assert(summaries.Any(s => s == "shell_crab|goat"), "Shell crab should route AnimalAI default-state lookup through the goat template.");
            Assert(summaries.Any(s => s == "fallback_crab|chicken"), "AI template metadata should fall back to templateSpeciesId when aiTemplate is omitted.");
            Assert(!summaries.Any(s => s.StartsWith("goat|", StringComparison.OrdinalIgnoreCase)), "Native species identity mappings should not install AI template bridge entries.");
        }

        private static void CustomAnimalSleepWakeDiagnosticsSpeciesAreScopedToRegisteredCustomAnimals()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService")
                ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService type should exist.");
            MethodInfo buildDiagnosticSpecies = serviceType.GetMethod("BuildDiagnosticSpeciesSummariesForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal sleep/wake diagnostic species helper should exist.");

            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", "CustomAnimalDiagnosticPack");
            string json =
                "[" +
                "{ \"speciesId\": \"shell_crab\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_animal_shell_crab\", \"childAnimatorKey\": \"dtmapi_anim_animal_shell_crab_child\", " +
                "\"animatorBundle\": \"Content/DTMAPI/assets/shell-crab/shell_crab_animators.bundle\", " +
                "\"adultAnimatorAsset\": \"shell_crab_adult\", \"childAnimatorAsset\": \"shell_crab_young\" }," +
                "{ \"speciesId\": \"hatch\", \"templateSpeciesId\": \"chicken\", \"aiTemplate\": \"chicken\", \"animatorMode\": \"pngSpriteOverride\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_animal_hatch\", \"childAnimatorKey\": \"dtmapi_anim_animal_hatch_child\", " +
                "\"templateSpritePrefix\": \"anim_animal_chicken\", \"customSpritePrefix\": \"anim_animal_hatch\" }," +
                "{ \"speciesId\": \"goat\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\" }" +
                "]";

            string[] species = ((IEnumerable<string>)(buildDiagnosticSpecies.Invoke(null, new object[] { "DTMAPI.CustomAnimalDiagnostics", root, json })
                ?? throw new InvalidOperationException("BuildDiagnosticSpeciesSummariesForTest returned null."))).ToArray();

            Assert(species.SequenceEqual(new[] { "hatch", "shell_crab" }, StringComparer.OrdinalIgnoreCase), "Sleep/wake diagnostics should be scoped to registered custom animals only.");
            Assert(!species.Contains("goat", StringComparer.OrdinalIgnoreCase), "Native identity species should not be included in custom animal sleep/wake diagnostics.");
        }

        private static void CustomAnimalSleepTaskBoundaryRulesAreScopedAndConservative()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService")
                ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService type should exist.");
            MethodInfo buildDiagnosticSpecies = serviceType.GetMethod("BuildDiagnosticSpeciesSummariesForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal diagnostic species helper should exist.");
            MethodInfo shouldApply = serviceType.GetMethod("ShouldApplySleepTaskBoundaryForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal sleep task boundary guard helper should exist.");
            MethodInfo isMovementTask = serviceType.GetMethod("IsMovementAnimalTaskNameForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal sleep task movement helper should exist.");

            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", "CustomAnimalSleepTaskBoundaryPack");
            string json =
                "[" +
                "{ \"speciesId\": \"shell_crab\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_animal_shell_crab\", \"childAnimatorKey\": \"dtmapi_anim_animal_shell_crab_child\", " +
                "\"animatorBundle\": \"Content/DTMAPI/assets/shell-crab/shell_crab_animators.bundle\", " +
                "\"adultAnimatorAsset\": \"shell_crab_adult\", \"childAnimatorAsset\": \"shell_crab_young\" }," +
                "{ \"speciesId\": \"hatch\", \"templateSpeciesId\": \"chicken\", \"aiTemplate\": \"chicken\", \"animatorMode\": \"pngSpriteOverride\", " +
                "\"adultAnimatorKey\": \"dtmapi_anim_animal_hatch\", \"childAnimatorKey\": \"dtmapi_anim_animal_hatch_child\", " +
                "\"templateSpritePrefix\": \"anim_animal_chicken\", \"customSpritePrefix\": \"anim_animal_hatch\" }," +
                "{ \"speciesId\": \"chicken\", \"templateSpeciesId\": \"chicken\", \"aiTemplate\": \"chicken\", \"animatorMode\": \"pngSpriteOverride\" }" +
                "]";

            string[] species = ((IEnumerable<string>)(buildDiagnosticSpecies.Invoke(null, new object[] { "DTMAPI.CustomAnimalSleepBoundary", root, json })
                ?? throw new InvalidOperationException("BuildDiagnosticSpeciesSummariesForTest returned null."))).ToArray();
            Assert(species.SequenceEqual(new[] { "hatch", "shell_crab" }, StringComparer.OrdinalIgnoreCase), "Sleep task boundary species scope should match registered custom animals only.");
            Assert((bool)(shouldApply.Invoke(null, new object?[] { true, true, "Night" }) ?? false), "Sleep task boundary should apply to registered custom animals that are already asleep at night.");
            Assert(!(bool)(shouldApply.Invoke(null, new object?[] { true, false, "Night" }) ?? true), "Sleep task boundary must not block animals that are still walking to bed.");
            Assert(!(bool)(shouldApply.Invoke(null, new object?[] { true, true, "Daytime" }) ?? true), "Sleep task boundary must not block morning wake or daytime behavior.");
            Assert(!(bool)(shouldApply.Invoke(null, new object?[] { false, true, "Night" }) ?? true), "Sleep task boundary must not apply to native unregistered animals.");

            Assert((bool)(isMovementTask.Invoke(null, new object[] { "DolocTown.AnimalMove" }) ?? false), "Sleep task boundary should clean stale AnimalMove tasks.");
            Assert((bool)(isMovementTask.Invoke(null, new object[] { "DolocTown.AnimalJump" }) ?? false), "Sleep task boundary should clean stale AnimalJump tasks.");
            Assert((bool)(isMovementTask.Invoke(null, new object[] { "DolocTown.AnimalEnterRoom" }) ?? false), "Sleep task boundary should clean stale AnimalEnterRoom tasks.");
            Assert(!(bool)(isMovementTask.Invoke(null, new object[] { "RedSaw.AI.LinearTask.LinearTaskWaitInt" }) ?? true), "Sleep task boundary must not clear native wait tasks.");
            Assert(!(bool)(isMovementTask.Invoke(null, new object[] { "DolocTown.AnimalEat" }) ?? true), "Sleep task boundary must not clear non-movement animal tasks.");
        }

        private static void CustomAnimalSleepWakeFollowUpDiagnosticsTrimStableSleep()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService")
                ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService type should exist.");
            MethodInfo isSettled = serviceType.GetMethod("IsSettledSleepingRendererFollowUpForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal sleep follow-up settled helper should exist.");
            MethodInfo normalize = serviceType.GetMethod("NormalizeRendererStateForDiagnosticSignatureForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Custom animal renderer-state diagnostic normalization helper should exist.");

            const string sleepRendererEarly = "animController=shell_crab_adult,animState=sleep@0.0853134,moving=false,eating=false,jumping=false,jumpReady=false,faceRight=true";
            const string sleepRendererLater = "animController=shell_crab_adult,animState=sleep@1.066762,moving=false,eating=false,jumping=false,jumpReady=false,faceRight=true";
            string normalizedEarly = (string)(normalize.Invoke(null, new object[] { sleepRendererEarly })
                ?? throw new InvalidOperationException("Renderer-state diagnostic normalization returned null."));
            string normalizedLater = (string)(normalize.Invoke(null, new object[] { sleepRendererLater })
                ?? throw new InvalidOperationException("Renderer-state diagnostic normalization returned null."));

            Assert(string.Equals(normalizedEarly, normalizedLater, StringComparison.Ordinal), "Sleep follow-up signatures should ignore animator normalized time.");
            Assert((bool)(isSettled.Invoke(null, new object?[] { true, "Normal_SleepState", "RedSaw.AI.LinearTask.LinearTaskWaitInt", sleepRendererEarly }) ?? false), "Stable native sleep wait should end renderer follow-up diagnostics.");
            Assert((bool)(isSettled.Invoke(null, new object?[] { true, "无状态", "RedSaw.AI.LinearTask.LinearTaskWaitInt", sleepRendererEarly }) ?? false), "Initial no-state native sleep wait should end renderer follow-up diagnostics.");
            Assert(!(bool)(isSettled.Invoke(null, new object?[] { true, "Normal_SleepState", "DolocTown.AnimalMove", sleepRendererEarly }) ?? true), "Sleeping movement tasks must keep renderer follow-up diagnostics active.");
            Assert(!(bool)(isSettled.Invoke(null, new object?[] { true, "Goat_FreeTimeState", "RedSaw.AI.LinearTask.LinearTaskWaitInt", sleepRendererEarly }) ?? true), "FreeTime AI state should keep renderer follow-up diagnostics active.");
            Assert(!(bool)(isSettled.Invoke(null, new object?[] { true, "Normal_SleepState", "RedSaw.AI.LinearTask.LinearTaskWaitInt", "animState=idle@0" }) ?? true), "Non-sleep renderer animation should keep renderer follow-up diagnostics active.");
        }

        private static void AudioReplacementHookReviewRetainsVerifiedState()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var service = new AudioReplacementService(runtime);
                var bridge = new AudioReplacementHookBridge(runtime, service);

                Assert(
                    bridge.PublishPhysicalHookStateIfChanged(
                        internalPostSoundEventPatched: true,
                        paperBoxInteractPatched: true,
                        animalPlayAnimalSoundPrefixPatched: false,
                        animalPlayAnimalSoundPostfixPatched: false),
                    "The first audio physical Hook state should be published.");
                runtime.FlushRuntimeQueues("AudioHookInitialPhysicalState");

                runtime.SetHookStatus(
                    "Audio.SoundEventReplacement",
                    "verified",
                    "unit replacement observation",
                    "played=True suppressed=True event=PLAY_RESOURCE_PAPER_BOX");
                runtime.FlushRuntimeQueues("AudioHookBehaviorVerified");
                Assert(
                    runtime.Diagnostics.GetHookStatuses().Single(status => status.HookId == "Audio.SoundEventReplacement").Status == "verified",
                    "A successful replacement observation should advance the audio Hook status to verified.");

                Assert(
                    !bridge.PublishPhysicalHookStateIfChanged(
                        internalPostSoundEventPatched: true,
                        paperBoxInteractPatched: true,
                        animalPlayAnimalSoundPrefixPatched: false,
                        animalPlayAnimalSoundPostfixPatched: false),
                    "An unchanged at-least-once audio Hook review should not republish physical state.");
                runtime.FlushRuntimeQueues("AudioHookStableReview");

                Assert(
                    runtime.Diagnostics.GetHookStatuses().Single(status => status.HookId == "Audio.SoundEventReplacement").Status == "verified",
                    "An unchanged physical review must not downgrade behavioral verification.");
                LifecycleBoundaryContractSnapshot lifecycle = runtime.LifecycleBoundaryContractSnapshot;
                Assert(
                    lifecycle.HookInstallSignalCounts.TryGetValue("Audio.SoundEventReplacement", out int installSignals) && installSignals == 1,
                    "The lifecycle contract should observe exactly one physical audio install signal.");
                Assert(
                    !lifecycle.Diagnostics.Any(diagnostic =>
                        diagnostic.Message.Contains("Hook install signal repeated", StringComparison.Ordinal) &&
                        diagnostic.Details.Contains("Audio.SoundEventReplacement", StringComparison.Ordinal)),
                    "A stable audio Hook review must not create a duplicate-install diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void AudioReplacementHookRetryPublishesPhysicalTransition()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var service = new AudioReplacementService(runtime);
                var bridge = new AudioReplacementHookBridge(runtime, service);

                Assert(
                    bridge.PublishPhysicalHookStateIfChanged(
                        internalPostSoundEventPatched: false,
                        paperBoxInteractPatched: false,
                        animalPlayAnimalSoundPrefixPatched: false,
                        animalPlayAnimalSoundPostfixPatched: false),
                    "The initial unavailable audio physical state should be published.");
                runtime.FlushRuntimeQueues("AudioHookInitialPending");
                Assert(
                    runtime.Diagnostics.GetHookStatuses().Single(status => status.HookId == "Audio.SoundEventReplacement").Status == "pending",
                    "An unavailable Wwise target should remain pending.");

                Assert(
                    bridge.PublishPhysicalHookStateIfChanged(
                        internalPostSoundEventPatched: true,
                        paperBoxInteractPatched: false,
                        animalPlayAnimalSoundPrefixPatched: false,
                        animalPlayAnimalSoundPostfixPatched: false),
                    "A later unavailable-to-installed transition should publish a new physical state.");
                runtime.FlushRuntimeQueues("AudioHookRetryInstalled");
                Assert(
                    runtime.Diagnostics.GetHookStatuses().Single(status => status.HookId == "Audio.SoundEventReplacement").Status == "experimental",
                    "A successful later Wwise target retry should publish experimental physical readiness.");
                Assert(
                    runtime.LifecycleBoundaryContractSnapshot.HookInstallSignalCounts.TryGetValue("Audio.SoundEventReplacement", out int installSignals) && installSignals == 1,
                    "The pending-to-installed transition should produce exactly one physical install signal.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void AudioReplacementContentPackMetadataParsesAnimalVoice()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.AudioReplacementService")
                ?? throw new InvalidOperationException("AudioReplacementService type should exist.");
            MethodInfo buildSummaries = serviceType.GetMethod("BuildContentPackReplacementSummariesForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Audio replacement metadata summaries helper should exist.");
            MethodInfo buildWarnings = serviceType.GetMethod("BuildContentPackReplacementWarningsForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Audio replacement metadata warnings helper should exist.");

            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", "HatchAudioPack");
            string audioDir = Path.Combine(root, "Content", "Audio");
            Directory.CreateDirectory(audioDir);
            string childWav = Path.Combine(audioDir, "hatch_pet_young.wav");
            string adultWav = Path.Combine(audioDir, "hatch_pet_adult.wav");
            File.WriteAllBytes(childWav, new byte[] { 0x52, 0x49, 0x46, 0x46 });
            File.WriteAllBytes(adultWav, new byte[] { 0x52, 0x49, 0x46, 0x46 });

            string validJson =
                "[" +
                "{ \"id\": \"hatch-pet-child\", \"category\": \"AnimalVoice\", \"speciesId\": \"hatch\", \"stage\": \"child\", \"nativeSoundEvent\": \"PLAY_ANIMAL_PET_CHICKEN_CHILD\", \"file\": \"Content/Audio/hatch_pet_young.wav\", \"suppressNativeWhenReady\": true, \"cooldownMilliseconds\": 80 }," +
                "{ \"id\": \"hatch-pet-adult\", \"category\": \"AnimalVoice\", \"speciesId\": \"hatch\", \"stage\": \"adult\", \"nativeSoundEvent\": \"PLAY_ANIMAL_PET_CHICKEN\", \"file\": \"Content/Audio/hatch_pet_adult.wav\", \"suppressNativeWhenReady\": true, \"cooldownMilliseconds\": 80 }" +
                "]";

            string[] summaries = ((IEnumerable<string>)(buildSummaries.Invoke(null, new object[] { "DTMAPI.HatchAssets", root, validJson })
                ?? throw new InvalidOperationException("BuildContentPackReplacementSummariesForTest returned null."))).ToArray();
            Assert(summaries.Length == 2, "Hatch audio metadata should register child and adult AnimalVoice replacements.");
            Assert(summaries.Any(s => s == "DTMAPI.HatchAssets|hatch-pet-child|AnimalVoice|hatch|child|PLAY_ANIMAL_PET_CHICKEN_CHILD|" + childWav + "|True|80"), "Hatch child voice should resolve to the package-local young WAV.");
            Assert(summaries.Any(s => s == "DTMAPI.HatchAssets|hatch-pet-adult|AnimalVoice|hatch|adult|PLAY_ANIMAL_PET_CHICKEN|" + adultWav + "|True|80"), "Hatch adult voice should resolve to the package-local adult WAV.");

            string invalidJson =
                "[" +
                "{ \"id\": \"bad-category\", \"category\": \"Music\", \"speciesId\": \"hatch\", \"stage\": \"adult\", \"nativeSoundEvent\": \"PLAY_BGM_FARM\", \"file\": \"Content/Audio/hatch_pet_adult.wav\" }," +
                "{ \"id\": \"bad-stage\", \"category\": \"AnimalVoice\", \"speciesId\": \"hatch\", \"stage\": \"teen\", \"nativeSoundEvent\": \"PLAY_ANIMAL_PET_CHICKEN\", \"file\": \"Content/Audio/hatch_pet_adult.wav\" }," +
                "{ \"id\": \"bad-event\", \"category\": \"AnimalVoice\", \"speciesId\": \"hatch\", \"stage\": \"adult\", \"nativeSoundEvent\": \"PLAY_BGM_FARM\", \"file\": \"Content/Audio/hatch_pet_adult.wav\" }," +
                "{ \"id\": \"bad-path\", \"category\": \"AnimalVoice\", \"speciesId\": \"hatch\", \"stage\": \"adult\", \"nativeSoundEvent\": \"PLAY_ANIMAL_PET_CHICKEN\", \"file\": \"../escape.wav\" }" +
                "]";
            string[] invalidSummaries = ((IEnumerable<string>)(buildSummaries.Invoke(null, new object[] { "DTMAPI.HatchAssets", root, invalidJson })
                ?? throw new InvalidOperationException("BuildContentPackReplacementSummariesForTest returned null."))).ToArray();
            string[] warnings = ((IEnumerable<string>)(buildWarnings.Invoke(null, new object[] { "DTMAPI.HatchAssets", root, invalidJson })
                ?? throw new InvalidOperationException("BuildContentPackReplacementWarningsForTest returned null."))).ToArray();
            Assert(invalidSummaries.Length == 0, "Invalid audio replacement definitions should not register.");
            Assert(warnings.Any(w => w.Contains("unsupported category", StringComparison.OrdinalIgnoreCase)), "Unsupported categories should be rejected.");
            Assert(warnings.Any(w => w.Contains("invalid AnimalVoice stage", StringComparison.OrdinalIgnoreCase)), "Invalid AnimalVoice stages should be rejected.");
            Assert(warnings.Any(w => w.Contains("unreviewed AnimalVoice event", StringComparison.OrdinalIgnoreCase)), "BGM/Music events should be rejected from AnimalVoice.");
            Assert(warnings.Any(w => w.Contains("escapes content pack root", StringComparison.OrdinalIgnoreCase)), "Audio file paths must not escape the content pack root.");
        }

        private static void AudioReplacementAnimalVoiceScopeDoesNotPolluteNativeChicken()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.AudioReplacementService")
                ?? throw new InvalidOperationException("AudioReplacementService type should exist.");
            MethodInfo matches = serviceType.GetMethod("MatchesAnimalVoiceScopeForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AnimalVoice scope helper should exist.");

            Assert((bool)(matches.Invoke(null, new object[] { "hatch", "child", "PLAY_ANIMAL_PET_CHICKEN_CHILD", "hatch", "child", "PLAY_ANIMAL_PET_CHICKEN_CHILD", "PLAY_ANIMAL_PET_CHICKEN_CHILD" }) ?? false), "Hatch child voice should match Hatch child context and child chicken event.");
            Assert((bool)(matches.Invoke(null, new object[] { "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "PLAY_ANIMAL_PET_CHICKEN" }) ?? false), "Hatch adult voice should match Hatch adult context and adult chicken event.");
            Assert(!(bool)(matches.Invoke(null, new object[] { "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "chicken", "adult", "PLAY_ANIMAL_PET_CHICKEN", "PLAY_ANIMAL_PET_CHICKEN" }) ?? true), "Native chicken context must not match Hatch replacement.");
            Assert(!(bool)(matches.Invoke(null, new object[] { "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "", "adult", "PLAY_ANIMAL_PET_CHICKEN", "PLAY_ANIMAL_PET_CHICKEN" }) ?? true), "AnimalVoice replacement must not match without an animal context.");
            Assert(!(bool)(matches.Invoke(null, new object[] { "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "hatch", "child", "PLAY_ANIMAL_PET_CHICKEN", "PLAY_ANIMAL_PET_CHICKEN" }) ?? true), "Mismatched Hatch stage must not match.");
            Assert(!(bool)(matches.Invoke(null, new object[] { "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN_CHILD", "PLAY_ANIMAL_PET_CHICKEN" }) ?? true), "Mismatched context expected event must not match.");
            Assert(!(bool)(matches.Invoke(null, new object[] { "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "hatch", "adult", "PLAY_ANIMAL_PET_CHICKEN", "PLAY_ANIMAL_PET_CHICKEN_CHILD" }) ?? true), "Mismatched posted event must not match.");
        }

        private static void AudioReplacementCooldownKeepsNativeSuppressedWhenReady()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.AudioReplacementService")
                ?? throw new InvalidOperationException("AudioReplacementService type should exist.");
            MethodInfo suppressOnCooldown = serviceType.GetMethod("ShouldSuppressNativeDuringReadyCooldownForTest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Audio replacement cooldown helper should exist.");

            DateTimeOffset now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
            DateTimeOffset recentPlay = now.AddMilliseconds(-20);
            DateTimeOffset oldPlay = now.AddMilliseconds(-200);

            Assert((bool)(suppressOnCooldown.Invoke(null, new object[] { true, true, 80, recentPlay, now }) ?? false), "Ready suppressing replacements should still suppress native sound during cooldown.");
            Assert(!(bool)(suppressOnCooldown.Invoke(null, new object[] { false, true, 80, recentPlay, now }) ?? true), "Pending replacements should not suppress native sound during cooldown.");
            Assert(!(bool)(suppressOnCooldown.Invoke(null, new object[] { true, false, 80, recentPlay, now }) ?? true), "Non-suppressing replacements should not suppress native sound during cooldown.");
            Assert(!(bool)(suppressOnCooldown.Invoke(null, new object[] { true, true, 80, oldPlay, now }) ?? true), "Expired cooldown should not use the cooldown suppression path.");
            Assert(!(bool)(suppressOnCooldown.Invoke(null, new object[] { true, true, 0, recentPlay, now }) ?? true), "Disabled cooldown should not use the cooldown suppression path.");
        }

        private static void AudioReplacementCodeApiStillRejectsUnreviewedEvents()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var service = new AudioReplacementService(runtime);
                IManifest owner = new ManifestModel
                {
                    Name = "Audio Replacement Unit Test",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AudioReplacement",
                    Type = "CodeMod"
                };

                AudioReplacementRegisterResult rejected = ((IAudioReplacementApi)service).RegisterReplacement(owner, new AudioReplacementOptions
                {
                    ReplacementId = "bad-animal-event",
                    NativeSoundEvent = "PLAY_ANIMAL_PET_CHICKEN",
                    AudioPath = Path.Combine(Path.GetTempPath(), "missing.wav"),
                    SuppressNativeWhenReady = true
                });
                Assert(!rejected.Success && rejected.FailureReason.Contains("not reviewed", StringComparison.OrdinalIgnoreCase), "Public code API should not globally allow animal voice events without content-pack scope.");

                AudioReplacementRegisterResult acceptedEvent = ((IAudioReplacementApi)service).RegisterReplacement(owner, new AudioReplacementOptions
                {
                    ReplacementId = "paper-box",
                    NativeSoundEvent = "PLAY_RESOURCE_PAPER_BOX",
                    AudioPath = Path.Combine(Path.GetTempPath(), "missing.wav"),
                    SuppressNativeWhenReady = true
                });
                Assert(!acceptedEvent.FailureReason.Contains("not reviewed", StringComparison.OrdinalIgnoreCase), "Existing paper-box code API event should remain reviewed even if the test WAV is missing.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void AudioReplacementExplicitReloadRetainsLastGoodGeneration()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                runtime.Start();
                const string ownerId = "DTMAPI.Tests.AudioAtomic";
                string packRoot = Path.Combine(gameDir, "Mods", ownerId);
                string contentRoot = Path.Combine(packRoot, "Content", "DTMAPI");
                string audioRoot = Path.Combine(packRoot, "Content", "Audio");
                Directory.CreateDirectory(contentRoot);
                Directory.CreateDirectory(audioRoot);
                string schemaPath = Path.Combine(contentRoot, "audio-replacements.json");
                File.WriteAllText(Path.Combine(packRoot, "info.json"), "{ \"name\": \"" + ownerId + "\", \"author\": \"DTMAPI\", \"version\": \"1.0.0\" }", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(contentRoot, "manifest.json"), "{ \"Name\": \"" + ownerId + "\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"Type\": \"ContentPack\" }", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(contentRoot, "dtmapi-package.json"), "{ \"schemaVersion\": 1, \"owner\": \"DTMAPI\", \"uniqueId\": \"" + ownerId + "\", \"version\": \"1.0.0\", \"packageKind\": \"ContentPack\", \"authorSdkVersion\": \"0.1.0\", \"targetRuntimeVersion\": \"" + ManagedModClassifier.TrackedAuthorSdkTargetDtmApiVersion + "\" }", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(contentRoot, "README.txt"), "standard SDK audio package", new UTF8Encoding(false));
                bool mutateTreeDuringPrepare = false;
                var service = new AudioReplacementService(runtime, (_, __) =>
                {
                    if (mutateTreeDuringPrepare)
                    {
                        mutateTreeDuringPrepare = false;
                        File.AppendAllText(schemaPath, " ", new UTF8Encoding(false));
                    }
                    return new object();
                });
                WritePcm16MonoWav(Path.Combine(audioRoot, "a.wav"), sample: 1000);
                WritePcm16MonoWav(Path.Combine(audioRoot, "b.wav"), sample: -1000);

                File.WriteAllText(
                    schemaPath,
                    "[{ \"id\": \"generation-a\", \"category\": \"SimpleSfx\", \"nativeSoundEvent\": \"PLAY_RESOURCE_PAPER_BOX\", \"file\": \"Content/Audio/a.wav\" }]");
                string generationATreeSha256 = AuthorFileTreeDigest.Compute(packRoot);
                AudioReplacementService.AudioReplacementOwnerReloadResult generationA = service.ReloadContentPackOwner(ownerId, packRoot, "unit-valid-a", generationATreeSha256);
                Assert(generationA.Success && generationA.Status == "committed" && generationA.Generation == 1 && generationA.ActiveEntryCount == 1, "Valid Audio generation A should commit as generation 1.");

                File.WriteAllText(schemaPath, "[{ invalid-json ]");
                AudioReplacementService.AudioReplacementOwnerReloadResult invalid = service.ReloadContentPackOwner(ownerId, packRoot, "unit-invalid");
                Assert(!invalid.Success && invalid.Status == "invalid-json" && invalid.RetainedPreviousGeneration && invalid.Generation == 1 && invalid.ActiveEntryCount == 1, "Invalid Audio generation should retain complete last-good generation A.");

                File.WriteAllText(
                    schemaPath,
                    "[{ \"id\": \"generation-b\", \"category\": \"SimpleSfx\", \"nativeSoundEvent\": \"PLAY_RESOURCE_PAPER_BOX\", \"file\": \"Content/Audio/b.wav\" }]");
                AudioReplacementService.AudioReplacementOwnerReloadResult generationB = service.ReloadContentPackOwner(ownerId, packRoot, "unit-valid-b");
                Assert(generationB.Success && generationB.Status == "committed" && generationB.Generation == 2 && generationB.ActiveEntryCount == 1 && !generationB.RetainedPreviousGeneration, "Valid Audio generation B should atomically replace A as generation 2.");

                File.WriteAllText(
                    schemaPath,
                    "[{ \"id\": \"generation-c\", \"category\": \"SimpleSfx\", \"nativeSoundEvent\": \"PLAY_RESOURCE_PAPER_BOX\", \"file\": \"Content/Audio/a.wav\" }]",
                    new UTF8Encoding(false));
                string expectedTreeSha256 = AuthorFileTreeDigest.Compute(packRoot);
                mutateTreeDuringPrepare = true;
                AudioReplacementService.AudioReplacementOwnerReloadResult changedDuringPrepare = service.ReloadContentPackOwner(
                    ownerId,
                    packRoot,
                    "unit-tree-toctou",
                    expectedTreeSha256);
                Assert(
                    !changedDuringPrepare.Success && changedDuringPrepare.Status == "source-tree-hash-mismatch" && changedDuringPrepare.RetainedPreviousGeneration && changedDuringPrepare.Generation == 2 && changedDuringPrepare.ActiveEntryCount == 1,
                    "A source-tree change during Audio staging must dispose the candidate and retain complete last-good generation B.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void AuthorSessionReloadRejectsUnownedFormatsDeterministically()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                MethodInfo reload = typeof(DolocTownGameBridge).GetMethod("ReloadSelectedAuthorContent", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Author-session reload classifier should exist.");

                string codeRoot = Path.Combine(gameDir, "author-reload", "code");
                Directory.CreateDirectory(codeRoot);
                AuthorSessionOperationResult code = InvokeAuthorReloadClassifier(
                    bridge,
                    reload,
                    CreateAuthorReloadCandidate(codeRoot, "DTMAPI.Tests.AuthorCode", "CodeMod", "mod.dll"));
                Assert(code.Status == "restart-required" && code.Code == "codemod-restart-required", "CodeMod DLLs must remain process-lifetime and restart-only.");

                string customAnimalRoot = Path.Combine(gameDir, "author-reload", "custom-animal");
                string customAnimalDtmapiRoot = Path.Combine(customAnimalRoot, "Content", "DTMAPI");
                Directory.CreateDirectory(customAnimalDtmapiRoot);
                File.WriteAllText(Path.Combine(customAnimalDtmapiRoot, "custom-animals.json"), "[]", new UTF8Encoding(false));
                AuthorSessionOperationResult customAnimal = InvokeAuthorReloadClassifier(
                    bridge,
                    reload,
                    CreateAuthorReloadCandidate(customAnimalRoot, "DTMAPI.Tests.AuthorCustomAnimal", "ContentPack", string.Empty));
                Assert(customAnimal.Status == "restart-required" && customAnimal.Code == "custom-animals-restart-required", "Custom Animals must remain restart-only until native caches and live object ownership are reviewed.");

                string nativeRoot = Path.Combine(gameDir, "author-reload", "native-json");
                Directory.CreateDirectory(Path.Combine(nativeRoot, "Content"));
                File.WriteAllText(Path.Combine(nativeRoot, "info.json"), "{}", new UTF8Encoding(false));
                AuthorSessionOperationResult native = InvokeAuthorReloadClassifier(
                    bridge,
                    reload,
                    CreateAuthorReloadCandidate(nativeRoot, "DTMAPI.Tests.AuthorNative", "ContentPack", string.Empty));
                Assert(native.Status == "restart-required" && native.Code == "native-content-restart-required", "Native official JSON/table content must remain restart-only.");

                string unknownRoot = Path.Combine(gameDir, "author-reload", "unknown");
                Directory.CreateDirectory(unknownRoot);
                AuthorSessionOperationResult unknown = InvokeAuthorReloadClassifier(
                    bridge,
                    reload,
                    CreateAuthorReloadCandidate(unknownRoot, "DTMAPI.Tests.AuthorUnknown", "ContentPack", string.Empty));
                Assert(unknown.Status == "restart-required" && unknown.Code == "content-format-restart-required", "Unknown content formats must fail closed as restart-only.");

                const string sdkOwnerId = "DTMAPI.Tests.AuthorSdkAudio";
                string sdkRoot = Path.Combine(gameDir, "author-reload", "sdk-audio");
                string sdkDtmapiRoot = Path.Combine(sdkRoot, "Content", "DTMAPI");
                string sdkAudioRoot = Path.Combine(sdkRoot, "Content", "Audio");
                Directory.CreateDirectory(sdkDtmapiRoot);
                Directory.CreateDirectory(sdkAudioRoot);
                File.WriteAllText(Path.Combine(sdkRoot, "info.json"), "{ \"name\": \"" + sdkOwnerId + "\", \"author\": \"DTMAPI\", \"version\": \"1.0.0\" }", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(sdkDtmapiRoot, "manifest.json"), "{}", new UTF8Encoding(false));
                File.WriteAllText(
                    Path.Combine(sdkDtmapiRoot, "dtmapi-package.json"),
                    "{ \"schemaVersion\": 1, \"owner\": \"DTMAPI\", \"uniqueId\": \"" + sdkOwnerId + "\", \"version\": \"1.0.0\", \"packageKind\": \"ContentPack\", \"authorSdkVersion\": \"0.1.0\", \"targetRuntimeVersion\": \"" + ManagedModClassifier.TrackedAuthorSdkTargetDtmApiVersion + "\" }",
                    new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(sdkDtmapiRoot, "audio-replacements.json"), "[]", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(sdkDtmapiRoot, "README.txt"), "author guidance", new UTF8Encoding(false));
                WritePcm16MonoWav(Path.Combine(sdkAudioRoot, "sound.wav"), sample: 10);
                DiscoveredMod sdkCandidate = CreateAuthorReloadCandidate(sdkRoot, sdkOwnerId, "ContentPack", string.Empty);
                MethodInfo findUnowned = typeof(DolocTownGameBridge).GetMethod("FindUnownedReloadContent", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Audio-only package classifier should exist.");
                Assert(findUnowned.Invoke(null, new object[] { sdkCandidate, sdkDtmapiRoot }) == null, "A standard SDK Audio package may include projected info.json, manifest, package metadata, README and WAV without becoming native/unknown content.");

                string unknownSidecar = Path.Combine(sdkDtmapiRoot, "unknown-format.json");
                File.WriteAllText(unknownSidecar, "{}", new UTF8Encoding(false));
                AuthorSessionOperationResult mixedUnknown = (AuthorSessionOperationResult)(findUnowned.Invoke(null, new object[] { sdkCandidate, sdkDtmapiRoot })
                    ?? throw new InvalidOperationException("Mixed unknown content should be rejected."));
                Assert(mixedUnknown.Status == "restart-required" && mixedUnknown.Code == "content-format-restart-required", "Audio plus an unknown DTMAPI format must not be reported as a complete Audio reload.");
                File.Delete(unknownSidecar);

                string nativeTable = Path.Combine(sdkRoot, "Content", "NativeTables", "items.json");
                Directory.CreateDirectory(Path.GetDirectoryName(nativeTable) ?? throw new InvalidOperationException("Native table fixture path is unavailable."));
                File.WriteAllText(nativeTable, "{}", new UTF8Encoding(false));
                AuthorSessionOperationResult mixedNative = (AuthorSessionOperationResult)(findUnowned.Invoke(null, new object[] { sdkCandidate, sdkDtmapiRoot })
                    ?? throw new InvalidOperationException("Mixed native content should be rejected."));
                Assert(mixedNative.Status == "restart-required" && mixedNative.Code == "native-content-restart-required", "Audio plus native Content tables must remain restart-only.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void AuthorSessionReloadRejectsManifestAuthorityDrift()
        {
            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-author-manifest-drift", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string manifestPath = Path.Combine(root, "manifest.json");
            const string uniqueId = "DTMAPI.Tests.AuthorManifestStable";
            const string baseline = "{ \"Name\": \"Author manifest stable\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.AuthorManifestStable\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.5.5\", \"Dependencies\": [] }";
            File.WriteAllText(manifestPath, baseline, new UTF8Encoding(false));
            ManifestModel loadedManifest = new ManifestReader().Read(manifestPath);
            var active = new DiscoveredMod(loadedManifest, root, manifestPath, "Local", null, true, false, string.Empty, false, string.Empty);
            MethodInfo validate = typeof(DolocTownGameBridge).GetMethod("ValidateReloadManifest", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Author-session manifest authority validator should exist.");
            Assert(validate.Invoke(null, new object[] { active }) == null, "An unchanged active manifest should remain eligible for its reviewed content reload classifier.");

            var drifts = new[]
            {
                new KeyValuePair<string, string>("UniqueID", baseline.Replace(uniqueId, uniqueId + ".Changed", StringComparison.Ordinal)),
                new KeyValuePair<string, string>("Type", baseline.Replace("\"Type\": \"ContentPack\"", "\"Type\": \"CodeMod\"", StringComparison.Ordinal)),
                new KeyValuePair<string, string>("EntryDll", baseline.Replace("\"Type\": \"ContentPack\"", "\"Type\": \"ContentPack\", \"EntryDll\": \"changed.dll\"", StringComparison.Ordinal)),
                new KeyValuePair<string, string>("MinimumDTMApiVersion", baseline.Replace("\"MinimumDTMApiVersion\": \"0.5.5\"", "\"MinimumDTMApiVersion\": \"9.9.9\"", StringComparison.Ordinal)),
                new KeyValuePair<string, string>("Dependencies", baseline.Replace("\"Dependencies\": []", "\"Dependencies\": [{ \"UniqueID\": \"DTMAPI.Tests.NewRequired\", \"Required\": true }]", StringComparison.Ordinal))
            };
            foreach (KeyValuePair<string, string> drift in drifts)
            {
                File.WriteAllText(manifestPath, drift.Value, new UTF8Encoding(false));
                AuthorSessionOperationResult result = (AuthorSessionOperationResult)(validate.Invoke(null, new object[] { active })
                    ?? throw new InvalidOperationException("Manifest drift should produce a restart-required result for " + drift.Key + "."));
                Assert(
                    result.Status == "restart-required" && result.Code == "manifest-changed-restart-required",
                    "Manifest authority drift must block content reload for " + drift.Key + ".");
            }
        }

        private static DiscoveredMod CreateAuthorReloadCandidate(string root, string uniqueId, string type, string entryDll)
        {
            var manifest = new ManifestModel
            {
                Name = uniqueId,
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = type,
                EntryDll = entryDll
            };
            string packagedManifest = Path.Combine(root, "Content", "DTMAPI", "manifest.json");
            string manifestPath = File.Exists(packagedManifest) ? packagedManifest : Path.Combine(root, "manifest.json");
            return new DiscoveredMod(manifest, root, manifestPath, "LocalDevelopment", null, true, false, string.Empty, false, "unit-test");
        }

        private static AuthorSessionOperationResult InvokeAuthorReloadClassifier(
            DolocTownGameBridge bridge,
            MethodInfo reload,
            DiscoveredMod candidate)
        {
            return (AuthorSessionOperationResult)(reload.Invoke(bridge, new object[] { candidate, new string('0', 64) })
                ?? throw new InvalidOperationException("Author-session reload classifier returned null."));
        }

        private static void WritePcm16MonoWav(string path, short sample)
        {
            const int sampleRate = 8000;
            const short channels = 1;
            const short bitsPerSample = 16;
            short[] samples = Enumerable.Repeat(sample, 80).ToArray();
            int dataLength = samples.Length * sizeof(short);
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);
            foreach (short value in samples)
                writer.Write(value);
        }

        private static void CustomAnimalAnimatorBridgeUnknownKeyFallsThroughAndMissingBundleDegrades()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")
                    ?? throw new InvalidOperationException("Temporary persistent root should be set.");
                string packageRoot = Path.Combine(persistentRoot, "MODS", "DTMAPI_ShellCrab");
                string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
                Directory.CreateDirectory(contentRoot);
                File.WriteAllText(
                    Path.Combine(contentRoot, "manifest.json"),
                    "{ \"Name\": \"DTMAPI抛壳蟹\", \"Author\": \"DTMAPI\", \"Version\": \"0.1.0\", \"UniqueID\": \"Local.DTMAPI_ShellCrab\", \"Type\": \"ContentPack\" }",
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                File.WriteAllText(
                    Path.Combine(contentRoot, "custom-animals.json"),
                    "[{ \"speciesId\": \"shell_crab\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\", " +
                    "\"adultAnimatorKey\": \"dtmapi_anim_animal_shell_crab\", \"childAnimatorKey\": \"dtmapi_anim_animal_shell_crab_child\", " +
                    "\"animatorBundle\": \"Content/DTMAPI/assets/shell-crab/missing.bundle\", " +
                    "\"adultAnimatorAsset\": \"shell_crab_adult\", \"childAnimatorAsset\": \"shell_crab_young\" }]");
                WriteOfficialModInfos(persistentRoot, "Local.DTMAPI_ShellCrab", true);

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                runtime.Start();
                Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
                Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService")
                    ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService type should exist.");
                object service = Activator.CreateInstance(
                    serviceType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { runtime },
                    culture: null) ?? throw new InvalidOperationException("CustomAnimalAnimatorBridgeService should be constructable.");
                MethodInfo refresh = serviceType.GetMethod("RefreshDefinitions", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("RefreshDefinitions should exist.");
                MethodInfo tryResolve = serviceType.GetMethod("TryResolveRuntimeAnimatorController", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("TryResolveRuntimeAnimatorController should exist.");
                MethodInfo check = serviceType.GetMethod("CheckRuntimeAnimatorController", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("CheckRuntimeAnimatorController should exist.");
                MethodInfo tryResolveAiState = serviceType.GetMethod("TryResolveDefaultAnimalAIStateType", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("TryResolveDefaultAnimalAIStateType should exist.");
                PropertyInfo registeredKeyCount = serviceType.GetProperty("RegisteredKeyCount", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("RegisteredKeyCount should exist.");
                PropertyInfo registeredAiTemplateCount = serviceType.GetProperty("RegisteredAiTemplateCount", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("RegisteredAiTemplateCount should exist.");

                refresh.Invoke(service, new object[] { "unit-test", true });
                Assert((int)(registeredKeyCount.GetValue(service) ?? 0) == 2, "Enabled Shell Crab content pack should register adult and child animator keys.");
                Assert((int)(registeredAiTemplateCount.GetValue(service) ?? 0) == 1, "Enabled Shell Crab content pack should register the shell_crab to goat AI template mapping.");

                object unknownResult = tryResolve.Invoke(service, new object[] { "game_anim_animal_goat" })
                    ?? throw new InvalidOperationException("Unknown-key TryResolve should return a result struct.");
                bool unknownHandled = (bool)(unknownResult.GetType().GetProperty("Handled")?.GetValue(unknownResult)
                    ?? throw new InvalidOperationException("Prefix result should expose Handled."));
                Assert(!unknownHandled, "Unknown RuntimeAnimatorController keys must fall through to the vanilla AnimatorAsset path.");
                Assert(check.Invoke(service, new object[] { "game_anim_animal_goat" }) == null, "Unknown service check keys must stay unhandled for vanilla asset logic.");

                object? missingBundleCheck = check.Invoke(service, new object[] { "dtmapi_anim_animal_shell_crab" });
                Assert(missingBundleCheck is bool checkValue && !checkValue, "Registered shell crab key with no bundle/Unity controller type should report unavailable in unit tests.");
                IHookStatusInfo degradedStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "CustomAnimals.AnimatorBridge.dtmapi_anim_animal_shell_crab");
                Assert(degradedStatus.Status == "degraded", "Missing custom animal animator bundle should mark the shell crab animator key degraded.");
                Assert(degradedStatus.Details.Contains("shell_crab", StringComparison.OrdinalIgnoreCase), "Degraded status should identify the affected shell crab species.");

                Type originalState = typeof(string);
                object? unknownAiState = tryResolveAiState.Invoke(service, new object[] { "goat", originalState });
                Assert(ReferenceEquals(unknownAiState, originalState), "Unknown native species must keep the original AnimalAI default-state result.");
                object? shellCrabAiState = tryResolveAiState.Invoke(service, new object[] { "shell_crab", originalState });
                Assert(ReferenceEquals(shellCrabAiState, originalState), "Registered shell crab AI template should fall back to the original state if the game AnimalAI template type is unavailable in unit tests.");
                IHookStatusInfo aiTemplateStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "CustomAnimals.AiTemplateBridge.shell_crab");
                Assert(aiTemplateStatus.Status == "degraded", "Missing game AnimalAI template type in unit tests should mark shell crab AI template mapping degraded.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }
    }
}
