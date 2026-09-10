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

        private static void FishingAutomationApiIsFeatureOwnedNotExperimentalBridgeOwned()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type experimentalBridgeType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi")
                ?? throw new InvalidOperationException("DolocTownExperimentalBridgeApi type should exist.");
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService")
                ?? throw new InvalidOperationException("LegacyFishingAutomationService type should exist.");
            Type compatibilityType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationCompatibilityAdapter")
                ?? throw new InvalidOperationException("FishingAutomationCompatibilityAdapter type should exist.");
            Type featureType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationCompatibilityFeature")
                ?? throw new InvalidOperationException("FishingAutomationCompatibilityFeature type should exist.");

            Assert(!typeof(IFishingAutomationApi).IsAssignableFrom(experimentalBridgeType), "DolocTownExperimentalBridgeApi should no longer implement IFishingAutomationApi.");
            Assert(!typeof(IFishingAutomationApi).IsAssignableFrom(serviceType), "The native runtime service should no longer be the public fishing API owner.");
            Assert(typeof(IFishingAutomationApi).IsAssignableFrom(compatibilityType), "FishingAutomationCompatibilityAdapter should be the only feature-owned IFishingAutomationApi facade.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object feature = Activator.CreateInstance(featureType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationCompatibilityFeature should be constructable for unit tests.");
                string id = (string)(featureType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature) ?? string.Empty);
                object? service = featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature);
                Assert(id == "FishingAutomationCompatibility", "The frozen facade must publish an explicitly compatibility-only feature id.");
                Assert(service == null, "The compatibility feature should not create the executor before an IFishingAutomationApi consumer appears.");

                var bridgeManifest = new ManifestModel
                {
                    Name = "DTMAPI Doloc Town GameBridge",
                    Author = "DTMAPI",
                    Version = DtmApiRuntime.ApiVersion,
                    UniqueID = "DTMAPI.GameBridge.DolocTown",
                    Type = "RuntimeApi"
                };
                featureType.GetMethod("RegisterApis", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(feature, new object[] { bridgeManifest });
                IFishingAutomationApi api = GetModRegistry(runtime).GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("FishingAutomationCompatibilityFeature should register the facade API.");
                Assert(featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature) == null, "RegisterApis should keep FishingAutomation service inactive.");
                Assert(api.GetStatus("DTMAPI.Tests.AutoFishing").Status == "inactive/no-consumer", "Facade status should report inactive/no-consumer before Configure.");

                var owner = new ManifestModel
                {
                    Name = "AutoFishing Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing"
                };
                api.Configure(owner, new FishingAutomationOptions());
                service = featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature);
                Assert(service == null, "Configure should only store compatibility policy and must not activate the LegacyFishingAutomationService runtime or hooks.");
                Assert(api.GetStatus(owner.UniqueID).Status == "configured", "Configure should report a configured consumer before it is enabled.");
                api.SetEnabled(owner, true, "unit-test");
                service = featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature);
                Assert(service != null, "First SetEnabled(true) should activate the internal native runtime service behind the compatibility adapter.");
                object? callbackRuntime = featureType.GetProperty("CallbackRuntime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature);
                Assert(callbackRuntime == null, "Pre-Initialize compatibility activation may retain owner state, but callback publication must remain fail-closed until the exact Harmony inventory is ready.");
                Assert(api.GetStatus(owner.UniqueID).Status == "active", "SetEnabled(true) should report active.");

                var competingOwner = new ManifestModel
                {
                    Name = "Competing Frozen Fishing Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing.Competing"
                };
                api.Configure(competingOwner, new FishingAutomationOptions());
                api.SetEnabled(competingOwner, true, "unit-test-busy");
                FishingAutomationState competingState = api.GetState(competingOwner.UniqueID);
                Assert(!competingState.Enabled && competingState.Phase == "Busy" && competingState.LastReason.Contains(owner.UniqueID, StringComparison.Ordinal),
                    "The frozen compatibility executor must reject a second enabled owner deterministically.");
                Assert(runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 2 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(competingOwner.UniqueID) == 0,
                    "Only the owner whose enable request was accepted may retain the compatibility hook/updater demand roots.");
                api.SetEnabled(owner, false, "unit-test-disable");
                Assert(api.GetStatus(owner.UniqueID).Status == "disabled/consumer-present", "SetEnabled(false) should report a disabled consumer while retaining policy.");
                Assert(CompatibilityHostBroker.For(runtime).TryGetService("FishingAutomation", out object? residentBackend) && residentBackend != null &&
                    CompatibilityHostBroker.ReadProperty(residentBackend, "ConfiguredOwnerCount", -1) == 0 &&
                    CompatibilityHostBroker.ReadProperty(residentBackend, "OwnerStateCount", -1) == 0,
                    "Disabling the last enabled compatibility owner must clear owner policy/state from the process-resident Host backend before dropping the proxy.");
                Assert(featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature) == null &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(competingOwner.UniqueID) == 0,
                    "Disabling the accepted owner after a busy rejection must leave no stale compatibility demand or executor.");

                var retryOwner = new ManifestModel
                {
                    Name = "Compatibility Cleanup Retry",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing.CleanupRetry"
                };
                api.Configure(retryOwner, new FishingAutomationOptions());
                api.SetEnabled(retryOwner, true, "unit-test-cleanup-retry");
                Assert(residentBackend != null &&
                    CompatibilityHostBroker.ReadProperty(residentBackend, "ConfiguredOwnerCount", -1) == 1 &&
                    CompatibilityHostBroker.ReadProperty(residentBackend, "OwnerStateCount", -1) == 1,
                    "Reactivating the resident Host for a new owner must rebuild only that owner and must not expose earlier owner state.");
                var failingHarmony = new FakeHarmonyUnpatcher { ThrowOnUnpatch = true };
                var failingPatcher = new HarmonyReflectionPatcher(runtime, "dtmapi.gamebridge.doloctown.fishing.compatibility");
                SetPrivateField(failingPatcher, "harmony", failingHarmony);
                SetPrivateField(failingPatcher, "harmonyType", typeof(FakeHarmonyUnpatcher));
                SetPrivateField(feature, "patcher", failingPatcher);
                bool cleanupFailed = false;
                try
                {
                    api.SetEnabled(retryOwner, false, "unit-test-unpatch-failure");
                }
                catch (InvalidOperationException ex)
                {
                    cleanupFailed = ex.Message.Contains("cleanup failed", StringComparison.OrdinalIgnoreCase);
                }
                Assert(cleanupFailed &&
                    featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature) != null &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(retryOwner.UniqueID) == 0,
                    "An exact-owner Harmony unpatch failure must propagate and retain a retryable compatibility cleanup tombstone without retaining logical demand.");
                failingHarmony.ThrowOnUnpatch = false;
                api.SetEnabled(retryOwner, false, "unit-test-unpatch-retry");
                Assert(featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature) == null &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(retryOwner.UniqueID) == 0 && failingHarmony.UnpatchCalls == 2 &&
                    residentBackend != null && CompatibilityHostBroker.ReadProperty(residentBackend, "ConfiguredOwnerCount", -1) == 0 &&
                    CompatibilityHostBroker.ReadProperty(residentBackend, "OwnerStateCount", -1) == 0,
                    "The next compatibility cleanup pass must retry the same exact Harmony owner and release the tombstone only after success.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void LegacyFishingCompatibilityIsObsoleteFrozenAndHasNoProductConsumers()
        {
            ObsoleteAttribute apiObsolete = typeof(IFishingAutomationApi).GetCustomAttribute<ObsoleteAttribute>()
                ?? throw new InvalidOperationException("IFishingAutomationApi should carry an Obsolete warning.");
            Assert(!apiObsolete.IsError && (apiObsolete.Message ?? string.Empty).Contains("deprecated and frozen", StringComparison.OrdinalIgnoreCase), "The legacy fishing API warning should be non-breaking and explicitly frozen.");
            DtmApiStatusAttribute status = typeof(IFishingAutomationApi).GetCustomAttribute<DtmApiStatusAttribute>()
                ?? throw new InvalidOperationException("IFishingAutomationApi should retain its Experimental status metadata.");
            Assert(status.Status == DtmApiStatus.Experimental && (status.Notes ?? string.Empty).Contains("Deprecated/Frozen", StringComparison.Ordinal), "The legacy fishing API must remain Experimental while documentation marks it Deprecated/Frozen.");
            foreach (Type type in new[] { typeof(FishingBiteWaitMode), typeof(FishingResultMode), typeof(FishingAnimationMode), typeof(FishingAutomationOptions), typeof(FishingAutomationState) })
                Assert(type.GetCustomAttribute<ObsoleteAttribute>()?.IsError == false, type.Name + " should carry the same non-error compatibility warning.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var host = new FakeHost(NewTempGameDir());
                var runtime = new DtmApiRuntime(host, new ConfigMenuRegistry());
                runtime.Start();
                _ = new DolocTownGameBridge(runtime);
                IFishingAutomationApi api = runtime.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown")!;
                var owner = new ManifestModel { Name = "FrozenLegacy", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.FrozenLegacy", Type = "CodeMod" };
                api.Configure(owner, new FishingAutomationOptions());
                api.Configure(owner, new FishingAutomationOptions());
                api.SetEnabled(owner, false, "warning-once");
                string warning = host.Warnings.Single(message => message.Contains("deprecated IFishingAutomationApi", StringComparison.Ordinal));
                Assert(host.Warnings.Count(message => message.Contains("deprecated IFishingAutomationApi", StringComparison.Ordinal)) == 1, "The compatibility facade should warn once per owner instead of logging every call.");
                Assert(warning.Contains("frozen to new consumers", StringComparison.Ordinal) &&
                    warning.Contains("existing binary compatibility is retained until a separately approved breaking change", StringComparison.Ordinal) &&
                    warning.Contains("does not replace or reopen this public API", StringComparison.Ordinal),
                    "The runtime warning must preserve the frozen no-new-consumer and separately-approved-breaking-change boundary.");
                Assert(!warning.Contains("0.5.5", StringComparison.Ordinal) && !warning.Contains("0.6.0", StringComparison.Ordinal),
                    "The frozen compatibility warning must not promise a fixed removal or migration release.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }

            string repo = FindRepositoryRoot();
            var violations = new List<string>();
            foreach (string directoryName in new[] { "src", "products", "first-party-mods", Path.Combine("author-sdk", "samples", "api-demand") })
            {
                string directory = Path.Combine(repo, directoryName);
                if (!Directory.Exists(directory))
                    continue;
                foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
                {
                    string normalized = file.Replace('\\', '/');
                    if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase) || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
                        normalized.EndsWith("/src/DTMAPI.Abstractions/ExperimentalGameBridge.cs", StringComparison.OrdinalIgnoreCase) ||
                        normalized.Contains("/src/DTMAPI.InstallDoctor/", StringComparison.OrdinalIgnoreCase) ||
                        normalized.Contains("/Compatibility/FishingAutomation/", StringComparison.OrdinalIgnoreCase) ||
                        normalized.EndsWith("/products/first-party/AutoFishing/qa/batch6/Batch6AutoFishingCompatibilityDriver.cs", StringComparison.OrdinalIgnoreCase) ||
                        normalized.EndsWith("/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/LegacyFishingAutomationCompatibilityFixtureCase.cs", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (File.ReadAllText(file).Contains("IFishingAutomationApi", StringComparison.Ordinal))
                        violations.Add(Path.GetRelativePath(repo, file));
                }
            }
            Assert(violations.Count == 0, "No product may consume frozen IFishingAutomationApi; unexpected consumers=" + string.Join(",", violations) + ".");
        }

        private static void FishingAnimationMultiplierUsesFullTrajectoryPreservingScale()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var service = new LegacyFishingAutomationService(runtime);
                var owner = new ManifestModel { Name = "Animation", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.FishingAnimation", Type = "CodeMod" };
                service.Configure(owner, new FishingAutomationOptions
                {
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 4,
                    CastChargeRatio = 0
                });
                service.SetEnabled(owner, true, "animation-unit");
                object backend = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type backendType = backend.GetType();

                float duration = 8f;
                service.AdjustFishingPullDurationResult(ref duration, "unit-pull");
                Assert(Math.Abs(duration - 2f) < 0.0001f, "Pull duration should divide by the full multiplier even when first-party casting uses zero charge.");

                var hook = new FakeFishingHook { Velocity = new FakeVector2(2f, 3f) };
                MethodInfo scaleVector = backendType.GetMethod("TryScaleVector2Member", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Fishing velocity scaler should exist.");
                var samples = new List<string>();
                int velocityChanged = (int)(scaleVector.Invoke(null, new object[] { hook, "Velocity", 4d, "hook.Velocity", samples }) ?? 0);
                Assert(velocityChanged == 1 && Math.Abs(hook.Velocity.x - 8f) < 0.0001f && Math.Abs(hook.Velocity.y - 12f) < 0.0001f, "Hook velocity should multiply by the full configured multiplier.");

                var rigidbody = new FakeFishingRigidbody { gravityScale = 2f };
                MethodInfo scaleGravity = backendType.GetMethod("TryScaleHookGravity", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Fishing gravity scaler should exist.");
                int gravityChanged = (int)(scaleGravity.Invoke(backend, new object[] { rigidbody, 4d, samples }) ?? 0);
                Assert(gravityChanged == 1 && Math.Abs(rigidbody.gravityScale - 32f) < 0.0001f, "Hook gravity should multiply by m² so the scaled flight keeps its native trajectory.");
                service.RestoreExperimentalAnimatorSpeeds("animation-unit-restore");
                Assert(Math.Abs(rigidbody.gravityScale - 2f) < 0.0001f, "Animation lease cleanup should restore the original hook gravity.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void LegacyFishingAutomationServiceFailureThrottleRecordsOneDiagnosticPerOperation()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type serviceType = service.GetType();
                MethodInfo recordFailure = serviceType.GetMethod("RecordFishingAutomationFailure", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService failure throttle helper should exist.");

                for (int i = 0; i < 6; i++)
                {
                    recordFailure.Invoke(service, new object?[]
                    {
                        "FishingAutomation.MiniGame.Update",
                        new InvalidOperationException("mini-game-boom"),
                        true,
                        "Smoke.AutoFishingMiniGameComplete",
                        "FishingGameScrollBar.UpdateGame Postfix"
                    });
                }

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.FishingAutomation") == 1, "Repeated FishingAutomation service failures for the same operation should record only the first diagnostics error.");
                IHookStatusInfo hookStatus = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Smoke.AutoFishingMiniGameComplete");
                Assert(hookStatus.Details.Contains("failureCount=3", StringComparison.Ordinal), "Repeated high-frequency FishingAutomation failures should publish hook status only through the short-warning limit.");
                Assert(!hookStatus.Details.Contains("failureCount=6", StringComparison.Ordinal), "Suppressed high-frequency FishingAutomation failures should not rewrite hook status on every repeat.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void LegacyFishingAutomationServiceFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type serviceType = service.GetType();
                MethodInfo recordFailure = serviceType.GetMethod("RecordFishingAutomationFailure", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService failure throttle helper should exist.");
                MethodInfo recordSuccess = serviceType.GetMethod("RecordFishingAutomationSuccess", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService success recovery helper should exist.");
                IDictionary failureEpisodes = (IDictionary)(serviceType.GetField("fishingAutomationFailures", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep service failure throttle state."));

                for (int i = 0; i < 6; i++)
                    recordFailure.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update", new InvalidOperationException("mini-game-boom"), true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Postfix" });

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.FishingAutomation") == 1, "Initial repeated FishingAutomation failures should record one diagnostics error.");
                recordSuccess.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update" });
                recordSuccess.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update" });
                Assert(failureEpisodes.Count == 1, "Two stable FishingAutomation successes should not clear a failure episode yet.");
                recordSuccess.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update" });
                Assert(failureEpisodes.Count == 0, "Three stable FishingAutomation successes should clear the failure episode.");

                recordFailure.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update", new InvalidOperationException("mini-game-boom-again"), true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Postfix" });
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.FishingAutomation") == 2, "A post-recovery FishingAutomation failure should start a new diagnostics episode.");
                IHookStatusInfo hookStatus = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Smoke.AutoFishingMiniGameComplete");
                Assert(hookStatus.Details.Contains("failureCount=1", StringComparison.Ordinal), "Post-recovery FishingAutomation failure should publish as a fresh first failure.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationSuccessDiagnosticsThrottleRepeatedHookStatus()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type serviceType = service.GetType();
                MethodInfo publishHookStatus = serviceType.GetMethod("PublishFishingAutomationHookStatus", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService success diagnostic publisher should exist.");
                IDictionary diagnosticStates = (IDictionary)(serviceType.GetField("fishingAutomationDiagnostics", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep success diagnostic throttle state."));

                for (int i = 0; i < 20; i++)
                {
                    publishHookStatus.Invoke(service, new object[]
                    {
                        "Smoke.AutoFishingReadyChargeSpeed",
                        "verified",
                        "AgentStateFishingReady.OnPlay",
                        "iteration=" + i.ToString(CultureInfo.InvariantCulture),
                        "ReadyChargeSpeed.Verified"
                    });
                }

                IHookStatusInfo hookStatus = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Smoke.AutoFishingReadyChargeSpeed");
                Assert(hookStatus.Details.Contains("iteration=0", StringComparison.Ordinal), "Repeated AutoFishing success diagnostics should publish the first state-change sample only.");
                Assert(!hookStatus.Details.Contains("iteration=19", StringComparison.Ordinal), "Suppressed AutoFishing success diagnostics should not rewrite hook status on every repeat.");
                Assert(diagnosticStates.Count == 1, "AutoFishing success diagnostics should track one throttle entry for one hook/source key.");
                object diagnosticState = diagnosticStates.Values.Cast<object>().Single();
                int observed = (int)(diagnosticState.GetType().GetProperty("ObservedCount")?.GetValue(diagnosticState) ?? -1);
                int published = (int)(diagnosticState.GetType().GetProperty("PublishedCount")?.GetValue(diagnosticState) ?? -1);
                Assert(observed == 20, "AutoFishing success diagnostic throttle should count every observed repeated success.");
                Assert(published == 1, "AutoFishing success diagnostic throttle should publish only the initial state-change sample.");

                publishHookStatus.Invoke(service, new object[]
                {
                    "Smoke.AutoFishingReadyChargeSpeed",
                    "pending",
                    "AgentStateFishingReady.OnPlay",
                    "iteration=status-change",
                    "ReadyChargeSpeed.Verified"
                });

                IHookStatusInfo changed = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Smoke.AutoFishingReadyChargeSpeed");
                Assert(changed.Status == "pending" && changed.Details.Contains("iteration=status-change", StringComparison.Ordinal), "AutoFishing success diagnostic throttle should still publish status changes immediately.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationRuntimeStateResetClearsTransientState()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type serviceType = service.GetType();

                object miniGameHandle = new object();
                var animator = new FakeAnimator { speed = 3.0 };
                IDictionary miniGameStartedAt = (IDictionary)(serviceType.GetField("fishingMiniGameStartedAt", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep mini-game handle state."));
                object loggedPhases = serviceType.GetField("loggedFishingPhases", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep phase log cooldown state.");
                IDictionary failureEpisodes = (IDictionary)(serviceType.GetField("fishingAutomationFailures", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep service failure throttle state."));
                IDictionary diagnosticStates = (IDictionary)(serviceType.GetField("fishingAutomationDiagnostics", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep success diagnostic throttle state."));
                IDictionary animatorSpeeds = (IDictionary)(serviceType.GetField("originalAnimatorSpeeds", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep original animator speed state."));

                miniGameStartedAt[miniGameHandle] = DateTimeOffset.UtcNow;
                loggedPhases.GetType().GetMethod("Add", new[] { typeof(string) })?.Invoke(loggedPhases, new object[] { "Pull" });
                MethodInfo recordFailure = serviceType.GetMethod("RecordFishingAutomationFailure", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService failure throttle helper should exist.");
                recordFailure.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update", new InvalidOperationException("mini-game-boom"), true, null, null });
                MethodInfo publishHookStatus = serviceType.GetMethod("PublishFishingAutomationHookStatus", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService success diagnostic publisher should exist.");
                publishHookStatus.Invoke(service, new object[] { "Smoke.AutoFishingReadyChargeSpeed", "verified", "AgentStateFishingReady.OnPlay", "iteration=reset", "ReadyChargeSpeed.Verified" });
                animatorSpeeds[animator] = 1.25;

                MethodInfo reset = serviceType.GetMethod("ResetFishingRuntimeState", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService reset helper should exist.");
                reset.Invoke(service, new object[] { "unit-test-reset" });

                Assert(miniGameStartedAt.Count == 0, "FishingAutomation reset should clear mini-game handle state.");
                int loggedPhaseCount = (int)(loggedPhases.GetType().GetProperty("Count")?.GetValue(loggedPhases) ?? -1);
                Assert(loggedPhaseCount == 0, "FishingAutomation reset should clear phase log cooldown state.");
                Assert(failureEpisodes.Count == 0, "FishingAutomation reset should clear service failure throttle state.");
                Assert(diagnosticStates.Count == 0, "FishingAutomation reset should clear success diagnostic throttle state.");
                Assert(animatorSpeeds.Count == 0 && Math.Abs(animator.speed - 1.25) < 0.0001, "FishingAutomation reset should restore and clear animator speed snapshots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationLifecycleSummaryClassifiesOwnerAndTransientState()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var service = new LegacyFishingAutomationService(runtime);
                IManifest owner = new ManifestModel
                {
                    Name = "AutoFishing Lifecycle Test",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishingLifecycle"
                };

                service.Configure(owner, new FishingAutomationOptions
                {
                    CastChargeRatio = 0,
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 3
                });
                service.SetEnabled(owner, true, "unit-enable");
                object backend = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");

                object miniGameHandle = new object();
                GetPrivateField<IDictionary>(backend, "fishingMiniGameStartedAt")[miniGameHandle] = DateTimeOffset.UtcNow;
                GetPrivateField<HashSet<object>>(backend, "fishingReadyChargeTargetStates").Add(new object());
                var animator = new FakeAnimator { speed = 4 };
                GetPrivateField<IDictionary>(backend, "originalAnimatorSpeeds")[animator] = 1.25d;

                LegacyFishingAutomationService.FishingAutomationLifecycleSnapshot before = service.GetFishingAutomationLifecycleSnapshot("unit-before-disable");
                Assert(before.OptionOwnerCount == 1 && before.StateOwnerCount == 1 && before.EnabledOwnerCount == 1, "AutoFishing lifecycle summary should keep owner policy/state separate from transient native handles.");
                Assert(before.NativeTransientHandleCount >= 3 && before.BoundaryClearCount >= 3, "AutoFishing lifecycle summary should count native object keyed transient state.");
                Assert(before.FormatShortSummary().Contains("charge=0", StringComparison.Ordinal) && before.FormatShortSummary().Contains("fast=True", StringComparison.Ordinal), "AutoFishing lifecycle summary should expose runtime charge and fast-animation settings for smoke evidence.");

                service.SetEnabled(owner, false, "unit-disable");

                LegacyFishingAutomationService.FishingAutomationLifecycleSnapshot after = service.GetFishingAutomationLifecycleSnapshot("unit-after-disable");
                Assert(after.OptionOwnerCount == 1 && after.StateOwnerCount == 1 && after.EnabledOwnerCount == 0, "AutoFishing disable should retain owner config/state while disabling automation.");
                Assert(after.NativeTransientHandleCount == 0 && after.BoundaryClearCount == 0, "AutoFishing disable boundary should clear native object keyed transient state.");
                Assert(GetPrivateField<IDictionary>(backend, "originalAnimatorSpeeds").Count == 0 && Math.Abs(animator.speed - 1.25) < 0.0001, "AutoFishing lifecycle disable should restore animator snapshots without destroying native objects.");
                IHookStatusInfo lifecycle = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Fishing.Automation.Lifecycle");
                Assert(lifecycle.Status == "ok" && lifecycle.Details.Contains("expectation=transient-clear", StringComparison.Ordinal), "AutoFishing lifecycle boundary should publish an ok transient-clear diagnostic after disable.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationPendingCastWatchdogPublishesStallAndClearsOnProgress()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var service = new LegacyFishingAutomationService(runtime);
                object backend = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type backendType = backend.GetType();
                MethodInfo markPending = backendType.GetMethod("MarkFishingAutoCastPending", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep a pending-cast marker.");
                MethodInfo checkWatchdog = backendType.GetMethod("CheckFishingAutoCastWatchdog", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep a pending-cast watchdog.");

                DateTimeOffset startedAt = DateTimeOffset.UtcNow;
                markPending.Invoke(backend, new object[] { "DTMAPI.Tests.AutoFishing", "unit-cast", "Ready", startedAt });
                checkWatchdog.Invoke(backend, new object[] { startedAt.AddSeconds(4) });
                Assert(!service.HasPendingFishingAutoCast, "FishingAutomation stalled pending-cast state should release the pending marker and enter a short retry backoff instead of stopping permanently.");
                Assert(service.FishingAutoCastPendingStallCount == 1, "FishingAutomation pending-cast watchdog should count one stall diagnostic.");
                Assert(service.FishingAutoCastApplicationCount == 0, "FishingAutomation stalled pending-cast state should not count as a confirmed auto-cast.");
                IHookStatusInfo stalled = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Fishing.Automation.AutoCastWatchdog");
                Assert(stalled.Status == "needs-review" && stalled.Details.Contains("unit-cast", StringComparison.Ordinal), "FishingAutomation pending-cast watchdog should publish a needs-review diagnostic with the cast summary.");
                Assert(service.LastFishingAutoCastAttemptSummary.Contains("backoffSeconds=", StringComparison.Ordinal), "FishingAutomation stalled pending-cast summary should explain the retry backoff.");

                markPending.Invoke(backend, new object[] { "DTMAPI.Tests.AutoFishing", "unit-cast-progress", "Ready", startedAt });
                service.NotifyFishingPhase("Cast", new object());
                Assert(!service.HasPendingFishingAutoCast, "FishingAutomation real phase progression should clear pending auto-cast state.");
                Assert(service.FishingAutoCastApplicationCount == 1, "FishingAutomation should count auto-cast only after real fishing phase progression confirms the native call.");
                Assert(service.LastFishingAutomationApplicationSummary.Contains("confirmedBy=phase=Cast", StringComparison.Ordinal), "FishingAutomation confirmed auto-cast summary should name the native phase that confirmed it.");
                IHookStatusInfo cleared = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Fishing.Automation.AutoCastWatchdog");
                Assert(cleared.Status == "cleared" && cleared.Details.Contains("phase=Cast", StringComparison.Ordinal), "FishingAutomation phase progress should publish a cleared watchdog status.");

                markPending.Invoke(backend, new object[] { "DTMAPI.Tests.AutoFishing", "unit-cast-reset", "Ready", startedAt });
                service.ResetFishingRuntimeState("unit-reset");
                Assert(!service.HasPendingFishingAutoCast, "FishingAutomation reset should clear pending auto-cast state.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationStaleWaitDoesNotConfirmPendingCast()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var service = new LegacyFishingAutomationService(runtime);
                object backend = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type backendType = backend.GetType();
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Stale Wait Test",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishingStaleWait"
                };
                service.Configure(owner, new FishingAutomationOptions
                {
                    BiteWaitMode = FishingBiteWaitMode.InstantNativeBite,
                    ResultMode = FishingResultMode.AutoCompleteVisibleMiniGame
                });
                service.SetEnabled(owner, true, "unit-test");

                MethodInfo markPending = backendType.GetMethod("MarkFishingAutoCastPending", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep a pending-cast marker.");
                MethodInfo applyWait = backendType.GetMethod("ApplyFishingWaitAutomation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(object), typeof(string) }, null)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should expose the wait automation helper.");

                var stateManager = new FakeFishingStateManager();
                var staleWait = new FakeFishingWaitStateWithInstantBite(stateManager, new FakeFishProto { Id = "unit_stale_fish", IsFish = true });
                stateManager.current = new object();
                markPending.Invoke(backend, new object[] { owner.UniqueID, "unit-stale-cast", "Ready", DateTimeOffset.UtcNow });

                bool applied = (bool)(applyWait.Invoke(backend, new object[] { staleWait, "AgentStateFishingWait.OnPlay Postfix" }) ?? true);
                Assert(!applied, "FishingAutomation stale Wait.OnPlay should not apply automation.");
                Assert(service.HasPendingFishingAutoCast, "FishingAutomation stale Wait.OnPlay must not clear pending auto-cast confirmation.");
                Assert(service.FishingAutoCastApplicationCount == 0, "FishingAutomation stale Wait.OnPlay must not count as confirmed native phase progression.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationHooksNotInstalledDoesNotCreatePendingCast()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var service = new LegacyFishingAutomationService(runtime);
                IManifest owner = new ManifestModel
                {
                    Name = "AutoFishing Hooks Test",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishingHooks"
                };

                service.Configure(owner, new FishingAutomationOptions());
                service.SetEnabled(owner, true, "unit-test");
                service.Update();

                FishingAutomationState state = service.GetState(owner.UniqueID);
                Assert(!service.HasPendingFishingAutoCast, "FishingAutomation should not create a pending-cast marker before fishing hooks are installed.");
                Assert(state.Phase == "WaitingHooks", "FishingAutomation should report WaitingHooks while configured but hook installation is unavailable.");
                Assert(service.LastFishingAutoCastAttemptSummary.Contains("skipped=hooks-not-installed", StringComparison.Ordinal), "FishingAutomation should explain hook-gated auto-cast skips.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationDisableClearsTransientHandles()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var service = new LegacyFishingAutomationService(runtime);
                object backend = CompatibilityHostBroker.For(runtime).GetService("FishingAutomation");
                Type backendType = backend.GetType();
                IManifest owner = new ManifestModel
                {
                    Name = "AutoFishing Disable Test",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishingDisable"
                };
                service.Configure(owner, new FishingAutomationOptions());
                service.SetEnabled(owner, true, "unit-enable");

                MethodInfo markPending = backendType.GetMethod("MarkFishingAutoCastPending", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep a pending-cast marker.");
                markPending.Invoke(backend, new object[] { owner.UniqueID, "unit-cast", "Ready", DateTimeOffset.UtcNow });
                GetPrivateField<IDictionary>(backend, "fishingMiniGameStartedAt")[new object()] = DateTimeOffset.UtcNow;
                Type inputStatsType = backendType.GetNestedType("FishingMiniGameInputStats", BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Fishing input stats type should exist.");
                GetPrivateField<IDictionary>(backend, "fishingMiniGameInputStats")[new object()] = Activator.CreateInstance(inputStatsType, true);
                GetPrivateField<HashSet<object>>(backend, "fishingReadyChargeAppliedStates").Add(new object());
                var animator = new FakeAnimator { speed = 4 };
                GetPrivateField<IDictionary>(backend, "originalAnimatorSpeeds")[animator] = 1.25d;

                service.SetEnabled(owner, false, "unit-disable");

                Assert(!service.HasPendingFishingAutoCast, "FishingAutomation disable should clear pending auto-cast state.");
                Assert(GetPrivateField<IDictionary>(backend, "fishingMiniGameStartedAt").Count == 0, "FishingAutomation disable should clear mini-game handles.");
                Assert(GetPrivateField<IDictionary>(backend, "fishingMiniGameInputStats").Count == 0, "FishingAutomation disable should clear mini-game input handles.");
                Assert(GetPrivateField<HashSet<object>>(backend, "fishingReadyChargeAppliedStates").Count == 0, "FishingAutomation disable should clear Ready charge handles.");
                Assert(GetPrivateField<IDictionary>(backend, "originalAnimatorSpeeds").Count == 0 && Math.Abs(animator.speed - 1.25) < 0.0001, "FishingAutomation disable should restore and clear animation speed snapshots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationBiteActionPrecedence()
        {
            Type serviceType = LoadFishingCompatibilityBackendType();
            MethodInfo resolve = serviceType.GetMethod("ResolveFishingBiteAutomationAction", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep an internal bite-action resolver.");

            string defaultFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions(), true, false })) ?? string.Empty;
            string defaultNonFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions(), false, false })) ?? string.Empty;
            string skipFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions { ResultMode = FishingResultMode.SkipMiniGameNativeResult }, true, true })) ?? string.Empty;
            string skipNonFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions { ResultMode = FishingResultMode.SkipMiniGameNativeResult }, false, false })) ?? string.Empty;
            string instantComplete = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions { BiteWaitMode = FishingBiteWaitMode.InstantNativeBite }, true, true })) ?? string.Empty;

            Assert(defaultFish == "NativeReel", "FishingAutomation default should reel bite-ready fish into the native minigame/result path.");
            Assert(defaultNonFish == "NativeReel", "FishingAutomation default should reel non-fish results through the native result path.");
            Assert(skipFish == "SkipMiniGameNativeResult", "FishingAutomation SkipMiniGame should use the native no-minigame result route for fish.");
            Assert(skipNonFish == "SkipMiniGameNativeResult", "FishingAutomation SkipMiniGame should use the native result route for non-fish.");
            Assert(instantComplete == "NativeReel", "FishingAutomation InstantBite should change wait timing only; result routing stays the normal native reel path.");
        }

        private static void FishingAutomationMiniGameInputDecisionMatchesNativeBars()
        {
            Type serviceType = LoadFishingCompatibilityBackendType();
            MethodInfo resolve = serviceType.GetMethod("ResolveFishingMiniGameInputDecision", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep a minigame input decision helper.");

            string stable = Convert.ToString(resolve.Invoke(null, new object?[] { "Stable", 2.0, 1.5, 3.0, false })) ?? string.Empty;
            string beforeStable = Convert.ToString(resolve.Invoke(null, new object?[] { "Stable", 1.0, 1.5, 3.0, false })) ?? string.Empty;
            string bonusFirst = Convert.ToString(resolve.Invoke(null, new object?[] { "Bonus", 2.0, 1.5, 3.0, false })) ?? string.Empty;
            string bonusAfterTap = Convert.ToString(resolve.Invoke(null, new object?[] { "Bonus", 2.1, 1.5, 3.0, true })) ?? string.Empty;
            string delay = Convert.ToString(resolve.Invoke(null, new object?[] { "Delay", 0.5, 0.0, 1.0, false })) ?? string.Empty;

            Assert(stable == "HoldStable", "FishingAutomation minigame should hold during native green/stable bars.");
            Assert(beforeStable == "Release", "FishingAutomation minigame should release during red/off-note time before the next stable bar.");
            Assert(bonusFirst == "TapBonus", "FishingAutomation minigame should short-press a native yellow/bonus bar once.");
            Assert(bonusAfterTap == "Release", "FishingAutomation minigame should release after the yellow/bonus tap.");
            Assert(delay == "Release", "FishingAutomation minigame should not press during native delay notes.");
        }

        private static void FishingAutomationOptionsNormalizeNativeStageDefaults()
        {
            Type serviceType = LoadFishingCompatibilityBackendType();
            MethodInfo normalize = serviceType.GetMethod("NormalizeFishingAutomationOptions", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep an options normalize helper.");

            FishingAutomationOptions defaults = (FishingAutomationOptions)(normalize.Invoke(null, new object?[] { null }) ?? throw new InvalidOperationException("Normalize should return options."));
            var invalid = new FishingAutomationOptions
            {
                BiteWaitMode = (FishingBiteWaitMode)999,
                ResultMode = (FishingResultMode)999,
                AnimationMode = (FishingAnimationMode)999,
                RecastDelaySeconds = -10,
                AnimationMultiplier = 100,
                CastChargeRatio = 2,
                StopOnManualMove = false
            };
            FishingAutomationOptions normalizedInvalid = (FishingAutomationOptions)(normalize.Invoke(null, new object?[] { invalid }) ?? throw new InvalidOperationException("Normalize should return options."));

            PropertyInfo stopOnManualMove = typeof(FishingAutomationOptions).GetProperty("StopOnManualMove")
                ?? throw new InvalidOperationException("FishingAutomationOptions must retain StopOnManualMove for old Workshop binaries.");
            Assert(stopOnManualMove.GetCustomAttribute<ObsoleteAttribute>()?.IsError == false, "StopOnManualMove should remain callable with a non-error obsolete marker.");
            Assert(defaults.StopOnManualMove, "FishingAutomationOptions should retain the historical StopOnManualMove=true default.");
            Assert(defaults.BiteWaitMode == FishingBiteWaitMode.NativeWait, "FishingAutomation default wait policy should be native wait.");
            Assert(defaults.ResultMode == FishingResultMode.AutoCompleteVisibleMiniGame, "FishingAutomation default result policy should auto-complete the visible native minigame.");
            Assert(defaults.AnimationMode == FishingAnimationMode.Normal, "FishingAutomation default animation policy should be normal speed.");
            Assert(Math.Abs(defaults.RecastDelaySeconds - 0.25) < 0.0001, "FishingAutomation default recast delay should preserve the native-loop cadence.");
            Assert(Math.Abs(defaults.AnimationMultiplier - 3) < 0.0001, "FishingAutomation default fast-animation multiplier should remain three.");
            Assert(Math.Abs(defaults.CastChargeRatio) < 0.0001, "FishingAutomation default cast charge should be no charge.");
            Assert(normalizedInvalid.BiteWaitMode == FishingBiteWaitMode.NativeWait, "Invalid bite wait mode should normalize to native wait.");
            Assert(normalizedInvalid.ResultMode == FishingResultMode.AutoCompleteVisibleMiniGame, "Invalid result mode should normalize to visible minigame completion.");
            Assert(normalizedInvalid.AnimationMode == FishingAnimationMode.Normal, "Invalid animation mode should normalize to normal speed.");
            Assert(Math.Abs(normalizedInvalid.RecastDelaySeconds - 0.05) < 0.0001, "FishingAutomation recast delay should clamp to the supported minimum.");
            Assert(Math.Abs(normalizedInvalid.AnimationMultiplier - 4) < 0.0001, "FishingAutomation animation multiplier should clamp to the supported maximum.");
            Assert(Math.Abs(normalizedInvalid.CastChargeRatio - 1) < 0.0001, "FishingAutomation cast charge should clamp to full charge.");
            Assert(!normalizedInvalid.StopOnManualMove, "FishingAutomation compatibility normalization should retain the old consumer-owned movement policy value.");
        }

        private static void FishingAutomationSkipMiniGamePreservesNativePullResult()
        {
            Type serviceType = LoadFishingCompatibilityBackendType();
            Type actionType = serviceType.GetNestedType("FishingBiteAutomationAction", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep an internal bite-action enum.");
            MethodInfo advance = serviceType.GetMethod("TryAdvanceFishingBite", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep a native bite advance helper.");

            DolocAPI.gameManager = new FakeDolocGameManager { gameInitConfig = new FakeDolocGameInitConfig { skipFishingGame = true } };
            try
            {
                var stateManager = new FakeFishingStateManager();
                var waitState = new FakeFishingWaitState(stateManager, new DolocTown.AgentStateFishingPull { IsFailed = true });
                object skipAction = Enum.Parse(actionType, "SkipMiniGameNativeResult");
                string targetState = Convert.ToString(advance.Invoke(null, new[] { waitState, skipAction })) ?? string.Empty;

                Assert(targetState == "AgentStateFishingPull", "FishingAutomation skip should advance to the native Pull result state.");
                Assert(stateManager.OverwrittenState is DolocTown.AgentStateFishingPull, "FishingAutomation skip should overwrite with the native Pull state returned by NextState.");
                Assert(((DolocTown.AgentStateFishingPull)stateManager.OverwrittenState!).IsFailed, "FishingAutomation skip must preserve the native Pull failure/success result instead of forcing success.");
                Assert(stateManager.ForceFlag == true, "FishingAutomation skip should keep the native state-manager overwrite flag path.");
            }
            finally
            {
                DolocAPI.gameManager = null;
            }
        }

        private static void FishingAutomationMirrorsNativeReelWhenInputEdgeIsAbsent()
        {
            Type serviceType = LoadFishingCompatibilityBackendType();
            Type actionType = serviceType.GetNestedType("FishingBiteAutomationAction", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep an internal bite-action enum.");
            MethodInfo advance = serviceType.GetMethod("TryAdvanceFishingBite", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep a native bite advance helper.");

            DolocAPI.gameManager = new FakeDolocGameManager { gameInitConfig = new FakeDolocGameInitConfig { skipFishingGame = false } };
            DolocAPI.GlobalParameter = new FakeGlobalParameter { FishingEnergyCost = 7 };
            DolocAPI.CostEnergyCalls = 0;
            DolocAPI.LastEnergyCost = 0;
            try
            {
                object nativeReel = Enum.Parse(actionType, "NativeReel");
                var stateManager = new FakeFishingStateManager();
                var waitState = new FakeFishingWaitStateWithNativeMirror(stateManager, new FakeFishProto { Id = "unit_fish", IsFish = true }, 3);
                string battleTarget = Convert.ToString(advance.Invoke(null, new[] { waitState, nativeReel })) ?? string.Empty;

                Assert(battleTarget == "AgentStateFishingBattle", "FishingAutomation should mirror native reel into Battle when NextState stays in Wait because no input edge is present.");
                Assert(stateManager.OverwrittenState is DolocTown.AgentStateFishingBattle, "FishingAutomation should overwrite with the native Battle state for fish when SkipMiniGame is off.");
                Assert(DolocAPI.CostEnergyCalls == 1 && DolocAPI.LastEnergyCost == 7, "FishingAutomation mirrored reel should cost native fishing energy exactly once.");

                object skipAction = Enum.Parse(actionType, "SkipMiniGameNativeResult");
                var skipStateManager = new FakeFishingStateManager();
                var skipWaitState = new FakeFishingWaitStateWithNativeMirror(skipStateManager, new FakeFishProto { Id = "unit_skip_fish", IsFish = true }, 3);
                string pullTarget = Convert.ToString(advance.Invoke(null, new[] { skipWaitState, skipAction })) ?? string.Empty;

                Assert(pullTarget == "AgentStateFishingPull", "FishingAutomation SkipMiniGame should mirror native reel into Pull when no input edge is present.");
                Assert(skipStateManager.OverwrittenState is DolocTown.AgentStateFishingPull pull && !pull.IsFailed, "FishingAutomation SkipMiniGame should use the native successful Pull route for a valid hooked fish.");
                Assert(DolocAPI.gameManager is FakeDolocGameManager manager && !manager.gameInitConfig.skipFishingGame, "FishingAutomation temporary native skip flag should be restored after mirrored reel routing.");
            }
            finally
            {
                DolocAPI.gameManager = null;
                DolocAPI.GlobalParameter = null;
                DolocAPI.CostEnergyCalls = 0;
                DolocAPI.LastEnergyCost = 0;
            }
        }

        private static void FishingAutomationInstantBiteDefersReelUntilWaitPlay()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService")
                ?? throw new InvalidOperationException("LegacyFishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((LegacyFishingAutomationService)service).Configure(owner, new FishingAutomationOptions
                {
                    BiteWaitMode = FishingBiteWaitMode.InstantNativeBite,
                    ResultMode = FishingResultMode.AutoCompleteVisibleMiniGame
                });
                ((LegacyFishingAutomationService)service).SetEnabled(owner, true, "unit-test");

                MethodInfo applyWait = serviceType.GetMethod("ApplyFishingWaitAutomation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(object), typeof(string) }, null)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should expose the wait automation helper.");

                DolocAPI.gameManager = new FakeDolocGameManager { gameInitConfig = new FakeDolocGameInitConfig { skipFishingGame = false } };
                DolocAPI.GlobalParameter = new FakeGlobalParameter { FishingEnergyCost = 7 };
                DolocAPI.CostEnergyCalls = 0;
                DolocAPI.LastEnergyCost = 0;

                var stateManager = new FakeFishingStateManager();
                var waitState = new FakeFishingWaitStateWithInstantBite(stateManager, new FakeFishProto { Id = "unit_instant_fish", IsFish = true });

                bool enterApplied = (bool)(applyWait.Invoke(service, new object[] { waitState, "AgentStateFishingWait.OnEnter Postfix" }) ?? false);
                Assert(!enterApplied, "FishingAutomation InstantBite should not overwrite fishing state from Wait.OnEnter because native Cast->Wait will set current after OnEnter returns.");
                Assert(stateManager.OverwrittenState == null, "FishingAutomation InstantBite Wait.OnEnter should only prepare bite state.");
                Assert(DolocAPI.CostEnergyCalls == 0, "FishingAutomation InstantBite Wait.OnEnter must not consume fishing energy.");
                Assert(!waitState._waitForFishBite && waitState._hasRolled, "FishingAutomation InstantBite Wait.OnEnter should prepare a bite for the next Wait.OnPlay pass.");

                bool playApplied = (bool)(applyWait.Invoke(service, new object[] { waitState, "AgentStateFishingWait.OnPlay Postfix" }) ?? false);
                Assert(playApplied, "FishingAutomation InstantBite should reel from Wait.OnPlay after the native state transition is complete.");
                Assert(stateManager.OverwrittenState is DolocTown.AgentStateFishingBattle, "FishingAutomation InstantBite should advance to the visible native battle/minigame path for fish.");
                Assert(DolocAPI.CostEnergyCalls == 1 && DolocAPI.LastEnergyCost == 7, "FishingAutomation InstantBite should consume native fishing energy exactly once.");
            }
            finally
            {
                DolocAPI.gameManager = null;
                DolocAPI.GlobalParameter = null;
                DolocAPI.CostEnergyCalls = 0;
                DolocAPI.LastEnergyCost = 0;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationAnimationSpeedOnlyRunsOnReadyCastAndPull()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService")
                ?? throw new InvalidOperationException("LegacyFishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((LegacyFishingAutomationService)service).Configure(owner, new FishingAutomationOptions
                {
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 2
                });
                ((LegacyFishingAutomationService)service).SetEnabled(owner, true, "unit-test");
                MethodInfo notify = serviceType.GetMethod("NotifyFishingPhase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should keep phase notification helper.");
                PropertyInfo applicationCount = serviceType.GetProperty("FishingAutomationApplicationCount", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should expose an internal application counter.");

                var readySource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1 } };
                notify.Invoke(service, new object?[] { "Ready", readySource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 0 && Math.Abs(readySource.animator.speed - 1) < 0.0001, "FishingAutomation FastAnimations should not run on the no-charge Ready phase.");

                var castSource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1.25 } };
                notify.Invoke(service, new object?[] { "Cast", castSource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 1 && Math.Abs(castSource.animator.speed - 2.5) < 0.0001, "FishingAutomation FastAnimations should run on Cast phase.");

                var waitSource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1 } };
                notify.Invoke(service, new object?[] { "Wait", waitSource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 1 && Math.Abs(waitSource.animator.speed - 1) < 0.0001, "FishingAutomation FastAnimations must not run on Wait phase.");

                var pullSource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1.5 } };
                notify.Invoke(service, new object?[] { "Pull", pullSource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 2 && Math.Abs(pullSource.animator.speed - 3) < 0.0001, "FishingAutomation FastAnimations should run on Pull phase.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationReadyChargeSpeedTicksNativeCastTimer()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService")
                ?? throw new InvalidOperationException("LegacyFishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((LegacyFishingAutomationService)service).Configure(owner, new FishingAutomationOptions
                {
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 3
                });
                ((LegacyFishingAutomationService)service).SetEnabled(owner, true, "unit-test");
                MethodInfo applyReadyCharge = serviceType.GetMethod("ApplyFishingReadyChargeSpeed", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should expose Ready charge speed helper.");

                var noChargeState = new FakeFishingReadyState();
                applyReadyCharge.Invoke(service, new object?[] { noChargeState });
                Assert(noChargeState._castTimer.TickCount == 0, "FishingAutomation no-charge Ready phase must not tick the native CastTimer.");
                Assert(noChargeState._powerBar.Progress == 0, "FishingAutomation no-charge Ready phase must not advance the progress circle.");

                ((LegacyFishingAutomationService)service).Configure(owner, new FishingAutomationOptions
                {
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 3,
                    CastChargeRatio = 0.5
                });
                var readyState = new FakeFishingReadyState();
                applyReadyCharge.Invoke(service, new object?[] { readyState });

                Assert(readyState._castTimer.TickCount == 1, "FishingAutomation Ready charge speed should tick the native CastTimer.");
                Assert(readyState._castTimer.LastDelta > 0.039 && readyState._castTimer.LastDelta < 0.041, "FishingAutomation Ready charge speed should add (multiplier-1)*fixedDeltaTime to CastTimer.");
                Assert(readyState._powerBar.Progress > 0.039 && readyState._powerBar.Progress < 0.041, "FishingAutomation Ready charge speed should refresh the native progress circle.");
                PropertyInfo lastSummaryProperty = serviceType.GetProperty("LastFishingAnimationSpeedSummary", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should expose its animation diagnostic summary.");
                string firstSummary = (string)(lastSummaryProperty.GetValue(service) ?? string.Empty);
                applyReadyCharge.Invoke(service, new object?[] { readyState });
                string secondSummary = (string)(lastSummaryProperty.GetValue(service) ?? string.Empty);
                Assert(ReferenceEquals(firstSummary, secondSummary), "Ready charge hot ticks after the first application should not rebuild diagnostic strings.");
                Assert(readyState._castTimer.TickCount == 2, "Eliminating Ready hot-path strings must not stop native charge acceleration.");
                for (int i = 0; i < 128; i++)
                    applyReadyCharge.Invoke(service, new object?[] { readyState });
                int readyAccessorBuilds = ((LegacyFishingAutomationService)service).ReadyAccessorBuildCount;
                long beforeAllocated = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 10000; i++)
                    ((LegacyFishingAutomationService)service).ApplyFishingReadyChargeSpeed(readyState);
                long readyAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocated;
                Assert(((LegacyFishingAutomationService)service).ReadyAccessorBuildCount == readyAccessorBuilds, "Warmed Ready charge frames must not rebuild native accessors.");
                Assert(readyAllocated == 0, "Warmed Ready charge frames should allocate zero bytes on the test thread, actual=" + readyAllocated + ".");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationReadyChargeTargetControlsUseToolRelease()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService")
                ?? throw new InvalidOperationException("LegacyFishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((LegacyFishingAutomationService)service).Configure(owner, new FishingAutomationOptions
                {
                    CastChargeRatio = 0.5,
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 3
                });
                ((LegacyFishingAutomationService)service).SetEnabled(owner, true, "unit-test");
                MethodInfo applyReady = serviceType.GetMethod("ApplyFishingReadyAutomation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should expose Ready automation helper.");
                MethodInfo overrideInput = serviceType.GetMethod("TryOverrideFishingReadyChargeInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("LegacyFishingAutomationService should expose Ready input override helper.");

                var readyState = new FakeFishingReadyState();
                applyReady.Invoke(service, new object?[] { readyState });
                object?[] holdArgs = { "NormalUseToolInProgress", false };
                bool holdHandled = (bool)(overrideInput.Invoke(service, holdArgs) ?? false);
                bool holdValue = (bool)(holdArgs[1] ?? false);
                Assert(holdHandled && holdValue, "FishingAutomation should hold the native use-tool input before the configured charge target.");

                readyState._castTimer.Tick(0.6f);
                object?[] releaseArgs = { "NormalUseToolInProgress", false };
                bool releaseHandled = (bool)(overrideInput.Invoke(service, releaseArgs) ?? false);
                bool releaseValue = (bool)(releaseArgs[1] ?? true);
                Assert(releaseHandled && !releaseValue, "FishingAutomation should release the native use-tool input after reaching the configured charge target.");
                double releasedProgress = readyState._castTimer.Progress;
                applyReady.Invoke(service, new object?[] { readyState });
                Assert(Math.Abs(readyState._castTimer.Progress - releasedProgress) < 0.0001, "FastAnimations must stop adding Ready timer progress after the configured target releases, so the backswing cannot change final cast power.");

                ((LegacyFishingAutomationService)service).Configure(owner, new FishingAutomationOptions());
                var noChargeState = new FakeFishingReadyState();
                applyReady.Invoke(service, new object?[] { noChargeState });
                object?[] noChargeReleaseArgs = { "NormalUseToolInProgress", true };
                bool noChargeReleaseHandled = (bool)(overrideInput.Invoke(service, noChargeReleaseArgs) ?? false);
                bool noChargeReleaseValue = (bool)(noChargeReleaseArgs[1] ?? true);
                Assert(noChargeReleaseHandled && !noChargeReleaseValue, "FishingAutomation zero charge should release immediately so native Ready keeps the exact minimum-power target while its own animation gate preserves the backswing.");

                var missingTimerState = new object();
                applyReady.Invoke(service, new object?[] { missingTimerState });
                object?[] missingTimerArgs = { "NormalUseToolInProgress", true };
                bool missingTimerHandled = (bool)(overrideInput.Invoke(service, missingTimerArgs) ?? false);
                bool missingTimerValue = (bool)(missingTimerArgs[1] ?? true);
                Assert(missingTimerHandled && !missingTimerValue, "FishingAutomation should fail open and release Ready input when the native CastTimer cannot be read.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static Type LoadFishingCompatibilityBackendType()
        {
            return LoadCompatibilityBackendType("FishingAutomation");
        }

        private sealed class FakeAnimator
        {
            public double speed { get; set; }
        }

        private sealed class FakeFishingHook
        {
            public FakeVector2 Velocity { get; set; }
        }

        private sealed class FakeFishingRigidbody
        {
            public float gravityScale { get; set; }
        }

        private sealed class FakeFishingAnimationSource
        {
            public FakeAnimator animator { get; set; } = new FakeAnimator();
        }

        private sealed class FakeFishingReadyState
        {
            public FakeCastTimer _castTimer { get; } = new FakeCastTimer();
            public FakeProgressCircle _powerBar { get; } = new FakeProgressCircle();
            public FakeFishingReadyBody body { get; } = new FakeFishingReadyBody();
        }

        private sealed class FakeCastTimer
        {
            public float Progress { get; private set; }
            public int TickCount { get; private set; }
            public double LastDelta { get; private set; }

            public void Tick(float deltaTime)
            {
                TickCount++;
                LastDelta = deltaTime;
                Progress += deltaTime;
            }
        }

        private sealed class FakeProgressCircle
        {
            public float Progress { get; set; }
            public object? Color { get; set; }
        }

        private sealed class FakeFishingReadyBody
        {
            public FakeFishingReadyRodRenderer fishRodRenderer { get; } = new FakeFishingReadyRodRenderer();
        }

        private sealed class FakeFishingReadyRodRenderer
        {
            public string GetCastForceColor(float value)
            {
                return "color:" + value.ToString("0.###");
            }
        }

        private sealed class FakeFishingStateManager
        {
            public object? current;

            public object? OverwrittenState { get; private set; }
            public bool? ForceFlag { get; private set; }

            public void Overwrite(object state, bool force)
            {
                OverwrittenState = state;
                ForceFlag = force;
            }
        }

        private sealed class FakeFishingBody
        {
            public FakeFishingBody(FakeFishingStateManager stateManager)
            {
                StateManager = stateManager;
            }

            public FakeFishingStateManager StateManager { get; }

            public object? FishingCache { get; set; }
        }

        private sealed class FakeFishingWaitState
        {
            private readonly object nextState;

            public FakeFishingWaitState(FakeFishingStateManager stateManager, object nextState)
            {
                body = new FakeFishingBody(stateManager);
                this.nextState = nextState;
            }

            public FakeFishingBody body { get; }

            public object NextState()
            {
                return nextState;
            }
        }

        private sealed class FakeFishingWaitStateWithNativeMirror
        {
            private readonly Dictionary<Type, object> states = new Dictionary<Type, object>();

            public FakeFishingWaitStateWithNativeMirror(FakeFishingStateManager stateManager, object fishProto, double hookDuration)
            {
                body = new FakeFishingBody(stateManager)
                {
                    FishingCache = new FakeFishingCache { FishProto = fishProto }
                };
                _fishOnHookDuration = hookDuration;
            }

            public FakeFishingBody body { get; }

            public double _fishOnHookDuration { get; }

            public object NextState()
            {
                return this;
            }

            public T GetState<T>() where T : class
            {
                Type type = typeof(T);
                if (!states.TryGetValue(type, out object? state))
                {
                    state = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Fake fishing state should be constructable.");
                    states[type] = state;
                }
                return (T)state;
            }
        }

        private sealed class FakeFishingWaitStateWithInstantBite
        {
            private readonly Dictionary<Type, object> states = new Dictionary<Type, object>();

            public FakeFishingWaitStateWithInstantBite(FakeFishingStateManager stateManager, object fishProto)
            {
                body = new FakeFishingBody(stateManager)
                {
                    FishingCache = new FakeFishingCache { FishProto = fishProto }
                };
                _hasRolled = false;
                _hookProbability = 0f;
                _fishOnHookDuration = 0f;
            }

            public FakeFishingBody body { get; }

            public bool _waitForFishBite = true;

            public bool _hasRolled;

            public float _hookProbability;

            public float _fishOnHookDuration;

            public object NextState()
            {
                return this;
            }

            public bool RollFish()
            {
                var cache = body.FishingCache as FakeFishingCache;
                return cache?.FishProto != null;
            }

            public T GetState<T>() where T : class
            {
                Type type = typeof(T);
                if (!states.TryGetValue(type, out object? state))
                {
                    state = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Fake fishing state should be constructable.");
                    states[type] = state;
                }
                return (T)state;
            }
        }

        private sealed class FakeFishingCache
        {
            public object? FishProto { get; set; }
        }

        private sealed class FakeFishProto
        {
            public string Id { get; set; } = string.Empty;

            public bool IsFish { get; set; }
        }
    }
}
