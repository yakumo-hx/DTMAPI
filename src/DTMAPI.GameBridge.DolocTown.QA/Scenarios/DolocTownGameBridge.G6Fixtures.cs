#pragma warning disable CS0618 // The retained owner-lifetime fixture intentionally exercises frozen CameraView compatibility.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private G6FixtureOptions? g6FixtureOptions;
        private bool g6OwnerLifetimePrepared;
        private bool g6OwnerLifetimeSaveExercised;
        private bool g6OwnerLifetimeReturnHomeRequested;
        private DateTimeOffset g6OwnerLifetimeReturnHomeRequestedAt;
        private DateTimeOffset g6OwnerLifetimeHomePageObservedAt;
        private const string OwnerLifetimeControlId = "DTMAPI.Smoke.OwnerLifetime.Control";
        private const string OwnerLifetimeCleanupId = "DTMAPI.Smoke.OwnerLifetime.Cleanup";
        private bool ownerLifetimePrepared;
        private bool ownerLifetimeSaveVerified;
        private int ownerLifetimeExpectedControlRoots;
        private int ownerLifetimeExpectedCleanupRoots;
        private bool g6LongTitleLoadRequested;
        private DateTimeOffset g6LongTitleHomePageObservedAt;

        internal void PrepareG6LifecycleForFixture(G6FixtureOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (g6FixtureOptions != null)
                throw new InvalidOperationException("The G6 fixture route may be prepared only once.");
            if (options.SaveSlot <= 0 || options.Cases == null || options.Cases.Length == 0)
                throw new InvalidOperationException("The G6 fixture route requires a positive save slot and at least one exact case.");

            g6FixtureOptions = options;
            g6FixtureState ??= new G6FixtureState();
            g6FixtureState.AutoLoadSaveSlot = options.SaveSlot;
            g6FixtureState.AutoLoadDelaySeconds = Math.Max(1, options.LongTitleIdleSeconds > 0 ? options.LongTitleIdleSeconds : options.InitialDelaySeconds);
            g6FixtureState.AutoExerciseSaveLoadCycle = HasG6Case("SaveLoadCycle");
            g6FixtureState.SaveLoadCycleCount = options.SaveLoadCycleCount;
            g6FixtureState.SaveLoadCycleInitialTitleIdleSeconds = options.SaveLoadCycleInitialTitleIdleSeconds;
            g6FixtureState.SaveLoadCycleIntervalSeconds = options.SaveLoadCycleIntervalSeconds;
            g6FixtureState.SaveLoadCycleInSaveSeconds = options.SaveLoadCycleInSaveSeconds;
            g6FixtureState.AutoExercisePreLoadGcProbe = options.PreLoadGcProbe;
            g6FixtureState.SaveLoadObjectSnapshotMode = options.SaveLoadObjectSnapshotMode ?? "Full";
            g6FixtureState.AutoExerciseSaveLoadCyclePendingPressure = HasG6Case("SaveLoadPendingPressure");
            g6FixtureState.SaveLoadCyclePendingPressureSeconds = options.PendingPressureSeconds;
            g6FixtureState.SaveLoadCyclePendingPressureIntervalSeconds = options.PendingPressureIntervalSeconds;

            runtime.ConfigureSaveLoadObjectSnapshotMode(g6FixtureState.SaveLoadObjectSnapshotMode, "qa-g6");
            if (!string.IsNullOrWhiteSpace(options.OwnerRootIsolationProfile) &&
                !string.Equals(options.OwnerRootIsolationProfile, "None", StringComparison.OrdinalIgnoreCase))
            {
                ApplyOwnerRootIsolationProfile(options.OwnerRootIsolationProfile);
            }
            if (HasG6Case("ModOwnerLifetime"))
            {
                PrepareOwnerLifetime();
                g6OwnerLifetimePrepared = true;
            }
            runtime.SetHookStatus(
                "Smoke.QaG6Lifecycle",
                "prepared",
                "optional QA G6 lifecycle fixture",
                "cases=" + string.Join("|", options.Cases) + "; saveSlot=" + options.SaveSlot.ToString(CultureInfo.InvariantCulture) + "; snapshotMode=" + g6FixtureState.SaveLoadObjectSnapshotMode + "; owner=qa; fallback=false");
        }

        internal void NotifyG6SaveLoadedForFixture()
        {
            if (g6FixtureOptions == null)
                return;
            RecordG6SaveLoadedForFixture();
            if (HasG6Case("ModOwnerLifetime") && g6OwnerLifetimePrepared && !g6OwnerLifetimeSaveExercised)
            {
                g6OwnerLifetimeSaveExercised = true;
                ExerciseOwnerLifetimeAfterSave();
            }
        }

        private void RecordG6SaveLoadedForFixture()
        {
            saveLoadedAt = DateTimeOffset.Now;
            // The QA participant receives SaveLoaded before product event
            // handlers. Observe on the next update so ProductNative has
            // completed its SaveLoaded reactivation and loaded-gun repair.
            MarkStrongPlantingGunReentrySaveLoadedForFixture();
            if (g6FixtureState?.AutoExerciseSaveLoadCycle == true)
            {
                saveLoadCycleSaveLoadedAt = saveLoadedAt;
                autoLoadOfficialPathRequested = false;
                modChangePromptConfirmed = false;
                pendingAutoLoadGameIndex = null;
                pendingAutoLoadGameDataState = null;
                autoLoadDirectFallbackAfterModChangeAttempted = false;
                if (saveLoadCycleStage == 1)
                {
                    saveLoadCycleStage = 2;
                    saveLoadCycleStageAt = saveLoadedAt;
                }
            }
            if (g6FixtureState?.AutoExerciseSaveLoadCyclePendingPressure == true)
            {
                saveLoadCyclePendingPressureSaveLoadedAt = saveLoadedAt;
                autoLoadOfficialPathRequested = false;
                modChangePromptConfirmed = false;
                pendingAutoLoadGameIndex = null;
                pendingAutoLoadGameDataState = null;
                autoLoadDirectFallbackAfterModChangeAttempted = false;
                if (saveLoadCyclePendingPressureStage == 2)
                    saveLoadCyclePendingPressureStage = 3;
            }
        }

        internal G4FixtureStepResult AdvanceG6LifecycleForFixture(string caseId)
        {
            if (g6FixtureOptions == null)
                throw new InvalidOperationException("The G6 fixture route was not prepared.");
            if (!HasG6Case(caseId))
                throw new ArgumentOutOfRangeException(nameof(caseId), caseId, "The G6 fixture seam accepts only cases from its validated activation receipt.");
            if (G6CaseRequiresSaveLoaded(caseId) && saveLoadedAt == default)
                return G4FixtureStepResult.Pending(G6Receipt(caseId, "waiting-save-loaded"));

            switch (caseId)
            {
                case "ModOwnerLifetime":
                    return AdvanceG6ModOwnerLifetime();
                case "SaveLoadCycle":
                    return FromG6Attempt(caseId, "Smoke.SaveLoadCycle", TryExerciseSaveLoadCycleForFixture((DateTimeOffset.Now - initializedAt).TotalSeconds));
                case "SaveLoadPendingPressure":
                    return FromG6Attempt(caseId, "Smoke.SaveLoadCyclePendingPressure", TryExerciseSaveLoadCyclePendingPressureForFixture());
                case "LongTitleLoad":
                    return AdvanceG6LongTitleLoad();
                case "MoreSavesFixed12EnabledLifecycle":
                case "MoreSavesFixed12DisabledCold":
                case "MoreSavesFixed12ReenabledCold":
                    return AdvanceMoreSavesFixed12ForFixture(caseId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(caseId), caseId, "Unknown G6 fixture case.");
            }
        }

        private G4FixtureStepResult AdvanceG6LongTitleLoad()
        {
            if (g6FixtureOptions == null)
                throw new InvalidOperationException("The G6 fixture route was not prepared.");
            if (saveLoadedAt != default)
            {
                runtime.SetHookStatus("Smoke.LongTitleIdleBeforeSave", "verified", "optional QA G6 title observer", "seconds=" + g6FixtureOptions.LongTitleIdleSeconds.ToString(CultureInfo.InvariantCulture) + "; saveSlot=" + g6FixtureOptions.SaveSlot.ToString(CultureInfo.InvariantCulture) + "; continuousHomePage=true; owner=qa");
                return G4FixtureStepResult.Verified(G6Receipt("LongTitleLoad", "verified"));
            }

            DateTimeOffset now = DateTimeOffset.Now;
            double elapsed = (now - initializedAt).TotalSeconds;
            if (elapsed < Math.Max(1, g6FixtureOptions.LongTitleIdleSeconds))
                return G4FixtureStepResult.Pending(G6Receipt("LongTitleLoad", "waiting-idle:" + elapsed.ToString("0", CultureInfo.InvariantCulture)));
            bool homePage = runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase);
            if (!HasContinuousStableObservationForFixture(homePage, now, 2d, ref g6LongTitleHomePageObservedAt))
                return G4FixtureStepResult.Pending(G6Receipt("LongTitleLoad", homePage ? "waiting-continuous-home" : "waiting-home"));
            if (!g6LongTitleLoadRequested)
            {
                g6LongTitleLoadRequested = TryAutoLoadSave(g6FixtureOptions.SaveSlot);
                if (g6LongTitleLoadRequested)
                    runtime.SetHookStatus("Smoke.LongTitleIdleBeforeSave", "pending", "optional QA G6 title observer", "loadRequested=true; seconds=" + g6FixtureOptions.LongTitleIdleSeconds.ToString(CultureInfo.InvariantCulture) + "; continuousHomePage=true; owner=qa");
            }
            return G4FixtureStepResult.Pending(G6Receipt("LongTitleLoad", g6LongTitleLoadRequested ? "waiting-save-loaded" : "waiting-load-request"));
        }

        private G4FixtureStepResult AdvanceG6ModOwnerLifetime()
        {
            const string caseId = "ModOwnerLifetime";
            IHookStatusInfo? status = runtime.Diagnostics.GetHookStatuses()
                .LastOrDefault(item => item.HookId.Equals("Smoke.OwnerLifetime", StringComparison.OrdinalIgnoreCase));
            if (status == null || status.Status.Equals("prepared", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Pending(G6Receipt(caseId, "Smoke.OwnerLifetime:" + (status?.Status ?? "missing")));
            if (status.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Failed(G6Receipt(caseId, "Smoke.OwnerLifetime:failed"));

            DateTimeOffset now = DateTimeOffset.Now;
            if (status.Status.Equals("save-preserved", StringComparison.OrdinalIgnoreCase))
            {
                if (!g6OwnerLifetimeReturnHomeRequested)
                {
                    patcher ??= new HarmonyReflectionPatcher(runtime);
                    Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                    if (!runtime.UI.InputContext.Equals("Gameplay", StringComparison.OrdinalIgnoreCase) ||
                        !TryGetStaticBoolProperty(dolocApi!, "IsNormalState"))
                    {
                        return G4FixtureStepResult.Pending(G6Receipt(caseId, "waiting-gameplay-before-return-home:" + runtime.UI.InputContext));
                    }

                    var returnHome = FindMethod(dolocApi, "ReturnHome", 1);
                    if (returnHome == null)
                        return G4FixtureStepResult.Failed(G6Receipt(caseId, "DolocAPI.ReturnHome(bool):missing"));
                    g6OwnerLifetimeReturnHomeRequested = true;
                    g6OwnerLifetimeReturnHomeRequestedAt = now;
                    runtime.RuntimeMonitor.Log("G6 owner-lifetime fixture requesting DolocAPI.ReturnHome after save-preserved evidence.");
                    returnHome.Invoke(null, new object[] { false });
                }
                else if ((now - g6OwnerLifetimeReturnHomeRequestedAt).TotalSeconds > 60)
                {
                    return G4FixtureStepResult.Failed(G6Receipt(caseId, "return-home-timeout:" + runtime.UI.InputContext));
                }
                return G4FixtureStepResult.Pending(G6Receipt(caseId, "waiting-title-cleanup:" + runtime.UI.InputContext));
            }

            if (!status.Status.Equals("verified", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Failed(G6Receipt(caseId, "Smoke.OwnerLifetime:" + status.Status));

            bool homePage = runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase);
            if (!HasContinuousStableObservationForFixture(homePage, now, 1.5d, ref g6OwnerLifetimeHomePageObservedAt))
                return G4FixtureStepResult.Pending(G6Receipt(caseId, homePage ? "waiting-continuous-home" : "waiting-home:" + runtime.UI.InputContext));
            runtime.NotifyTitleStable("QaHost.G6.ModOwnerLifetime", "Owner cleanup verified after save-preserved -> ReturnHome -> continuous HomePage.");
            return G4FixtureStepResult.Verified(G6Receipt(caseId, "Smoke.OwnerLifetime:verified"));
        }

        private void ApplyOwnerRootIsolationProfile(string profile)
        {
            string[] owners = GetOwnerRootIsolationOwners(profile);
            string[] rootTypes = GetOwnerRootIsolationRootTypes(profile);
            if (owners.Length == 0 || rootTypes.Length == 0)
            {
                string unsupported = "profile=" + (profile ?? string.Empty) + "; suppressionSucceeded=False; reason=unsupported-profile; owner=qa";
                runtime.SetHookStatus("Smoke.OwnerRootIsolation", "warning", "optional QA owner-root isolation", unsupported);
                return;
            }

            string summary = "profile=" + profile + "; owner=qa; " + runtime.SuppressOwnerRoots(owners, rootTypes);
            bool succeeded = summary.IndexOf("suppressionSucceeded=True", StringComparison.OrdinalIgnoreCase) >= 0;
            runtime.SetHookStatus("Smoke.OwnerRootIsolation", succeeded ? "applied" : "warning", "optional QA owner-root isolation", summary);
        }

        private static string[] GetOwnerRootIsolationOwners(string profile)
        {
            if (profile.Equals("YConsoleZoomNoInput", StringComparison.OrdinalIgnoreCase) ||
                profile.Equals("YConsoleZoomNoEvents", StringComparison.OrdinalIgnoreCase) ||
                profile.Equals("YConsoleZoomNoInputEvents", StringComparison.OrdinalIgnoreCase) ||
                profile.Equals("YConsoleZoomNoConfig", StringComparison.OrdinalIgnoreCase))
                return new[] { "DTMAPI.DebugConsoleMod", "DTMAPI.ZoomMod" };
            if (profile.Equals("ZoomNoInput", StringComparison.OrdinalIgnoreCase))
                return new[] { "DTMAPI.ZoomMod" };
            if (profile.Equals("YConsoleNoInput", StringComparison.OrdinalIgnoreCase))
                return new[] { "DTMAPI.DebugConsoleMod" };
            return Array.Empty<string>();
        }

        private static string[] GetOwnerRootIsolationRootTypes(string profile)
        {
            if (profile.Equals("YConsoleZoomNoInput", StringComparison.OrdinalIgnoreCase) ||
                profile.Equals("ZoomNoInput", StringComparison.OrdinalIgnoreCase) ||
                profile.Equals("YConsoleNoInput", StringComparison.OrdinalIgnoreCase))
                return new[] { "InputButton" };
            if (profile.Equals("YConsoleZoomNoEvents", StringComparison.OrdinalIgnoreCase))
                return new[] { "EventHandler" };
            if (profile.Equals("YConsoleZoomNoInputEvents", StringComparison.OrdinalIgnoreCase))
                return new[] { "InputButton", "EventHandler" };
            if (profile.Equals("YConsoleZoomNoConfig", StringComparison.OrdinalIgnoreCase))
                return new[] { "ConfigPage" };
            return Array.Empty<string>();
        }

        private void PrepareOwnerLifetime()
        {
            if (ownerLifetimePrepared) return;
            try
            {
                RegisterOwnerLifetimeOwner(OwnerLifetimeControlId);
                RegisterOwnerLifetimeOwner(OwnerLifetimeCleanupId);
                ownerLifetimeExpectedControlRoots = runtime.CountCoreOwnerRoots(OwnerLifetimeControlId);
                ownerLifetimeExpectedCleanupRoots = runtime.CountCoreOwnerRoots(OwnerLifetimeCleanupId);
                ownerLifetimePrepared = true;
                runtime.SetHookStatus("Smoke.OwnerLifetime", "prepared", "optional QA owner-lifetime scenario", "controlRoots=" + ownerLifetimeExpectedControlRoots + "; cleanupRoots=" + ownerLifetimeExpectedCleanupRoots);
            }
            catch { CleanupOwnerLifetimeOnClose("PrepareFailure"); throw; }
        }

        private bool ExerciseOwnerLifetimeAfterSave()
        {
            if (!ownerLifetimePrepared) return false;
            int controlBefore = runtime.CountCoreOwnerRoots(OwnerLifetimeControlId);
            int cleanupBefore = runtime.CountCoreOwnerRoots(OwnerLifetimeCleanupId);
            string cleanup = runtime.DeactivateOwner(OwnerLifetimeCleanupId, ModOwnerCleanupReason.Unload, true, string.Empty);
            int cleanupRemaining = runtime.CountCoreOwnerRoots(OwnerLifetimeCleanupId);
            int controlAfter = runtime.CountCoreOwnerRoots(OwnerLifetimeControlId);
            ownerLifetimeSaveVerified = ownerLifetimeExpectedControlRoots > 0 && controlBefore == ownerLifetimeExpectedControlRoots && controlAfter == controlBefore && cleanupBefore == ownerLifetimeExpectedCleanupRoots && cleanupRemaining == 0;
            runtime.SetHookStatus("Smoke.OwnerLifetime", ownerLifetimeSaveVerified ? "save-preserved" : "failed", "owner isolation across save", "controlBefore=" + controlBefore + "; controlAfter=" + controlAfter + "; cleanupRemaining=" + cleanupRemaining + "; " + cleanup);
            return ownerLifetimeSaveVerified;
        }

        private void FinalizeOwnerLifetimeAfterTitle()
        {
            if (!ownerLifetimePrepared || !ownerLifetimeSaveVerified) return;
            int before = runtime.CountCoreOwnerRoots(OwnerLifetimeControlId);
            string cleanup = runtime.DeactivateOwner(OwnerLifetimeControlId, ModOwnerCleanupReason.Unload, true, string.Empty);
            bool verified = before == ownerLifetimeExpectedControlRoots && runtime.CountCoreOwnerRoots(OwnerLifetimeControlId) == 0 && runtime.CountCoreOwnerRoots(OwnerLifetimeCleanupId) == 0;
            runtime.SetHookStatus("Smoke.OwnerLifetime", verified ? "verified" : "failed", "owner cleanup after title", "controlBefore=" + before + "; " + cleanup);
            if (verified) ownerLifetimePrepared = false;
        }

        private void RegisterOwnerLifetimeOwner(string ownerId)
        {
            var manifest = new ManifestModel { Name = ownerId, Author = "DTMAPI", Version = DtmApiRuntime.ApiVersion, UniqueID = ownerId, Type = "CodeMod" };
            try
            {
                runtime.BeginOwnerEntry(ownerId);
                Action ensure = () => runtime.EnsureModOwnerRegistrationAllowed(ownerId);
                runtime.Events.CreateOwnerBoundProxy(ownerId, ensure).GameLoop.UpdateTicked += (_, _) => { };
                runtime.Input.CreateOwnerBound(ownerId, ensure).RegisterButton(ownerId.EndsWith("Control", StringComparison.Ordinal) ? "F14" : "F15");
                runtime.Config.CreateOwnerBound(manifest, ensure).RegisterMigration<OwnerLifetimeConfig>(manifest, _ => { });
                IModRegistry registry = runtime.ModRegistry.CreateOwnerBoundRegistry(manifest, ensure);
                registry.RegisterApi<IOwnerLifetimeApi>(new OwnerLifetimeApi());
                registry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu")?.Register(manifest, () => { }, () => { });
                runtime.ActivateOwnerEntry(manifest);
            }
            catch { runtime.DeactivateOwner(ownerId, ModOwnerCleanupReason.EntryFailed, true, string.Empty); throw; }
        }

        internal void CleanupOwnerLifetimeOnClose(string reason)
        {
            var failures = new List<string>();
            string control = TryDeactivateOwnerLifetimeOwner(OwnerLifetimeControlId, failures);
            string cleanup = TryDeactivateOwnerLifetimeOwner(OwnerLifetimeCleanupId, failures);
            foreach (string owner in new[] { OwnerLifetimeControlId, OwnerLifetimeCleanupId })
                if (runtime.CountCoreOwnerRoots(owner) != 0 || runtime.HasOwnerInstance(owner)) failures.Add(owner + " retained roots");
            string details = "reason=" + reason + "; " + control + "; " + cleanup +
                "; controlRoots=" + runtime.CountCoreOwnerRoots(OwnerLifetimeControlId) +
                "; cleanupRoots=" + runtime.CountCoreOwnerRoots(OwnerLifetimeCleanupId) +
                "; controlInstance=" + runtime.HasOwnerInstance(OwnerLifetimeControlId) +
                "; cleanupInstance=" + runtime.HasOwnerInstance(OwnerLifetimeCleanupId) +
                "; failures=" + (failures.Count == 0 ? "none" : string.Join("|", failures)) + ";";
            runtime.SetHookStatus("Smoke.OwnerLifetimeCleanup", failures.Count == 0 ? "verified" : "failed", "synthetic owner cleanup", details);
            if (failures.Count != 0) throw new InvalidOperationException(details);
            ownerLifetimePrepared = false;
        }

        private string TryDeactivateOwnerLifetimeOwner(string ownerId, List<string> failures)
        {
            try
            {
                return runtime.DeactivateOwner(ownerId, ModOwnerCleanupReason.Unload, shutdown: true, transactionId: string.Empty);
            }
            catch (Exception ex)
            {
                failures.Add(ownerId + ":deactivate=" + ex.GetType().Name + ":" + ex.Message);
                return "OwnerBoundCleanup: owner=" + ownerId + ", exception=" + ex.GetType().Name;
            }
        }

        private interface IOwnerLifetimeApi
        {
        }

        private sealed class OwnerLifetimeApi : IOwnerLifetimeApi
        {
        }

        private sealed class OwnerLifetimeConfig
        {
            public bool Enabled { get; set; } = true;
        }

        private bool HasG6Case(string caseId) =>
            g6FixtureOptions?.Cases.Contains(caseId, StringComparer.Ordinal) == true;

        private static bool G6CaseRequiresSaveLoaded(string caseId) =>
            string.Equals(caseId, "ModOwnerLifetime", StringComparison.Ordinal) ||
            string.Equals(caseId, "LegacyFishingCompatibility", StringComparison.Ordinal);

        private static G4FixtureStepResult FromG6Attempt(string caseId, string hookId, FixtureAttemptResult result)
        {
            if (result == FixtureAttemptResult.Pending)
                return G4FixtureStepResult.Pending(G6Receipt(caseId, hookId + ":pending"));
            if (result == FixtureAttemptResult.Failed)
                return G4FixtureStepResult.Failed(G6Receipt(caseId, hookId + ":failed"));
            return G4FixtureStepResult.Verified(G6Receipt(caseId, hookId + ":verified"));
        }

        private G4FixtureStepResult ReadG6Status(string caseId, string hookId, string pendingStatuses)
        {
            IHookStatusInfo? status = runtime.Diagnostics.GetHookStatuses()
                .LastOrDefault(item => item.HookId.Equals(hookId, StringComparison.OrdinalIgnoreCase));
            if (status == null || pendingStatuses.Split('|').Contains(status.Status, StringComparer.OrdinalIgnoreCase))
                return G4FixtureStepResult.Pending(G6Receipt(caseId, hookId + ":" + (status?.Status ?? "missing")));
            if (!status.Status.Equals("verified", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Failed(G6Receipt(caseId, hookId + ":" + status.Status));
            return G4FixtureStepResult.Verified(G6Receipt(caseId, hookId + ":verified"));
        }

        private static string G6Receipt(string caseId, string terminal) =>
            "case=" + (caseId ?? string.Empty) +
            "; setup=qa-g6-explicit-route" +
            "; commit=" + (terminal ?? string.Empty) +
            "; cleanup=title-boundary+shutdown+runner-post-exit-transaction" +
            "; productionSaveLoadOrder=unchanged; fallback=false";
    }
}
