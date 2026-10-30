#pragma warning disable CS0618 // This product-absent L0 driver intentionally exercises the frozen compatibility API.
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class Batch6AutoFishingCompatibilityDriver
    {
        internal const string DriverKind = "CompatibilityNativeControl";
        internal const int ExpectedPatchCount = 22;
        private static readonly string[] ExactPatchProperties =
        {
            "ReadyEnterPatched",
            "ReadyPlayPatched",
            "CastEnterPatched",
            "WaitEnterPatched",
            "WaitPlayPatched",
            "WaitNextStatePatched",
            "MiniGameStartPatched",
            "MiniGameUpdatePrefixPatched",
            "MiniGameUpdatePatched",
            "MiniGameStopPatched",
            "InputUseToolPatched",
            "InputUseToolInProgressPatched",
            "InputUseItemPatched",
            "InputUseItemInProgressPatched",
            "InputFishingPatched",
            "InputFishingInProgressPatched",
            "FishRodCastHookPatched",
            "FishRodPullPatched",
            "FishRodPullCancelPatched",
            "PullEnterPatched",
            "PullExitPatched",
            "BaseExitPatched"
        };
        private readonly GameBridgeFixtureAccess access;
        private readonly Batch6AutoFishingPilotSettings settings;
        private readonly string ownerId;
        private readonly IManifest owner;
        private IFishingAutomationApi? api;
        private global::DTMAPI.GameBridge.DolocTown.FishingAutomationCompatibilityFeature? feature;
        private int activatedPatchCount;
        private bool started;
        private bool cleanupComplete;

        internal Batch6AutoFishingCompatibilityDriver(GameBridgeFixtureAccess access, Batch6AutoFishingPilotSettings settings)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ownerId = "DTMAPI.QA.Batch6.AutoFishingPilot.L0." + access.RunId;
            owner = new ManifestModel
            {
                Name = "Batch 6 AutoFishing Product-Absent L0 Driver",
                Author = "DTMAPI QA",
                Version = "1.0.0",
                UniqueID = ownerId,
                Type = "CodeMod"
            };
        }

        internal string OwnerId => ownerId;
        internal bool Started => started;
        internal bool CleanupComplete => cleanupComplete;

        internal Batch6AutoFishingCompatibilityDriverSnapshot Start()
        {
            if (started)
                throw new InvalidOperationException("The Batch6 AutoFishing L0 compatibility driver may start only once.");
            feature = access.Bridge.FishingAutomationCompatibilityFeatureForQa
                ?? throw new InvalidOperationException("FishingAutomationCompatibilityFeature is unavailable for product-absent L0.");
            if (feature.CountOwnerResources(ownerId) != 0 || feature.Service != null || feature.CallbackRuntime != null)
                throw new InvalidOperationException("The compatibility-native-control driver was not clean before L0 activation.");

            api = access.Runtime.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown")
                ?? throw new InvalidOperationException("The frozen IFishingAutomationApi is unavailable for product-absent L0.");
            FishingAutomationOptions options = BuildOptions();
            try
            {
                api.Configure(owner, options);
                if (feature.Service != null)
                    throw new InvalidOperationException("Compatibility Configure activated the native executor before SetEnabled(true).");
                api.SetEnabled(owner, true, "batch6-product-absent-l0-enable");
                activatedPatchCount = CountPatches(feature);
                started = true;
                Batch6AutoFishingCompatibilityDriverSnapshot snapshot = Capture();
                if (!snapshot.Enabled || !snapshot.HooksReady || snapshot.PatchCount != ExpectedPatchCount || snapshot.OwnerResourceCount <= 0 || !snapshot.CallbackRuntimePresent)
                    throw new InvalidOperationException("The compatibility-native-control driver did not activate its exact owner, updater, callback runtime, and complete " + ExpectedPatchCount.ToString(CultureInfo.InvariantCulture) + "-patch inventory.");
                access.Log(
                    "Batch6 AutoFishing L0 driver started driverKind=" + DriverKind +
                    "; driverOwner=" + ownerId +
                    "; hooks=" + snapshot.PatchCount.ToString(CultureInfo.InvariantCulture) +
                    "; productPresent=false.");
                return snapshot;
            }
            catch
            {
                TryCleanup("start-failed");
                throw;
            }
        }

        internal Batch6AutoFishingCompatibilityDriverSnapshot Capture()
        {
            if (!started || feature == null || api == null)
                throw new InvalidOperationException("The compatibility-native-control driver is not active.");
            global::DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService service = feature.Service
                ?? throw new InvalidOperationException("The active compatibility-native-control service disappeared.");
            FishingAutomationState state = api.GetState(ownerId);
            global::DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService.FishingAutomationLifecycleSnapshot lifecycle =
                service.GetFishingAutomationLifecycleSnapshot("Batch6AutoFishingPilot.L0");
            return new Batch6AutoFishingCompatibilityDriverSnapshot
            {
                ObservedAtUtc = DateTimeOffset.UtcNow,
                DriverKind = DriverKind,
                DriverOwner = ownerId,
                Enabled = state.Enabled && service.HasEnabledOwner,
                Phase = state.Phase ?? string.Empty,
                LastReason = state.LastReason ?? string.Empty,
                LastNativeAction = state.LastNativeAction ?? string.Empty,
                HooksReady = feature.HooksReady,
                PatchCount = activatedPatchCount,
                CallbackRuntimePresent = feature.CallbackRuntime != null,
                OwnerResourceCount = feature.CountOwnerResources(ownerId),
                NativeExitCount = ReadNativeExitCount(service),
                AutoCastAttemptCount = lifecycle.AutoCastAttemptCount,
                AutoCastConfirmedCount = lifecycle.AutoCastConfirmedCount,
                AutomationApplicationCount = lifecycle.AutomationApplicationCount,
                MiniGameCompleteApplicationCount = lifecycle.MiniGameCompleteApplicationCount,
                NativeTransientHandleCount = lifecycle.NativeTransientHandleCount,
                BoundaryClearCount = lifecycle.BoundaryClearCount,
                FailureCount = lifecycle.FailureCount
            };
        }

        internal Batch6AutoFishingCompatibilityDriverCleanup Cleanup(string reason)
        {
            if (cleanupComplete)
                return VerifyCleanup("already-clean");
            string cleanupSummary = string.Empty;
            Exception? failure = null;
            try
            {
                api?.SetEnabled(owner, false, "batch6-product-absent-l0-disable:" + (reason ?? string.Empty));
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            try
            {
                int removed = feature?.RemoveOwner(ownerId, "batch6-product-absent-l0-owner-cleanup:" + (reason ?? string.Empty)) ?? 0;
                cleanupSummary = "compatibilityOwnerResourcesRemoved=" + removed.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                failure = failure == null ? ex : new AggregateException(failure, ex);
            }

            Batch6AutoFishingCompatibilityDriverCleanup receipt = VerifyCleanup(cleanupSummary);
            cleanupComplete = receipt.Verified;
            if (failure != null)
                throw new InvalidOperationException("The compatibility-native-control cleanup raised an exception after exact-owner cleanup was attempted. " + receipt.Details, failure);
            if (!receipt.Verified)
                throw new InvalidOperationException("The compatibility-native-control exact-owner cleanup did not reach zero. " + receipt.Details);
            access.Log("Batch6 AutoFishing L0 driver cleanup verified " + receipt.Details + ".");
            return receipt;
        }

        internal void TryCleanup(string reason)
        {
            if (cleanupComplete)
                return;
            try { Cleanup(reason); }
            catch (Exception ex)
            {
                access.Log("Batch6 AutoFishing L0 driver cleanup failed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private FishingAutomationOptions BuildOptions()
        {
            bool instant = settings.Scenario == "InstantBite" || settings.Scenario == "InstantSkip" ||
                settings.Scenario == "CombinedInstantSkip" || settings.Scenario == "CombinedInstantComplete";
            bool skip = settings.Scenario == "SkipMiniGame" || settings.Scenario == "InstantSkip" || settings.Scenario == "CombinedInstantSkip";
            bool fast = settings.Scenario == "FastAnimations" || settings.Scenario == "CombinedInstantSkip" || settings.Scenario == "CombinedInstantComplete";
            return new FishingAutomationOptions
            {
                BiteWaitMode = instant ? FishingBiteWaitMode.InstantNativeBite : FishingBiteWaitMode.NativeWait,
                ResultMode = skip ? FishingResultMode.SkipMiniGameNativeResult : FishingResultMode.AutoCompleteVisibleMiniGame,
                AnimationMode = fast ? FishingAnimationMode.FastCastPull : FishingAnimationMode.Normal,
                AnimationMultiplier = settings.Multiplier,
                CastChargeRatio = 0d,
                RecastDelaySeconds = 0.25d,
                StopOnManualMove = false,
                VerboseLogging = false
            };
        }

        private Batch6AutoFishingCompatibilityDriverCleanup VerifyCleanup(string cleanupSummary)
        {
            int resources = feature?.CountOwnerResources(ownerId) ?? 0;
            bool serviceAbsent = feature?.Service == null;
            bool callbackAbsent = feature?.CallbackRuntime == null;
            bool hooksAbsent = feature?.HooksReady != true;
            int patchCount = feature == null ? 0 : CountPatches(feature);
            FishingAutomationState state = api?.GetState(ownerId) ?? new FishingAutomationState();
            bool verified = !state.Enabled && resources == 0 && serviceAbsent && callbackAbsent && hooksAbsent && patchCount == 0;
            return new Batch6AutoFishingCompatibilityDriverCleanup
            {
                Verified = verified,
                OwnerResourceCount = resources,
                ServicePresent = !serviceAbsent,
                CallbackRuntimePresent = !callbackAbsent,
                HooksPresent = !hooksAbsent,
                PatchCount = patchCount,
                Details = "driverKind=" + DriverKind +
                    "; driverOwner=" + ownerId +
                    "; enabled=" + state.Enabled.ToString().ToLowerInvariant() +
                    "; ownerResources=" + resources.ToString(CultureInfo.InvariantCulture) +
                    "; servicePresent=" + (!serviceAbsent).ToString().ToLowerInvariant() +
                    "; callbackRuntimePresent=" + (!callbackAbsent).ToString().ToLowerInvariant() +
                    "; hooksPresent=" + (!hooksAbsent).ToString().ToLowerInvariant() +
                    "; patchCount=" + patchCount.ToString(CultureInfo.InvariantCulture) +
                    "; ownerCleanup={" + (cleanupSummary ?? string.Empty) + "}"
            };
        }

        private static int CountPatches(global::DTMAPI.GameBridge.DolocTown.FishingAutomationCompatibilityFeature feature)
        {
            FieldInfo field = feature.GetType().GetField("hookBridge", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidDataException("FishingAutomationCompatibilityFeature.hookBridge is unavailable.");
            object? bridge = field.GetValue(feature);
            if (bridge == null)
                return 0;
            int count = 0;
            foreach (string name in ExactPatchProperties)
            {
                PropertyInfo property = bridge.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidDataException("The compatibility hook inventory is missing " + name + ".");
                if (property.PropertyType != typeof(bool))
                    throw new InvalidDataException("The compatibility hook receipt " + name + " is not Boolean.");
                if ((bool)(property.GetValue(bridge, null) ?? false))
                    count++;
            }
            return count;
        }

        private long ReadNativeExitCount(global::DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService service)
        {
            return service.FishingNativeExitObservedCount;
        }
    }

    internal sealed class Batch6AutoFishingCompatibilityDriverSnapshot
    {
        internal DateTimeOffset ObservedAtUtc { get; set; }
        internal string DriverKind { get; set; } = string.Empty;
        internal string DriverOwner { get; set; } = string.Empty;
        internal bool Enabled { get; set; }
        internal string Phase { get; set; } = string.Empty;
        internal string LastReason { get; set; } = string.Empty;
        internal string LastNativeAction { get; set; } = string.Empty;
        internal bool HooksReady { get; set; }
        internal int PatchCount { get; set; }
        internal bool CallbackRuntimePresent { get; set; }
        internal int OwnerResourceCount { get; set; }
        internal long NativeExitCount { get; set; }
        internal int AutoCastAttemptCount { get; set; }
        internal int AutoCastConfirmedCount { get; set; }
        internal int AutomationApplicationCount { get; set; }
        internal int MiniGameCompleteApplicationCount { get; set; }
        internal int NativeTransientHandleCount { get; set; }
        internal int BoundaryClearCount { get; set; }
        internal int FailureCount { get; set; }
    }

    internal sealed class Batch6AutoFishingCompatibilityDriverCleanup
    {
        internal bool Verified { get; set; }
        internal int OwnerResourceCount { get; set; }
        internal bool ServicePresent { get; set; }
        internal bool CallbackRuntimePresent { get; set; }
        internal bool HooksPresent { get; set; }
        internal int PatchCount { get; set; }
        internal string Details { get; set; } = string.Empty;
    }
}
