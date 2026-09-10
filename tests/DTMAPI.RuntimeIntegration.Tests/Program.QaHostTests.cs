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
using DTMAPI.BepInExBootstrap;
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

        private static void QaHostActivationIsOneShotIntegrityBoundAndLifecycleClosed()
        {
            string gameDir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            Directory.CreateDirectory(runtime.Paths.DtmApiPath);
            string activationPath = Path.Combine(runtime.Paths.DtmApiPath, QaHostActivationLoader.ActivationFileName);

            Assert(QaHostActivationLoader.LoadIfRequested(runtime) == null, "No QA activation receipt must leave the optional host inactive without loading an assembly.");
            File.WriteAllText(activationPath, "{}");
            AssertThrows<InvalidDataException>(() => QaHostActivationLoader.LoadIfRequested(runtime), "A malformed/requested QA activation must block instead of falling back.");

            const string runId = "0123456789abcdef0123456789abcdef";
            string runRoot = Path.Combine(runtime.Paths.DtmApiPath, QaHostActivationLoader.HostDirectoryName, runId);
            Directory.CreateDirectory(runRoot);
            string sourceDll = FindRepositoryFile(Path.Combine("src", "DTMAPI.GameBridge.DolocTown.QA", "bin", "Release", "netstandard2.0", QaHostActivationLoader.AssemblyFileName));
            string stagedDll = Path.Combine(runRoot, QaHostActivationLoader.AssemblyFileName);
            File.Copy(sourceDll, stagedDll, overwrite: true);
            string settingsPath = Path.Combine(runRoot, QaHostActivationLoader.SettingsFileName);
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                mode = QaHostProtocol.ParticipantOnlyMode
            }));

            // Match the production smoke transaction: read version resources from
            // the source before copying. FileVersionInfo can return empty values
            // for the deliberately deep managed-test staging path on Windows.
            FileVersionInfo qaVersion = FileVersionInfo.GetVersionInfo(sourceDll);
            AssemblyName qaIdentity = AssemblyName.GetAssemblyName(stagedDll);
            var receipt = new
            {
                schemaVersion = QaHostProtocol.SchemaVersion,
                protocolVersion = QaHostProtocol.ProtocolVersion,
                runId,
                runtimeReleaseVersion = DtmApiRuntime.ApiVersion,
                runtimeBinaryVersion = DtmApiRuntime.BinaryVersion,
                runtimeAssemblyVersion = typeof(DtmApiRuntime).Assembly.GetName().Version?.ToString() ?? string.Empty,
                assemblyName = QaHostProtocol.AssemblySimpleName,
                factoryTypeName = QaHostProtocol.FactoryTypeName,
                dllLength = new FileInfo(stagedDll).Length,
                dllSha256 = ComputeFileSha256(stagedDll),
                dllAssemblyVersion = qaIdentity.Version?.ToString() ?? string.Empty,
                dllFileVersion = qaVersion.FileVersion ?? string.Empty,
                dllProductVersion = qaVersion.ProductVersion ?? string.Empty,
                settingsLength = new FileInfo(settingsPath).Length,
                settingsSha256 = new string('0', 64)
            };
            File.WriteAllText(activationPath, JsonSerializer.Serialize(receipt));
            AssertThrows<InvalidDataException>(() => QaHostActivationLoader.LoadIfRequested(runtime), "A settings hash mismatch must block before loading the QA assembly.");
            Assert(!AppDomain.CurrentDomain.GetAssemblies().Any(item => string.Equals(item.GetName().Name, QaHostProtocol.AssemblySimpleName, StringComparison.OrdinalIgnoreCase)), "Rejected QA activation must not load its assembly.");

            File.WriteAllText(activationPath, JsonSerializer.Serialize(new
            {
                receipt.schemaVersion,
                receipt.protocolVersion,
                receipt.runId,
                receipt.runtimeReleaseVersion,
                receipt.runtimeBinaryVersion,
                receipt.runtimeAssemblyVersion,
                receipt.assemblyName,
                receipt.factoryTypeName,
                receipt.dllLength,
                receipt.dllSha256,
                receipt.dllAssemblyVersion,
                receipt.dllFileVersion,
                receipt.dllProductVersion,
                settingsLength = new FileInfo(settingsPath).Length,
                settingsSha256 = ComputeFileSha256(settingsPath)
            }));

            PreparedQaHost prepared = QaHostActivationLoader.LoadIfRequested(runtime)
                ?? throw new InvalidOperationException("A valid QA activation receipt should create a prepared host.");
            Assert(string.IsNullOrEmpty(prepared.Factory.GetType().Assembly.Location), "The QA host must be loaded from the exact receipt-validated byte array instead of reopening its path.");
            AssertThrows<InvalidOperationException>(() => QaHostActivationLoader.LoadIfRequested(runtime), "A second activation attempt in the same AppDomain must reject the already-loaded reserved QA identity.");
            var bridge = new DolocTownGameBridge(runtime, null, prepared);
            Assert(bridge.QaHostAttachedForTests && !bridge.QaHostStartedForTests, "QA participant should attach exactly once before bridge initialization.");
            int fixtureQuitCalls = 0;
            bridge.ApplicationQuitOverrideForTests = () =>
            {
                Assert(bridge.QaHostClosedForTests && !bridge.QaHostAttachedForTests,
                    "The QA participant must close before the application quit delegate runs.");
                fixtureQuitCalls++;
            };
            bridge.StartQaHostForTests();
            bridge.StartQaHostForTests();
            bridge.UpdateQaHostForTests();
            bridge.UpdateQaHostForTests();
            Assert(bridge.QaHostStartedForTests && bridge.QaHostUpdateCountForTests == 1,
                "An empty optional participant should request its deterministic quit after the first active update.");
            bridge.CloseQaHostForTests("unit-test-repeat");
            bridge.Shutdown("unit-after-smoke-exit");
            Assert(fixtureQuitCalls == 1 && bridge.QaHostClosedForTests && !bridge.QaHostAttachedForTests && bridge.QaHostUpdateCountForTests == 1, "QA close must precede application quit, remain idempotent through shutdown, detach listeners and clear the participant without retry/fallback.");
            IHookStatusInfo smokeExitStatus = runtime.Diagnostics.GetHookStatuses().Single(item => item.HookId == "InternalFixture.QaHost");
            Assert(smokeExitStatus.Status == "closed" && smokeExitStatus.Details.Contains("reason=fixture-exit:optional QA participant reached its terminal state", StringComparison.Ordinal),
                "The optional fixture exit must close its participant before Application.Quit can bypass Unity shutdown logging.");

            string ordinaryPlayerGame = NewTempGameDir();
            var noQaRuntime = new DtmApiRuntime(new FakeHost(ordinaryPlayerGame), new ConfigMenuRegistry());
            var noQaBridge = new DolocTownGameBridge(noQaRuntime);
            noQaBridge.Update();
            noQaBridge.Update();
            Assert(!noQaBridge.QaHostAttachedForTests && noQaBridge.QaHostUpdateCountForTests == 0,
                "Ordinary-player frames must have no optional participant, QA updater, or settings-driven fallback lane.");
            string repoRoot = FindRepositoryRoot();
            string bridgeSource = File.ReadAllText(Path.Combine(repoRoot, "src", "DTMAPI.GameBridge.DolocTown", "DolocTownGameBridge.Update.cs"));
            string runtimePathsSource = File.ReadAllText(Path.Combine(repoRoot, "src", "DTMAPI.Core", "Runtime", "RuntimePaths.cs"));
            Assert(!bridgeSource.Contains("SmokeUpdate", StringComparison.Ordinal) &&
                !bridgeSource.Contains("LoadSmokeSettings", StringComparison.Ordinal) &&
                !runtimePathsSource.Contains("smoke-settings.json", StringComparison.OrdinalIgnoreCase),
                "Production Update and RuntimePaths must contain no embedded scheduler or ordinary-player smoke-settings path.");

            string closeFailureGame = NewTempGameDir();
            var closeFailureRuntime = new DtmApiRuntime(new FakeHost(closeFailureGame), new ConfigMenuRegistry());
            const string closeFailureRunId = "fedcba9876543210fedcba9876543210";
            var closeFailurePrepared = new PreparedQaHost(
                new ThrowingCloseQaHostFactory(),
                new QaHostPreparationContext(closeFailureRuntime, closeFailureRunId, closeFailureRuntime.Paths.DtmApiPath, Array.Empty<byte>()),
                new GameBridgeFixtureStartupOptions(closeFailureRunId));
            var closeFailureBridge = new DolocTownGameBridge(closeFailureRuntime, null, closeFailurePrepared);
            closeFailureBridge.StartQaHostForTests();
            closeFailureBridge.UpdateQaHostForTests();
            int failedCloseQuitCalls = 0;
            closeFailureBridge.ApplicationQuitOverrideForTests = () => failedCloseQuitCalls++;
            closeFailureBridge.RequestApplicationQuitForTests("unit-close-failure");
            Assert(closeFailureBridge.QaHostFailureObservedForTests &&
                !closeFailureBridge.QaHostClosedForTests &&
                closeFailureBridge.QaHostClosingForTests &&
                closeFailureBridge.QaHostAttachedForTests,
                "A participant Close exception must retain the participant and closing state so cleanup can be retried after listeners are detached.");
            IHookStatusInfo closeFailureStatus = closeFailureRuntime.Diagnostics.GetHookStatuses().Single(item => item.HookId == "InternalFixture.QaHost");
            Assert(failedCloseQuitCalls == 1 && closeFailureStatus.Status == "cleanup-failed" &&
                closeFailureStatus.Details.Contains("participantRetained=true", StringComparison.Ordinal) &&
                closeFailureStatus.Details.Contains("retryable=true", StringComparison.Ordinal),
                "A participant Close exception must publish retryable cleanup failure without preventing the application quit delegate.");
            closeFailureBridge.UpdateQaHostForTests();
            Assert(closeFailureBridge.QaHostUpdateCountForTests == 1,
                "A retained participant in qaHostClosing state must not receive more Update calls while cleanup is pending.");
            closeFailureBridge.CloseQaHostForTests("unit-close-retry");
            closeFailureStatus = closeFailureRuntime.Diagnostics.GetHookStatuses().Single(item => item.HookId == "InternalFixture.QaHost");
            Assert(closeFailureBridge.QaHostClosedForTests &&
                !closeFailureBridge.QaHostClosingForTests &&
                !closeFailureBridge.QaHostAttachedForTests &&
                closeFailureStatus.Status == "failed-closed" &&
                closeFailureStatus.Details.Contains("participantCloseSucceeded=true", StringComparison.Ordinal),
                "A later cleanup retry must close only after the participant succeeds, then clear the retained participant/closing state.");
        }

        private static void QaInitialSaveLoadDispositionRetriesUntilBridgeAccepts()
        {
            const string runId = "30303030303030303030303030303030";
            string gameDir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var prepared = new PreparedQaHost(
                new InitialSaveLoadDispositionQaHostFactory(),
                new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, Array.Empty<byte>()),
                new GameBridgeFixtureStartupOptions(runId, saveSlot: 3));
            var bridge = new DolocTownGameBridge(runtime, null, prepared);
            int attempts = 0;
            int continuations = 0;
            bridge.FixtureInitialSaveLoadRequestOverrideForTests = slot =>
            {
                Assert(slot == 3, "The initial-save disposition must preserve the validated startup slot.");
                attempts++;
                return attempts >= 2;
            };
            bridge.FixtureInitialSaveLoadContinuationOverrideForTests = () => continuations++;

            bridge.StartQaHostForTests();
            bridge.UpdateQaHostForTests();
            bridge.UpdateQaHostForTests();
            bridge.UpdateQaHostForTests();

            Assert(attempts == 2 && continuations == 1 && bridge.QaHostUpdateCountForTests == 3,
                "A deferred title-state save-load request must retry until the bridge accepts it, then remain one-shot while its QA-only continuation advances until SaveLoaded.");
        }

        private static void QaTerminalTitleBoundaryUsesUnifiedQuitPath()
        {
            const string runId = "40404040404040404040404040404040";
            string gameDir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var prepared = new PreparedQaHost(
                new TerminalTitleDispositionQaHostFactory(),
                new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, Array.Empty<byte>()),
                new GameBridgeFixtureStartupOptions(runId));
            var bridge = new DolocTownGameBridge(runtime, null, prepared);
            int quitCalls = 0;
            bridge.ApplicationQuitOverrideForTests = () =>
            {
                Assert(bridge.QaHostClosedForTests && !bridge.QaHostAttachedForTests,
                    "A terminal title boundary must close the participant before requesting application quit.");
                quitCalls++;
            };

            bridge.StartQaHostForTests();
            runtime.NotifyReturnedToTitle();

            Assert(quitCalls == 1 && bridge.QaHostClosedForTests,
                "A Close title-boundary disposition must use the unified one-shot application-quit path instead of consuming the participant before quit.");
        }

        private static void QaParticipantRunDispositionOwnsDeterministicQuitWithoutEmbeddedGate()
        {
            const string runId = "10101010101010101010101010101010";
            string gameDir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var prepared = new PreparedQaHost(
                new QuitDispositionQaHostFactory(),
                new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, Array.Empty<byte>()),
                new GameBridgeFixtureStartupOptions(runId));
            var bridge = new DolocTownGameBridge(runtime, null, prepared);
            int quitCalls = 0;
            bridge.ApplicationQuitOverrideForTests = () =>
            {
                Assert(bridge.QaHostClosedForTests && !bridge.QaHostAttachedForTests,
                    "The disposition-driven quit must close and detach the optional participant first.");
                quitCalls++;
            };

            bridge.StartQaHostForTests();
            bridge.UpdateQaHostForTests();
            bridge.UpdateQaHostForTests();

            Assert(quitCalls == 1 && bridge.QaHostClosedForTests && bridge.QaHostUpdateCountForTests == 1,
                "RequestQuit must be owned by the optional participant and execute exactly once without an embedded scheduler gate.");
        }

        private static void QaParticipantFailureStillClosesAndQuitsWithoutEmbeddedScheduler()
        {
            const string runId = "20202020202020202020202020202020";
            string gameDir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var prepared = new PreparedQaHost(
                new ThrowingUpdateQaHostFactory(closeAlwaysThrows: false),
                new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, Array.Empty<byte>()),
                new GameBridgeFixtureStartupOptions(runId));
            var bridge = new DolocTownGameBridge(runtime, null, prepared);
            int quitCalls = 0;
            bridge.ApplicationQuitOverrideForTests = () => quitCalls++;

            bridge.StartQaHostForTests();
            bridge.UpdateQaHostForTests();
            bridge.UpdateQaHostForTests();

            Assert(quitCalls == 1 && bridge.QaHostFailureObservedForTests && bridge.QaHostClosedForTests,
                "An optional participant Update failure must fail closed and request one deterministic quit without SmokeUpdate.");
        }

        private static void QaPreRuntimeOwnerSurvivesPostRuntimeStartupFailureUntilShutdown()
        {
            const string runId = "30303030303030303030303030303030";
            string gameDir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
            var factory = new ThrowingStartPreRuntimeQaHostFactory();
            var prepared = new PreparedQaHost(
                factory,
                new QaHostPreparationContext(runtime, runId, runtime.Paths.DtmApiPath, Array.Empty<byte>()),
                new GameBridgeFixtureStartupOptions(runId, requirePreRuntimeSaveIsolation: true));
            var bridge = new DolocTownGameBridge(runtime, null, prepared);
            int quitCalls = 0;
            bridge.ApplicationQuitOverrideForTests = () =>
            {
                Assert(bridge.QaHostFailureObservedForTests && bridge.QaHostClosingForTests &&
                    bridge.QaHostAttachedForTests && !bridge.QaHostClosedForTests && factory.Participant.CloseCount == 0,
                    "A post-Runtime startup failure must request exit while retaining the prepared participant and its pre-Runtime owner.");
                quitCalls++;
            };

            bridge.PrepareQaHostBeforeRuntimeStart();
            AssertThrows<InvalidOperationException>(() => bridge.StartQaHostForTests(),
                "The startup fixture must inject a participant Start failure after the pre-Runtime owner is prepared.");
            bridge.RetainQaHostAndRequestApplicationQuitAfterRuntimeStartFailure("InvalidOperationException");

            Assert(factory.Participant.PrepareCount == 1 && factory.Participant.CloseCount == 0 && quitCalls == 1 &&
                bridge.QaHostFailureObservedForTests && bridge.QaHostClosingForTests &&
                bridge.QaHostAttachedForTests && !bridge.QaHostClosedForTests,
                "The pre-Runtime participant must remain attached exactly until process shutdown, and the bounded quit request must be idempotent across participant and Bootstrap failure boundaries.");

            bridge.CloseQaHostForTests("unit-process-shutdown");
            Assert(factory.Participant.CloseCount == 1 && bridge.QaHostClosedForTests && !bridge.QaHostAttachedForTests,
                "The retained pre-Runtime owner must release exactly once at the explicit process-shutdown boundary.");
        }

        private sealed class InitialSaveLoadDispositionQaHostFactory : IQaHostFactory
        {
            public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

            public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

            public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context) =>
                new GameBridgeFixtureStartupOptions(context.RunId, saveSlot: 3);

            public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access) =>
                new InitialSaveLoadDispositionQaHostParticipant();
        }

        private sealed class InitialSaveLoadDispositionQaHostParticipant : IQaHostParticipant
        {
            public string Id => "DTMAPI.QA.Tests.RequestInitialSaveLoad";

            public void Start()
            {
            }

            public void Update()
            {
            }

            public QaHostRunDisposition GetRunDisposition() => QaHostRunDisposition.RequestInitialSaveLoad;

            public void OnSaveLoaded(int? slot, bool isNewGame)
            {
            }

            public void OnSaveSaved(int? slot)
            {
            }

            public void OnWorkshopReloadCompleted()
            {
            }

            public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded) => QaHostBoundaryDisposition.Continue;

            public void Close(string reason)
            {
            }
        }

        private sealed class TerminalTitleDispositionQaHostFactory : IQaHostFactory
        {
            public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

            public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

            public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context) =>
                new GameBridgeFixtureStartupOptions(context.RunId);

            public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access) =>
                new TerminalTitleDispositionQaHostParticipant();
        }

        private sealed class TerminalTitleDispositionQaHostParticipant : IQaHostParticipant
        {
            public string Id => "DTMAPI.QA.Tests.TerminalTitle";

            public void Start()
            {
            }

            public void Update()
            {
            }

            public QaHostRunDisposition GetRunDisposition() => QaHostRunDisposition.Continue;

            public void OnSaveLoaded(int? slot, bool isNewGame)
            {
            }

            public void OnSaveSaved(int? slot)
            {
            }

            public void OnWorkshopReloadCompleted()
            {
            }

            public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded) => QaHostBoundaryDisposition.Close;

            public void Close(string reason)
            {
            }
        }

        private sealed class QuitDispositionQaHostFactory : IQaHostFactory
        {
            public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

            public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

            public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context) =>
                new GameBridgeFixtureStartupOptions(context.RunId);

            public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access) =>
                new QuitDispositionQaHostParticipant();
        }

        private sealed class QuitDispositionQaHostParticipant : IQaHostParticipant
        {
            public string Id => "DTMAPI.QA.Tests.RequestQuit";

            public void Start()
            {
            }

            public void Update()
            {
            }

            public QaHostRunDisposition GetRunDisposition() => QaHostRunDisposition.RequestQuit;

            public void OnSaveLoaded(int? slot, bool isNewGame)
            {
            }

            public void OnSaveSaved(int? slot)
            {
            }

            public void OnWorkshopReloadCompleted()
            {
            }

            public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded) => QaHostBoundaryDisposition.Continue;

            public void Close(string reason)
            {
            }
        }

        private sealed class ThrowingCloseQaHostFactory : IQaHostFactory
        {
            public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

            public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

            public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context) =>
                new GameBridgeFixtureStartupOptions(context.RunId);

            public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access) =>
                new ThrowingCloseQaHostParticipant();
        }

        private sealed class ThrowingStartPreRuntimeQaHostFactory : IQaHostFactory
        {
            internal ThrowingStartPreRuntimeQaHostParticipant Participant { get; } = new ThrowingStartPreRuntimeQaHostParticipant();

            public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

            public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

            public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context) =>
                new GameBridgeFixtureStartupOptions(context.RunId, requirePreRuntimeSaveIsolation: true);

            public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access) => Participant;
        }

        private sealed class ThrowingStartPreRuntimeQaHostParticipant : IQaHostParticipant, IQaHostPreRuntimeParticipant
        {
            internal int PrepareCount { get; private set; }

            internal int CloseCount { get; private set; }

            public string Id => "DTMAPI.QA.Tests.ThrowingStartPreRuntime";

            public void PrepareBeforeRuntimeStart()
            {
                PrepareCount++;
            }

            public void Start()
            {
                throw new InvalidOperationException("intentional post-Runtime QA Start failure");
            }

            public void Update()
            {
            }

            public QaHostRunDisposition GetRunDisposition() => QaHostRunDisposition.Continue;

            public void OnSaveLoaded(int? slot, bool isNewGame)
            {
            }

            public void OnSaveSaved(int? slot)
            {
            }

            public void OnWorkshopReloadCompleted()
            {
            }

            public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded) => QaHostBoundaryDisposition.Continue;

            public void Close(string reason)
            {
                CloseCount++;
            }
        }

        private sealed class PassiveQaHostFactory : IQaHostFactory
        {
            public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

            public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

            public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context) =>
                new GameBridgeFixtureStartupOptions(context.RunId);

            public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access) =>
                new PassiveQaHostParticipant();
        }

        private sealed class PassiveQaHostParticipant : IQaHostParticipant
        {
            public string Id => "DTMAPI.QA.Tests.Passive";

            public void Start()
            {
            }

            public void Update()
            {
            }

            public QaHostRunDisposition GetRunDisposition() => QaHostRunDisposition.Continue;

            public void OnSaveLoaded(int? slot, bool isNewGame)
            {
            }

            public void OnSaveSaved(int? slot)
            {
            }

            public void OnWorkshopReloadCompleted()
            {
            }

            public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded) => QaHostBoundaryDisposition.Continue;

            public void Close(string reason)
            {
            }
        }

        private sealed class ThrowingUpdateQaHostFactory : IQaHostFactory
        {
            private readonly bool closeAlwaysThrows;

            internal ThrowingUpdateQaHostFactory(bool closeAlwaysThrows)
            {
                this.closeAlwaysThrows = closeAlwaysThrows;
            }

            public int ProtocolVersion => QaHostProtocol.ProtocolVersion;

            public string SupportedRuntimeReleaseVersion => DtmApiRuntime.ApiVersion;

            public GameBridgeFixtureStartupOptions PrepareStartupOptions(QaHostPreparationContext context) =>
                new GameBridgeFixtureStartupOptions(context.RunId);

            public IQaHostParticipant CreateParticipant(GameBridgeFixtureAccess access) =>
                new ThrowingUpdateQaHostParticipant(access, closeAlwaysThrows);
        }

        private sealed class ThrowingUpdateQaHostParticipant : IQaHostParticipant
        {
            private readonly GameBridgeFixtureAccess access;
            private readonly bool closeAlwaysThrows;

            internal ThrowingUpdateQaHostParticipant(GameBridgeFixtureAccess access, bool closeAlwaysThrows)
            {
                this.access = access;
                this.closeAlwaysThrows = closeAlwaysThrows;
            }

            public string Id => "DTMAPI.QA.Tests.ThrowingUpdate";

            public void Start()
            {
            }

            public void Update()
            {
                access.SetHookStatus("Smoke.TitleSettingsMenu", "failed", "unit", "intentional first-case failure");
                throw new InvalidOperationException("intentional QA update failure");
            }

            public QaHostRunDisposition GetRunDisposition() => QaHostRunDisposition.Continue;

            public void OnSaveLoaded(int? slot, bool isNewGame)
            {
            }

            public void OnSaveSaved(int? slot)
            {
            }

            public void OnWorkshopReloadCompleted()
            {
            }

            public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded) => QaHostBoundaryDisposition.Continue;

            public void Close(string reason)
            {
                if (closeAlwaysThrows)
                    throw new InvalidOperationException("intentional persistent close failure");
            }
        }

        private sealed class ThrowingCloseQaHostParticipant : IQaHostParticipant
        {
            private int closeAttempts;

            public string Id => "DTMAPI.QA.Tests.ThrowingClose";

            public void Start()
            {
            }

            public void Update()
            {
            }

            public QaHostRunDisposition GetRunDisposition() => QaHostRunDisposition.Continue;

            public void OnSaveLoaded(int? slot, bool isNewGame)
            {
            }

            public void OnSaveSaved(int? slot)
            {
            }

            public void OnWorkshopReloadCompleted()
            {
            }

            public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded) => QaHostBoundaryDisposition.Continue;

            public void Close(string reason)
            {
                closeAttempts++;
                if (closeAttempts == 1)
                    throw new InvalidOperationException("intentional first close failure");
            }
        }
    }
}
