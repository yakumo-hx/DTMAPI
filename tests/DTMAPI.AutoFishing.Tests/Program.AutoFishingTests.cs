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
using Yuuka.DTMAPI.AutoFishing;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void AutoFishingProductOwnsExecutableSessionLifetime()
        {
            string source = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "products",
                "first-party",
                "AutoFishing",
                "src",
                "ModEntry.cs"));
            Assert(source.Contains("SetUpdateSubscription(true)", StringComparison.Ordinal), "AutoFishing must subscribe its updater only after its product-owned session is active.");
            Assert(source.Contains("SetUpdateSubscription(false)", StringComparison.Ordinal), "F6 off and lifecycle boundaries must detach the product updater.");
            Assert(source.Contains("ReleasePrimitiveSession(reason)", StringComparison.Ordinal), "F6 off must release product-owned input, animation, and native session state.");
            Assert(source.Contains("session.SetInputFaultHandler(OnFishingInputFault)", StringComparison.Ordinal) &&
                source.Contains("private bool OnFishingInputFault(string reason)", StringComparison.Ordinal) &&
                source.Contains("SetAutomation(false, reason ?? \"minigame-input-fault\")", StringComparison.Ordinal) &&
                source.Contains("TryClosePendingInputFault()", StringComparison.Ordinal),
                "A ProductNative minigame-input fault must immediately return to ModEntry, use the complete automation-disable path, and retain a lifecycle-level retry.");
            Assert(!source.Contains("GetApi<IFirstPartyFishing", StringComparison.Ordinal), "AutoFishing must not reacquire the deleted GameBridge primitive provider.");
        }

        private static void AutoFishingMiniGameInputStateIsCurrentGameBoundedAndFaultClosed()
        {
            var transaction = new FishingMiniGameInputTransaction();
            var firstGame = new object();
            var secondGame = new object();
            bool observedAlreadyTapped = false;
            int successes = 0;
            int faults = 0;
            string faultStage = string.Empty;
            string faultDetail = string.Empty;
            Exception? observedFault = null;

            FishingMiniGameFrameReader ready = (
                object handle,
                long sequence,
                out FishingMiniGameFrame frame,
                out string failureReason) =>
            {
                frame = new FishingMiniGameFrame(sequence, 1.5d, FishingMiniGameNoteKind.Bonus, int.MaxValue, 1d, 2d, false);
                failureReason = string.Empty;
                return FishingMiniGameFrameReadStatus.Ready;
            };
            Action<string, string, Exception?> recordFault = (stage, detail, error) =>
            {
                faults++;
                faultStage = stage;
                faultDetail = detail;
                observedFault = error;
            };

            bool first = transaction.TryExecute(
                firstGame,
                1,
                ready,
                frame =>
                {
                    observedAlreadyTapped = frame.BonusAlreadyTapped;
                    return FishingSyntheticInputAction.TapBonus;
                },
                () => successes++,
                recordFault,
                out FishingSyntheticInputAction firstAction,
                out _);
            Assert(first && firstAction == FishingSyntheticInputAction.TapBonus &&
                !observedAlreadyTapped && transaction.TrackedBonusNoteCount == 1,
                "The first bonus note must be tapped and recorded without a composite identity hash.");

            bool repeated = transaction.TryExecute(
                firstGame,
                2,
                ready,
                frame =>
                {
                    observedAlreadyTapped = frame.BonusAlreadyTapped;
                    return frame.BonusAlreadyTapped
                        ? FishingSyntheticInputAction.Release
                        : FishingSyntheticInputAction.TapBonus;
                },
                () => successes++,
                recordFault,
                out FishingSyntheticInputAction repeatedAction,
                out _);
            Assert(repeated && repeatedAction == FishingSyntheticInputAction.Release &&
                observedAlreadyTapped && transaction.TrackedBonusNoteCount == 1,
                "A repeated note in the same native minigame must be exposed as already tapped exactly once.");

            bool secondHandle = transaction.TryExecute(
                secondGame,
                3,
                ready,
                frame =>
                {
                    observedAlreadyTapped = frame.BonusAlreadyTapped;
                    return FishingSyntheticInputAction.TapBonus;
                },
                () => successes++,
                recordFault,
                out _,
                out _);
            Assert(secondHandle && !observedAlreadyTapped && transaction.TrackedBonusNoteCount == 1,
                "A different native minigame handle must discard the previous handle even when the note index is identical.");

            FishingMiniGameFrameReader notReady = (
                object handle,
                long sequence,
                out FishingMiniGameFrame frame,
                out string failureReason) =>
            {
                frame = default;
                failureReason = "note-spawner-not-ready";
                return FishingMiniGameFrameReadStatus.NotReady;
            };
            Assert(!transaction.TryExecute(secondGame, 4, notReady, _ => FishingSyntheticInputAction.Hold, () => successes++, recordFault, out _, out _) &&
                faults == 0,
                "A bounded transient frame-not-ready result must wait without poisoning the product session.");

            FishingMiniGameFrameReader frameFault = (
                object handle,
                long sequence,
                out FishingMiniGameFrame frame,
                out string failureReason) =>
            {
                frame = default;
                failureReason = "invoke:frame:TargetInvocationException";
                return FishingMiniGameFrameReadStatus.Faulted;
            };
            Assert(!transaction.TryExecute(secondGame, 5, frameFault, _ => FishingSyntheticInputAction.Hold, () => successes++, recordFault, out _, out _) &&
                faults == 1 && faultStage == "frame" &&
                faultDetail == "invoke:frame:TargetInvocationException" &&
                observedFault == null,
                "A permanent native frame-access failure must enter the same explicit fault transaction.");

            Assert(!transaction.TryExecute(
                    secondGame,
                    6,
                    ready,
                    _ => throw new OverflowException("provider"),
                    () => successes++,
                    recordFault,
                    out _,
                    out _) &&
                faults == 2 && faultStage == "provider" && observedFault is OverflowException,
                "A provider exception must be attributed to the provider stage and published to the immediate fault closer.");

            transaction.Clear();
            Assert(transaction.TrackedBonusNoteCount == 0 && successes == 3,
                "MiniGameStop, Pull and session release can use one transaction clear operation to remove the current handle and note set.");

            string serviceSource = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "products",
                "first-party",
                "AutoFishing",
                "src",
                "Native",
                "FishingPrimitivesService.cs"));
            Assert(!serviceSource.Contains("BonusTapKey", StringComparison.Ordinal) &&
                !serviceSource.Contains("RuntimeHelpers.GetHashCode", StringComparison.Ordinal) &&
                serviceSource.Contains("RecordMiniGameInputFault", StringComparison.Ordinal) &&
                serviceSource.Contains("session.NotifyInputFault(reason)", StringComparison.Ordinal) &&
                serviceSource.Contains("pendingInputFaultReason", StringComparison.Ordinal),
                "The production transaction must remove the composite identity hash, close immediately, and retain an unclosed fault across session release.");
            string cacheSource = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "products",
                "first-party",
                "AutoFishing",
                "src",
                "Native",
                "FishingMiniGameNativeCache.cs"));
            Assert(cacheSource.Contains("FishingMiniGameFrameReadStatus.NotReady", StringComparison.Ordinal) &&
                cacheSource.Contains("FishingMiniGameFrameReadStatus.Faulted", StringComparison.Ordinal) &&
                cacheSource.Contains("LastAccessorFailure", StringComparison.Ordinal),
                "The native frame reader must distinguish a transient absent spawner from accessor/build/invocation faults.");
        }

        private static void AutoFishingArchitectureBoundariesRemainIsolated()
        {
            string repo = FindRepositoryRoot();
            string productDirectory = Path.Combine(repo, "products", "first-party", "AutoFishing", "src");
            string productSource = string.Join("\n", Directory.EnumerateFiles(productDirectory, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

            foreach (string required in new[]
            {
                "using HarmonyLib;",
                "System.Reflection",
                "AgentStateFishingReady",
                "FishingGameScrollBar",
                "AgentStateBase",
                "dtmapi.mod.yuuka.dtmapi.autofishing",
                "ExpectedPatchCount = 22",
                "SetUpdateSubscription(false)"
            })
            {
                Assert(productSource.Contains(required, StringComparison.Ordinal), "The admitted Advanced AutoFishing product must own native/lifecycle token " + required + ".");
            }
            foreach (string forbidden in new[]
            {
                "IFishingAutomationApi",
                "IFirstPartyFishing",
                "DTMAPI.GameBridge.DolocTown",
                "GetApi<IFirstPartyFishing",
                "GameBridgeDemandRoutes"
            })
            {
                Assert(!productSource.Contains(forbidden, StringComparison.Ordinal), "The self-contained AutoFishing product must not consume deleted/compatibility token " + forbidden + ".");
            }
            Assert(CountTextOccurrences(productSource, "RegisterKeybind(") == 1, "AutoFishing should retain exactly one owner-bound Gameplay toggle registration.");
            Assert(CountTextOccurrences(productSource, "GameLoop.UpdateTicked += OnUpdateTicked") == 1 &&
                CountTextOccurrences(productSource, "GameLoop.UpdateTicked -= OnUpdateTicked") == 1 &&
                productSource.Contains("SetAutomation(false, \"update session unavailable\")", StringComparison.Ordinal) &&
                productSource.Contains("SetAutomation(false, \"SaveLoaded session unavailable\")", StringComparison.Ordinal),
                "AutoFishing's updater must remain session-scoped and fail closed on session reacquisition failure.");
            string modEntrySource = File.ReadAllText(Path.Combine(productDirectory, "ModEntry.cs"));
            int disposeStart = modEntrySource.IndexOf("public void Dispose()", StringComparison.Ordinal);
            int disposeEnd = modEntrySource.IndexOf("private static void TryCleanup", disposeStart, StringComparison.Ordinal);
            Assert(modEntrySource.Contains("public sealed class ModEntry : DtmMod, IDisposable", StringComparison.Ordinal) &&
                disposeStart >= 0 && disposeEnd > disposeStart,
                "The Advanced product must expose one standard optional owner-deactivation callback without expanding DTMAPI.Abstractions.");
            string disposeSource = modEntrySource.Substring(disposeStart, disposeEnd - disposeStart);
            Assert(disposeSource.Contains("enabled = false;", StringComparison.Ordinal) &&
                disposeSource.Contains("updateSubscribed = false;", StringComparison.Ordinal) &&
                disposeSource.Contains("ResetPrimitiveLifecycleBoundary(\"OwnerDeactivation\")", StringComparison.Ordinal) &&
                disposeSource.Contains("toggleRegistration?.Dispose();", StringComparison.Ordinal) &&
                disposeSource.Contains("nativeRuntime.DeactivateOwner(\"OwnerDeactivation\")", StringComparison.Ordinal) &&
                !disposeSource.Contains("SetUpdateSubscription", StringComparison.Ordinal) &&
                !disposeSource.Contains("DetachProductHandlers", StringComparison.Ordinal),
                "Manager deactivation must restore product-private state directly while Core owns removal of guarded event proxies.");
            int refreshNativeState = modEntrySource.IndexOf("primitives.RefreshNativeState();", StringComparison.Ordinal);
            int updateSnapshot = modEntrySource.IndexOf("FishingPrimitiveSnapshot snapshot = session!.GetSnapshot();", StringComparison.Ordinal);
            Assert(refreshNativeState >= 0 && updateSnapshot > refreshNativeState,
                "AutoFishing must refresh the native frame before the F6 updater consumes its session snapshot.");
            Assert(modEntrySource.Contains("ResetPrimitiveLifecycleBoundary(\"SaveLoaded\")", StringComparison.Ordinal) &&
                modEntrySource.Contains("ResetPrimitiveLifecycleBoundary(\"ReturnedToTitle\")", StringComparison.Ordinal) &&
                modEntrySource.Contains("primitives.ResetForLifecycleBoundary", StringComparison.Ordinal) &&
                modEntrySource.Contains("primitives.InvalidateEnvironment(\"automation-enable:", StringComparison.Ordinal) &&
                modEntrySource.Contains("primitives.InvalidateEnvironment(\"automation-disable:", StringComparison.Ordinal),
                "AutoFishing SaveLoaded, title return, and F6 re-entry boundaries must invalidate scene-sensitive native caches.");
            string productHookSource = File.ReadAllText(Path.Combine(productDirectory, "Native", "FishingProductHookInstaller.cs"));
            Assert(productHookSource.Contains("CompatibilityHarmonyOwner = \"dtmapi.gamebridge.doloctown.fishingcompatibility\"", StringComparison.Ordinal) &&
                productHookSource.Contains("ThrowIfCompatibilityOwnerIsPresent(resolved)", StringComparison.Ordinal) &&
                productHookSource.Contains("Harmony.GetPatchInfo(patch.Target)", StringComparison.Ordinal),
                "The product owner must fail closed before patching when the frozen compatibility owner is already present.");
            string nativeRuntimeSource = File.ReadAllText(Path.Combine(productDirectory, "Native", "AutoFishingNativeRuntime.cs"));
            int nativeDeactivate = nativeRuntimeSource.IndexOf("internal void DeactivateOwner(string reason)", StringComparison.Ordinal);
            int callbackDetach = nativeRuntimeSource.IndexOf("FishingProductCallbacks.Detach(this);", nativeDeactivate, StringComparison.Ordinal);
            int primitiveRollback = nativeRuntimeSource.IndexOf("RollbackPrimitiveActivation(primitiveHookRuntime", nativeDeactivate, StringComparison.Ordinal);
            int exactUnpatch = nativeRuntimeSource.IndexOf("hookInstaller.UnpatchOwnedHooks();", nativeDeactivate, StringComparison.Ordinal);
            Assert(nativeDeactivate >= 0 && callbackDetach > nativeDeactivate && primitiveRollback > callbackDetach && exactUnpatch > primitiveRollback,
                "Owner deactivation must sever the static callback root before native rollback and still attempt exact-owner Harmony cleanup afterward.");

            string abstractionsDirectory = Path.Combine(repo, "src", "DTMAPI.Abstractions");
            Assert(!File.Exists(Path.Combine(abstractionsDirectory, "FirstPartyFishingPrimitives.cs")), "The single-consumer first-party fishing seam must be deleted.");
            string abstractionsAssemblyInfo = File.ReadAllText(Path.Combine(abstractionsDirectory, "AssemblyInfo.cs"));
            Assert(!abstractionsAssemblyInfo.Contains("InternalsVisibleTo(\"AutoFishingMod\")", StringComparison.Ordinal), "AutoFishing must not retain an Abstractions friend opening.");

            string bridgeDirectory = Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown");
            string bridgeSource = string.Join("\n", Directory.EnumerateFiles(bridgeDirectory, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
            foreach (string forbidden in new[]
            {
                "IFirstPartyFishing",
                "FishingPrimitivesService",
                "FishingPrimitiveHookRuntime",
                "SetPrimitiveDemand",
                "AttachPrimitives",
                "FishingBaseExitOwned",
                "fishing-base-exit"
            })
            {
                Assert(!bridgeSource.Contains(forbidden, StringComparison.Ordinal), "Mandatory GameBridge must not retain moved ProductNative seam " + forbidden + ".");
            }

            string compatibilityDirectory = Path.Combine(bridgeDirectory, "Compatibility", "FishingAutomation");
            string compatibilitySource = string.Join("\n", Directory.EnumerateFiles(compatibilityDirectory, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
            string demandSource = File.ReadAllText(Path.Combine(bridgeDirectory, "Demand", "GameBridgeDemandRoutes.cs"));
            Assert(compatibilitySource.Contains("class FishingAutomationCompatibilityFeature", StringComparison.Ordinal) &&
                compatibilitySource.Contains("IFishingCompatibilityHookRuntime", StringComparison.Ordinal) &&
                demandSource.Contains("FishingAutomation.Compatibility", StringComparison.Ordinal) &&
                compatibilitySource.Contains("FishingCompatibilityBaseExitPostfix", StringComparison.Ordinal),
                "The frozen IFishingAutomationApi executor must use compatibility-named construction, callback, demand, and BaseExit roots.");
            string compatibilityHookSource = File.ReadAllText(Path.Combine(compatibilityDirectory, "FishingCompatibilityHookBridge.cs"));
            string compatibilityFeatureSource = File.ReadAllText(Path.Combine(compatibilityDirectory, "FishingAutomationCompatibilityFeature.cs"));
            int compatibilityFailClosed = compatibilityHookSource.IndexOf("private void FailClosed", StringComparison.Ordinal);
            int compatibilityOwnerRollback = compatibilityHookSource.IndexOf("patcher.TryUnpatchAllOwnedPatches()", compatibilityFailClosed, StringComparison.Ordinal);
            int compatibilityStatusReset = compatibilityHookSource.IndexOf("ResetPatchStatuses();", compatibilityOwnerRollback, StringComparison.Ordinal);
            Assert(compatibilityHookSource.Contains("HarmonyOwner = \"dtmapi.gamebridge.doloctown.fishingcompatibility\"", StringComparison.Ordinal) &&
                compatibilityHookSource.Contains("ManagedProductUniqueId = \"Yuuka.DTMAPI.AutoFishing\"", StringComparison.Ordinal) &&
                compatibilityHookSource.Contains("ManagedModClassifier.GetExpectedHarmonyOwner(ManagedProductUniqueId)", StringComparison.Ordinal) &&
                compatibilityHookSource.Contains("TryFindManagedModOwnerOnInventory(patcher", StringComparison.Ordinal) &&
                compatibilityHookSource.Contains("patch.Targets(target)", StringComparison.Ordinal) &&
                compatibilityHookSource.Contains("patch.Owner.Equals(ManagedProductHarmonyOwner, StringComparison.Ordinal)", StringComparison.Ordinal) &&
                !compatibilityHookSource.Contains("StartsWith(ManagedModHarmonyOwnerPrefix", StringComparison.Ordinal) &&
                compatibilityFailClosed >= 0 && compatibilityOwnerRollback > compatibilityFailClosed && compatibilityStatusReset > compatibilityOwnerRollback &&
                compatibilityFeatureSource.Contains("CallbackRuntime => HooksReady &&", StringComparison.Ordinal) &&
                compatibilityFeatureSource.Contains("UninstallHooks(patcher, reason)", StringComparison.Ordinal) &&
                compatibilityFeatureSource.Contains("SetCompatibilityDemand(owner.UniqueID, active: false, \"compatibility activation rejected", StringComparison.Ordinal) &&
                compatibilityFeatureSource.Contains("if (patcher == null)", StringComparison.Ordinal) &&
                compatibilityFeatureSource.Contains("service.SetFishingHooksInstalled(false);", StringComparison.Ordinal) &&
                compatibilityFeatureSource.Contains("if (hookBridge?.HooksReady != true)", StringComparison.Ordinal) &&
                compatibilityFeatureSource.Contains("service = null;", StringComparison.Ordinal) &&
                compatibilityFeatureSource.Contains("return null;", StringComparison.Ordinal),
                "Frozen fishing compatibility must defer callback publication before patcher binding, then use a private owner, roll back an incomplete 22-patch inventory atomically, disable the whole service/demand after rejection, reject the exact AutoFishing product owner, and preserve cross-domain shared-target owners.");
            string legacyService = File.ReadAllText(Path.Combine(compatibilityDirectory, "LegacyFishingAutomationService.cs"));
            foreach (string forbidden in new[] { "AttachPrimitives", "FishingPrimitivesService", "primitiveDiagnosticState", "primitives?." })
                Assert(!legacyService.Contains(forbidden, StringComparison.Ordinal), "The frozen compatibility executor must not attach the new product through " + forbidden + ".");

            string callbacks = File.ReadAllText(Path.Combine(bridgeDirectory, "Hooking", "DolocTownHookCallbacks.cs"));
            int sharedBaseExitStart = callbacks.IndexOf("public static void AgentStateBaseExitPostfix()", StringComparison.Ordinal);
            int compatibilityBaseExitStart = callbacks.IndexOf("public static void FishingCompatibilityBaseExitPostfix()", StringComparison.Ordinal);
            Assert(sharedBaseExitStart >= 0 && compatibilityBaseExitStart > sharedBaseExitStart, "Shared and compatibility BaseExit callbacks should remain independently named.");
            string sharedBaseExit = callbacks.Substring(sharedBaseExitStart, compatibilityBaseExitStart - sharedBaseExitStart);
            Assert(!sharedBaseExit.Contains("Fishing", StringComparison.Ordinal), "The shared AgentStateBase exit callback must retain only ActionSpeed ownership.");

            string coreDirectory = Path.Combine(repo, "src", "DTMAPI.Core");
            string[] coreFiles = Directory.EnumerateFiles(coreDirectory, "*.cs", SearchOption.AllDirectories).ToArray();
            string advancedAdmissionAuthorityPath = Path.Combine(coreDirectory, "Manifesting", "ManagedModClassification.cs");
            string coreWithoutAdmissionAuthority = string.Join("\n", coreFiles
                .Where(path => !path.Equals(advancedAdmissionAuthorityPath, StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));
            Assert(!coreWithoutAdmissionAuthority.Contains("AutoFishing", StringComparison.Ordinal) &&
                !coreWithoutAdmissionAuthority.Contains("FishingPrimitive", StringComparison.Ordinal),
                "Core may name AutoFishing only in its exact Advanced policy/identity authority and must own no fishing implementation.");
            string advancedAdmissionAuthority = File.ReadAllText(advancedAdmissionAuthorityPath);
            string coreProjectSource = File.ReadAllText(Path.Combine(coreDirectory, "DTMAPI.Core.csproj"));
            string advancedPolicyRegistry = File.ReadAllText(Path.Combine(repo, "author-sdk", "advanced-reference-policies", "registry.json"));
            Assert(advancedAdmissionAuthority.Contains("AdvancedReferencePolicyRegistry.json", StringComparison.Ordinal) &&
                advancedAdmissionAuthority.Contains("PolicyResourcePrefix", StringComparison.Ordinal) &&
                coreProjectSource.Contains("advanced-reference-policies\\registry.json", StringComparison.Ordinal) &&
                coreProjectSource.Contains("advanced-reference-policies\\*.json", StringComparison.Ordinal) &&
                advancedPolicyRegistry.Contains("doloctown-24456188-autofishing-v1", StringComparison.Ordinal) &&
                advancedPolicyRegistry.Contains("Yuuka.DTMAPI.AutoFishing", StringComparison.Ordinal),
                "Core admission authority must embed the registry-driven exact AutoFishing policy/UniqueID binding.");

            string unitProject = File.ReadAllText(Path.Combine(repo, "tests", "DTMAPI.AutoFishing.Tests", "DTMAPI.AutoFishing.Tests.csproj"));
            Assert(!unitProject.Contains("first-party-mods\\AutoFishingMod\\AutoFishingMod.csproj", StringComparison.OrdinalIgnoreCase),
                "Unit tests must not restore the raw legacy AutoFishing project as a production build authority.");
        }

        private static void AutoFishingModConfigPreservesCustomToggleKey()
        {
            var customConfig = new AutoFishingConfig { ToggleKey = "F7", AnimationMultiplier = 0, CastChargeRatio = 2 };
            customConfig.Normalize();
            Assert(customConfig.ToggleKey == "F7", "AutoFishing custom toggle key should not be reset to F6.");
            Assert(Math.Abs(customConfig.AnimationMultiplier - 3) < 0.0001, "AutoFishing missing animation multiplier should migrate to default three.");
            Assert(Math.Abs(customConfig.CastChargeRatio - 1) < 0.0001, "AutoFishing cast charge should clamp to full charge.");

            var missingConfig = new AutoFishingConfig { ToggleKey = "  ", AnimationMultiplier = 3 };
            missingConfig.Normalize();
            Assert(missingConfig.ToggleKey == "F6", "AutoFishing missing toggle key should migrate to default F6.");

            var noneConfig = new AutoFishingConfig { ToggleKey = "None", AnimationMultiplier = 3 };
            noneConfig.Normalize();
            Assert(noneConfig.ToggleKey == "None", "AutoFishing explicit None toggle key should remain disabled.");
        }

        private static void AutoFishingDecisionEngineKeepsProductOptionsIndependent()
        {
            var engine = new FishingDecisionEngine();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var idle = new FishingPrimitiveSnapshot(1, FishingPrimitivePhase.Idle, true, true, true, false, false, false, false, true, 0, 0, 0);
            Assert(engine.Decide(idle, instantBite: false, skipMiniGame: false, nowUtc: now, nextCastAtUtc: now).Action == FishingProductAction.Cast, "Idle due state should request a cast.");

            var wait = new FishingPrimitiveSnapshot(2, FishingPrimitivePhase.WaitPlayable, false, true, true, true, false, false, false, true, 0, 0, 0);
            Assert(engine.Decide(wait, instantBite: false, skipMiniGame: false, nowUtc: now, nextCastAtUtc: now).Action == FishingProductAction.None, "Default loop should keep the native bite wait.");
            Assert(engine.Decide(wait, instantBite: true, skipMiniGame: false, nowUtc: now, nextCastAtUtc: now).Action == FishingProductAction.PrepareNativeBite, "InstantBite should only change WaitPlayable behavior.");

            int combinations = 0;
            foreach (bool instant in new[] { false, true })
            {
                foreach (bool skip in new[] { false, true })
                {
                    foreach (bool fast in new[] { false, true })
                    {
                        var bite = new FishingPrimitiveSnapshot(3 + combinations, FishingPrimitivePhase.BiteReady, false, true, true, false, true, true, false, true, 0, 0, 0);
                        FishingProductAction action = engine.Decide(bite, instant, skip, now, now).Action;
                        Assert(action == (skip ? FishingProductAction.ReelSkipMiniGame : FishingProductAction.ReelVisibleMiniGame), "BiteReady result choice should depend only on SkipMiniGame across all eight option combinations; fast=" + fast + " instant=" + instant + ".");
                        combinations++;
                    }
                }
            }
            Assert(combinations == 8, "All three independent product switches should produce eight tested combinations.");

            Assert(engine.DecideMiniGameInput(new FishingMiniGameFrame(1, 1.5, FishingMiniGameNoteKind.Stable, 0, 1, 2, false)) == FishingSyntheticInputAction.Hold, "Stable notes should hold native use input.");
            Assert(engine.DecideMiniGameInput(new FishingMiniGameFrame(1, 1.5, FishingMiniGameNoteKind.Bonus, 1, 1, 2, false)) == FishingSyntheticInputAction.TapBonus, "Untapped bonus notes should tap once.");
            Assert(engine.DecideMiniGameInput(new FishingMiniGameFrame(1, 1.5, FishingMiniGameNoteKind.Bonus, 1, 1, 2, true)) == FishingSyntheticInputAction.Release, "Already-tapped bonus notes should release.");
            Assert(engine.DecideMiniGameInput(new FishingMiniGameFrame(1, 1.5, FishingMiniGameNoteKind.Avoid, 2, 1, 2, false)) == FishingSyntheticInputAction.Release, "Avoid notes should release native use input.");

            var defaults = new AutoFishingConfig();
            Assert(!defaults.InstantBite && !defaults.SkipMiniGame && !defaults.FastAnimations && Math.Abs(defaults.AnimationMultiplier - 3) < 0.0001 && Math.Abs(defaults.CastChargeRatio) < 0.0001, "First-party defaults should keep optional switches off, zero charge, and animation multiplier three.");
        }
    }
}
