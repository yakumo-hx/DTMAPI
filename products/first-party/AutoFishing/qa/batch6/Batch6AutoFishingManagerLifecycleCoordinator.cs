using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class Batch6AutoFishingManagerLifecycleCoordinator
    {
        private enum LifecycleState
        {
            Created,
            WaitingForSaveLoaded,
            WaitingForInitialEnable,
            WaitingForNativeCast,
            WaitingForMarkerCreate,
            WaitingForFirstReload,
            WaitingForMarkerRemove,
            WaitingForSecondReload,
            Passed,
            Failed
        }

        private readonly GameBridgeFixtureAccess access;
        private readonly Batch6AutoFishingManagerLifecycleSettings settings;
        private readonly Func<G4FixtureStepResult> requestProductOwnerRefresh;
        private readonly Batch6AutoFishingReflectionObserver observer = new Batch6AutoFishingReflectionObserver();
        private readonly Batch6AutoFishingManagerLifecycleEvidenceWriter writer;
        private LifecycleState state;
        private DateTimeOffset stateEnteredAtUtc;
        private DateTimeOffset nextObservationAtUtc;
        private string terminalDetails = string.Empty;

        internal Batch6AutoFishingManagerLifecycleCoordinator(
            GameBridgeFixtureAccess access,
            Batch6AutoFishingManagerLifecycleSettings settings,
            Func<G4FixtureStepResult> requestProductOwnerRefresh)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.requestProductOwnerRefresh = requestProductOwnerRefresh ?? throw new ArgumentNullException(nameof(requestProductOwnerRefresh));
            DateTimeOffset now = DateTimeOffset.UtcNow;
            writer = new Batch6AutoFishingManagerLifecycleEvidenceWriter(access.RuntimeEvidenceRoot, access.RunId, settings, now);
            state = LifecycleState.Created;
            stateEnteredAtUtc = now;
            nextObservationAtUtc = now;
        }

        internal bool Enabled => settings.Enabled;
        internal bool Terminal => state == LifecycleState.Passed || state == LifecycleState.Failed;
        internal string ResultPath => writer.ResultPath;

        internal void Start()
        {
            if (!Enabled || state != LifecycleState.Created)
                throw new InvalidOperationException("Batch6AutoFishingManagerLifecycle may start exactly once while enabled.");
            try
            {
                Batch6AutoFishingPackageProvenance package = Batch6AutoFishingPackageVerifier.Verify(
                    settings.ExpectedProductRoot,
                    settings.CreatePackageVerificationSettings());
                writer.BindPackage(package);
                if (settings.IsSameProcessDisable && File.Exists(settings.DisabledMarkerPath))
                    throw new InvalidDataException("SameProcessDisable requires dtmapi.disabled to be absent before the clean process starts.");
                if (settings.IsColdDisabled && !MarkerMatchesExpected())
                    throw new InvalidDataException("ColdDisabled requires the exact receipt-bound dtmapi.disabled marker before process startup.");

                Batch6AutoFishingObservation initial = Observe(DateTimeOffset.UtcNow);
                if (settings.IsSameProcessDisable)
                    RequireLoadedProduct(initial, "manager lifecycle initial state");
                else
                    RequireColdDisabled(initial, "manager lifecycle cold startup before SaveLoaded");
                writer.SetInitial(initial, initial.CastAppliedCount);
                MoveTo(LifecycleState.WaitingForSaveLoaded, "Package and initial manager state verified; waiting for authoritative fifth SaveLoaded.", initial);
            }
            catch (Exception ex)
            {
                Fail("manager-lifecycle-start-failed", ex);
            }
        }

        internal void OnSaveLoaded(int? slot, bool isNewGame)
        {
            if (!Enabled || Terminal)
                return;
            try
            {
                if (!slot.HasValue || slot.Value != 4 || isNewGame)
                    throw new InvalidDataException("Batch6AutoFishingManagerLifecycle requires existing fifth save index 4.");
                if (state != LifecycleState.WaitingForSaveLoaded)
                    throw new InvalidOperationException("Unexpected fifth SaveLoaded in manager lifecycle state " + state + ".");
                Batch6AutoFishingObservation observation = Observe(DateTimeOffset.UtcNow);
                if (settings.IsColdDisabled)
                {
                    RequireColdDisabled(observation, "cold-disabled fifth-save state");
                    writer.SetColdDisabled(observation, notRestartRequired: true);
                    Pass("Cold-disabled clean process proved assembly/instance/loaded/root/callback/patch absence and no restart-required diagnostic.", observation);
                    return;
                }
                RequireLoadedProduct(observation, "same-process fifth-save initial state");
                MoveTo(LifecycleState.WaitingForInitialEnable, "Fifth save loaded with exactly one inactive product owner; waiting for runner physical F6.", observation);
                PublishHandshake("awaiting-initial-enable-f6", "fifth-save-ready");
            }
            catch (Exception ex)
            {
                Fail("manager-lifecycle-save-loaded-failed", ex);
            }
        }

        internal void OnWorkshopReloadCompleted()
        {
            if (!Enabled || Terminal || !settings.IsSameProcessDisable)
                return;
            if (writer.Evidence.WorkshopReloadReceiptCount < writer.Evidence.ReloadRequestCount)
            {
                writer.RecordWorkshopReloadReceipt();
                access.Log(Batch6AutoFishingManagerLifecycleSettings.OverallHookId +
                    " observed real ModManager.ReloadMods Postfix receipt ordinal=" +
                    writer.Evidence.WorkshopReloadReceiptCount.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        internal G4FixtureStepResult Advance()
        {
            if (!Enabled)
                return G4FixtureStepResult.Failed("Batch6AutoFishingManagerLifecycle was selected without Enabled=true.");
            if (state == LifecycleState.Passed)
                return G4FixtureStepResult.Verified(terminalDetails);
            if (state == LifecycleState.Failed)
                return G4FixtureStepResult.Failed(terminalDetails);
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (now - stateEnteredAtUtc > TimeSpan.FromSeconds(300))
                    throw new TimeoutException("Batch6AutoFishingManagerLifecycle timed out in state " + state + ".");
                if (state == LifecycleState.WaitingForSaveLoaded || now < nextObservationAtUtc)
                    return G4FixtureStepResult.Pending(Details("state=" + state));
                nextObservationAtUtc = now.AddMilliseconds(Batch6AutoFishingPilotSettings.ObservationCadenceMilliseconds);
                Batch6AutoFishingObservation observation = Observe(now);

                switch (state)
                {
                    case LifecycleState.WaitingForInitialEnable:
                        RequireLoadedProduct(observation, "waiting for initial enable");
                        if (!IsActive(observation))
                            return G4FixtureStepResult.Pending(Details("awaiting-initial-enable-f6"));
                        MoveTo(LifecycleState.WaitingForNativeCast, "Real product session is active; waiting for at least one native cast before manager mutation.", observation);
                        break;

                    case LifecycleState.WaitingForNativeCast:
                        RequireLoadedProduct(observation, "waiting for native cast");
                        if (!IsActive(observation) || observation.CastAppliedCount - writer.Evidence.NativeCastBaseline < 1)
                            return G4FixtureStepResult.Pending(Details("awaiting-active-native-cast"));
                        writer.SetActiveWithNativeCast(observation);
                        MoveTo(LifecycleState.WaitingForMarkerCreate, "Active session and native cast observed; runner may create the exact disabled marker.", observation);
                        PublishHandshake("awaiting-create-disabled-marker", "marker=" + settings.DisabledMarkerPath + "; sha256=" + settings.ExpectedDisabledMarkerSha256);
                        break;

                    case LifecycleState.WaitingForMarkerCreate:
                        RequireLoadedProduct(observation, "waiting for disabled marker creation");
                        if (!File.Exists(settings.DisabledMarkerPath))
                            return G4FixtureStepResult.Pending(Details("awaiting-exact-disabled-marker"));
                        if (!MarkerMatchesExpected())
                            throw new InvalidDataException("Runner-created dtmapi.disabled does not match the exact expected SHA-256.");
                        writer.MarkMarkerCreateObserved();
                        MoveTo(LifecycleState.WaitingForFirstReload, "Exact marker observed; requesting the first real ModManager.ReloadMods through QaScenarioController.", observation);
                        RequestReload("first-marker-disable");
                        break;

                    case LifecycleState.WaitingForFirstReload:
                        if (writer.Evidence.WorkshopReloadReceiptCount < 1)
                            return G4FixtureStepResult.Pending(Details("awaiting-first-workshop-reload-receipt"));
                        RequireSameProcessRemoved(observation, "after first marker-driven reload");
                        writer.SetAfterFirstReload(observation);
                        MoveTo(LifecycleState.WaitingForMarkerRemove, "First reload removed all Core/product roots and left restart-required; runner may remove the marker.", observation);
                        PublishHandshake("awaiting-remove-disabled-marker", "same-process-reentry-must-remain-blocked");
                        break;

                    case LifecycleState.WaitingForMarkerRemove:
                        RequireSameProcessRemoved(observation, "waiting for marker removal");
                        if (File.Exists(settings.DisabledMarkerPath))
                            return G4FixtureStepResult.Pending(Details("awaiting-disabled-marker-removal"));
                        writer.MarkMarkerRemoveObserved();
                        MoveTo(LifecycleState.WaitingForSecondReload, "Marker removal observed; requesting the second real ModManager.ReloadMods in the same Mono process.", observation);
                        RequestReload("second-marker-removed-no-reentry");
                        break;

                    case LifecycleState.WaitingForSecondReload:
                        if (writer.Evidence.WorkshopReloadReceiptCount < 2)
                            return G4FixtureStepResult.Pending(Details("awaiting-second-workshop-reload-receipt"));
                        RequireSameProcessRemoved(observation, "after second marker-removed reload");
                        writer.SetAfterSecondReload(observation, reentryBlocked: true);
                        Pass("Same-process marker disable removed exact Core/product roots; marker removal plus second real reload could not re-enter and retained restart-required.", observation);
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported manager lifecycle state " + state + ".");
                }
                return state == LifecycleState.Passed
                    ? G4FixtureStepResult.Verified(terminalDetails)
                    : G4FixtureStepResult.Pending(Details("state=" + state));
            }
            catch (Exception ex)
            {
                Fail("manager-lifecycle-advance-failed", ex);
                return G4FixtureStepResult.Failed(terminalDetails);
            }
        }

        internal void Close(string reason)
        {
            if (!Enabled || Terminal)
                return;
            Fail("host-closed-before-terminal", new InvalidOperationException("QA host closed before manager lifecycle terminal. reason=" + (reason ?? string.Empty)));
        }

        private Batch6AutoFishingObservation Observe(DateTimeOffset now)
        {
            Batch6AutoFishingObservation observation = observer.Observe(access.Runtime, now);
            observation.CoreOwnerRootCount = access.Runtime.CountCoreOwnerRoots(Batch6AutoFishingPilotSettings.ProductUniqueId);
            observation.CoreInstanceCount = access.Runtime.HasOwnerInstance(Batch6AutoFishingPilotSettings.ProductUniqueId) ? 1 : 0;
            observation.LoadedOwnerCount = access.Runtime.LoadedMods.Count(mod =>
                mod.Manifest.UniqueID.Equals(Batch6AutoFishingPilotSettings.ProductUniqueId, StringComparison.OrdinalIgnoreCase));
            observation.OwnerRequiresRestart = access.Runtime.OwnerRequiresRestart(Batch6AutoFishingPilotSettings.ProductUniqueId);
            IDtmModStatusInfo? status = access.GetDiagnosticsSnapshot().Mods.FirstOrDefault(item =>
                item.UniqueID.Equals(Batch6AutoFishingPilotSettings.ProductUniqueId, StringComparison.OrdinalIgnoreCase));
            observation.DiagnosticsStatusCode = status?.StatusCode ?? string.Empty;
            return observation;
        }

        private void RequireLoadedProduct(Batch6AutoFishingObservation observation, string phase)
        {
            if (!observation.ProductPresent || !observation.ProductAssemblyLoaded || observation.CoreInstanceCount != 1 ||
                observation.LoadedOwnerCount != 1 || observation.InstalledPatchCount != Batch6AutoFishingPilotSettings.ExpectedProductPatchCount ||
                !observation.ProductCallbackRuntimePresent || observation.CanonicalHarmonyPatchCount != Batch6AutoFishingPilotSettings.ExpectedProductPatchCount ||
                observation.OwnerRequiresRestart || observation.DiagnosticsStatusCode.Equals("restart-required", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Manager lifecycle expected exactly one loaded product with callback root and 22 patches during " + phase + ". " + ObservationDetails(observation));
            }
            string[] roots = access.Runtime.LoadedMods
                .Where(mod => mod.Manifest.UniqueID.Equals(Batch6AutoFishingPilotSettings.ProductUniqueId, StringComparison.OrdinalIgnoreCase))
                .Select(mod => Path.GetFullPath(mod.RootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .ToArray();
            if (roots.Length != 1 || !string.Equals(roots[0], settings.ExpectedProductRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Manager lifecycle loaded owner root does not match the exact receipt-bound product root.");
        }

        private static bool IsActive(Batch6AutoFishingObservation observation) =>
            observation.Enabled && observation.UpdateSubscribed && observation.SessionPresent && !observation.SessionReleased;

        private static void RequireSameProcessRemoved(Batch6AutoFishingObservation observation, string phase)
        {
            if (observation.ProductPresent || !observation.ProductAssemblyLoaded || observation.CoreOwnerRootCount != 0 ||
                observation.CoreInstanceCount != 0 || observation.LoadedOwnerCount != 0 || observation.ProductCallbackRuntimePresent ||
                observation.CanonicalHarmonyPatchCount != 0 || !observation.OwnerRequiresRestart ||
                !observation.DiagnosticsStatusCode.Equals("restart-required", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Manager lifecycle did not retain exact same-process removal/restart-required state during " + phase + ". " + ObservationDetails(observation));
            }
        }

        private static void RequireColdDisabled(Batch6AutoFishingObservation observation, string phase)
        {
            if (observation.ProductPresent || observation.ProductAssemblyLoaded || observation.CoreOwnerRootCount != 0 ||
                observation.CoreInstanceCount != 0 || observation.LoadedOwnerCount != 0 || observation.ProductCallbackRuntimePresent ||
                observation.CanonicalHarmonyPatchCount != 0 || observation.OwnerRequiresRestart ||
                observation.DiagnosticsStatusCode.Equals("restart-required", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Manager lifecycle cold-disabled process is not exact during " + phase + ". " + ObservationDetails(observation));
            }
        }

        private void RequestReload(string reason)
        {
            writer.RecordReloadRequest();
            G4FixtureStepResult result = requestProductOwnerRefresh();
            if (!result.Completed || !result.Succeeded)
                throw new InvalidOperationException("QaScenarioController.RequestProductOwnerRefresh failed for " + reason + ": " + result.Details);
            access.Log(Batch6AutoFishingManagerLifecycleSettings.OverallHookId +
                " requested real QaScenarioController.RequestProductOwnerRefresh/ModManager.ReloadMods ordinal=" +
                writer.Evidence.ReloadRequestCount.ToString(CultureInfo.InvariantCulture) + "; reason=" + reason + "; " + result.Details);
        }

        private bool MarkerMatchesExpected()
        {
            if (!File.Exists(settings.DisabledMarkerPath))
                return false;
            using (var stream = new FileStream(settings.DisabledMarkerPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA256 sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                return actual.Equals(settings.ExpectedDisabledMarkerSha256, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void MoveTo(LifecycleState next, string details, Batch6AutoFishingObservation observation)
        {
            state = next;
            stateEnteredAtUtc = DateTimeOffset.UtcNow;
            nextObservationAtUtc = stateEnteredAtUtc;
            access.SetHookStatus(Batch6AutoFishingManagerLifecycleSettings.OverallHookId, "pending", "formal AutoFishing Manager lifecycle", Details(details));
            access.Log(Batch6AutoFishingManagerLifecycleSettings.OverallHookId + " state=" + state + "; " + Details(details) + "; " + ObservationDetails(observation));
        }

        private void PublishHandshake(string handshake, string details)
        {
            access.SetHookStatus(Batch6AutoFishingManagerLifecycleSettings.HandshakeHookId, "ready", "runner-visible Manager lifecycle handshake", Details("handshake=" + handshake + "; " + details));
            access.Log(Batch6AutoFishingManagerLifecycleSettings.HandshakeHookId + " state=" + handshake + " " + Details(details) + ".");
        }

        private void Pass(string details, Batch6AutoFishingObservation observation)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            writer.Complete(details, now);
            state = LifecycleState.Passed;
            stateEnteredAtUtc = now;
            terminalDetails = Details("status=Passed; " + details + "; evidence=" + writer.ResultPath);
            access.SetHookStatus(Batch6AutoFishingManagerLifecycleSettings.CleanupHookId, "verified", "exact Manager lifecycle product cleanup", terminalDetails);
            access.SetHookStatus(Batch6AutoFishingManagerLifecycleSettings.OverallHookId, "verified", "formal AutoFishing Manager lifecycle", terminalDetails);
            access.Log(Batch6AutoFishingManagerLifecycleSettings.OverallHookId + " terminal=Passed; " + terminalDetails + "; " + ObservationDetails(observation));
        }

        private void Fail(string code, Exception ex)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string details = ex.GetType().Name + ": " + ex.Message;
            writer.Fail(code, details, now);
            state = LifecycleState.Failed;
            stateEnteredAtUtc = now;
            terminalDetails = Details("status=Failed; code=" + code + "; " + details + "; evidence=" + writer.ResultPath);
            access.SetHookStatus(Batch6AutoFishingManagerLifecycleSettings.CleanupHookId, "failed", "exact Manager lifecycle product cleanup", terminalDetails);
            access.SetHookStatus(Batch6AutoFishingManagerLifecycleSettings.OverallHookId, "failed", "formal AutoFishing Manager lifecycle", terminalDetails);
            access.Log(Batch6AutoFishingManagerLifecycleSettings.OverallHookId + " terminal=Failed; " + terminalDetails, LogLevel.Error);
        }

        private string Details(string details) =>
            "case=" + Batch6AutoFishingManagerLifecycleSettings.CaseId +
            "; mode=" + settings.Mode +
            "; reloadRequests=" + writer.Evidence.ReloadRequestCount.ToString(CultureInfo.InvariantCulture) +
            "; workshopReceipts=" + writer.Evidence.WorkshopReloadReceiptCount.ToString(CultureInfo.InvariantCulture) +
            "; " + (details ?? string.Empty);

        private static string ObservationDetails(Batch6AutoFishingObservation observation) =>
            "assembly=" + observation.ProductAssemblyLoaded.ToString().ToLowerInvariant() +
            "; instance=" + observation.CoreInstanceCount.ToString(CultureInfo.InvariantCulture) +
            "; loaded=" + observation.LoadedOwnerCount.ToString(CultureInfo.InvariantCulture) +
            "; roots=" + observation.CoreOwnerRootCount.ToString(CultureInfo.InvariantCulture) +
            "; callback=" + observation.ProductCallbackRuntimePresent.ToString().ToLowerInvariant() +
            "; patches=" + observation.CanonicalHarmonyPatchCount.ToString(CultureInfo.InvariantCulture) +
            "; restartRequired=" + observation.OwnerRequiresRestart.ToString().ToLowerInvariant() +
            "; status=" + observation.DiagnosticsStatusCode;
    }
}
