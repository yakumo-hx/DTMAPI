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
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void AutoHarvestUpdaterFollowsOrdinarySaveLifecycle()
        {
            string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "author-sdk", "samples", "api-demand", "AutoHarvest", "ModEntry.cs"));
            Assert(!source.Contains("IInstantSaveDebugApi", StringComparison.Ordinal) &&
                !source.Contains("InstantSaveDebugState", StringComparison.Ordinal),
                "The ordinary AutoHarvest sample must not use Diagnostic save state as gameplay lifecycle authority.");
            RunAutoHarvestColdLoadLifetime();
            RunAutoHarvestHotEnableRequiresColdStart();
        }

        private static void RunAutoHarvestColdLoadLifetime()
        {
            const string ownerId = "Yuuka.DTMAPI.AutoHarvest";
            const string officialId = "Local.DTMAPI_AutoHarvest";
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")
                    ?? throw new InvalidOperationException("AutoHarvest cold-load fixture requires a temporary persistent root.");
                string gameDir = NewTempGameDir();
                WriteAutoHarvestOfficialPackage(persistentRoot);
                WriteOfficialModInfos(persistentRoot, officialId, enabled: true);

                var menu = new ConfigMenuRegistry();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), menu);
                RegisterAutoHarvestTestApis(runtime);
                WriteAutoHarvestConfig(runtime, enabled: true);
                runtime.Start();

                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1,
                    "The AutoHarvest cold-load fixture should load the real product assembly once.");
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 0,
                    "Enabled AutoHarvest must remain asleep until an ordinary SaveLoaded lifecycle event is observed.");

                runtime.NotifyLoadGameRequested(2);
                runtime.NotifySaveLoaded(isNewGame: false);
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 1,
                    "SaveLoaded plus Enabled must move AutoHarvest's automatic updater 0->1.");
                runtime.NotifySaveLoaded(isNewGame: false);
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 1,
                    "Repeated SaveLoaded boundaries must not duplicate AutoHarvest's automatic updater.");

                IConfigMenuRuntime menuRuntime = menu;
                IConfigMenuPage page = menuRuntime.GetPage(ownerId)
                    ?? throw new InvalidOperationException("AutoHarvest config page should be registered.");
                IConfigMenuItem enabledItem = page.Items.First(item => item.Kind == "Bool");
                menuRuntime.BeginEditing(ownerId);
                Assert(enabledItem.TrySetPendingValue("false", out _), "AutoHarvest Enabled should accept false.");
                menuRuntime.Save(ownerId);
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 0,
                    "Disabling AutoHarvest in a loaded save must release its automatic updater 1->0.");
                menuRuntime.BeginEditing(ownerId);
                Assert(enabledItem.TrySetPendingValue("true", out _), "AutoHarvest Enabled should accept true after disable.");
                menuRuntime.Save(ownerId);
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 1,
                    "Re-enabling AutoHarvest in a loaded save must restore exactly one updater.");

                runtime.NotifyReturnedToTitle();
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 0,
                    "ReturnedToTitle must release AutoHarvest's automatic updater 1->0.");
                runtime.NotifyReturnedToTitle();
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 0,
                    "Repeated title boundaries must leave AutoHarvest asleep without duplicate unsubscribe state.");
                runtime.NotifyLoadGameRequested(2);
                runtime.NotifySaveLoaded(isNewGame: false);
                Assert(CountEventOwnerHandlers(runtime, ownerId, "GameLoop.OneSecondUpdateTicked") == 1,
                    "A later SaveLoaded must restore AutoHarvest's updater after title cleanup.");

                WriteOfficialModInfos(persistentRoot, officialId, enabled: false);
                runtime.NotifyWorkshopModListChanged();
                Assert(CountEventOwnerHandlers(runtime, ownerId) == 0 &&
                    runtime.Input.CountOwnerResources(ownerId) == 0 &&
                    menuRuntime.GetPage(ownerId) == null &&
                    CountCoreOwnerRootsForTest(runtime, ownerId) == 0,
                    "Official owner cleanup must reduce all AutoHarvest Event, Input, ConfigPage, facade, and loaded roots to zero.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RunAutoHarvestHotEnableRequiresColdStart()
        {
            const string ownerId = "Yuuka.DTMAPI.AutoHarvest";
            const string officialId = "Local.DTMAPI_AutoHarvest";
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")
                    ?? throw new InvalidOperationException("AutoHarvest hot-load fixture requires a temporary persistent root.");
                string gameDir = NewTempGameDir();
                WriteAutoHarvestOfficialPackage(persistentRoot);
                WriteOfficialModInfos(persistentRoot, officialId, enabled: false);

                var menu = new ConfigMenuRegistry();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), menu);
                RegisterAutoHarvestTestApis(runtime);
                WriteAutoHarvestConfig(runtime, enabled: true);
                runtime.Start();
                runtime.NotifyLoadGameRequested(2);
                runtime.NotifySaveLoaded(isNewGame: false);
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId),
                    "Disabled AutoHarvest should not be resident before the hot-load transition.");

                WriteOfficialModInfos(persistentRoot, officialId, enabled: true);
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) &&
                    runtime.OwnerRequiresRestart(ownerId) &&
                    CountEventOwnerHandlers(runtime, ownerId) == 0 &&
                    runtime.Input.CountOwnerResources(ownerId) == 0,
                    "A newly enabled omitted-kind AutoHarvest must remain cold-start-only and expose restart-required without loading its assembly or owner roots.");
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) &&
                    runtime.OwnerRequiresRestart(ownerId) &&
                    CountEventOwnerHandlers(runtime, ownerId) == 0,
                    "Repeated Workshop refresh must not bypass the legacy cold-start boundary or invent a SaveLoaded lifecycle.");

                WriteOfficialModInfos(persistentRoot, officialId, enabled: false);
                runtime.NotifyWorkshopModListChanged();
                Assert(CountEventOwnerHandlers(runtime, ownerId) == 0 &&
                    runtime.Input.CountOwnerResources(ownerId) == 0 &&
                    CountCoreOwnerRootsForTest(runtime, ownerId) == 0,
                    "A never-loaded legacy owner must leave zero platform roots when disabled again.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void WriteAutoHarvestOfficialPackage(string persistentRoot)
        {
            string repo = FindRepositoryRoot();
            DirectoryInfo outputDirectory = new DirectoryInfo(AppContext.BaseDirectory);
            string configuration = outputDirectory.Parent?.Name ?? "Release";
            string builtDll = Path.Combine(repo, "author-sdk", "samples", "api-demand", "AutoHarvest", "bin", configuration, "netstandard2.0", "AutoHarvestMod.dll");
            Assert(File.Exists(builtDll), "AutoHarvestMod must be built before its executable product-lifetime tests. path=" + builtDll);

            string contentRoot = Path.Combine(persistentRoot, "MODS", "DTMAPI_AutoHarvest", "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);
            File.Copy(builtDll, Path.Combine(contentRoot, "AutoHarvestMod.dll"), overwrite: true);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                "{ \"Name\": \"Auto Harvest\", \"Author\": \"Yuuka\", \"Version\": \"0.1.0-dtmapi\", \"UniqueID\": \"Yuuka.DTMAPI.AutoHarvest\", \"EntryDll\": \"Content/DTMAPI/AutoHarvestMod.dll\", \"MinimumDTMApiVersion\": \"0.5.1-alpha\", \"Type\": \"CodeMod\", \"Dependencies\": [{ \"UniqueID\": \"DTMAPI.GameBridge.DolocTown\", \"MinimumVersion\": \"0.5.1-alpha\", \"IsRequired\": true }, { \"UniqueID\": \"DTMAPI.ModConfigMenu\", \"MinimumVersion\": \"0.5.1-alpha\", \"IsRequired\": true }] }");
        }

        private static void WriteAutoHarvestConfig(DtmApiRuntime runtime, bool enabled)
        {
            Directory.CreateDirectory(runtime.Paths.ConfigPath);
            File.WriteAllText(
                Path.Combine(runtime.Paths.ConfigPath, "Yuuka.DTMAPI.AutoHarvest.json"),
                "{ \"Enabled\": " + (enabled ? "true" : "false") + ", \"ManualHarvestKey\": \"F7\", \"IntervalSeconds\": 30, \"MaxHarvestsPerRun\": 24, \"IncludeVines\": true, \"IncludeMushroomBags\": true, \"IncludeBushes\": true, \"SendNativeMessage\": false, \"VerboseLogging\": false }");
        }

        private static IManifest CreateTestGameBridgeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Doloc Town GameBridge",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.GameBridge.DolocTown",
                Type = "RuntimeApi"
            };
        }

        private static void RegisterAutoHarvestTestApis(DtmApiRuntime runtime)
        {
            IManifest bridgeManifest = CreateTestGameBridgeManifest();
            runtime.RegisterRuntimeApi<ICropHarvestingApi>(bridgeManifest, new FakeCropHarvestingApi());
        }

        private static int CountEventOwnerHandlers(DtmApiRuntime runtime, string ownerId)
        {
            return runtime.Events.GetHandlerCleanupSnapshot().Slots.Sum(slot =>
                slot.ByOwner.TryGetValue(ownerId, out EventOwnerSlotCounts? counts) ? counts?.ActiveHandlers ?? 0 : 0);
        }

        private static int CountEventOwnerHandlers(DtmApiRuntime runtime, string ownerId, string eventName)
        {
            return runtime.Events.GetHandlerCleanupSnapshot().Slots
                .Where(slot => slot.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase))
                .Sum(slot => slot.ByOwner.TryGetValue(ownerId, out EventOwnerSlotCounts? counts) ? counts?.ActiveHandlers ?? 0 : 0);
        }

        private sealed class FakeCropHarvestingApi : ICropHarvestingApi
        {
            public CropHarvestResult ScanMatureCrops(IManifest owner, CropHarvestRequest request)
            {
                return Build(owner, request, dryRun: true);
            }

            public CropHarvestResult HarvestMatureCrops(IManifest owner, CropHarvestRequest request)
            {
                return Build(owner, request, request.DryRun);
            }

            public BridgeFeatureStatus GetStatus(string uniqueId)
            {
                return new BridgeFeatureStatus("unit-test", "Fake crop API for AutoHarvest event-lifetime tests.");
            }

            private static CropHarvestResult Build(IManifest owner, CropHarvestRequest request, bool dryRun)
            {
                return new CropHarvestResult
                {
                    Success = true,
                    DryRun = dryRun,
                    OwnerId = owner.UniqueID,
                    Scope = request.Scope,
                    Targets = Array.Empty<CropHarvestTargetResult>(),
                    Message = "No fake crops are mature."
                };
            }
        }
    }
}
