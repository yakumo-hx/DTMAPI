using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const string ActionSpeedOwnerId = "Yuuka.DTMAPI.ActionSpeed";
        private const string OneActionOwnerId = "Yuuka.DTMAPI.OneActionComplete";
        private const string OneActionAssemblyName = "Yuuka.DTMAPI.OneActionComplete";
        private const string OneActionHarmonyOwner = "dtmapi.mod.yuuka.dtmapi.oneactioncomplete";
        private const string FishBreedingOwnerId = "Yuuka.DTMAPI.FishBreedingAssistant";
        private const string FishBreedingAssemblyName = "Yuuka.DTMAPI.FishBreedingAssistant";
        private const string FishBreedingHarmonyOwner = "dtmapi.mod.yuuka.dtmapi.fishbreedingassistant";
        private const string AnimalHusbandryOwnerId = "Yuuka.DTMAPI.AnimalHusbandryProgress";
        private const string AnimalHusbandryAssemblyName = "Yuuka.DTMAPI.AnimalHusbandryProgress";
        private const string AnimalHusbandryHarmonyOwner = "dtmapi.mod.yuuka.dtmapi.animalhusbandryprogress";
        private const string MoreSavesOwnerId = "DTMAPI.MoreSavesMod";
        private const string MoreSavesAssemblyName = "DTMAPI.MoreSaves";
        private const string MoreSavesHarmonyOwner = "dtmapi.mod.dtmapi.moresavesmod";
        private const string ChestLocatorOwnerId = "DTMAPI.ChestLocatorEnhancerMod";
        private const string MoreEquipmentSlotsOwnerId = "DTMAPI.MoreEquipmentSlotsMod";
        private const string MoreEquipmentSlotsAssemblyName = "DTMAPI.MoreEquipmentSlots";
        private const string MoreEquipmentSlotsHarmonyOwner = "dtmapi.mod.dtmapi.moreequipmentslotsmod";
        private const string MineOwnerId = "DTMAPI.MineMod";
        private const string MineAssemblyName = "DTMAPI.Mine";
        private const string MineHarmonyOwner = "dtmapi.mod.dtmapi.minemod";
        private const string DebugConsoleOwnerId = "DTMAPI.DebugConsoleMod";
        private const string DebugConsoleAssemblyName = "DTMAPI.DebugConsole";
        private const string DebugConsoleHarmonyOwner = "dtmapi.mod.dtmapi.debugconsolemod";
        private static readonly Batch6HarmonyPatchTarget[] OneActionHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget("DolocTown.ToolCollider", "HandleTools", 1),
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateInteract", "OnExit", 0)
        };
        private static readonly Batch6HarmonyPatchTarget[] FishBreedingHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget("DolocTown.Item", "get_title", 0)
        };
        private static readonly Batch6HarmonyPatchTarget[] AnimalHusbandryHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget("DolocTown.UI.AnimalFullInfoData", ".ctor", 1),
            new Batch6HarmonyPatchTarget("DolocTown.UI.AnimalViewer", "Show", 1, expectedOwnerPatchCount: 2),
            new Batch6HarmonyPatchTarget("DolocTown.AnimalPanelUiState", "Unregister", 0)
        };
        private static readonly Batch6HarmonyPatchTarget[] MoreEquipmentSlotsHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget("DolocTown.GameData.AgentEquipmentManager", "ReloadParams", 0),
            new Batch6HarmonyPatchTarget("DolocTown.BodyController", "OnAttacked", 4),
            new Batch6HarmonyPatchTarget("DolocTown.UI.AccessoriesBar", "__Init", 0),
            new Batch6HarmonyPatchTarget("DolocTown.UI.AccessoriesBar", "OnStartShow", 0)
        };
        private static readonly Batch6HarmonyPatchTarget[] MineHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget(
                "DolocTown.EquipmentRenderer",
                "OnReuse",
                0),
            new Batch6HarmonyPatchTarget(
                "DolocTown.EquipmentBuilder",
                "CreateIndicator",
                0),
            new Batch6HarmonyPatchTarget(
                "DolocTown.EquipmentBuilder",
                "TurnIndicator",
                0)
        };
        private static readonly Batch6HarmonyPatchTarget[] DebugConsoleHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget(
                "DolocTown.AgentControllerState",
                "UseTool",
                1),
            new Batch6HarmonyPatchTarget(
                "DolocTown.AgentControllerState",
                "UseItem",
                1),
            new Batch6HarmonyPatchTarget(
                "DolocTown.AgentControllerState",
                "EnterUICheck",
                2)
        };
        internal G4FixtureStepResult DeactivateAdvancedProductOwnersForFixture(IReadOnlyCollection<string> requestedOwnerIds)
        {
            try
            {
                var requested = new HashSet<string>(requestedOwnerIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                string[] supported = { ActionSpeedOwnerId, OneActionOwnerId, FishBreedingOwnerId, AnimalHusbandryOwnerId, MoreSavesOwnerId, ChestLocatorOwnerId, MoreEquipmentSlotsOwnerId, StrongPlantingGunOwnerId, ZoomProductOwnerId, MineOwnerId, DebugConsoleOwnerId };
                string[] unknown = requested.Where(ownerId => !supported.Contains(ownerId, StringComparer.OrdinalIgnoreCase)).ToArray();
                if (requested.Count == 0 || unknown.Length > 0)
                    throw new InvalidOperationException("Advanced owner-deactivation fixture requires a non-empty supported owner set; unknown=" + string.Join("|", unknown) + ".");

                bool actionSpeedRequested = requested.Contains(ActionSpeedOwnerId);
                bool oneActionRequested = requested.Contains(OneActionOwnerId);
                bool fishBreedingRequested = requested.Contains(FishBreedingOwnerId);
                bool animalHusbandryRequested = requested.Contains(AnimalHusbandryOwnerId);
                bool moreSavesRequested = requested.Contains(MoreSavesOwnerId);
                bool chestLocatorRequested = requested.Contains(ChestLocatorOwnerId);
                bool moreEquipmentSlotsRequested = requested.Contains(MoreEquipmentSlotsOwnerId);
                bool strongPlantingGunRequested = requested.Contains(StrongPlantingGunOwnerId);
                bool zoomRequested = requested.Contains(ZoomProductOwnerId);
                bool mineRequested = requested.Contains(MineOwnerId);
                bool debugConsoleRequested = requested.Contains(DebugConsoleOwnerId);
                if (strongPlantingGunRequested &&
                    !string.IsNullOrWhiteSpace(
                        strongPlantingGunReentryFailure))
                {
                    return G4FixtureStepResult.Failed(
                        "StrongPlantingGun native-save re-entry failed before Loader deactivation: " +
                        strongPlantingGunReentryFailure);
                }
                if (strongPlantingGunRequested &&
                    strongPlantingGunNativeSavePrepared &&
                    !strongPlantingGunReentryVerified)
                {
                    return G4FixtureStepResult.Pending(
                        "Waiting for the requested third-save reload to publish SaveLoaded and verify StrongPlantingGun native JSON re-entry before Loader deactivation.");
                }
                Batch6ActionSpeedObservation? actionSpeedBefore = actionSpeedRequested ? ObserveActionSpeedProduct() : null;
                Batch6HarmonyOwnerInventory? oneActionBefore = oneActionRequested ? Batch6AdvancedHarmonyOwnerObserver.Observe(
                    OneActionAssemblyName,
                    OneActionHarmonyOwner,
                    OneActionHarmonyTargets) : null;
                bool oneActionCallbackBefore = oneActionRequested && ReadStaticCallbackRuntimePresent(
                    OneActionAssemblyName,
                    "Yuuka.DTMAPI.OneActionComplete.OneActionCallbacks");
                int oneActionLoadedBefore = oneActionRequested ? CountLoadedOwners(OneActionOwnerId) : 0;
                int oneActionRootsBefore = oneActionRequested ? runtime.CountCoreOwnerRoots(OneActionOwnerId) : 0;
                Batch6HarmonyOwnerInventory? fishBreedingBefore = fishBreedingRequested ? Batch6AdvancedHarmonyOwnerObserver.Observe(
                    FishBreedingAssemblyName,
                    FishBreedingHarmonyOwner,
                    FishBreedingHarmonyTargets) : null;
                bool fishBreedingCallbackBefore = fishBreedingRequested && ReadStaticCallbackRuntimePresent(
                    FishBreedingAssemblyName,
                    "Yuuka.DTMAPI.FishBreedingAssistant.FishBreedingCallbacks");
                int fishBreedingLoadedBefore = fishBreedingRequested ? CountLoadedOwners(FishBreedingOwnerId) : 0;
                int fishBreedingRootsBefore = fishBreedingRequested ? runtime.CountCoreOwnerRoots(FishBreedingOwnerId) : 0;
                Batch6HarmonyOwnerInventory? animalHusbandryBefore = animalHusbandryRequested ? Batch6AdvancedHarmonyOwnerObserver.Observe(
                    AnimalHusbandryAssemblyName,
                    AnimalHusbandryHarmonyOwner,
                    AnimalHusbandryHarmonyTargets) : null;
                bool animalHusbandryCallbackBefore = animalHusbandryRequested && ReadStaticCallbackRuntimePresent(
                    AnimalHusbandryAssemblyName,
                    "Yuuka.DTMAPI.AnimalHusbandryProgress.AnimalHusbandryCallbacks");
                int animalHusbandryLoadedBefore = animalHusbandryRequested ? CountLoadedOwners(AnimalHusbandryOwnerId) : 0;
                int animalHusbandryRootsBefore = animalHusbandryRequested ? runtime.CountCoreOwnerRoots(AnimalHusbandryOwnerId) : 0;
                Assembly? moreSavesAssembly = moreSavesRequested
                    ? AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, MoreSavesAssemblyName, StringComparison.Ordinal))
                    : null;
                int moreSavesHarmonyBefore = moreSavesRequested ? Batch6AdvancedHarmonyOwnerObserver.CountAllOwnerPatches(MoreSavesHarmonyOwner) : 0;
                int moreSavesLoadedBefore = moreSavesRequested ? CountLoadedOwners(MoreSavesOwnerId) : 0;
                int moreSavesRootsBefore = moreSavesRequested ? runtime.CountCoreOwnerRoots(MoreSavesOwnerId) : 0;
                int moreSavesNativeBefore = moreSavesRequested ? ReadMoreSavesArchiveCount() : 0;
                Batch6HarmonyOwnerInventory? chestLocatorBefore = chestLocatorRequested ? Batch6AdvancedHarmonyOwnerObserver.Observe(
                    ChestLocatorProductAssemblyName,
                    ChestLocatorProductHarmonyOwner,
                    ChestLocatorProductHarmonyTargets) : null;
                bool chestLocatorCallbackBefore = chestLocatorRequested && ReadStaticCallbackRuntimePresent(
                    ChestLocatorProductAssemblyName,
                    "DTMAPI.ChestLocatorEnhancer.ChestLocatorEnhancerCallbacks");
                int chestLocatorLoadedBefore = chestLocatorRequested ? CountLoadedOwners(ChestLocatorOwnerId) : 0;
                int chestLocatorRootsBefore = chestLocatorRequested ? runtime.CountCoreOwnerRoots(ChestLocatorOwnerId) : 0;
                Batch6HarmonyOwnerInventory? zoomBefore =
                    zoomRequested
                        ? ObserveZoomProductOwnerForFixture()
                        : null;
                bool zoomCallbackBefore =
                    zoomRequested &&
                    ReadStaticCallbackRuntimePresent(
                        ZoomProductAssemblyName,
                        ZoomProductCallbackTypeName);
                object? zoomRuntimeBefore =
                    zoomRequested
                        ? ReadStaticCallbackRuntime(
                            ZoomProductAssemblyName,
                            ZoomProductCallbackTypeName)
                        : null;
                double zoomScaleBefore =
                    zoomRuntimeBefore == null
                        ? 0d
                        : Convert.ToDouble(
                            ReadMember(
                                zoomRuntimeBefore,
                                "CurrentViewScale"),
                            CultureInfo.InvariantCulture);
                string zoomMessageBefore =
                    zoomRuntimeBefore == null
                        ? string.Empty
                        : Convert.ToString(
                            ReadMember(
                                zoomRuntimeBefore,
                                "LastMessage"),
                            CultureInfo.InvariantCulture) ??
                          string.Empty;
                double zoomVanillaBefore =
                    zoomRuntimeBefore == null
                        ? 0d
                        : Convert.ToDouble(
                            ReadMember(
                                zoomRuntimeBefore,
                                "VanillaOrthographicSize"),
                            CultureInfo.InvariantCulture);
                double zoomNativeSizeBefore = 0d;
                double zoomNativeCamHeightBefore = 0d;
                if (zoomRequested)
                {
                    patcher ??=
                        new HarmonyReflectionPatcher(runtime);
                    Type dolocApi =
                        patcher.ResolveType(
                            "DolocAPI, Assembly-CSharp")
                        ?? throw new TypeLoadException(
                            "DolocAPI was unavailable for the Zoom title-restoration observation.");
                    object mainCamera =
                        ReadStaticMember(
                            dolocApi,
                            "mainCamera")
                        ?? throw new InvalidOperationException(
                            "DolocAPI.mainCamera was unavailable after ReturnedToTitle.");
                    zoomNativeSizeBefore =
                        ReadPositiveDouble(
                            mainCamera,
                            "orthographicSize");
                    object cameraController =
                        ReadStaticMember(
                            dolocApi,
                            "cameraController")
                        ?? throw new InvalidOperationException(
                            "DolocAPI.cameraController was unavailable after ReturnedToTitle.");
                    zoomNativeCamHeightBefore =
                        ReadNestedFiniteDouble(
                            cameraController,
                            "CamSize",
                            "y");
                }
                int zoomLoadedBefore =
                    zoomRequested
                        ? CountLoadedOwners(ZoomProductOwnerId)
                        : 0;
                int zoomRootsBefore =
                    zoomRequested
                        ? runtime.CountCoreOwnerRoots(
                            ZoomProductOwnerId)
                        : 0;
                Batch6HarmonyOwnerInventory? moreEquipmentSlotsBefore = moreEquipmentSlotsRequested
                    ? Batch6AdvancedHarmonyOwnerObserver.Observe(
                        MoreEquipmentSlotsAssemblyName,
                        MoreEquipmentSlotsHarmonyOwner,
                        MoreEquipmentSlotsHarmonyTargets)
                    : null;
                object? moreEquipmentSlotsRuntimeBefore = moreEquipmentSlotsRequested
                    ? ReadStaticCallbackRuntime(
                        MoreEquipmentSlotsAssemblyName,
                        "DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks")
                    : null;
                bool moreEquipmentSlotsCallbackBefore = moreEquipmentSlotsRuntimeBefore != null;
                int moreEquipmentSlotsLoadedBefore = moreEquipmentSlotsRequested ? CountLoadedOwners(MoreEquipmentSlotsOwnerId) : 0;
                int moreEquipmentSlotsRootsBefore = moreEquipmentSlotsRequested ? runtime.CountCoreOwnerRoots(MoreEquipmentSlotsOwnerId) : 0;
                Batch6HarmonyOwnerInventory? strongPlantingGunBefore =
                    strongPlantingGunRequested
                        ? ObserveStrongPlantingGunOwnerForFixture()
                        : null;
                bool strongPlantingGunCallbackBefore =
                    strongPlantingGunRequested &&
                    ReadStaticCallbackRuntimePresent(
                        StrongPlantingGunAssemblyName,
                        StrongPlantingGunCallbackTypeName);
                string strongPlantingGunLifecycleBefore =
                    strongPlantingGunRequested
                        ? ReadStaticCallbackLifecycleSummary(
                            StrongPlantingGunAssemblyName,
                            StrongPlantingGunCallbackTypeName)
                        : string.Empty;
                int strongPlantingGunLoadedBefore =
                    strongPlantingGunRequested
                        ? CountLoadedOwners(
                            StrongPlantingGunOwnerId)
                        : 0;
                int strongPlantingGunRootsBefore =
                    strongPlantingGunRequested
                        ? runtime.CountCoreOwnerRoots(
                            StrongPlantingGunOwnerId)
                        : 0;
                Batch6HarmonyOwnerInventory? mineBefore =
                    mineRequested
                        ? Batch6AdvancedHarmonyOwnerObserver.Observe(
                            MineAssemblyName,
                            MineHarmonyOwner,
                            MineHarmonyTargets)
                        : null;
                bool mineCallbackBefore =
                    mineRequested &&
                    ReadStaticCallbackRuntimePresent(
                        MineAssemblyName,
                        "DTMAPI.Mine.MineCallbacks");
                int mineLoadedBefore =
                    mineRequested
                        ? CountLoadedOwners(MineOwnerId)
                        : 0;
                int mineRootsBefore =
                    mineRequested
                        ? runtime.CountCoreOwnerRoots(MineOwnerId)
                        : 0;
                Batch6HarmonyOwnerInventory? debugConsoleBefore =
                    debugConsoleRequested
                        ? Batch6AdvancedHarmonyOwnerObserver.Observe(
                            DebugConsoleAssemblyName,
                            DebugConsoleHarmonyOwner,
                            DebugConsoleHarmonyTargets)
                        : null;
                int debugConsoleHarmonyBefore =
                    debugConsoleRequested
                        ? Batch6AdvancedHarmonyOwnerObserver.CountAllOwnerPatches(
                            DebugConsoleHarmonyOwner)
                        : 0;
                int debugConsoleLoadedBefore =
                    debugConsoleRequested
                        ? CountLoadedOwners(DebugConsoleOwnerId)
                        : 0;
                int debugConsoleRootsBefore =
                    debugConsoleRequested
                        ? runtime.CountCoreOwnerRoots(DebugConsoleOwnerId)
                        : 0;

                if (actionSpeedRequested && (!actionSpeedBefore!.ProductPresent || !actionSpeedBefore.ActualHarmonyOwnerReady ||
                    actionSpeedBefore.InstalledPatchCount != 9 || !actionSpeedBefore.ProductCallbackRuntimePresent))
                {
                    throw new InvalidOperationException(
                        "ActionSpeed did not reach the required pre-deactivation owner state. present=" + actionSpeedBefore.ProductPresent +
                        "; internal=" + actionSpeedBefore.InstalledPatchCount +
                        "; actual=" + actionSpeedBefore.CanonicalHarmonyPatchCount +
                        "; targets=" + actionSpeedBefore.CanonicalHarmonyTargetCount + "/" + actionSpeedBefore.ResolvedHarmonyTargetCount +
                        "; callback=" + actionSpeedBefore.ProductCallbackRuntimePresent + ".");
                }
                if (oneActionRequested && (!oneActionBefore!.IsComplete || !oneActionCallbackBefore ||
                    !runtime.HasOwnerInstance(OneActionOwnerId) || oneActionLoadedBefore != 1 || oneActionRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "OneActionComplete did not reach the required pre-deactivation owner state. instance=" + runtime.HasOwnerInstance(OneActionOwnerId) +
                        "; loaded=" + oneActionLoadedBefore +
                        "; roots=" + oneActionRootsBefore +
                        "; patches=" + oneActionBefore.ExactOwnerPatchCount +
                        "; targets=" + oneActionBefore.ExactOwnerTargetCount + "/" + oneActionBefore.ResolvedTargetCount +
                        "; callback=" + oneActionCallbackBefore +
                        "; inventory=" + oneActionBefore.Details + ".");
                }
                if (fishBreedingRequested && (!fishBreedingBefore!.IsComplete || fishBreedingBefore.ExactOwnerPatchCount != 1 ||
                    !fishBreedingCallbackBefore || !runtime.HasOwnerInstance(FishBreedingOwnerId) ||
                    fishBreedingLoadedBefore != 1 || fishBreedingRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "FishBreedingAssistant did not reach the required pre-deactivation owner state. instance=" + runtime.HasOwnerInstance(FishBreedingOwnerId) +
                        "; loaded=" + fishBreedingLoadedBefore +
                        "; roots=" + fishBreedingRootsBefore +
                        "; patches=" + fishBreedingBefore.ExactOwnerPatchCount +
                        "; targets=" + fishBreedingBefore.ExactOwnerTargetCount + "/" + fishBreedingBefore.ResolvedTargetCount +
                        "; callback=" + fishBreedingCallbackBefore +
                        "; inventory=" + fishBreedingBefore.Details + ".");
                }
                if (animalHusbandryRequested && (!animalHusbandryBefore!.IsComplete || animalHusbandryBefore.ExactOwnerPatchCount != 4 ||
                    !animalHusbandryCallbackBefore || !runtime.HasOwnerInstance(AnimalHusbandryOwnerId) ||
                    animalHusbandryLoadedBefore != 1 || animalHusbandryRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "AnimalHusbandryProgress did not reach the required pre-deactivation owner state. instance=" + runtime.HasOwnerInstance(AnimalHusbandryOwnerId) +
                        "; loaded=" + animalHusbandryLoadedBefore +
                        "; roots=" + animalHusbandryRootsBefore +
                        "; patches=" + animalHusbandryBefore.ExactOwnerPatchCount +
                        "; targets=" + animalHusbandryBefore.ExactOwnerTargetCount + "/" + animalHusbandryBefore.ResolvedTargetCount +
                        "; callback=" + animalHusbandryCallbackBefore +
                        "; inventory=" + animalHusbandryBefore.Details + ".");
                }
                if (moreSavesRequested && (moreSavesAssembly == null ||
                    moreSavesAssembly.GetReferencedAssemblies().Any(reference => string.Equals(reference.Name, "0Harmony", StringComparison.Ordinal)) ||
                    moreSavesHarmonyBefore != 0 || moreSavesNativeBefore != 12 ||
                    !runtime.HasOwnerInstance(MoreSavesOwnerId) || moreSavesLoadedBefore != 1 || moreSavesRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "MoreSaves did not reach its zero-Harmony ProductNative pre-deactivation state. instance=" + runtime.HasOwnerInstance(MoreSavesOwnerId) +
                        "; loaded=" + moreSavesLoadedBefore +
                        "; roots=" + moreSavesRootsBefore +
                        "; patches=" + moreSavesHarmonyBefore +
                        "; native=" + moreSavesNativeBefore +
                        "; harmonyReference=" + (moreSavesAssembly?.GetReferencedAssemblies().Any(reference => string.Equals(reference.Name, "0Harmony", StringComparison.Ordinal)) == true) + ".");
                }
                if (chestLocatorRequested && (!chestLocatorBefore!.IsComplete ||
                    chestLocatorBefore.ExactOwnerPatchCount != 1 ||
                    !chestLocatorCallbackBefore ||
                    !runtime.HasOwnerInstance(ChestLocatorOwnerId) ||
                    chestLocatorLoadedBefore != 1 ||
                    chestLocatorRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "ChestLocatorEnhancer did not reach the required ProductNative pre-deactivation owner state. instance=" + runtime.HasOwnerInstance(ChestLocatorOwnerId) +
                        "; loaded=" + chestLocatorLoadedBefore +
                        "; roots=" + chestLocatorRootsBefore +
                        "; patches=" + chestLocatorBefore.ExactOwnerPatchCount +
                        "; targets=" + chestLocatorBefore.ExactOwnerTargetCount + "/" + chestLocatorBefore.ResolvedTargetCount +
                        "; callback=" + chestLocatorCallbackBefore +
                        "; inventory=" + chestLocatorBefore.Details + ".");
                }
                if (zoomRequested &&
                    (!zoomBefore!.IsComplete ||
                     zoomBefore.ExactOwnerPatchCount != 3 ||
                     !zoomCallbackBefore ||
                     Math.Abs(zoomScaleBefore - 1d) > 0.001d ||
                     zoomVanillaBefore <= 0d ||
                     zoomNativeSizeBefore <= 0d ||
                     Math.Abs(
                         zoomNativeSizeBefore -
                         zoomVanillaBefore) > 0.001d ||
                     Math.Abs(
                         zoomNativeCamHeightBefore -
                         (zoomVanillaBefore * 2d)) >
                         0.001d ||
                     zoomMessageBefore.IndexOf(
                         "ReturnedToTitle",
                         StringComparison.OrdinalIgnoreCase) < 0 ||
                     !runtime.HasOwnerInstance(ZoomProductOwnerId) ||
                     zoomLoadedBefore != 1 ||
                     zoomRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "Zoom did not reach the required two-target, three-patch " +
                        "ProductNative pre-deactivation owner state. " +
                        "instance=" +
                        runtime.HasOwnerInstance(ZoomProductOwnerId) +
                        "; loaded=" +
                        zoomLoadedBefore +
                        "; roots=" +
                        zoomRootsBefore +
                        "; patches=" +
                        zoomBefore.ExactOwnerPatchCount +
                        "; targets=" +
                        zoomBefore.ExactOwnerTargetCount +
                        "/" +
                        zoomBefore.ResolvedTargetCount +
                        "; callback=" +
                        zoomCallbackBefore +
                        "; titleScale=" +
                        zoomScaleBefore.ToString(
                            "0.###",
                            CultureInfo.InvariantCulture) +
                        "; titleVanilla=" +
                        zoomVanillaBefore.ToString(
                            "0.###",
                            CultureInfo.InvariantCulture) +
                        "; titleNative=" +
                        zoomNativeSizeBefore.ToString(
                            "0.###",
                            CultureInfo.InvariantCulture) +
                        "; titleCamHeight=" +
                        zoomNativeCamHeightBefore.ToString(
                            "0.###",
                            CultureInfo.InvariantCulture) +
                        "; titleMessage=" +
                        zoomMessageBefore +
                        "; inventory=" +
                        zoomBefore.Details +
                        ".");
                }
                if (moreEquipmentSlotsRequested && (!moreEquipmentSlotsBefore!.IsComplete ||
                    moreEquipmentSlotsBefore.ExactOwnerPatchCount != 4 ||
                    !moreEquipmentSlotsCallbackBefore ||
                    !runtime.HasOwnerInstance(MoreEquipmentSlotsOwnerId) ||
                    moreEquipmentSlotsLoadedBefore != 1 ||
                    moreEquipmentSlotsRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots did not reach the required four-target ProductNative pre-deactivation owner state. instance=" + runtime.HasOwnerInstance(MoreEquipmentSlotsOwnerId) +
                        "; loaded=" + moreEquipmentSlotsLoadedBefore +
                        "; roots=" + moreEquipmentSlotsRootsBefore +
                        "; patches=" + moreEquipmentSlotsBefore.ExactOwnerPatchCount +
                        "; targets=" + moreEquipmentSlotsBefore.ExactOwnerTargetCount + "/" + moreEquipmentSlotsBefore.ResolvedTargetCount +
                        "; callback=" + moreEquipmentSlotsCallbackBefore +
                        "; inventory=" + moreEquipmentSlotsBefore.Details + ".");
                }
                if (strongPlantingGunRequested &&
                    (!strongPlantingGunBefore!.IsComplete ||
                     strongPlantingGunBefore.ExactOwnerPatchCount != 5 ||
                     !strongPlantingGunCallbackBefore ||
                     !runtime.HasOwnerInstance(
                         StrongPlantingGunOwnerId) ||
                     strongPlantingGunLoadedBefore != 1 ||
                     strongPlantingGunRootsBefore <= 0 ||
                     !StrongPlantingGunLifecycleSummaryProvesActive(
                         strongPlantingGunLifecycleBefore) ||
                     !strongPlantingGunReentryVerified ||
                     !string.IsNullOrWhiteSpace(
                         strongPlantingGunReentryFailure)))
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun did not reach the required five-target ProductNative/native-save re-entry state before Loader deactivation. instance=" +
                        runtime.HasOwnerInstance(
                            StrongPlantingGunOwnerId) +
                        "; loaded=" +
                        strongPlantingGunLoadedBefore +
                        "; roots=" +
                        strongPlantingGunRootsBefore +
                        "; patches=" +
                        strongPlantingGunBefore.ExactOwnerPatchCount +
                        "; targets=" +
                        strongPlantingGunBefore.ExactOwnerTargetCount +
                        "/" +
                        strongPlantingGunBefore.ResolvedTargetCount +
                        "; callback=" +
                        strongPlantingGunCallbackBefore +
                        "; reentry=" +
                        strongPlantingGunReentryVerified +
                        "; reentryFailure=" +
                        strongPlantingGunReentryFailure +
                        "; lifecycle={" +
                        strongPlantingGunLifecycleBefore +
                        "}; inventory={" +
                        strongPlantingGunBefore.Details +
                        "}.");
                }
                if (mineRequested &&
                    (mineBefore!.ExactOwnerPatchCount != 0 ||
                     mineBefore.ExactOwnerTargetCount != 0 ||
                     mineCallbackBefore ||
                     !runtime.HasOwnerInstance(MineOwnerId) ||
                     mineLoadedBefore != 1 ||
                     mineRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "Mine did not reach its required post-title pre-Loader state. " +
                        "The in-save fixture must first prove three ProductNative hooks; " +
                        "ReturnedToTitle must then clear hooks/callbacks/session state while " +
                        "the restart-required Loader instance remains. instance=" +
                        runtime.HasOwnerInstance(MineOwnerId) +
                        "; loaded=" + mineLoadedBefore +
                        "; roots=" + mineRootsBefore +
                        "; patches=" +
                        mineBefore.ExactOwnerPatchCount +
                        "; targets=" +
                        mineBefore.ExactOwnerTargetCount +
                        "; callback=" + mineCallbackBefore +
                        "; inventory=" + mineBefore.Details + ".");
                }
                if (debugConsoleRequested &&
                    (debugConsoleBefore!.ExactOwnerPatchCount != 3 ||
                     debugConsoleBefore.ExactOwnerTargetCount != 3 ||
                     debugConsoleHarmonyBefore != 19 ||
                     !runtime.HasOwnerInstance(DebugConsoleOwnerId) ||
                     debugConsoleLoadedBefore != 1 ||
                     debugConsoleRootsBefore <= 0))
                {
                    throw new InvalidOperationException(
                        "DebugConsole did not reach its required post-title pre-Loader state. " +
                        "The in-save fixture must first prove the three ProductNative input hooks " +
                        "and real UI/input cycle; ReturnedToTitle must then clear modal, input-drain " +
                        "and transient leases while the inactive exact-owner patches and the " +
                        "restart-required Loader instance remain. instance=" +
                        runtime.HasOwnerInstance(DebugConsoleOwnerId) +
                        "; loaded=" + debugConsoleLoadedBefore +
                        "; roots=" + debugConsoleRootsBefore +
                        "; patches=" +
                        debugConsoleBefore.ExactOwnerPatchCount +
                        "; targets=" +
                        debugConsoleBefore.ExactOwnerTargetCount +
                        "; ownerPatches=" +
                        debugConsoleHarmonyBefore +
                        "; inventory=" + debugConsoleBefore.Details + ".");
                }

                var detailsParts = new List<string>();
                if (actionSpeedRequested)
                {
                    string actionSpeedCleanup = runtime.DeactivateOwner(
                        ActionSpeedOwnerId,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false,
                        transactionId: string.Empty);
                    Batch6ActionSpeedObservation actionSpeedAfter = ObserveActionSpeedProduct();
                    if (actionSpeedAfter.ProductPresent || actionSpeedAfter.CanonicalHarmonyPatchCount != 0 ||
                        actionSpeedAfter.CanonicalHarmonyTargetCount != 0 || actionSpeedAfter.ProductCallbackRuntimePresent ||
                        actionSpeedAfter.CoreOwnerRootCount != 0 || actionSpeedAfter.LoadedOwnerCount != 0 ||
                        !CleanupSummaryProvesZero(actionSpeedCleanup))
                    {
                        throw new InvalidOperationException(
                            "ActionSpeed Loader owner-deactivation cleanup was incomplete. present=" + actionSpeedAfter.ProductPresent +
                            "; roots=" + actionSpeedAfter.CoreOwnerRootCount +
                            "; loaded=" + actionSpeedAfter.LoadedOwnerCount +
                            "; patches=" + actionSpeedAfter.CanonicalHarmonyPatchCount +
                            "; targets=" + actionSpeedAfter.CanonicalHarmonyTargetCount +
                            "; callback=" + actionSpeedAfter.ProductCallbackRuntimePresent +
                            "; cleanup={" + actionSpeedCleanup + "}.");
                    }
                    detailsParts.Add("ActionSpeed=internal9+actual9+callback1->instance0+actual0+callback0+roots0");
                }

                if (oneActionRequested)
                {
                    string oneActionCleanup = runtime.DeactivateOwner(
                        OneActionOwnerId,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false,
                        transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory oneActionAfter = Batch6AdvancedHarmonyOwnerObserver.Observe(
                        OneActionAssemblyName,
                        OneActionHarmonyOwner,
                        OneActionHarmonyTargets);
                    bool oneActionCallbackAfter = ReadStaticCallbackRuntimePresent(
                        OneActionAssemblyName,
                        "Yuuka.DTMAPI.OneActionComplete.OneActionCallbacks");
                    int oneActionLoadedAfter = CountLoadedOwners(OneActionOwnerId);
                    int oneActionRootsAfter = runtime.CountCoreOwnerRoots(OneActionOwnerId);
                    if (runtime.HasOwnerInstance(OneActionOwnerId) || oneActionLoadedAfter != 0 || oneActionRootsAfter != 0 ||
                        oneActionAfter.ExactOwnerPatchCount != 0 || oneActionAfter.ExactOwnerTargetCount != 0 ||
                        oneActionCallbackAfter || !CleanupSummaryProvesZero(oneActionCleanup))
                    {
                        throw new InvalidOperationException(
                            "OneActionComplete Loader owner-deactivation cleanup was incomplete. instance=" + runtime.HasOwnerInstance(OneActionOwnerId) +
                            "; roots=" + oneActionRootsAfter +
                            "; loaded=" + oneActionLoadedAfter +
                            "; patches=" + oneActionAfter.ExactOwnerPatchCount +
                            "; targets=" + oneActionAfter.ExactOwnerTargetCount +
                            "; callback=" + oneActionCallbackAfter +
                            "; cleanup={" + oneActionCleanup + "}.");
                    }
                    detailsParts.Add("OneActionComplete=actual2+callback1->instance0+actual0+callback0+roots0");
                }

                if (fishBreedingRequested)
                {
                    string fishBreedingCleanup = runtime.DeactivateOwner(
                        FishBreedingOwnerId,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false,
                        transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory fishBreedingAfter = Batch6AdvancedHarmonyOwnerObserver.Observe(
                        FishBreedingAssemblyName,
                        FishBreedingHarmonyOwner,
                        FishBreedingHarmonyTargets);
                    bool fishBreedingCallbackAfter = ReadStaticCallbackRuntimePresent(
                        FishBreedingAssemblyName,
                        "Yuuka.DTMAPI.FishBreedingAssistant.FishBreedingCallbacks");
                    int fishBreedingLoadedAfter = CountLoadedOwners(FishBreedingOwnerId);
                    int fishBreedingRootsAfter = runtime.CountCoreOwnerRoots(FishBreedingOwnerId);
                    if (runtime.HasOwnerInstance(FishBreedingOwnerId) || fishBreedingLoadedAfter != 0 || fishBreedingRootsAfter != 0 ||
                        fishBreedingAfter.ExactOwnerPatchCount != 0 || fishBreedingAfter.ExactOwnerTargetCount != 0 ||
                        fishBreedingCallbackAfter || !CleanupSummaryProvesZero(fishBreedingCleanup))
                    {
                        throw new InvalidOperationException(
                            "FishBreedingAssistant Loader owner-deactivation cleanup was incomplete. instance=" + runtime.HasOwnerInstance(FishBreedingOwnerId) +
                            "; roots=" + fishBreedingRootsAfter +
                            "; loaded=" + fishBreedingLoadedAfter +
                            "; patches=" + fishBreedingAfter.ExactOwnerPatchCount +
                            "; targets=" + fishBreedingAfter.ExactOwnerTargetCount +
                            "; callback=" + fishBreedingCallbackAfter +
                            "; cleanup={" + fishBreedingCleanup + "}.");
                    }
                    detailsParts.Add("FishBreedingAssistant=actual1+callback1->instance0+actual0+callback0+roots0");
                }

                if (animalHusbandryRequested)
                {
                    string animalHusbandryCleanup = runtime.DeactivateOwner(
                        AnimalHusbandryOwnerId,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false,
                        transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory animalHusbandryAfter = Batch6AdvancedHarmonyOwnerObserver.Observe(
                        AnimalHusbandryAssemblyName,
                        AnimalHusbandryHarmonyOwner,
                        AnimalHusbandryHarmonyTargets);
                    bool animalHusbandryCallbackAfter = ReadStaticCallbackRuntimePresent(
                        AnimalHusbandryAssemblyName,
                        "Yuuka.DTMAPI.AnimalHusbandryProgress.AnimalHusbandryCallbacks");
                    int animalHusbandryLoadedAfter = CountLoadedOwners(AnimalHusbandryOwnerId);
                    int animalHusbandryRootsAfter = runtime.CountCoreOwnerRoots(AnimalHusbandryOwnerId);
                    if (runtime.HasOwnerInstance(AnimalHusbandryOwnerId) || animalHusbandryLoadedAfter != 0 || animalHusbandryRootsAfter != 0 ||
                        animalHusbandryAfter.ExactOwnerPatchCount != 0 || animalHusbandryAfter.ExactOwnerTargetCount != 0 ||
                        animalHusbandryCallbackAfter || !CleanupSummaryProvesZero(animalHusbandryCleanup))
                    {
                        throw new InvalidOperationException(
                            "AnimalHusbandryProgress Loader owner-deactivation cleanup was incomplete. instance=" + runtime.HasOwnerInstance(AnimalHusbandryOwnerId) +
                            "; roots=" + animalHusbandryRootsAfter +
                            "; loaded=" + animalHusbandryLoadedAfter +
                            "; patches=" + animalHusbandryAfter.ExactOwnerPatchCount +
                            "; targets=" + animalHusbandryAfter.ExactOwnerTargetCount +
                            "; callback=" + animalHusbandryCallbackAfter +
                            "; cleanup={" + animalHusbandryCleanup + "}.");
                    }
                    detailsParts.Add("AnimalHusbandryProgress=actual4+targets3+callback1->instance0+actual0+callback0+roots0");
                }

                if (moreSavesRequested)
                {
                    string moreSavesCleanup = runtime.DeactivateOwner(
                        MoreSavesOwnerId,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false,
                        transactionId: string.Empty);
                    int moreSavesHarmonyAfter = Batch6AdvancedHarmonyOwnerObserver.CountAllOwnerPatches(MoreSavesHarmonyOwner);
                    int moreSavesLoadedAfter = CountLoadedOwners(MoreSavesOwnerId);
                    int moreSavesRootsAfter = runtime.CountCoreOwnerRoots(MoreSavesOwnerId);
                    int moreSavesNativeAfter = ReadMoreSavesArchiveCount();
                    if (runtime.HasOwnerInstance(MoreSavesOwnerId) || moreSavesLoadedAfter != 0 || moreSavesRootsAfter != 0 ||
                        moreSavesHarmonyAfter != 0 || moreSavesNativeAfter != 6 || !CleanupSummaryProvesZero(moreSavesCleanup))
                    {
                        throw new InvalidOperationException(
                            "MoreSaves Loader owner-deactivation cleanup was incomplete. instance=" + runtime.HasOwnerInstance(MoreSavesOwnerId) +
                            "; roots=" + moreSavesRootsAfter +
                            "; loaded=" + moreSavesLoadedAfter +
                            "; patches=" + moreSavesHarmonyAfter +
                            "; native=" + moreSavesNativeAfter +
                            "; cleanup={" + moreSavesCleanup + "}.");
                    }
                    detailsParts.Add("MoreSaves=native12+actual0->instance0+native6+actual0+roots0");
                }

                if (chestLocatorRequested)
                {
                    string chestLocatorCleanup = runtime.DeactivateOwner(
                        ChestLocatorOwnerId,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false,
                        transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory chestLocatorAfter = Batch6AdvancedHarmonyOwnerObserver.Observe(
                        ChestLocatorProductAssemblyName,
                        ChestLocatorProductHarmonyOwner,
                        ChestLocatorProductHarmonyTargets);
                    bool chestLocatorCallbackAfter = ReadStaticCallbackRuntimePresent(
                        ChestLocatorProductAssemblyName,
                        "DTMAPI.ChestLocatorEnhancer.ChestLocatorEnhancerCallbacks");
                    int chestLocatorLoadedAfter = CountLoadedOwners(ChestLocatorOwnerId);
                    int chestLocatorRootsAfter = runtime.CountCoreOwnerRoots(ChestLocatorOwnerId);
                    if (runtime.HasOwnerInstance(ChestLocatorOwnerId) ||
                        chestLocatorLoadedAfter != 0 ||
                        chestLocatorRootsAfter != 0 ||
                        chestLocatorAfter.ExactOwnerPatchCount != 0 ||
                        chestLocatorAfter.ExactOwnerTargetCount != 0 ||
                        chestLocatorCallbackAfter ||
                        !CleanupSummaryProvesZero(chestLocatorCleanup))
                    {
                        throw new InvalidOperationException(
                            "ChestLocatorEnhancer Loader owner-deactivation cleanup was incomplete. instance=" + runtime.HasOwnerInstance(ChestLocatorOwnerId) +
                            "; roots=" + chestLocatorRootsAfter +
                            "; loaded=" + chestLocatorLoadedAfter +
                            "; patches=" + chestLocatorAfter.ExactOwnerPatchCount +
                            "; targets=" + chestLocatorAfter.ExactOwnerTargetCount +
                            "; callback=" + chestLocatorCallbackAfter +
                            "; cleanup={" + chestLocatorCleanup + "}.");
                    }
                    detailsParts.Add("ChestLocatorEnhancer=actual1+callback1->instance0+actual0+callback0+roots0");
                }

                if (zoomRequested)
                {
                    string zoomCleanup =
                        runtime.DeactivateOwner(
                            ZoomProductOwnerId,
                            ModOwnerCleanupReason.Unload,
                            shutdown: false,
                            transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory zoomAfter =
                        ObserveZoomProductOwnerForFixture();
                    bool zoomCallbackAfter =
                        ReadStaticCallbackRuntimePresent(
                            ZoomProductAssemblyName,
                            ZoomProductCallbackTypeName);
                    int zoomLoadedAfter =
                        CountLoadedOwners(ZoomProductOwnerId);
                    int zoomRootsAfter =
                        runtime.CountCoreOwnerRoots(
                            ZoomProductOwnerId);
                    if (runtime.HasOwnerInstance(ZoomProductOwnerId) ||
                        zoomLoadedAfter != 0 ||
                        zoomRootsAfter != 0 ||
                        zoomAfter.ExactOwnerPatchCount != 0 ||
                        zoomAfter.ExactOwnerTargetCount != 0 ||
                        zoomCallbackAfter ||
                        !CleanupSummaryProvesZero(zoomCleanup))
                    {
                        throw new InvalidOperationException(
                            "Zoom Loader owner-deactivation cleanup was " +
                            "incomplete. instance=" +
                            runtime.HasOwnerInstance(
                                ZoomProductOwnerId) +
                            "; roots=" +
                            zoomRootsAfter +
                            "; loaded=" +
                            zoomLoadedAfter +
                            "; patches=" +
                            zoomAfter.ExactOwnerPatchCount +
                            "; targets=" +
                            zoomAfter.ExactOwnerTargetCount +
                            "; callback=" +
                            zoomCallbackAfter +
                            "; cleanup={" +
                            zoomCleanup +
                            "}.");
                    }
                    detailsParts.Add(
                        "Zoom=title4to1+native1+derived1+actual1+callback1->instance0+actual0+" +
                        "callback0+roots0");
                }

                if (moreEquipmentSlotsRequested)
                {
                    string moreEquipmentSlotsCleanup = runtime.DeactivateOwner(
                        MoreEquipmentSlotsOwnerId,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false,
                        transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory moreEquipmentSlotsAfter = Batch6AdvancedHarmonyOwnerObserver.Observe(
                        MoreEquipmentSlotsAssemblyName,
                        MoreEquipmentSlotsHarmonyOwner,
                        MoreEquipmentSlotsHarmonyTargets);
                    bool moreEquipmentSlotsCallbackAfter =
                        ReadStaticCallbackRuntimePresent(
                            MoreEquipmentSlotsAssemblyName,
                            "DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks");
                    string moreEquipmentSlotsLifecycleAfter =
                        ReadStaticCallbackLifecycleSummary(
                            MoreEquipmentSlotsAssemblyName,
                            "DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks");
                    int moreEquipmentSlotsLoadedAfter = CountLoadedOwners(MoreEquipmentSlotsOwnerId);
                    int moreEquipmentSlotsRootsAfter = runtime.CountCoreOwnerRoots(MoreEquipmentSlotsOwnerId);
                    if (runtime.HasOwnerInstance(MoreEquipmentSlotsOwnerId) ||
                        moreEquipmentSlotsLoadedAfter != 0 ||
                        moreEquipmentSlotsRootsAfter != 0 ||
                        moreEquipmentSlotsAfter.ExactOwnerPatchCount != 0 ||
                        moreEquipmentSlotsAfter.ExactOwnerTargetCount != 0 ||
                        moreEquipmentSlotsCallbackAfter ||
                        !LifecycleSummaryProvesZero(moreEquipmentSlotsLifecycleAfter) ||
                        !CleanupSummaryProvesZero(moreEquipmentSlotsCleanup))
                    {
                        throw new InvalidOperationException(
                            "MoreEquipmentSlots Loader owner-deactivation cleanup was incomplete. instance=" + runtime.HasOwnerInstance(MoreEquipmentSlotsOwnerId) +
                            "; roots=" + moreEquipmentSlotsRootsAfter +
                            "; loaded=" + moreEquipmentSlotsLoadedAfter +
                            "; patches=" + moreEquipmentSlotsAfter.ExactOwnerPatchCount +
                            "; targets=" + moreEquipmentSlotsAfter.ExactOwnerTargetCount +
                            "; callback=" + moreEquipmentSlotsCallbackAfter +
                            "; lifecycle={" + moreEquipmentSlotsLifecycleAfter + "}" +
                            "; cleanup={" + moreEquipmentSlotsCleanup + "}.");
                    }
                    detailsParts.Add("MoreEquipmentSlots=actual4+targets4+callback1->instance0+actual0+callback0+clones0+listeners0+functions0+roots0");
                }

                if (strongPlantingGunRequested)
                {
                    string strongPlantingGunCleanup =
                        runtime.DeactivateOwner(
                            StrongPlantingGunOwnerId,
                            ModOwnerCleanupReason.Unload,
                            shutdown: false,
                            transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory
                        strongPlantingGunAfter =
                            ObserveStrongPlantingGunOwnerForFixture();
                    bool strongPlantingGunCallbackAfter =
                        ReadStaticCallbackRuntimePresent(
                            StrongPlantingGunAssemblyName,
                            StrongPlantingGunCallbackTypeName);
                    string strongPlantingGunLifecycleAfter =
                        ReadStaticCallbackLifecycleSummary(
                            StrongPlantingGunAssemblyName,
                            StrongPlantingGunCallbackTypeName);
                    int strongPlantingGunLoadedAfter =
                        CountLoadedOwners(
                            StrongPlantingGunOwnerId);
                    int strongPlantingGunRootsAfter =
                        runtime.CountCoreOwnerRoots(
                            StrongPlantingGunOwnerId);
                    if (runtime.HasOwnerInstance(
                            StrongPlantingGunOwnerId) ||
                        strongPlantingGunLoadedAfter != 0 ||
                        strongPlantingGunRootsAfter != 0 ||
                        strongPlantingGunAfter
                            .ExactOwnerPatchCount != 0 ||
                        strongPlantingGunAfter
                            .ExactOwnerTargetCount != 0 ||
                        strongPlantingGunCallbackAfter ||
                        !StrongPlantingGunLifecycleSummaryProvesZero(
                            strongPlantingGunLifecycleAfter) ||
                        !CleanupSummaryProvesZero(
                            strongPlantingGunCleanup))
                    {
                        throw new InvalidOperationException(
                            "StrongPlantingGun Loader owner-deactivation cleanup was incomplete. instance=" +
                            runtime.HasOwnerInstance(
                                StrongPlantingGunOwnerId) +
                            "; roots=" +
                            strongPlantingGunRootsAfter +
                            "; loaded=" +
                            strongPlantingGunLoadedAfter +
                            "; patches=" +
                            strongPlantingGunAfter
                                .ExactOwnerPatchCount +
                            "; targets=" +
                            strongPlantingGunAfter
                                .ExactOwnerTargetCount +
                            "; callback=" +
                            strongPlantingGunCallbackAfter +
                            "; lifecycle={" +
                            strongPlantingGunLifecycleAfter +
                            "}; cleanup={" +
                            strongPlantingGunCleanup +
                            "}.");
                    }
                    detailsParts.Add(
                        "StrongPlantingGun=nativeSave+titleReentry+actual5+callback1->instance0+actual0+callback0+listeners0+cachedObjects0+cachedMembers0+capacitySnapshots0+roots0");
                }

                if (mineRequested)
                {
                    string mineCleanup =
                        runtime.DeactivateOwner(
                            MineOwnerId,
                            ModOwnerCleanupReason.Unload,
                            shutdown: false,
                            transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory mineAfter =
                        Batch6AdvancedHarmonyOwnerObserver.Observe(
                            MineAssemblyName,
                            MineHarmonyOwner,
                            MineHarmonyTargets);
                    bool mineCallbackAfter =
                        ReadStaticCallbackRuntimePresent(
                            MineAssemblyName,
                            "DTMAPI.Mine.MineCallbacks");
                    int mineLoadedAfter =
                        CountLoadedOwners(MineOwnerId);
                    int mineRootsAfter =
                        runtime.CountCoreOwnerRoots(MineOwnerId);
                    if (runtime.HasOwnerInstance(MineOwnerId) ||
                        mineLoadedAfter != 0 ||
                        mineRootsAfter != 0 ||
                        mineAfter.ExactOwnerPatchCount != 0 ||
                        mineAfter.ExactOwnerTargetCount != 0 ||
                        mineCallbackAfter ||
                        !CleanupSummaryProvesZero(mineCleanup))
                    {
                        throw new InvalidOperationException(
                            "Mine Loader owner-deactivation cleanup was incomplete. instance=" +
                            runtime.HasOwnerInstance(MineOwnerId) +
                            "; roots=" + mineRootsAfter +
                            "; loaded=" + mineLoadedAfter +
                            "; patches=" +
                            mineAfter.ExactOwnerPatchCount +
                            "; targets=" +
                            mineAfter.ExactOwnerTargetCount +
                            "; callback=" + mineCallbackAfter +
                            "; cleanup={" + mineCleanup + "}.");
                    }
                    detailsParts.Add(
                        "Mine=inSaveActual3+callback1+sessionDerived->titleActual0+callback0->instance0+actual0+callback0+roots0");
                }

                if (debugConsoleRequested)
                {
                    string debugConsoleCleanup =
                        runtime.DeactivateOwner(
                            DebugConsoleOwnerId,
                            ModOwnerCleanupReason.Unload,
                            shutdown: false,
                            transactionId: string.Empty);
                    Batch6HarmonyOwnerInventory debugConsoleAfter =
                        Batch6AdvancedHarmonyOwnerObserver.Observe(
                            DebugConsoleAssemblyName,
                            DebugConsoleHarmonyOwner,
                            DebugConsoleHarmonyTargets);
                    int debugConsoleLoadedAfter =
                        CountLoadedOwners(DebugConsoleOwnerId);
                    int debugConsoleRootsAfter =
                        runtime.CountCoreOwnerRoots(DebugConsoleOwnerId);
                    int debugConsoleHarmonyAfter =
                        Batch6AdvancedHarmonyOwnerObserver.CountAllOwnerPatches(
                            DebugConsoleHarmonyOwner);
                    if (runtime.HasOwnerInstance(DebugConsoleOwnerId) ||
                        debugConsoleLoadedAfter != 0 ||
                        debugConsoleRootsAfter != 0 ||
                        debugConsoleAfter.ExactOwnerPatchCount != 0 ||
                        debugConsoleAfter.ExactOwnerTargetCount != 0 ||
                        debugConsoleHarmonyAfter != 0 ||
                        !CleanupSummaryProvesZero(debugConsoleCleanup))
                    {
                        throw new InvalidOperationException(
                            "DebugConsole Loader owner-deactivation cleanup was incomplete. instance=" +
                            runtime.HasOwnerInstance(DebugConsoleOwnerId) +
                            "; roots=" + debugConsoleRootsAfter +
                            "; loaded=" + debugConsoleLoadedAfter +
                            "; patches=" +
                            debugConsoleAfter.ExactOwnerPatchCount +
                            "; targets=" +
                            debugConsoleAfter.ExactOwnerTargetCount +
                            "; ownerPatches=" +
                            debugConsoleHarmonyAfter +
                            "; cleanup={" + debugConsoleCleanup + "}.");
                    }
                    detailsParts.Add(
                        "DebugConsole=inSaveActual19+typedInput+modalUi->titleGated19+leases0->instance0+actual0+input0+ui0+roots0");
                }

                string details =
                    "requestedOwners=" + string.Join("|", requested.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)) +
                    "; " + string.Join("; ", detailsParts) +
                    "; loaderPath=DtmApiRuntime.DeactivateOwner(Unload)" +
                    "; restartRequired=true-loaded-assembly";
                runtime.SetHookStatus("Smoke.AdvancedProductOwnerDeactivation", "verified", "Core Loader owner deactivation -> IDisposable -> exact Harmony owner cleanup", details);
                runtime.RuntimeMonitor.Log("Smoke exercise AdvancedProductOwnerDeactivation OK " + details + ".");
                return G4FixtureStepResult.Verified(details);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Advanced product owner-deactivation fixture failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AdvancedProductOwnerDeactivation", "failed", "Core Loader owner deactivation", ex.GetType().Name + ": " + ex.Message);
                return G4FixtureStepResult.Failed(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private int CountLoadedOwners(string ownerId) => runtime.LoadedMods.Count(mod =>
            mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));

        private static int ReadMoreSavesArchiveCount()
        {
            Assembly? gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
            Type? dolocApi = gameAssembly?.GetType("DolocAPI", throwOnError: false, ignoreCase: false);
            object? manager = dolocApi?.GetField("gameManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) ??
                dolocApi?.GetProperty("gameManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            if (manager == null)
                return 0;
            Type managerType = manager.GetType();
            object? value = managerType.GetField("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manager) ??
                managerType.GetProperty("archiveFileCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manager);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private static bool ReadStaticCallbackRuntimePresent(string assemblyName, string callbackTypeName)
        {
            return ReadStaticCallbackRuntime(assemblyName, callbackTypeName) != null;
        }

        private static object? ReadStaticCallbackRuntime(string assemblyName, string callbackTypeName)
        {
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, assemblyName, StringComparison.Ordinal));
            Type? callbacks = assembly?.GetType(callbackTypeName, throwOnError: false, ignoreCase: false);
            FieldInfo? runtimeField = callbacks?.GetField("runtime", BindingFlags.Static | BindingFlags.NonPublic);
            return runtimeField?.GetValue(null);
        }

        private static string ReadStaticCallbackLifecycleSummary(
            string assemblyName,
            string callbackTypeName)
        {
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(
                    candidate.GetName().Name,
                    assemblyName,
                    StringComparison.Ordinal));
            Type? callbackType = assembly?.GetType(
                callbackTypeName,
                throwOnError: false,
                ignoreCase: false);
            object? value =
                callbackType?.GetProperty(
                    "LastLifecycleSummary",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) ??
                callbackType?.GetField(
                    "LastLifecycleSummary",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) ??
                callbackType?.GetField(
                    "lastLifecycleSummary",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            return value as string ?? string.Empty;
        }

        private static bool LifecycleSummaryProvesZero(string summary)
        {
            string value = summary ?? string.Empty;
            return value.IndexOf("clones=0", StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf("listeners=0", StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf("functions=0", StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf("callbacks=0", StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf("hooks=0", StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf("roots=0", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool
            StrongPlantingGunLifecycleSummaryProvesActive(
                string summary) =>
            ReadLifecycleSummaryCount(
                summary,
                "listeners") == 0 &&
            ReadLifecycleSummaryCount(
                summary,
                "callbacks") == 1 &&
            ReadLifecycleSummaryCount(
                summary,
                "hooks") == 5 &&
            ReadLifecycleSummaryCount(
                summary,
                "cachedObjects") == 0 &&
            ReadLifecycleSummaryCount(
                summary,
                "capacitySnapshots") > 0 &&
            ReadLifecycleSummaryCount(
                summary,
                "roots") > 0;

        private static int ReadLifecycleSummaryCount(
            string summary,
            string key)
        {
            string prefix = key + "=";
            string[] parts =
                (summary ?? string.Empty).Split(';');
            for (int index = 0;
                 index < parts.Length;
                 index++)
            {
                string part = parts[index].Trim();
                if (part.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(
                        part.Substring(prefix.Length),
                        out int value))
                {
                    return value;
                }
            }
            return -1;
        }

        private static bool
            StrongPlantingGunLifecycleSummaryProvesZero(
                string summary)
        {
            string value = summary ?? string.Empty;
            return value.IndexOf(
                       "listeners=0",
                       StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf(
                    "callbacks=0",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf(
                    "hooks=0",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf(
                    "cachedObjects=0",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf(
                    "cachedMembers=0",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf(
                    "capacitySnapshots=0",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf(
                    "roots=0",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool CleanupSummaryProvesZero(string summary) =>
            (summary ?? string.Empty).IndexOf("remaining=0", StringComparison.Ordinal) >= 0 &&
            (summary ?? string.Empty).IndexOf("modDisposeFailures=0", StringComparison.Ordinal) >= 0 &&
            (summary ?? string.Empty).IndexOf("coreCleanupFailures=0", StringComparison.Ordinal) >= 0 &&
            (summary ?? string.Empty).IndexOf("participantCleanupFailures=0", StringComparison.Ordinal) >= 0;
    }
}
