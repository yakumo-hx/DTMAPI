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

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void ProductQaNoRodRestorationDoesNotLeakIntoGenericHost()
        {
            string repo = FindRepositoryRoot();
            string actionSource = File.ReadAllText(Path.Combine(repo, "products", "first-party", "AutoFishing", "qa", "fifth-save", "AutoFishingFixtureCase.cs"));
            string controllerSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "QaScenarioController.cs"));
            string participantSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string bridgeSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "DolocTownGameBridge.cs"));
            string shutdownSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "DolocTownGameBridge.Hooks.cs"));
            Assert(actionSource.Contains("RestoreEnabledNoRodSelectionIfNeeded(\"phase-failure\")", StringComparison.Ordinal) &&
                actionSource.Contains("private void RestoreEnabledNoRodSelectionIfNeeded", StringComparison.Ordinal) &&
                !controllerSource.Contains("RestoreEnabledNoRodSelectionIfNeeded", StringComparison.Ordinal) &&
                !controllerSource.Contains("AutoFishing", StringComparison.Ordinal) &&
                !participantSource.Contains("RestoreEnabledNoRodSelectionIfNeeded", StringComparison.Ordinal) &&
                !participantSource.Contains("EnabledNoRod", StringComparison.Ordinal) &&
                !bridgeSource.Contains("RestoreEnabledNoRodSelectionIfNeeded", StringComparison.Ordinal) &&
                !shutdownSource.Contains("RestoreEnabledNoRodSelectionIfNeeded", StringComparison.Ordinal),
                "Retired EnabledNoRod fixture restoration must remain product-QA-owned and must not leak back into the generic QA host or mandatory GameBridge.");
        }

        private static void PreviewVersionMetadataIsConsistent()
        {
            PlatformVersionProjectionTests.RunAll(FindRepositoryRoot());
        }

        private static void PublicApiStatusMetadataMatchesMatrix()
        {
            foreach ((Type Type, string Since) row in new[]
                     {
                         (typeof(IModRegistry), "0.1.0"),
                         (typeof(ITranslationHelper), "0.1.12"),
                         (typeof(IDtmConfigMenuApi), "0.1.0")
                     })
            {
                DtmApiStatusAttribute status = row.Type.GetCustomAttribute<DtmApiStatusAttribute>()
                    ?? throw new InvalidOperationException(row.Type.Name + " must publish explicit stability metadata.");
                Assert(status.Status == DtmApiStatus.StableCandidate && status.Since == row.Since,
                    row.Type.Name + " must mechanically match the public API matrix StableCandidate status without rewriting its historical Since value.");
            }

            DtmApiStatusAttribute keybindDefaults = typeof(IDtmConfigMenuKeybindDefaultsApi).GetCustomAttribute<DtmApiStatusAttribute>()
                ?? throw new InvalidOperationException(nameof(IDtmConfigMenuKeybindDefaultsApi) + " must publish explicit stability metadata.");
            Assert(keybindDefaults.Status == DtmApiStatus.Experimental,
                "The optional config-menu keybind Reset capability must remain Experimental when the primary config-menu surface becomes StableCandidate.");
        }

        private static void BootstrapDefersNativeSourceCaptureUntilNativeReadyLifecycle()
        {
            string repo = FindRepositoryRoot();
            string bootstrap = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.BepInExBootstrap", "BootstrapPlugin.cs"));
            int awakeStart = bootstrap.IndexOf("public void Awake()", StringComparison.Ordinal);
            int awakeEnd = bootstrap.IndexOf("private TitleReturnObjectGraphSection", awakeStart, StringComparison.Ordinal);
            int startStart = bootstrap.IndexOf("public void Start()", StringComparison.Ordinal);
            int startEnd = bootstrap.IndexOf("public void Update()", startStart, StringComparison.Ordinal);
            Assert(awakeStart >= 0 && awakeEnd > awakeStart && startStart >= 0 && startEnd > startStart, "Bootstrap lifecycle source boundaries should remain inspectable.");

            string awake = bootstrap.Substring(awakeStart, awakeEnd - awakeStart);
            string start = bootstrap.Substring(startStart, startEnd - startStart);
            Assert(!awake.Contains("CaptureNativeWorkshopSubscriptions", StringComparison.Ordinal) &&
                !awake.Contains("runtime.Start();", StringComparison.Ordinal) &&
                !awake.Contains("bridge.Initialize();", StringComparison.Ordinal),
                "Bootstrap Awake must only prepare objects; native ModManager does not exist until the game's Awake initialization completes.");

            int capture = start.IndexOf("CaptureNativeWorkshopSubscriptions(\"Bootstrap.\" + source + \".PreRuntime\")", StringComparison.Ordinal);
            int prepareQaSaveGuard = start.IndexOf("bridge.PrepareQaHostBeforeRuntimeStart();", StringComparison.Ordinal);
            int runtimeStartEntered = start.IndexOf("runtimeStartEntered = true;", StringComparison.Ordinal);
            int runtimeStart = start.IndexOf("runtime.Start();", StringComparison.Ordinal);
            int bridgeInitialize = start.IndexOf("bridge.Initialize();", StringComparison.Ordinal);
            Assert(capture >= 0 && prepareQaSaveGuard > capture && runtimeStartEntered > prepareQaSaveGuard &&
                runtimeStart > runtimeStartEntered && bridgeInitialize > runtimeStart,
                "The native-ready one-shot lifecycle must capture source authority, install the receipt-bound QA save guard before Core Mod discovery, and initialize ordinary GameBridge Harmony only after Runtime Start.");
            int preRuntimeAbort = start.IndexOf("bridge?.AbortQaHostBeforeRuntimeStart(ex.GetType().Name)", StringComparison.Ordinal);
            int postRuntimeRetain = start.IndexOf("bridge?.RetainQaHostAndRequestApplicationQuitAfterRuntimeStartFailure(ex.GetType().Name)", StringComparison.Ordinal);
            Assert(start.Contains("if (!runtimeStartEntered)", StringComparison.Ordinal) &&
                preRuntimeAbort > bridgeInitialize && postRuntimeRetain > preRuntimeAbort,
                "Bootstrap must release the optional pre-Runtime QA owner only before Runtime Start is entered; every later failure must retain isolation and request bounded process exit.");
            Assert(bootstrap.Contains("TryStartRuntimeOnce(\"Unity.Start\")", StringComparison.Ordinal) &&
                bootstrap.Contains("TryStartRuntimeOnce(\"Unity.Update.FirstFrame\")", StringComparison.Ordinal) &&
                bootstrap.Contains("TryStartRuntimeOnce(\"PlayerLoop.FirstFrame\")", StringComparison.Ordinal) &&
                start.Contains("startupAttempted", StringComparison.Ordinal) && start.Contains("if (initialized)", StringComparison.Ordinal),
                "Bootstrap must use the existing first PlayerLoop frame when ordinary Unity callbacks are unavailable and refuse a partial retry without recurring readiness polling.");
            Assert(awake.Contains("TryInstallPlayerLoopFrameDriver();", StringComparison.Ordinal),
                "Awake must install the existing PlayerLoop driver early enough to provide the one-shot native-ready callback.");

            string analyzer = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "analyze-startup-evidence.ps1"));
            int currentMetric = analyzer.IndexOf("Bootstrap\\.StartRuntime totalMs", StringComparison.Ordinal);
            int historicalMetric = analyzer.IndexOf("Bootstrap\\.Awake totalMs", StringComparison.Ordinal);
            Assert(currentMetric >= 0 && historicalMetric > currentMetric,
                "Startup evidence analysis must prefer the StartRuntime metric while preserving historical Awake evidence compatibility.");
        }

        private static void ProductOwnerRefreshSmokeReturnsHomeBeforeExit()
        {
            string repo = FindRepositoryRoot();
            string routingSource = ReadSmokeRunnerSource(repo, "phases", "routing.ps1");
            string assessmentSource = ReadSmokeRunnerSource(repo, "phases", "assess-evidence.ps1");
            string publicationSource = ReadSmokeRunnerSource(repo, "phases", "publish-result.ps1");
            string participant = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string bridge = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "QaHost", "DolocTownGameBridge.QaHost.cs"));

            Assert(participant.Contains("QaHostRunDisposition.RequestReturnHome", StringComparison.Ordinal) &&
                participant.Contains("QaHostRunDisposition.RequestQuit", StringComparison.Ordinal) &&
                bridge.Contains("case QaHostRunDisposition.RequestReturnHome", StringComparison.Ordinal) &&
                bridge.Contains("RequestReturnHomeFromQaHost();", StringComparison.Ordinal),
                "The optional participant must solely own ReturnHome and quit disposition after its requested terminals complete.");
            Assert(routingSource.Contains("ContinuousHomePageTerminalEnabled = $qaG4ContinuousHomePageTerminalEnabled", StringComparison.Ordinal) &&
                assessmentSource.Contains("[bool]$AssertProductOwnerRefresh", StringComparison.Ordinal) &&
                publicationSource.Contains("ProductOwnerRefreshReturnHomeOrchestration", StringComparison.Ordinal),
                "The runner must project the product-owner continuous-HomePage terminal and fail closed unless its ReturnHome orchestration is verified.");
            Assert(!SmokeRunnerSourcesContain(repo, "smoke-settings.json", StringComparison.OrdinalIgnoreCase),
                "The product-owner gate must not regain the deleted embedded settings channel.");
        }

        private static void Batch2PlayerLikeSmokeGatesAreOrderedAndRecoverable()
        {
            string repo = FindRepositoryRoot();
            string assessmentSource = ReadSmokeRunnerSource(repo, "phases", "assess-evidence.ps1");
            string exerciseSource = ReadSmokeRunnerSource(repo, "phases", "exercise-session.ps1");
            string preparationSource = ReadSmokeRunnerSource(repo, "phases", "prepare-session.ps1");
            string restorationSource = ReadSmokeRunnerSource(repo, "phases", "restore-session.ps1");
            string preflightSource = ReadSmokeRunnerSource(repo, "phases", "preflight.ps1");
            string evidenceSource = ReadSmokeRunnerSource(repo, "core", "evidence.ps1");
            string inputSource = ReadSmokeRunnerSource(repo, "scenarios", "desktop-input.ps1");
            string participant = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown.QA", "QaHostParticipant.cs"));
            string callbacks = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "Hooking", "DolocTownHookCallbacks.cs"));
            string workshopTransaction = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "WorkshopOfficialModUiTransaction.cs"));
            string runner = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "run-game-smoke.ps1"));
            string debugConsoleHost = string.Join(
                "\n",
                Directory.GetFiles(
                        Path.Combine(repo, "products", "first-party", "DebugConsole", "src", "Ui"),
                        "DebugConsoleUi*.cs",
                        SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
            string debugConsoleMod = File.ReadAllText(Path.Combine(repo, "products", "first-party", "DebugConsole", "src", "ModEntry.cs"));
            string debugConsoleCompatibilityInput = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown.Compatibility", "DebugConsole", "CompatibilityDebugConsoleInputHooks.cs"));
            string debugConsoleQa = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown.QA", "Scenarios", "DolocTownGameBridge.G4Fixtures.cs"));
            string coreInput = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.Core", "Services", "WorkshopContentInputUi.cs"));
            string coreRuntime = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.Core", "Runtime", "DtmApiRuntime.cs"));
            string eventManager = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.Core", "Services", "EventManager.cs"));

            int deferredCommit = workshopTransaction.IndexOf(
                "private void ProcessPendingOfficialModUiCommit()",
                StringComparison.Ordinal);
            int publishCommittedSnapshot = deferredCommit < 0
                ? -1
                : workshopTransaction.IndexOf(
                    "PublishNativeWorkshopSubscriptionSnapshot(",
                    deferredCommit,
                    StringComparison.Ordinal);
            int notifyCommittedRefresh = publishCommittedSnapshot < 0
                ? -1
                : workshopTransaction.IndexOf(
                    "runtime.NotifyWorkshopModListChanged();",
                    publishCommittedSnapshot,
                    StringComparison.Ordinal);
            int notifyOptionalParticipant = notifyCommittedRefresh < 0
                ? -1
                : workshopTransaction.IndexOf(
                    "QaHostWorkshopReloadNotification?.Invoke();",
                    notifyCommittedRefresh,
                    StringComparison.Ordinal);

            Assert(callbacks.Contains("HandleNativeModManagerReloaded(__instance)", StringComparison.Ordinal) &&
                workshopTransaction.Contains("Official Mod UI opening reload observed as preview", StringComparison.Ordinal) &&
                workshopTransaction.Contains("saveObserved && saveSucceeded && workshopModUiCloseCandidate != null", StringComparison.Ordinal) &&
                deferredCommit >= 0 &&
                publishCommittedSnapshot > deferredCommit &&
                notifyCommittedRefresh > publishCommittedSnapshot &&
                notifyOptionalParticipant > notifyCommittedRefresh &&
                participant.Contains("OnWorkshopReloadCompleted", StringComparison.Ordinal),
                "Workshop refresh completion must follow a successful official close-save commit and reach the activation-bound optional participant only after publication and Runtime refresh.");
            Assert(participant.Contains("Smoke external player input READY token=", StringComparison.Ordinal) &&
                participant.Contains("Smoke exercise ExternalPlayerInputCleanup OK", StringComparison.Ordinal) &&
                participant.Contains("ExternalPlayerInputObservation", StringComparison.Ordinal) &&
                preparationSource.Contains("external-player-input-complete.signal", StringComparison.Ordinal) &&
                exerciseSource.Contains("YHoldCleanupEscape", StringComparison.Ordinal) &&
                exerciseSource.Contains("Issue011InteractiveCleanupEscape", StringComparison.Ordinal) &&
                exerciseSource.Contains("DebugConsole status UI\\.DebugConsoleItemTooltip=visible", StringComparison.Ordinal) &&
                exerciseSource.Contains("Debug console open lifecycle state searchText=(?!<empty>).*? category=", StringComparison.Ordinal) &&
                exerciseSource.Contains("$issue011TooltipObserved -and $issue011SearchObserved -and $issue011GiveObserved", StringComparison.Ordinal) &&
                exerciseSource.IndexOf("Issue011InteractiveEvidence=", StringComparison.Ordinal) <
                    exerciseSource.IndexOf("-Label 'Issue011InteractiveCleanupEscape'", StringComparison.Ordinal) &&
                assessmentSource.Contains("'Smoke external player input marker token='", StringComparison.Ordinal) &&
                assessmentSource.Contains("'Smoke exercise ExternalPlayerInputCleanup OK token='", StringComparison.Ordinal) &&
                assessmentSource.Contains("'Optional QA participant requesting game quit: optional QA participant reached its terminal state'", StringComparison.Ordinal),
                "The no-HookProbe player-input gate must use the optional participant's token-bound READY/marker observation and close held-key UI before ReturnHome cleanup.");
            Assert(debugConsoleHost.Contains("DebugConsoleRawInput.Sample(inputMask)", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("DebugConsoleRawInputMask.EscapePressed", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("DebugConsoleRawInputMask.YDown", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("suppressEscapeCloseUntilReleased", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("AdvanceEscapeCloseDrain", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("ShouldUseLegacyYCloseFallback", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("legacy RegisterButton Y close compatibility dispatched", StringComparison.Ordinal) &&
                coreInput.Contains("HasOwnerLegacyButtonRegistration", StringComparison.Ordinal) &&
                coreInput.Contains("OpenOwnerBoundCustomMenu", StringComparison.Ordinal) &&
                coreRuntime.Contains("TryDispatchLegacyModalButtonPressed", StringComparison.Ordinal) &&
                eventManager.Contains("DispatchButtonPressedToOwner", StringComparison.Ordinal) &&
                coreInput.Contains("HasOwnerTypedKeybindForButton", StringComparison.Ordinal) &&
                debugConsoleCompatibilityInput.Contains("NativeInputDrainActive", StringComparison.Ordinal) &&
                debugConsoleCompatibilityInput.Contains("dtmapi.compatibility.debugconsole.legacy", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("Debug console close boundary reason=", StringComparison.Ordinal) &&
                debugConsoleHost.Contains("ApplyVisibilityState();", StringComparison.Ordinal) &&
                debugConsoleQa.Contains("runtime.UI.IsOwnerBoundCustomMenuOpen(", StringComparison.Ordinal) &&
                debugConsoleQa.Contains("\"DTMAPI.DebugConsoleMod\"", StringComparison.Ordinal) &&
                !debugConsoleQa.Contains("debugConsoleApi.IsOpen", StringComparison.Ordinal) &&
                debugConsoleMod.Contains("\"Escape\"", StringComparison.Ordinal) &&
                debugConsoleMod.Contains("DtmInputScope.SaveLoaded", StringComparison.Ordinal) &&
                debugConsoleMod.Contains("helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;", StringComparison.Ordinal) &&
                debugConsoleMod.Contains("if (!saveSession.IsActive)", StringComparison.Ordinal) &&
                debugConsoleMod.Contains("saveSession.Enter(", StringComparison.Ordinal) &&
                !debugConsoleMod.Contains("SetUpdateSubscription(true);", StringComparison.Ordinal) &&
                CountTextOccurrences(debugConsoleMod, "SetUpdateSubscription(false)") == 1 &&
                exerciseSource.Contains("Debug console closed reason=hotkey Y owner=DTMAPI.DebugConsoleMod.", StringComparison.Ordinal) &&
                exerciseSource.Contains("Wait-ForLogLineAfterOffset", StringComparison.Ordinal) &&
                inputSource.Contains("KeyDownAt", StringComparison.Ordinal) &&
                assessmentSource.Contains("NativeMenuLeakDetected", StringComparison.Ordinal) &&
                assessmentSource.Contains("LegacyModalYCompatibilityCount", StringComparison.Ordinal) &&
                assessmentSource.Contains("LegacyModalYOwnerDispatchCount", StringComparison.Ordinal) &&
                assessmentSource.Contains("LegacyModToggleCloseCount", StringComparison.Ordinal) &&
                inputSource.Contains("permits exactly one send attempt per label", StringComparison.Ordinal) &&
                assessmentSource.Contains("ModalInputIsolationPassed", StringComparison.Ordinal),
                "The Advanced DebugConsole product must solely own typed Y toggles, synchronously apply the product Canvas at modal acquisition, and keep its gated frame root stable from Entry through title; QA must observe that exact owner-bound menu instead of the frozen Compatibility API, while ProductNative input guards preserve bounded Escape drain behavior without leaking to MainMenuUiState.");
            Assert(exerciseSource.Contains("PostExit = [ordered]@{", StringComparison.Ordinal) &&
                exerciseSource.Contains("UnchangedDuringRun = $publishedProductArtifactsUnchangedDuringRun", StringComparison.Ordinal) &&
                evidenceSource.Contains("$Product.PSObject.Properties['currentPublishedArtifact']", StringComparison.Ordinal) &&
                preparationSource.Contains("DTMAPI-Published-SHA256SUMS-v1", StringComparison.Ordinal) &&
                evidenceSource.Contains("New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::Ordinal)", StringComparison.Ordinal) &&
                evidenceSource.Contains("New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::OrdinalIgnoreCase)", StringComparison.Ordinal) &&
                preparationSource.Contains("-ArtifactBoundary Retained", StringComparison.Ordinal) &&
                evidenceSource.Contains("Content/.tools/bepinex/extract/", StringComparison.Ordinal) &&
                assessmentSource.Contains("ExactCodeOwnerSetPassed", StringComparison.Ordinal) &&
                assessmentSource.Contains("WorkshopProvenancePassed", StringComparison.Ordinal) &&
                assessmentSource.Contains("OrderedLifecyclePassed", StringComparison.Ordinal),
                "The public-product gate must bind current post-publication artifacts with ordinal normalization while retaining explicit legacy boundaries, re-hash subscriptions after exit, and reject extra code owners, non-Workshop resolution, or out-of-order refresh/cleanup evidence.");
            int externalArtifactPreflight = preparationSource.IndexOf("external-input-artifact-preflight.json", StringComparison.Ordinal);
            int externalProvenanceAssignment = assessmentSource.IndexOf("$externalPlayerInputProvenanceOk = $externalPlayerInputArtifactOk -and", StringComparison.Ordinal);
            int preparePhase = runner.IndexOf("'phases/prepare-session.ps1'", StringComparison.Ordinal);
            int assessPhase = runner.IndexOf("'phases/assess-evidence.ps1'", StringComparison.Ordinal);
            int codeModLoadSourceLog = coreRuntime.IndexOf("\"Code mod load-source owner=\" + mod.Manifest.UniqueID", StringComparison.Ordinal);
            int codeModAssemblyLoad = coreRuntime.IndexOf("Assembly.LoadFrom(dllPath)", StringComparison.Ordinal);
            int plannedCodeModAssemblyLoad = coreRuntime.IndexOf(".LoadEntry(mod.Manifest.UniqueID, dllPath)", StringComparison.Ordinal);
            Assert(preparationSource.Contains("[string]$_.catalogId -eq 'y-console'", StringComparison.Ordinal) &&
                preparationSource.Contains("[string]$_.uniqueId -eq 'DTMAPI.DebugConsoleMod'", StringComparison.Ordinal) &&
                preparationSource.Contains("[string]$_.workshopId -eq '3742714442'", StringComparison.Ordinal) &&
                preparationSource.Contains("$externalPlayerInputProduct.PSObject.Properties['currentPublishedArtifact']", StringComparison.Ordinal) &&
                preparationSource.Contains("[string]$externalPlayerInputProduct.packageDll -ne 'DTMAPI.DebugConsole.dll'", StringComparison.Ordinal) &&
                preparationSource.Contains("Get-SmokeWorkshopArtifactSnapshot -Path $expectedYConsoleWorkshopRoot -Product $externalPlayerInputProduct -ArtifactBoundary CurrentPublished", StringComparison.Ordinal) &&
                preparationSource.Contains("ExpectedWorkshopDllLength", StringComparison.Ordinal) &&
                preparationSource.Contains("WorkshopDllLengthPassed", StringComparison.Ordinal) &&
                preparationSource.Contains("External player-input Workshop artifact preflight failed", StringComparison.Ordinal) &&
                assessmentSource.Contains("ExpectedWorkshopYConsoleDllSha256", StringComparison.Ordinal) &&
                assessmentSource.Contains("ActualWorkshopYConsoleDllSha256", StringComparison.Ordinal) &&
                assessmentSource.Contains("WorkshopArtifactPassed", StringComparison.Ordinal) &&
                assessmentSource.Contains("DuplicateSelectionEvidenceAvailable", StringComparison.Ordinal) &&
                assessmentSource.Contains("Code mod load-source owner=DTMAPI\\.DebugConsoleMod;", StringComparison.Ordinal) &&
                assessmentSource.Contains("source=Workshop; workshopId=3742714442; root=", StringComparison.Ordinal) &&
                assessmentSource.Contains("[regex]::Escape($expectedYConsoleWorkshopDll) + '\\.\\s*$'", StringComparison.Ordinal) &&
                assessmentSource.Contains("$externalPlayerInputLoadSourceOk = $yConsoleLoadSourceMatches.Count -eq 1 -and", StringComparison.Ordinal) &&
                assessmentSource.Contains("LoadSourceEvidenceCount", StringComparison.Ordinal) &&
                assessmentSource.Contains("LoadSourcePassed", StringComparison.Ordinal) &&
                assessmentSource.Contains("LoadedSourceRootAsserted", StringComparison.Ordinal) &&
                externalArtifactPreflight >= 0 && externalProvenanceAssignment >= 0 &&
                preparePhase >= 0 && assessPhase > preparePhase &&
                !SmokeRunnerSourcesContain(repo, "$externalPlayerInputProvenanceOk = $yConsoleDuplicateMatches.Count -eq 0 -or", StringComparison.Ordinal),
                "The external-input gate must require the exact current-published Catalog Workshop tree and DLL identity plus one exact runtime load-source record; zero duplicate-selection logs must never be sufficient provenance by itself.");
            Assert(codeModLoadSourceLog >= 0 && codeModAssemblyLoad > codeModLoadSourceLog && plannedCodeModAssemblyLoad > codeModLoadSourceLog &&
                coreRuntime.Contains("\"; source=\" + mod.Source", StringComparison.Ordinal) &&
                coreRuntime.Contains("\"; workshopId=\" + (mod.WorkshopId.HasValue", StringComparison.Ordinal) &&
                coreRuntime.Contains("\"; root=\" + Path.GetFullPath(mod.RootPath)", StringComparison.Ordinal) &&
                coreRuntime.Contains("\"; dll=\" + dllPath + \".\");", StringComparison.Ordinal) &&
                assessmentSource.Contains("$externalPlayerInputLoadSourceOk -and", StringComparison.Ordinal),
                "Both legacy and dependency-plan CodeMod load paths must first publish stable owner/source/workshopId/root/dll evidence, and external provenance must require its exact match.");
            Assert(!SmokeRunnerSourcesContain(repo, "smoke-settings.json", StringComparison.OrdinalIgnoreCase) &&
                restorationSource.Contains("one-action-config:", StringComparison.Ordinal) &&
                restorationSource.Contains("auto-fishing-config:", StringComparison.Ordinal) &&
                restorationSource.Contains("action-speed-config:", StringComparison.Ordinal) &&
                restorationSource.IndexOf("one-action-config:", StringComparison.Ordinal) < restorationSource.IndexOf("if ($restoreErrors.Count -gt 0)", StringComparison.Ordinal),
                "Recovery must leave the deleted settings channel untouched and attempt every temporary product-config restore before reporting aggregated failures.");
            int liveProcessGuard = restorationSource.IndexOf("if (-not [bool]$recoveryExit.Exited)", StringComparison.Ordinal);
            int playerSavePreCleanupCheck = restorationSource.IndexOf("if ($noNativeSaveMetadataRequested)", liveProcessGuard + 1, StringComparison.Ordinal);
            int profileRestore = restorationSource.IndexOf("if ($null -ne $officialModProfileSummary", playerSavePreCleanupCheck + 1, StringComparison.Ordinal);
            Assert(restorationSource.Contains("Wait-SmokeProcessExitBeforeRecovery", StringComparison.Ordinal) &&
                restorationSource.Contains("manual-recovery-required.json", StringComparison.Ordinal) &&
                restorationSource.Contains("No routine player archive backup or writeback exists.", StringComparison.Ordinal) &&
                restorationSource.Contains("player-save-unchanged-before-cleanup.json", StringComparison.Ordinal) &&
                liveProcessGuard >= 0 && playerSavePreCleanupCheck > liveProcessGuard && profileRestore > playerSavePreCleanupCheck,
                "Smoke cleanup must prove bounded process exit, classify metadata-only save state before any non-save cleanup, and never invent an archive restore receipt.");
            Assert(assessmentSource.Contains("'HookProbe|DTMAPI\\.HookProbeMod'", StringComparison.Ordinal) &&
                preflightSource.Contains("requires a real player save slot in the range 1..3", StringComparison.Ordinal),
                "Player-like gates must fail fast without a real save and treat any HookProbe discovery/transaction/execution evidence as presence.");
        }

        private static void Batch5GcLadderRunnerEvidenceDiscoveryContracts()
        {
            string repo = FindRepositoryRoot();
            string publicationSource = ReadSmokeRunnerSource(repo, "phases", "publish-result.ps1");
            string exerciseSource = ReadSmokeRunnerSource(repo, "phases", "exercise-session.ps1");
            string wrapper = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "run-batch5-gc-ladder.ps1"));
            string sourceTest = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "test-batch5-gc-ladder.ps1"));

            int resultWrite = publicationSource.IndexOf("Write-SmokeJsonObject -Path (Join-Path $evidence 'result.json')", StringComparison.Ordinal);
            int machineMarker = publicationSource.IndexOf("Write-Output ('DTMAPI_SMOKE_EVIDENCE_PATH=' + $machineEvidencePath)", StringComparison.Ordinal);
            int failureExit = publicationSource.IndexOf("if ($runFailed)", machineMarker + 1, StringComparison.Ordinal);
            Assert(exerciseSource.Contains("elseif ($probeOk -and $batch5GcLadderEnabled -and $SaveSlot -gt 0)", StringComparison.Ordinal) &&
                exerciseSource.Contains("$saveLoadedOk = Wait-SmokeLogLine -Deadline $SmokeRunnerScenarioDeadline -LogPath $logPath -Pattern 'SaveLoaded hook dispatched.'", StringComparison.Ordinal),
                "Batch 5 GC smoke stages must independently bind the neutral SaveLoaded receipt.");
            Assert(CountTextOccurrences(publicationSource, "DTMAPI_SMOKE_EVIDENCE_PATH=") == 1 &&
                resultWrite >= 0 && machineMarker > resultWrite && failureExit > machineMarker,
                "Game smoke must emit one evidence-path marker after result.json and before every terminal exit route.");

            int emptyGuard = wrapper.IndexOf("if ([string]::IsNullOrWhiteSpace($Candidate))", StringComparison.Ordinal);
            int candidateFullPath = wrapper.IndexOf("$canonicalCandidate = [System.IO.Path]::GetFullPath($trimmedCandidate)", StringComparison.Ordinal);
            int markerSearch = wrapper.IndexOf("^DTMAPI_SMOKE_EVIDENCE_PATH=(.*)$", StringComparison.Ordinal);
            int humanFallbackSearch = wrapper.IndexOf(@"Evidence:[ \t]*(.*)$", StringComparison.Ordinal);
            Assert(wrapper.Contains("function Find-Batch5GcSmokeEvidence", StringComparison.Ordinal) &&
                wrapper.Contains("function Resolve-Batch5GcSmokeEvidenceCandidate", StringComparison.Ordinal) &&
                wrapper.Contains("docs\\debug\\evidence\\GAME-SMOKE", StringComparison.Ordinal) &&
                wrapper.Contains("Test-Path -LiteralPath $canonicalCandidate -PathType Container", StringComparison.Ordinal) &&
                wrapper.Contains("produced an out-of-bound path", StringComparison.Ordinal) &&
                wrapper.Contains("smokeExit=$($stage.SmokeExitCode) remains authoritative", StringComparison.Ordinal) &&
                !wrapper.Contains("[System.IO.Path]::GetFullPath($Matches[1].Trim())", StringComparison.Ordinal) &&
                emptyGuard >= 0 && candidateFullPath > emptyGuard && markerSearch >= 0 && humanFallbackSearch > markerSearch,
                "The GC wrapper must prefer the marker and validate nonempty, existing, in-root evidence before canonical consumption while retaining the smoke exit code.");
            foreach (string contract in new[]
            {
                "failed smoke with one valid machine marker",
                "split or whitespace-only human Evidence line",
                "neither a DTMAPI_SMOKE_EVIDENCE_PATH marker nor a human Evidence: fallback",
                "nonexistent evidence directory",
                "marker outside the GAME-SMOKE root",
                "human Evidence fallback must enforce the same GAME-SMOKE boundary",
                "Multiple machine markers must be rejected",
                "in-root reparse-point marker must be rejected",
                "stage schema is missing evidence-discovery fields"
            })
            {
                Assert(sourceTest.Contains(contract, StringComparison.OrdinalIgnoreCase),
                    "The executable Batch 5 GC parser test is missing contract: " + contract);
            }
        }

        private static void Batch4RunnerRoutingMatrixUsesWindowsPowerShell51()
        {
            string repo = FindRepositoryRoot();
            string assessmentSource = ReadSmokeRunnerSource(repo, "phases", "assess-evidence.ps1");
            string publicationSource = ReadSmokeRunnerSource(repo, "phases", "publish-result.ps1");
            string exerciseSource = ReadSmokeRunnerSource(repo, "phases", "exercise-session.ps1");
            string preparationSource = ReadSmokeRunnerSource(repo, "phases", "prepare-session.ps1");
            string preflightSource = ReadSmokeRunnerSource(repo, "phases", "preflight.ps1");
            string deploymentSource = ReadSmokeRunnerSource(repo, "phases", "deploy-session.ps1");
            string qaHostSource = ReadSmokeRunnerSource(repo, "core", "qa-host.ps1");
            string inputSource = ReadSmokeRunnerSource(repo, "scenarios", "desktop-input.ps1");
            string shell = GetWindowsPowerShell51Path();
            string runnerSource = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "run-game-smoke.ps1"));
            string candidate11Source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tools", "scripts", "candidate11-source-transaction.ps1"));
            WindowsPowerShellResult version = RunWindowsPowerShell(shell, new[]
            {
                "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", "$PSVersionTable.PSVersion.ToString()"
            });
            Assert(version.ExitCode == 0 && version.Output.Trim().StartsWith("5.1", StringComparison.Ordinal),
                "The executed Batch 4 routing matrix must use Windows PowerShell 5.1, not PowerShell Core or an unverified shell. output=" + version.Output + "; error=" + version.Error);

            using (JsonDocument cleanup = RunBatch4Routing(shell, "-ValidateQaG4EvidenceCleanupOnly"))
            {
                JsonElement[] cases = cleanup.RootElement.GetProperty("Cases").EnumerateArray().ToArray();
                JsonElement exact = cases.Single(item => item.GetProperty("Name").GetString() == "exact");
                JsonElement equipmentSlots = cases.Single(item => item.GetProperty("Name").GetString() == "g9-equipment-slots");
                JsonElement damaged = cases.Single(item => item.GetProperty("Name").GetString() == "damaged");
                JsonElement truncatedPng = cases.Single(item => item.GetProperty("Name").GetString() == "truncated-png");
                JsonElement badCrcPng = cases.Single(item => item.GetProperty("Name").GetString() == "bad-crc-png");
                JsonElement headerOnlyCsv = cases.Single(item => item.GetProperty("Name").GetString() == "header-only-csv");
                JsonElement badTimestampCsv = cases.Single(item => item.GetProperty("Name").GetString() == "bad-timestamp-csv");
                JsonElement badNumberCsv = cases.Single(item => item.GetProperty("Name").GetString() == "bad-number-csv");
                JsonElement emptyMarkerOwnerCsv = cases.Single(item => item.GetProperty("Name").GetString() == "empty-marker-owner-csv");
                JsonElement artifactMismatch = cases.Single(item => item.GetProperty("Name").GetString() == "artifact-mismatch-retains-evidence");
                JsonElement outsideG4Unknown = cases.Single(item => item.GetProperty("Name").GetString() == "outside-g4-unknown-retains-evidence");
                Assert(cleanup.RootElement.GetProperty("Passed").GetBoolean() && cases.Length == 14 &&
                    exact.GetProperty("Cleaned").GetBoolean() && exact.GetProperty("ExactQaEvidence").GetBoolean() &&
                    equipmentSlots.GetProperty("Cleaned").GetBoolean() && equipmentSlots.GetProperty("ExactQaEvidence").GetBoolean() &&
                    equipmentSlots.GetProperty("ContentValidation").GetArrayLength() == 2 &&
                    !damaged.GetProperty("Cleaned").GetBoolean() && !damaged.GetProperty("ExactQaEvidence").GetBoolean() &&
                    damaged.GetProperty("Damaged").GetArrayLength() == 2 &&
                    new[] { truncatedPng, badCrcPng, headerOnlyCsv, badTimestampCsv, badNumberCsv, emptyMarkerOwnerCsv }.All(item =>
                        !item.GetProperty("Cleaned").GetBoolean() &&
                        !item.GetProperty("ExactQaEvidence").GetBoolean() &&
                        item.GetProperty("Damaged").GetArrayLength() == 1) &&
                    !artifactMismatch.GetProperty("Cleaned").GetBoolean() &&
                    artifactMismatch.GetProperty("ExactQaEvidence").GetBoolean() &&
                    !artifactMismatch.GetProperty("ExactArtifacts").GetBoolean() &&
                    artifactMismatch.GetProperty("SourceEvidenceRetained").GetBoolean() &&
                    artifactMismatch.GetProperty("ArchiveEvidenceAbsent").GetBoolean() &&
                    !outsideG4Unknown.GetProperty("Cleaned").GetBoolean() &&
                    outsideG4Unknown.GetProperty("ExactQaEvidence").GetBoolean() &&
                    outsideG4Unknown.GetProperty("ExactArtifacts").GetBoolean() &&
                    !outsideG4Unknown.GetProperty("ExactTree").GetBoolean() &&
                    outsideG4Unknown.GetProperty("SourceEvidenceRetained").GetBoolean() &&
                    outsideG4Unknown.GetProperty("ArchiveEvidenceAbsent").GetBoolean(),
                    "The temporary-tree QA evidence test must accept only a complete CRC-valid PNG plus populated typed CSV, retain malformed evidence, and preserve the complete source evidence tree before any artifact or outside-G4 membership failure.");
            }

            Assert(exerciseSource.Contains("'Smoke external player input READY token=' + $externalPlayerInputHandshakeToken + '.'", StringComparison.Ordinal) &&
                exerciseSource.Contains("'Smoke exercise ExternalPlayerInputCleanup OK token=' + $externalPlayerInputHandshakeToken + '; continuousHomePage=true; owner=qa-observation.'", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "'Smoke.ExternalPlayerInput = ready.*token='", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "'Smoke.ExternalPlayerInputCleanup = verified.*token='", StringComparison.Ordinal),
                "The staged external-input wait must pass literal token-bound log text to Wait-ForLogLine, whose contract escapes patterns instead of evaluating regex wildcards.");
            Assert(runnerSource.Contains("[switch] $AssertNoQaUiEvidence", StringComparison.Ordinal) &&
                runnerSource.Contains("[switch] $AutoDriveNoQaAnimalViewer", StringComparison.Ordinal) &&
                preparationSource.Contains("Get-SmokeDirectoryReceipt", StringComparison.Ordinal) &&
                assessmentSource.Contains("Compare-SmokeDirectoryReceipt", StringComparison.Ordinal) &&
                preparationSource.Contains("DEBUG-CONSOLE-UI", StringComparison.Ordinal) &&
                preparationSource.Contains("ANIMAL-001", StringComparison.Ordinal) &&
                preparationSource.Contains("EQUIPMENT-SLOTS-UI", StringComparison.Ordinal) &&
                exerciseSource.Contains("$noQaAnimalViewerReceiptPattern = 'AnimalHusbandryProgress product viewer active state=visible,.*receiptSequence=\\d+, receipt=new-data,'", StringComparison.Ordinal) &&
                exerciseSource.Contains("$noQaAnimalViewerProductClosePattern = 'AnimalHusbandryProgress native panel closed; product-owned derived rows and clones are zero.'", StringComparison.Ordinal) &&
                exerciseSource.Contains("$noQaAnimalViewerOverlayClearOk = $noQaAnimalViewerNativeCloseOk", StringComparison.Ordinal) &&
                exerciseSource.Contains("$noQaEquipmentSlotsDownstreamInteractionOk = $noQaAnimalViewerUiOk", StringComparison.Ordinal) &&
                assessmentSource.Contains("EquipmentSlots UI lifecycle cleared reason=ReturnedToTitle, clones=[1-9]\\d*, binders=[1-9]\\d*, rendered=True\\.", StringComparison.Ordinal) &&
                publicationSource.Contains("NoQaAnimalViewerNativeClose", StringComparison.Ordinal) &&
                publicationSource.Contains("NoQaAnimalViewerOverlayClear", StringComparison.Ordinal) &&
                exerciseSource.Contains("Send-DolocTownNormalizedMouseClick", StringComparison.Ordinal) &&
                inputSource.Contains("NoQaAnimalApproachA1", StringComparison.Ordinal) &&
                inputSource.Contains("NoQaAnimalOpenE1", StringComparison.Ordinal) &&
                inputSource.Contains("NoQaAnimalApproachA2", StringComparison.Ordinal) &&
                inputSource.Contains("NoQaAnimalOpenE2", StringComparison.Ordinal) &&
                exerciseSource.Contains("$animalOpenAttemptDeadline = (Get-Date).AddSeconds(3)", StringComparison.Ordinal) &&
                exerciseSource.Contains("NoQaAnimalSelectSecondRow", StringComparison.Ordinal) &&
                exerciseSource.Contains("NormalizedX 0.357 -NormalizedY 0.427", StringComparison.Ordinal) &&
                preflightSource.Contains("-AutoDriveNoQaAnimalViewer is an explicit ordinary-player input option and requires -AssertNoQaUiEvidence.", StringComparison.Ordinal) &&
                exerciseSource.IndexOf("$noQaAnimalViewerNativeCloseOk = Wait-ForLogLineAfterOffset", StringComparison.Ordinal) <
                    exerciseSource.IndexOf("$noQaAnimalViewerOverlayClearOk = $noQaAnimalViewerNativeCloseOk", StringComparison.Ordinal) &&
                deploymentSource.Contains("QaG4EquipmentSlotsObservationEnabled", StringComparison.Ordinal) &&
                qaHostSource.Contains("ui\\equipment-slots.png", StringComparison.Ordinal) &&
                qaHostSource.Contains("ui\\equipment-slots-summary.txt", StringComparison.Ordinal),
                "G9 routing must compare all three retired player evidence trees and retain staged-QA EquipmentSlots evidence in the run-owned G4 root.");
            Assert(candidate11Source.Contains("'TooltipEvidenceCount','TooltipLine','SearchEvidenceCount','SearchText','SearchEvidenceKind','SearchLine'", StringComparison.Ordinal) &&
                candidate11Source.Contains("DebugConsole status UI\\.DebugConsoleItemTooltip=visible", StringComparison.Ordinal) &&
                candidate11Source.Contains("Debug console open lifecycle state searchText=(?!<empty>).*? category=", StringComparison.Ordinal) &&
                candidate11Source.Contains("[ordered]@{ Name = '-SaveSlot'; Expected = '10' }", StringComparison.Ordinal) &&
                candidate11Source.Contains("[int]$receipt.SaveSlot -ne 10", StringComparison.Ordinal) &&
                candidate11Source.Contains("GameDir/slot-10/NoNativeSave/Steam/Local11/no-QA contract", StringComparison.Ordinal) &&
                !candidate11Source.Contains("UI\\.DebugConsoleItemTooltip = visible\\.", StringComparison.Ordinal),
                "The Candidate11 ISSUE-011 verifier must consume the current DebugConsole tooltip/search schema and bind both invocation and receipt to the tenth-save fixture.");

            // The route below executes the deadline and bounded input-sequence self-tests.
            // test-noqa-deadline.ps1 and test-game-smoke-modules.ps1 cover expired/late waits;
            // do not duplicate those behaviors with private variable names or whole prompt text.

            using (JsonDocument noQaAutoDrive = RunBatch4Routing(shell,
                "-AssertNoQaUiEvidence", "-AutoDriveNoQaAnimalViewer", "-UseSteam", "-SkipInstall",
                "-OfficialModProfile", "Published11", "-IsolateAllOfficialMods", "-SaveSlot", "10",
                "-TimeoutSeconds", "900", "-AutoExitAfterSecondsOverride", "360",
                "-ValidateNoQaUiEvidenceGateOnly"))
            {
                JsonElement inputSequenceSelfTest = noQaAutoDrive.RootElement.GetProperty("AnimalInputSequenceSelfTest");
                JsonElement deadlineSelfTest = noQaAutoDrive.RootElement.GetProperty("DeadlineSelfTest");
                string[] autoDriveSequences = inputSequenceSelfTest.GetProperty("AutoDriveSequences")
                    .EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
                Assert(noQaAutoDrive.RootElement.GetProperty("AssertNoQaUiEvidence").GetBoolean() &&
                    noQaAutoDrive.RootElement.GetProperty("AutoDriveNoQaAnimalViewer").GetBoolean() &&
                    noQaAutoDrive.RootElement.GetProperty("RunnerDeadlineBudgetSeconds").GetInt32() == 360 &&
                    noQaAutoDrive.RootElement.GetProperty("RunnerDeadlineBudgetSource").GetString() == "AutoExitAfterSecondsOverride" &&
                    noQaAutoDrive.RootElement.GetProperty("RunnerDeadlineScope").GetString()!.Contains("one launch-to-process-wait deadline", StringComparison.Ordinal) &&
                    !noQaAutoDrive.RootElement.GetProperty("InternalQaAutoLoad").GetBoolean() &&
                    !noQaAutoDrive.RootElement.GetProperty("InternalQaReturnHome").GetBoolean() &&
                    noQaAutoDrive.RootElement.GetProperty("OrdinaryPlayerHandshake").GetString()!.Contains("title-screen -> tenth-save", StringComparison.Ordinal) &&
                    noQaAutoDrive.RootElement.GetProperty("AnimalViewerAction").GetString()!.Contains("foreground SendInput A,E", StringComparison.Ordinal) &&
                    noQaAutoDrive.RootElement.GetProperty("AnimalViewerAction").GetString()!.Contains("only if it did not open", StringComparison.Ordinal) &&
                    noQaAutoDrive.RootElement.GetProperty("AnimalViewerAction").GetString()!.Contains("normalized foreground SendInput click", StringComparison.Ordinal) &&
                    deadlineSelfTest.GetProperty("Passed").GetBoolean() &&
                    deadlineSelfTest.GetProperty("ExpiredAtEntryDidNotExecute").GetBoolean() &&
                    deadlineSelfTest.GetProperty("ShortBudgetDidNotExecute").GetBoolean() &&
                    deadlineSelfTest.GetProperty("LateCompletionRejected").GetBoolean() &&
                    deadlineSelfTest.GetProperty("OnTimeCompletionAccepted").GetBoolean() &&
                    deadlineSelfTest.GetProperty("CappedWaitMilliseconds").GetInt32() == 125 &&
                    inputSequenceSelfTest.GetProperty("Passed").GetBoolean() &&
                    inputSequenceSelfTest.GetProperty("AutoDriveSequenceCount").GetInt32() == 2 &&
                    autoDriveSequences.Length == 2 &&
                    autoDriveSequences[0].Split('\n').SequenceEqual(new[]
                    {
                        "NoQaAnimalApproachA1", "NoQaAnimalOpenE1", "NoQaAnimalSelectSecondRow", "NoQaAnimalViewerEscape"
                    }, StringComparer.Ordinal) &&
                    autoDriveSequences[1].Split('\n').SequenceEqual(new[]
                    {
                        "NoQaAnimalApproachA1", "NoQaAnimalOpenE1", "NoQaAnimalApproachA2", "NoQaAnimalOpenE2",
                        "NoQaAnimalSelectSecondRow", "NoQaAnimalViewerEscape"
                    }, StringComparer.Ordinal) &&
                    autoDriveSequences.All(sequence => !sequence.Contains("System.Object[]", StringComparison.Ordinal)),
                    "The optional no-QA animal auto-drive must be explicit, remain inside the ordinary-player lane, describe its auditable foreground input sequence, and pass deterministic absolute-deadline guards.");
            }
            string noQaAutoDriveOutsideGate = RunBatch4RoutingFailure(shell,
                "-AutoDriveNoQaAnimalViewer", "-ValidateNoQaUiEvidenceGateOnly");
            Assert(noQaAutoDriveOutsideGate.Contains("requires -AssertNoQaUiEvidence", StringComparison.Ordinal),
                "The animal auto-drive switch must be rejected outside the strict ordinary-player no-QA gate.");

            Assert(assessmentSource.Contains("Mod-owner cleanup participants owner=Yuuka\\.DTMAPI\\.ActionSpeed, reason=ReturnedToTitle", StringComparison.Ordinal) &&
                assessmentSource.Contains("remaining=0, failures=0, results=.*DTMAPI\\.GameBridge\\.DolocTown\\.OwnerResources:[0-9]+/0:ok", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "ActionSpeed restore logic OK reason=ReturnedToTitle", StringComparison.Ordinal),
                "Product-owner refresh must bind ActionSpeed cleanup to the authoritative zero-remaining GameBridge owner cleanup receipt.");

            using (JsonDocument title = RunBatch4Routing(shell,
                "-StageQaHost", "-AutoOpenTitleSettingsMenu", "-ValidateQaG4RoutingOnly"))
            {
                Assert(title.RootElement.GetProperty("TitleSettingsUiEnabled").GetBoolean() &&
                    !title.RootElement.GetProperty("ManagerStatusUiEnabled").GetBoolean() &&
                    !title.RootElement.GetProperty("ManagerMvpUiEnabled").GetBoolean() &&
                    title.RootElement.GetProperty("TitleSettingsMode").GetString() == "Basic" &&
                    title.RootElement.GetProperty("ExpectedQaEvidencePaths").EnumerateArray().Single().GetString() == "ui\\title-settings.png",
                    "The optional QA host must own only the basic title-settings overlay and its exact screenshot receipt when explicitly staged.");
            }

            using (JsonDocument status = RunBatch4Routing(shell,
                "-StageQaHost", "-AutoOpenTitleSettingsStatusPage", "-ValidateQaG4RoutingOnly"))
            using (JsonDocument manager = RunBatch4Routing(shell,
                "-StageQaHost", "-AutoOpenTitleSettingsManagerMvp", "-ValidateQaG4RoutingOnly"))
            {
                string[] statusEvidence = status.RootElement.GetProperty("ExpectedQaEvidencePaths").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
                string[] managerEvidence = manager.RootElement.GetProperty("ExpectedQaEvidencePaths").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
                Assert(!status.RootElement.GetProperty("TitleSettingsUiEnabled").GetBoolean() &&
                    status.RootElement.GetProperty("ManagerStatusUiEnabled").GetBoolean() &&
                    !status.RootElement.GetProperty("ManagerMvpUiEnabled").GetBoolean() &&
                    status.RootElement.GetProperty("TitleSettingsMode").GetString() == "Status" &&
                    statusEvidence.SequenceEqual(new[] { "ui\\manager-status-page.png" }, StringComparer.OrdinalIgnoreCase),
                    "Manager Status must project to the QA owner with only its status-page screenshot receipt.");
                Assert(!manager.RootElement.GetProperty("TitleSettingsUiEnabled").GetBoolean() &&
                    !manager.RootElement.GetProperty("ManagerStatusUiEnabled").GetBoolean() &&
                    manager.RootElement.GetProperty("ManagerMvpUiEnabled").GetBoolean() &&
                    manager.RootElement.GetProperty("TitleSettingsMode").GetString() == "ManagerMvp" &&
                    managerEvidence.Length == 4 &&
                    managerEvidence.Contains("ui\\manager-status-page.png", StringComparer.OrdinalIgnoreCase) &&
                    managerEvidence.Contains("ui\\manager-mods-page.png", StringComparer.OrdinalIgnoreCase) &&
                    managerEvidence.Contains("ui\\manager-advanced-page.png", StringComparer.OrdinalIgnoreCase) &&
                    managerEvidence.Contains("ui\\manager-logs-page.png", StringComparer.OrdinalIgnoreCase),
                    "Manager MVP must project to the QA owner with exact status, Mods interaction, Advanced interaction, and logs screenshot receipts, without a basic-overlay receipt.");
            }

            var migratedCases = new (string Property, string[] Arguments, string[] ExpectedEvidence)[]
            {
                ("OfficialModUiEnabled", new[] { "-StageQaHost", "-AutoOpenOfficialModUi", "-ValidateQaG4RoutingOnly" }, new[] { "ui\\official-mod-ui.png" }),
                ("PauseMenuLayoutEnabled", new[] { "-StageQaHost", "-AutoExercisePauseMenuLayout", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly" }, new[] { "ui\\pause-menu-layout.png" }),
                ("DebugConsoleEnabled", new[] { "-StageQaHost", "-AutoExerciseDebugConsole", "-SaveSlot", "10", "-ValidateQaG4RoutingOnly" }, new[] { "ui\\debug-console.png" }),
                ("SaveSlotsPagingEnabled", new[] { "-StageQaHost", "-AutoExerciseMoreSavesOfficialSaveUi", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly" }, new[] { "ui\\official-save-ui.png" }),
                ("AnimalObservationEnabled", new[] { "-StageQaHost", "-AutoOpenAnimalPanel", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly" }, new[] { "ui\\animal-panel.png" }),
                ("AudioObservationEnabled", new[] { "-StageQaHost", "-AutoExerciseAudioReplacement", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly" }, Array.Empty<string>()),
                ("HatchVoiceEnabled", new[] { "-StageQaHost", "-AutoExerciseHatchAnimalVoice", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly" }, Array.Empty<string>()),
                ("CameraPlayableEnabled", new[] { "-StageQaHost", "-AutoExerciseZoom", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly" }, new[]
                {
                    "camera-playable\\before.png",
                    "camera-playable\\fallback-2x.png",
                    "camera-playable\\movement-end.png",
                    "camera-playable\\movement-mid.png",
                    "camera-playable\\movement-start.png",
                    "camera-playable\\reset.png",
                    "camera-playable\\scale-4x.png",
                    "camera-playable\\telemetry.csv"
                })
            };
            foreach ((string property, string[] arguments, string[] expectedEvidence) in migratedCases)
            {
                using JsonDocument route = RunBatch4Routing(shell, arguments);
                string[] actualEvidence = route.RootElement.GetProperty("ExpectedQaEvidencePaths").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
                Assert(route.RootElement.GetProperty(property).GetBoolean() &&
                    actualEvidence.SequenceEqual(expectedEvidence, StringComparer.OrdinalIgnoreCase),
                    "Every migrated G4 case must project to the QA owner and its exact bounded evidence set when staged; route=" + property +
                    "; expected=" + string.Join("|", expectedEvidence) + "; actual=" + string.Join("|", actualEvidence) + ".");
            }

            using (JsonDocument equipment = RunBatch4Routing(shell,
                "-StageQaHost", "-QaObserveEquipmentSlotsUi", "-UseSteam", "-SkipInstall", "-OfficialModProfile", "Published11", "-IsolateAllOfficialMods", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly"))
            {
                string[] equipmentEvidence = equipment.RootElement.GetProperty("ExpectedQaEvidencePaths").EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
                Assert(equipment.RootElement.GetProperty("ProtocolVersion").GetInt32() == 7 &&
                    equipment.RootElement.GetProperty("EquipmentSlotsObservationEnabled").GetBoolean() &&
                    equipment.RootElement.GetProperty("UseSteam").GetBoolean() &&
                    equipment.RootElement.GetProperty("SkipInstall").GetBoolean() &&
                    !equipment.RootElement.GetProperty("IncludeHookProbe").GetBoolean() &&
                    !equipment.RootElement.GetProperty("AnimalObservationEnabled").GetBoolean() &&
                    !equipment.RootElement.GetProperty("DebugConsoleEnabled").GetBoolean() &&
                    equipmentEvidence.SequenceEqual(new[] { "ui\\equipment-slots.png", "ui\\equipment-slots-summary.txt" }, StringComparer.OrdinalIgnoreCase),
                    "G9 EquipmentSlots UI evidence must project through one independent protocol-7 G4 transaction with exact run-owned artifacts.");
            }

            string equipmentWithoutQa = RunBatch4RoutingFailure(shell,
                "-QaObserveEquipmentSlotsUi", "-OfficialModProfile", "Published11", "-IsolateAllOfficialMods", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly");
            string equipmentWithG5 = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-QaObserveEquipmentSlotsUi", "-AutoExerciseNewContentApis", "-UseSteam", "-SkipInstall", "-OfficialModProfile", "Published11", "-IsolateAllOfficialMods", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly");
            Assert(equipmentWithoutQa.Contains("All automated fixture scenarios require -StageQaHost", StringComparison.Ordinal) &&
                equipmentWithG5.Contains("-QaObserveEquipmentSlotsUi accepts only its bounded observation route (or the exact 1.0 NoNativeSave combination)", StringComparison.Ordinal) &&
                equipmentWithG5.Contains("cannot be combined with: AutoExerciseNewContentApis", StringComparison.Ordinal),
                "EquipmentSlots G4 must reject both the no-QA fallback and any G5 NewContent co-transaction.");

            using (JsonDocument content = RunBatch4Routing(shell,
                "-StageQaHost", "-QaObserveContentMetadata", "-ExpectOilAbsent", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly"))
            using (JsonDocument legacyOilAbsent = RunBatch4Routing(shell,
                "-StageQaHost", "-AutoExerciseNewContentApis", "-ExpectOilAbsent", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly"))
            {
                Assert(content.RootElement.GetProperty("ContentMetadataObservationEnabled").GetBoolean() &&
                    content.RootElement.GetProperty("ContentMetadataExpectOilAbsent").GetBoolean() &&
                    legacyOilAbsent.RootElement.GetProperty("ContentMetadataObservationEnabled").GetBoolean() &&
                    legacyOilAbsent.RootElement.GetProperty("ContentMetadataExpectOilAbsent").GetBoolean(),
                    "Both the explicit observation switch and the legacy AutoExerciseNewContentApis Oil-absent route must project to the same read-only G4 terminal.");
            }

            string contentMutationConflict = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-QaObserveContentMetadata", "-AutoExerciseNewContentApis", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly");
            Assert(contentMutationConflict.Contains("cannot be combined with the positive -AutoExerciseNewContentApis world-mutation route", StringComparison.Ordinal),
                "The read-only content observer must reject the positive AutoExerciseNewContentApis world-mutation route while preserving the legacy Oil-absent read-only route.");

            string missingQaHost = RunBatch4RoutingFailure(shell,
                "-AutoOpenAnimalPanel", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly");
            Assert(missingQaHost.Contains("All automated fixture scenarios require -StageQaHost", StringComparison.Ordinal),
                "G7 must reject every automation request without explicit QA staging; there is no embedded rollback route.");

            string missingSaveSlot = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-AutoOpenAnimalPanel", "-ValidateQaG4RoutingOnly");
            Assert(missingSaveSlot.Contains("require an explicit positive -SaveSlot", StringComparison.Ordinal),
                "A staged save-bound QA case must reject the implicit default slot because it is not a fixture-selection receipt.");

            string saveIsolation = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-AutoExerciseMoreSavesOfficialSaveUi", "-SaveSlot", "3", "-SmokeRootIsolationProfile", "UiRuntime", "-ValidateQaG4RoutingOnly");
            string cameraIsolation = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-AutoExerciseZoom", "-SaveSlot", "3", "-SmokeRootIsolationProfile", "UiRuntime", "-ValidateQaG4RoutingOnly");
            Assert(saveIsolation.Contains("UiRuntime isolation disables the production SaveSlots/Camera roots", StringComparison.Ordinal) &&
                cameraIsolation.Contains("UiRuntime isolation disables the production SaveSlots/Camera roots", StringComparison.Ordinal),
                "UiRuntime isolation must reject both staged SaveSlots and CameraPlayable instead of masquerading as a QA ownership switch.");

            string managerWithSaveCase = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-AutoOpenTitleSettingsStatusPage", "-AutoExercisePauseMenuLayout", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly");
            string managerWithContinuousCase = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-AutoOpenTitleSettingsManagerMvp", "-AutoExerciseTitleButtonLifecycle", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly");
            Assert(new[] { managerWithSaveCase, managerWithContinuousCase }.All(output =>
                    output.Contains("Manager Status/MVP is a title-only G4 route and cannot be combined", StringComparison.Ordinal)),
                "Manager Status/MVP must fail fast when combined with save-bound or continuous G4 routes instead of silently projecting AutoLoadSaveSlot=0 and timing out.");

            string[] externalBase =
            {
                "-StageQaHost", "-RequireExternalPlayerInputGate", "-AutoExerciseDebugConsole",
                "-OfficialModProfile", "CoreUi", "-IsolateAllOfficialMods", "-SaveSlot", "10", "-ValidateQaG4RoutingOnly"
            };
            using (JsonDocument external = RunBatch4Routing(shell,
                externalBase.Concat(new[] { "-UseSteam", "-SkipInstall" }).ToArray()))
            {
                Assert(external.RootElement.GetProperty("ExternalPlayerInputObservationEnabled").GetBoolean() &&
                    external.RootElement.GetProperty("ContinuousHomePageTerminalEnabled").GetBoolean() &&
                    external.RootElement.GetProperty("ContinuousHomePageRequiresSaveLoaded").GetBoolean() &&
                    external.RootElement.GetProperty("UseSteam").GetBoolean() &&
                    external.RootElement.GetProperty("SkipInstall").GetBoolean() &&
                    !external.RootElement.GetProperty("DirectExe").GetBoolean() &&
                    !external.RootElement.GetProperty("IncludeHookProbe").GetBoolean(),
                    "The external-input G4 route must accept only the player-like Steam/no-HookProbe/no-install launch envelope and own its continuous HomePage terminal.");
            }

            string externalMissingSteam = RunBatch4RoutingFailure(shell,
                externalBase.Concat(new[] { "-SkipInstall" }).ToArray());
            string externalMissingSkipInstall = RunBatch4RoutingFailure(shell,
                externalBase.Concat(new[] { "-UseSteam" }).ToArray());
            string externalHookProbe = RunBatch4RoutingFailure(shell,
                externalBase.Concat(new[] { "-UseSteam", "-SkipInstall", "-IncludeHookProbe" }).ToArray());
            string externalDirectExe = RunBatch4RoutingFailure(shell,
                externalBase.Concat(new[] { "-UseSteam", "-SkipInstall", "-DirectExe" }).ToArray());
            Assert(new[] { externalMissingSteam, externalMissingSkipInstall, externalHookProbe, externalDirectExe }.All(output =>
                    output.Contains("requires explicit -UseSteam -SkipInstall, no -DirectExe, and no -IncludeHookProbe", StringComparison.Ordinal)),
                "The external-input G4 gate must reject each of its four launch-envelope violations independently.");

            using (JsonDocument titleLifecycle = RunBatch4Routing(shell,
                "-StageQaHost", "-AutoExerciseTitleButtonLifecycle", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly"))
            using (JsonDocument productRefresh = RunBatch4RoutingCommand(shell,
                "-StageQaHost -AssertProductOwnerRefresh -AutoReloadMods -AutoExerciseActionSpeedConfigApply " +
                "-TitleIdleBeforeSaveSeconds 13 -OfficialModProfile Current -SaveSlot 3 -ValidateQaG4RoutingOnly " +
                "-OfficialModProfileExtraEnabledIds @('Local.Yuuka_DTMAPI_ActionSpeed','Local.DTMAPI_CropHarvestingQA')"))
            {
                Assert(titleLifecycle.RootElement.GetProperty("ContinuousHomePageTerminalEnabled").GetBoolean() &&
                    titleLifecycle.RootElement.GetProperty("ContinuousHomePageRequiresSaveLoaded").GetBoolean() &&
                    productRefresh.RootElement.GetProperty("ContinuousHomePageTerminalEnabled").GetBoolean() &&
                    productRefresh.RootElement.GetProperty("ContinuousHomePageRequiresSaveLoaded").GetBoolean(),
                    "Title lifecycle and product refresh must each project their continuous HomePage terminal to the staged QA host; external input is covered separately as the third route.");
            }

            string continuousMissingSaveSlot = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-AutoExerciseTitleButtonLifecycle", "-ValidateQaG4RoutingOnly");
            Assert(continuousMissingSaveSlot.Contains("require an explicit positive -SaveSlot", StringComparison.Ordinal),
                "A QA-owned continuous HomePage terminal must require an explicit save-slot fixture receipt.");

            using (JsonDocument mouseGive = RunBatch4Routing(shell,
                "-StageQaHost", "-AutoExerciseDebugConsole", "-AutoExerciseDebugConsoleMouseGive", "-SaveSlot", "10", "-ValidateQaG4RoutingOnly"))
            {
                Assert(mouseGive.RootElement.GetProperty("DebugConsoleEnabled").GetBoolean() &&
                    mouseGive.RootElement.GetProperty("DebugConsoleMouseGiveOwner").GetString() == "runner-real-click+qa-G4-observation",
                    "DebugConsole MouseGive must retain the QA lifecycle observer until the runner's real left/right click receipts and full key matrix complete.");
            }
            string debugConsoleWrongSlot = RunBatch4RoutingFailure(shell,
                "-StageQaHost", "-AutoExerciseDebugConsole", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly");
            Assert(debugConsoleWrongSlot.Contains("Y-console test routes require explicit -SaveSlot 10", StringComparison.Ordinal),
                "Every Y-console route must reject the old third-save fixture before any game or shared-runtime work begins.");
            Assert(
                inputSource.Contains("qa-screenshot-render-space", StringComparison.Ordinal) &&
                inputSource.Contains("DebugConsole action=item-give success=True owner=ProductNative", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "Inventory debug give owner=DTMAPI\\.DebugConsoleMod", StringComparison.Ordinal),
                "DebugConsole MouseGive must project the current Unity render-space cell into the Win32 client and verify the current ProductNative item-give receipt.");

            using (JsonDocument oldLifecycle = RunBatch4Routing(shell,
                "-StageQaHost", "-AutoExerciseSaveLoadCycle", "-AutoExerciseSaveLoadCyclePendingPressure", "-SaveSlot", "3", "-ValidateQaG4RoutingOnly"))
            {
                Assert(!oldLifecycle.RootElement.GetProperty("ContinuousHomePageTerminalEnabled").GetBoolean() &&
                    !oldLifecycle.RootElement.GetProperty("ContinuousHomePageRequiresSaveLoaded").GetBoolean(),
                    "SaveLoadCycle and PendingPressure must not project into the G4 continuous-HomePage terminal owner.");
            }
        }

        private static void MoreSavesAcceptanceRoutingRequiresPostTitlePanelAndSaveProtection()
        {
            string repo = FindRepositoryRoot();
            string exerciseSource = ReadSmokeRunnerSource(repo, "phases", "exercise-session.ps1");
            string restorationSource = ReadSmokeRunnerSource(repo, "phases", "restore-session.ps1");
            string preflightSource = ReadSmokeRunnerSource(repo, "phases", "preflight.ps1");
            string saveEnvironmentSource = ReadSmokeRunnerSource(repo, "phases", "enter-save-environment.ps1");
            string savePolicySource = ReadSmokeRunnerSource(repo, "core", "save-policy.ps1");
            string moreSavesSource = ReadSmokeRunnerSource(repo, "scenarios", "more-saves.ps1");
            string shell = GetWindowsPowerShell51Path();
            using JsonDocument route = RunBatch4Routing(
                shell,
                "-StageQaHost",
                "-AutoExerciseMoreSavesOfficialSaveUi",
                "-SaveSlot", "3",
                "-AssertAdvancedProductOwnerDeactivation",
                "-AdvancedProductOwnerDeactivationOwnerIds", "DTMAPI.MoreSavesMod",
                "-AutoExerciseTitleButtonLifecycle",
                "-ValidateQaG4RoutingOnly");
            string[] expectedEvidencePaths = route.RootElement.GetProperty("ExpectedQaEvidencePaths")
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();

            Assert(
                route.RootElement.GetProperty("SaveSlotsPagingEnabled").GetBoolean() &&
                expectedEvidencePaths.Length == 2 &&
                expectedEvidencePaths.Contains("ui\\official-save-ui.png", StringComparer.OrdinalIgnoreCase) &&
                expectedEvidencePaths.Contains("ui\\official-save-ui-post-title.png", StringComparer.OrdinalIgnoreCase),
                "The MoreSaves acceptance route must require both the initial and post-title official save-panel receipts.");

            string projectionFixture = Path.Combine(
                FindRepositoryRoot(),
                "temp",
                "moresaves-fixed12-routing-projection");
            foreach (var phase in new[]
            {
                new
                {
                    Id = "EnabledLifecycle",
                    CaseId = "MoreSavesFixed12EnabledLifecycle",
                    SaveMode = "ArchiveMutation",
                    EnableProduct = true
                },
                new
                {
                    Id = "DisabledCold",
                    CaseId = "MoreSavesFixed12DisabledCold",
                    SaveMode = "NoNativeSave",
                    EnableProduct = false
                },
                new
                {
                    Id = "ReenabledCold",
                    CaseId = "MoreSavesFixed12ReenabledCold",
                    SaveMode = "NoNativeSave",
                    EnableProduct = true
                }
            })
            {
                var arguments = new List<string>
                {
                    "-StageQaHost",
                    "-DirectExe",
                    "-SkipInstall",
                    "-SaveSlot", "7",
                    "-SaveTestMode", phase.SaveMode,
                    "-DisposableSaveFixtureRoot", projectionFixture,
                    "-OfficialModProfile", "CoreOnly",
                    "-IsolateAllOfficialMods",
                    "-MoreSavesFixed12AcceptancePhase", phase.Id,
                    "-ValidateQaG6RoutingOnly"
                };
                if (phase.EnableProduct)
                {
                    arguments.Add("-OfficialModProfileExtraEnabledIds");
                    arguments.Add("Local.DTMAPI_MoreSaves");
                }

                using JsonDocument phaseRoute = RunBatch4Routing(
                    shell,
                    arguments.ToArray());
                string[] cases = phaseRoute.RootElement
                    .GetProperty("Cases")
                    .EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
                Assert(
                    phaseRoute.RootElement.GetProperty("SaveSlotExplicit").GetBoolean() &&
                    phaseRoute.RootElement.GetProperty("SaveSlot").GetInt32() == 7 &&
                    phaseRoute.RootElement.GetProperty("MoreSavesFixed12AcceptancePhase").GetString() == phase.Id &&
                    phaseRoute.RootElement.GetProperty("MoreSavesFixed12OfficialLocalRootMode").GetString() == "LiveOfficialUploadRoot" &&
                    cases.SequenceEqual(new[] { phase.CaseId }, StringComparer.Ordinal),
                    "Each MoreSaves fixed-12 phase must project one exact cold G6 case over disposable slot 7 while retaining the real official Local source root.");
            }

            string unsafeDisabled = RunBatch4RoutingFailure(
                shell,
                "-StageQaHost",
                "-DirectExe",
                "-SkipInstall",
                "-SaveSlot", "7",
                "-SaveTestMode", "NoNativeSave",
                "-DisposableSaveFixtureRoot", projectionFixture,
                "-OfficialModProfile", "CoreOnly",
                "-IsolateAllOfficialMods",
                "-OfficialModProfileExtraEnabledIds", "Local.DTMAPI_MoreSaves",
                "-MoreSavesFixed12AcceptancePhase", "DisabledCold",
                "-ValidateQaG6RoutingOnly");
            Assert(
                unsafeDisabled.Contains("more-saves.ps1", StringComparison.OrdinalIgnoreCase) &&
                unsafeDisabled.Contains("MoreSaves", StringComparison.Ordinal) &&
                unsafeDisabled.Contains("official source", StringComparison.OrdinalIgnoreCase),
                "Changing only the enabled product IDs of the accepted DisabledCold route must be rejected by its official-source boundary. output=" + unsafeDisabled);

            string participantSource = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "src",
                "DTMAPI.GameBridge.DolocTown.QA",
                "QaHostParticipant.cs"));
            Assert(
                preflightSource.Contains("$archiveMutationRouteRequested =", StringComparison.Ordinal) &&
                preflightSource.Contains("$MoreSavesFixed12AcceptancePhase -eq 'EnabledLifecycle'", StringComparison.Ordinal) &&
                exerciseSource.Contains("$MoreSavesFixed12AcceptancePhase -in @('EnabledLifecycle', 'ReenabledCold')", StringComparison.Ordinal) &&
                moreSavesSource.Contains("'LiveOfficialUploadRoot'", StringComparison.Ordinal) &&
                saveEnvironmentSource.Contains("Get-DolocTownLivePersistentRootForSmoke", StringComparison.Ordinal) &&
                preflightSource.Contains("The MoreSaves archive-management route requires SaveTestMode ArchiveMutation", StringComparison.Ordinal) &&
                preflightSource.Contains("requires -DisposableSaveFixtureRoot", StringComparison.Ordinal) &&
                savePolicySource.Contains("steamAutoCloudIsolated", StringComparison.Ordinal) &&
                restorationSource.Contains("PlayerArchiveWritebackPerformed = $false", StringComparison.Ordinal) &&
                participantSource.Contains("string.Equals(item, \"MoreSavesPostTitlePanel\", StringComparison.Ordinal)", StringComparison.Ordinal),
                "The MoreSaves archive-management route must isolate native SAVE/DTMAPI state, retain the real official Local source root, and forbid player archive writeback.");
        }

        private static void MoreEquipmentSlotsAcceptanceRoutingRequiresProtectedThirdSaveAndExactOwnerCleanup()
        {
            string shell = GetWindowsPowerShell51Path();
            using JsonDocument g5Route = RunBatch4Routing(
                shell,
                "-StageQaHost",
                "-AutoExerciseMoreEquipmentSlots",
                "-SaveSlot", "3",
                "-ValidateQaG5RoutingOnly");
            string[] cases = g5Route.RootElement.GetProperty("Cases")
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
            Assert(
                g5Route.RootElement.GetProperty("SaveSlotExplicit").GetBoolean() &&
                g5Route.RootElement.GetProperty("SaveSlot").GetInt32() == 3 &&
                cases.SequenceEqual(new[] { "MoreEquipmentSlots" }, StringComparer.Ordinal),
                "The MoreEquipmentSlots behavior route must be one explicit third-save G5 transaction.");

            using JsonDocument ownerRoute = RunBatch4Routing(
                shell,
                "-StageQaHost",
                "-AutoExerciseMoreEquipmentSlots",
                "-SaveSlot", "3",
                "-AssertAdvancedProductOwnerDeactivation",
                "-AdvancedProductOwnerDeactivationOwnerIds", "DTMAPI.MoreEquipmentSlotsMod",
                "-AutoExerciseTitleButtonLifecycle",
                "-ValidateQaG4RoutingOnly");
            Assert(
                ownerRoute.RootElement.GetProperty("AdvancedProductOwnerDeactivationEnabled").GetBoolean() &&
                ownerRoute.RootElement.GetProperty("ContinuousHomePageTerminalEnabled").GetBoolean() &&
                ownerRoute.RootElement.GetProperty("ContinuousHomePageRequiresSaveLoaded").GetBoolean(),
                "The MoreEquipmentSlots Loader-deactivation route must run only after the third-save title-return terminal.");

            string coldRoutePaused = RunBatch4RoutingFailure(
                shell,
                "-StageQaHost",
                "-AssertMoreEquipmentSlotsColdRecovery",
                "-ValidateQaG4RoutingOnly");
            Assert(
                coldRoutePaused.Contains("is paused", StringComparison.Ordinal) &&
                coldRoutePaused.Contains("three-process", StringComparison.Ordinal) &&
                coldRoutePaused.Contains("ColdPrepare", StringComparison.Ordinal) &&
                coldRoutePaused.Contains("ColdCommit", StringComparison.Ordinal) &&
                coldRoutePaused.Contains("ColdObserve", StringComparison.Ordinal),
                "The former one-process cold recovery route must fail closed and point to the dedicated disposable three-process acceptance route.");

            string missingBehavior = RunBatch4RoutingFailure(
                shell,
                "-StageQaHost",
                "-SaveSlot", "3",
                "-AssertAdvancedProductOwnerDeactivation",
                "-AdvancedProductOwnerDeactivationOwnerIds", "DTMAPI.MoreEquipmentSlotsMod",
                "-AutoExerciseTitleButtonLifecycle",
                "-ValidateQaG4RoutingOnly");
            string implicitSave = RunBatch4RoutingFailure(
                shell,
                "-StageQaHost",
                "-AutoExerciseMoreEquipmentSlots",
                "-ValidateQaG5RoutingOnly");
            Assert(
                missingBehavior.Contains("requires the matching in-save behavior request", StringComparison.Ordinal) &&
                implicitSave.Contains("requires explicit -SaveSlot 3", StringComparison.Ordinal),
                "The MoreEquipmentSlots route must reject owner cleanup without behavior evidence and an implicit save fixture.");

            string repo = FindRepositoryRoot();
            string assessmentSource = ReadSmokeRunnerSource(repo, "phases", "assess-evidence.ps1");
            string exerciseSource = ReadSmokeRunnerSource(repo, "phases", "exercise-session.ps1");
            string preparationSource = ReadSmokeRunnerSource(repo, "phases", "prepare-session.ps1");
            string restorationSource = ReadSmokeRunnerSource(repo, "phases", "restore-session.ps1");
            string preflightSource = ReadSmokeRunnerSource(repo, "phases", "preflight.ps1");
            string deploymentSource = ReadSmokeRunnerSource(repo, "phases", "deploy-session.ps1");
            string equipmentSource = ReadSmokeRunnerSource(repo, "scenarios", "more-equipment.ps1");
            string fixtureSource = File.ReadAllText(Path.Combine(
                repo,
                "src",
                "DTMAPI.GameBridge.DolocTown.QA",
                "Scenarios",
                "Fixtures",
                "AdvancedProductOwnerDeactivationFixture.cs"));
            string uiAcceptanceSource = File.ReadAllText(Path.Combine(
                repo,
                "src",
                "DTMAPI.GameBridge.DolocTown.QA",
                "Scenarios",
                "Fixtures",
                "MoreEquipmentSlots100UiFixtureCase.cs"));
            string coldHostSource = File.ReadAllText(Path.Combine(
                repo,
                "src",
                "DTMAPI.GameBridge.DolocTown.Compatibility",
                "EquipmentSlots",
                "EquipmentSlotsProductColdRecovery.cs"));
            string compatibilityServiceSource = File.ReadAllText(
                Path.Combine(
                    repo,
                    "src",
                    "DTMAPI.GameBridge.DolocTown",
                    "Compatibility",
                    "EquipmentSlots",
                    "EquipmentSlotsCompatibilityService.cs"));
            string compatibilityProject = File.ReadAllText(Path.Combine(
                repo,
                "src",
                "DTMAPI.GameBridge.DolocTown.Compatibility",
                "DTMAPI.GameBridge.DolocTown.Compatibility.csproj"));
            string coldAcceptanceSource = File.ReadAllText(Path.Combine(
                repo,
                "tools",
                "scripts",
                "run-moreequipment-cold-recovery-acceptance.ps1"));
            string transitionFixtureSource = File.ReadAllText(Path.Combine(
                repo,
                "src",
                "DTMAPI.GameBridge.DolocTown.QA",
                "Scenarios",
                "Fixtures",
                "MoreEquipmentSlotsTransitionFixtureCase.cs"));
            Assert(
                preparationSource.Contains("$preservePlayerSaveFiles = $false", StringComparison.Ordinal) &&
                restorationSource.Contains("committed-sidecar-unchanged-before-cleanup.json", StringComparison.Ordinal) &&
                assessmentSource.Contains("$g5CommittedSidecarUnchangedOk", StringComparison.Ordinal) &&
                restorationSource.IndexOf("committed-sidecar:changed-before-config-cleanup", StringComparison.Ordinal) <
                    restorationSource.IndexOf("strong-planting-gun-config:verification-failed", StringComparison.Ordinal) &&
                preparationSource.Contains("ConfigDirectoryTransaction = 'forbidden'", StringComparison.Ordinal) &&
                exerciseSource.Contains("Smoke exercise MoreEquipmentSlots OK", StringComparison.Ordinal) &&
                exerciseSource.Contains("MoreEquipmentSlots protected transaction committed destination=(Backpack|Mail)' -TimeoutSeconds $TimeoutSeconds -Regex", StringComparison.Ordinal) &&
                preflightSource.Contains("AssertMoreEquipmentSlotsColdRecovery", StringComparison.Ordinal) &&
                preflightSource.Contains("the former one-process NoNativeSave route could pass without a native SaveGame/SaveSaved commit or a second cold-process replay check", StringComparison.Ordinal) &&
                preflightSource.Contains("dedicated disposable three-process ColdPrepare/ColdCommit/ColdObserve route", StringComparison.Ordinal) &&
                preflightSource.Contains("$moreEquipmentSlotsColdRecoveryTransitionRequested", StringComparison.Ordinal) &&
                exerciseSource.Contains("MoreEquipmentSlotsTransitionPhase -eq 'ColdCommit'", StringComparison.Ordinal) &&
                equipmentSource.Contains("function Test-SmokeMoreEquipmentSlotsOfficialPackage", StringComparison.Ordinal) &&
                equipmentSource.Contains("[Array]::Sort($relativeFiles, [System.StringComparer]::Ordinal)", StringComparison.Ordinal) &&
                equipmentSource.Contains("[Array]::Sort($expectedFileSet, [System.StringComparer]::Ordinal)", StringComparison.Ordinal) &&
                equipmentSource.Contains("MinimumDTMApiVersion -ceq '0.6.0'", StringComparison.Ordinal) &&
                equipmentSource.Contains("referencePolicySha256", StringComparison.Ordinal) &&
                equipmentSource.Contains("entryDllLength", StringComparison.Ordinal) &&
                equipmentSource.Contains("harmonyOwner", StringComparison.Ordinal) &&
                equipmentSource.Contains("Advanced receipt reference row", StringComparison.Ordinal) &&
                exerciseSource.Contains("EquipmentSlots cold compatibility demand detected source=(Sidecar|Journal)' -TimeoutSeconds $TimeoutSeconds -Regex", StringComparison.Ordinal) &&
                exerciseSource.Contains("Hook status: Compatibility\\.Host = resident\\..*service=EquipmentSlots' -TimeoutSeconds $TimeoutSeconds -Regex", StringComparison.Ordinal) &&
                exerciseSource.Contains("DTMAPI\\.MoreEquipmentSlotsMod recovered=1 message=.*(native backpack|native item mail)' -TimeoutSeconds $TimeoutSeconds -Regex", StringComparison.Ordinal) &&
                assessmentSource.Contains("MoreEquipmentSlots product-owned Hook set installed|MoreEquipmentSlots ProductNative ready", StringComparison.Ordinal) &&
                deploymentSource.Contains("more-equipment-slots-cold-official-package.json", StringComparison.Ordinal) &&
                deploymentSource.Contains("Local.DTMAPI_MoreEquipmentSlots", StringComparison.Ordinal) &&
                deploymentSource.Contains("MODS/DTMAPI_MoreEquipmentSlots", StringComparison.Ordinal) &&
                equipmentSource.Contains("[string]$receipt.referencePolicyId -ceq $policyId", StringComparison.Ordinal) &&
                deploymentSource.Contains("more-equipment-slots-cold-official-disable.json", StringComparison.Ordinal) &&
                deploymentSource.Contains("Set-SmokeRecoveryOnlyAuthorSourceState", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "Set-SmokeLocal11AuthorSourceState", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "game Mods directory", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "Mods/DTMAPI.MoreEquipmentSlotsMod", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "more-equipment-slots-cold-marker", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "more-equipment-slots-cold-fixture-stage.json", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "more-equipment-slots-cold-fixture-restore.json", StringComparison.Ordinal) &&
                coldAcceptanceSource.Contains("Id = 'ColdPrepare'", StringComparison.Ordinal) &&
                coldAcceptanceSource.Contains("Id = 'ColdCommit'", StringComparison.Ordinal) &&
                coldAcceptanceSource.Contains("Id = 'ColdObserve'", StringComparison.Ordinal) &&
                coldAcceptanceSource.Contains("wait-runtime-lock.ps1", StringComparison.Ordinal) &&
                coldAcceptanceSource.Contains("release-runtime-lock.ps1", StringComparison.Ordinal) &&
                coldAcceptanceSource.Contains("'PlayerSaveUnchangedBeforeCleanup'", StringComparison.Ordinal) &&
                coldAcceptanceSource.Contains("'CommittedSidecarsUnchangedBeforeCleanup'", StringComparison.Ordinal) &&
                transitionFixtureSource.Contains("StageMoreEquipmentSlotsProductColdSeed", StringComparison.Ordinal) &&
                transitionFixtureSource.Contains("ReadMoreEquipmentSlotsProductNativeScope", StringComparison.Ordinal) &&
                transitionFixtureSource.Contains("TotalGameSeconds = totalGameSeconds", StringComparison.Ordinal) &&
                transitionFixtureSource.Contains("equipmentProductColdRecoverySessions=", StringComparison.Ordinal) &&
                coldHostSource.Contains("DTMAPI.MoreEquipmentSlotsMod", StringComparison.Ordinal) &&
                coldHostSource.Contains("ProductNative assembly is loaded; dormant Compatibility Host refused to take over its storage.", StringComparison.Ordinal) &&
                coldHostSource.Contains("GetProductNativeSaveFingerprint", StringComparison.Ordinal) &&
                coldHostSource.Contains("journal.Replacements.Count > 0", StringComparison.Ordinal) &&
                coldHostSource.Contains("ProductDocumentStore.TryLoadValidated(", StringComparison.Ordinal) &&
                coldHostSource.Contains("DiscardUncommittedReplacementForColdRecovery", StringComparison.Ordinal) &&
                coldHostSource.Contains("finalizedReplacement", StringComparison.Ordinal) &&
                compatibilityServiceSource.Contains("string exactProductPath = Path.Combine(", StringComparison.Ordinal) &&
                compatibilityServiceSource.Contains("TryRecoverMoreEquipmentSlotsProductStorage(", StringComparison.Ordinal) &&
                compatibilityProject.Contains("ProductSchema\\EquipmentSlotDocumentStore.cs", StringComparison.Ordinal) &&
                compatibilityProject.Contains("ProductSchema\\EquipmentSlotStorageFormatProbe.cs", StringComparison.Ordinal) &&
                !compatibilityProject.Contains("ProductSchema\\EquipmentSlotLegacyMigration.cs", StringComparison.Ordinal) &&
                !compatibilityProject.Contains("ProductSchema\\EquipmentSlotDocumentStore.Migration.cs", StringComparison.Ordinal) &&
                fixtureSource.Contains("DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks", StringComparison.Ordinal) &&
                fixtureSource.Contains("MoreEquipmentSlots=actual5+targets5+callback1->instance0+actual0+callback0+clones0+listeners0+functions0+roots0", StringComparison.Ordinal) &&
                fixtureSource.Contains("TryGetShieldItem", StringComparison.Ordinal) &&
                fixtureSource.Contains("get_allSelectablesArray", StringComparison.Ordinal) &&
                fixtureSource.Contains("CleanupSummaryProvesZero", StringComparison.Ordinal) &&
                uiAcceptanceSource.Contains("TryObserveMoreEquipmentSlotsStableReopenSurface", StringComparison.Ordinal) &&
                uiAcceptanceSource.Contains("five consecutive stable in-viewport frames", StringComparison.Ordinal) &&
                uiAcceptanceSource.Contains("RequireMoreEquipmentSlotsViewportContainment", StringComparison.Ordinal) &&
                uiAcceptanceSource.Contains("ViewportContainment=exact", StringComparison.Ordinal) &&
                uiAcceptanceSource.Contains("!ReadMoreEquipmentSlotsIgnoreLayout(productRow)", StringComparison.Ordinal) &&
                uiAcceptanceSource.Contains("NativeTailOrder=exact", StringComparison.Ordinal) &&
                !uiAcceptanceSource.Contains("DrawerExpanded", StringComparison.Ordinal),
                "The MoreEquipmentSlots route must protect the save/config transaction, bind current five-target Product readiness, require a stable visible reopen surface, and verify exact callback/root cleanup through the real Loader.");
        }

        private static void ZoomAcceptanceRoutingRequiresProductNativeTitleAndExactOwnerCleanup()
        {
            string shell = GetWindowsPowerShell51Path();
            using JsonDocument g5Route = RunBatch4Routing(
                shell,
                "-StageQaHost",
                "-AutoExerciseZoomProductNative",
                "-SaveSlot", "3",
                "-ValidateQaG5RoutingOnly");
            string[] cases = g5Route.RootElement.GetProperty("Cases")
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
            Assert(
                g5Route.RootElement.GetProperty("SaveSlotExplicit").GetBoolean() &&
                g5Route.RootElement.GetProperty("SaveSlot").GetInt32() == 3 &&
                cases.SequenceEqual(new[] { "ZoomProductNative" }, StringComparer.Ordinal),
                "Zoom ProductNative behavior must use one explicit third-save G5 route.");

            using JsonDocument ownerRoute = RunBatch4Routing(
                shell,
                "-StageQaHost",
                "-AutoExerciseZoomProductNative",
                "-AutoExerciseTitleButtonLifecycle",
                "-AssertAdvancedProductOwnerDeactivation",
                "-AdvancedProductOwnerDeactivationOwnerIds", "DTMAPI.ZoomMod",
                "-SaveSlot", "3",
                "-ValidateQaG4RoutingOnly");
            Assert(
                ownerRoute.RootElement.GetProperty("AdvancedProductOwnerDeactivationEnabled").GetBoolean() &&
                ownerRoute.RootElement.GetProperty("ContinuousHomePageTerminalEnabled").GetBoolean() &&
                ownerRoute.RootElement.GetProperty("ContinuousHomePageRequiresSaveLoaded").GetBoolean(),
                "Zoom exact-owner deactivation must run after the real ReturnedToTitle terminal.");

            string missingBehavior = RunBatch4RoutingFailure(
                shell,
                "-StageQaHost",
                "-AutoExerciseTitleButtonLifecycle",
                "-AssertAdvancedProductOwnerDeactivation",
                "-AdvancedProductOwnerDeactivationOwnerIds", "DTMAPI.ZoomMod",
                "-SaveSlot", "3",
                "-ValidateQaG4RoutingOnly");
            Assert(
                missingBehavior.Contains("requires the matching in-save behavior request", StringComparison.Ordinal),
                "Zoom owner cleanup must reject a route without ProductNative behavior evidence.");

            string repo = FindRepositoryRoot();
            string runnerSource = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "run-game-smoke.ps1"));
            string behaviorFixture = File.ReadAllText(Path.Combine(
                repo,
                "src",
                "DTMAPI.GameBridge.DolocTown.QA",
                "Scenarios",
                "Fixtures",
                "ZoomProductNativeFixtureCase.cs"));
            string ownerFixture = File.ReadAllText(Path.Combine(
                repo,
                "src",
                "DTMAPI.GameBridge.DolocTown.QA",
                "Scenarios",
                "Fixtures",
                "AdvancedProductOwnerDeactivationFixture.cs"));
            Assert(
                !SmokeRunnerSourcesContain(repo, "AutoExerciseZoomOwnerLifetime", StringComparison.Ordinal) &&
                !SmokeRunnerSourcesContain(repo, "Smoke.ZoomOwnerLifetime", StringComparison.Ordinal) &&
                runnerSource.Contains("AutoExerciseZoomProductNative", StringComparison.Ordinal) &&
                behaviorFixture.Contains("titlePendingScale=4", StringComparison.Ordinal) &&
                behaviorFixture.Contains("DolocAPI.SetEnvCamera", StringComparison.Ordinal) &&
                behaviorFixture.Contains("\"DolocTown.CameraController\"", StringComparison.Ordinal) &&
                !behaviorFixture.Contains("\"CameraController\",", StringComparison.Ordinal) &&
                behaviorFixture.Contains("expectedOwnerPatchCount: 2", StringComparison.Ordinal) &&
                ownerFixture.Contains("ReturnedToTitle", StringComparison.Ordinal) &&
                ownerFixture.Contains("zoomBefore.ExactOwnerPatchCount != 3", StringComparison.Ordinal) &&
                ownerFixture.Contains("required two-target, three-patch", StringComparison.Ordinal) &&
                ownerFixture.Contains("Zoom=title4to1+native1+derived1+actual1+callback1->instance0+actual0+", StringComparison.Ordinal) &&
                ownerFixture.Contains("callback0+roots0", StringComparison.Ordinal),
                "Zoom acceptance must prove ProductNative 4x at the title boundary, real native reset reapply, 4x-to-1x title restore, and exact Loader cleanup without the retired Strict seam.");
        }

        private static string ReadSmokeRunnerSource(string repo, string area, string file)
        {
            return File.ReadAllText(Path.Combine(repo, "tools", "scripts", "game-smoke", area, file));
        }

        private static bool SmokeRunnerSourcesContain(string repo, string text, StringComparison comparison)
        {
            // Retired channels are forbidden across the facade and every current module.
            // Positive contracts and ordering checks read their exact owner files above.
            string scripts = Path.Combine(repo, "tools", "scripts");
            return File.ReadAllText(Path.Combine(scripts, "run-game-smoke.ps1")).Contains(text, comparison) ||
                Directory.EnumerateFiles(Path.Combine(scripts, "game-smoke"), "*.ps1", SearchOption.AllDirectories)
                    .Any(path => File.ReadAllText(path).Contains(text, comparison));
        }

        private static JsonDocument RunBatch4RoutingCommand(string shell, string scriptArguments)
        {
            string runner = Path.Combine(FindRepositoryRoot(), "tools", "scripts", "run-game-smoke.ps1");
            string commandText = "& '" + runner.Replace("'", "''", StringComparison.Ordinal) + "' " + scriptArguments;
            WindowsPowerShellResult result = RunWindowsPowerShell(shell, new[]
            {
                "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", commandText
            });
            if (result.ExitCode != 0)
                throw new InvalidOperationException("Batch 4 Windows PowerShell command routing probe failed: " + result.Error + result.Output);
            return JsonDocument.Parse(result.Output);
        }

        private static JsonDocument RunBatch4Routing(string shell, params string[] arguments)
        {
            WindowsPowerShellResult result = RunBatch4Runner(shell, arguments);
            if (result.ExitCode != 0)
                throw new InvalidOperationException("Batch 4 Windows PowerShell routing probe failed: " + result.Error + result.Output);
            return JsonDocument.Parse(result.Output);
        }

        private static string RunBatch4RoutingFailure(string shell, params string[] arguments)
        {
            WindowsPowerShellResult result = RunBatch4Runner(shell, arguments);
            Assert(result.ExitCode != 0, "The Batch 4 Windows PowerShell routing probe unexpectedly succeeded: " + string.Join(" ", arguments));
            return result.Error + Environment.NewLine + result.Output;
        }

        private static WindowsPowerShellResult RunBatch4Runner(string shell, params string[] arguments)
        {
            string runner = Path.Combine(FindRepositoryRoot(), "tools", "scripts", "run-game-smoke.ps1");
            var command = new List<string>
            {
                "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", runner
            };
            command.AddRange(arguments);
            return RunWindowsPowerShell(shell, command);
        }

        private static WindowsPowerShellResult RunWindowsPowerShell(string shell, IEnumerable<string> arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = shell,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Windows PowerShell 5.1 for the Batch 4 routing matrix.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new WindowsPowerShellResult(process.ExitCode, output, error);
        }

        private static string GetWindowsPowerShell51Path()
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string shell = Path.Combine(windows, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            Assert(File.Exists(shell), "Windows PowerShell 5.1 is required for the Batch 4 runner routing matrix: " + shell);
            return shell;
        }

        private readonly struct WindowsPowerShellResult
        {
            internal WindowsPowerShellResult(int exitCode, string output, string error)
            {
                ExitCode = exitCode;
                Output = output;
                Error = error;
            }

            internal int ExitCode { get; }
            internal string Output { get; }
            internal string Error { get; }
        }
    }
}
