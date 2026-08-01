using System;
using System.Collections.Generic;
using System.Linq;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private static readonly string[] FallbackManualCancelKeys = { "A", "D", "Space", "LeftShift", "RightShift" };
        private static readonly TimeSpan RecastDelay = TimeSpan.FromSeconds(0.25);
        private const string ToggleRegistrationId = "Yuuka.DTMAPI.AutoFishing.Toggle";
        private IDtmHelper helper = null!;
        private AutoFishingConfig config = new AutoFishingConfig();
        private IInputRegistration? toggleRegistration;
        private DtmButton[] manualCancelKeyButtons = Array.Empty<DtmButton>();
        private readonly FishingDecisionEngine decisionEngine = new FishingDecisionEngine();
        private AutoFishingNativeRuntime nativeRuntime = null!;
        private FishingPrimitivesService primitives = null!;
        private FishingRuntimeSession? session;
        private DateTimeOffset nextCastAtUtc = DateTimeOffset.MinValue;
        private DateTimeOffset manualCancelEnabledAtUtc = DateTimeOffset.MinValue;
        private long lastActionSequence = -1;
        private bool updateSubscribed;
        private bool movementFallbackLogged;
        private bool toggleAwaitingRelease;
        private bool toggleDuplicatePressLogged;
        private bool entryStarted;
        private bool nativeRuntimeCreated;
        private bool primitivesInitialized;
        private bool disposed;
        private bool enabled;
        private FishingPrimitivePhase lastPhase = FishingPrimitivePhase.Idle;
        private string lastReason = "not-enabled";

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper;
            entryStarted = true;
            nativeRuntime = new AutoFishingNativeRuntime(new FishingProductContext(helper));
            nativeRuntimeCreated = true;
            try
            {
                nativeRuntime.InstallHooksAtomically();
                primitives = nativeRuntime.Primitives;
                primitivesInitialized = true;
                config = helper.ReadConfig<AutoFishingConfig>();
                NormalizeConfig();
                RefreshInputCaches();
                RegisterConfigMenu();

                helper.Events.Input.KeybindPressed += OnKeybindPressed;
                helper.Events.Input.KeybindReleased += OnKeybindReleased;
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                toggleRegistration = helper.Input.RegisterKeybind(ToggleRegistrationId, DtmKeybindList.Parse(config.ToggleKey), DtmInputScope.Gameplay);
                helper.Monitor.Log("AutoFishing product-native state machine ready. Owner=" + FishingProductHookInstaller.HarmonyOwner + " patches=" + nativeRuntime.InstalledPatchCount + "; callbacks remain fast-return inactive until enabled. Toggle=" + NormalizeKey(config.ToggleKey) + ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                enabled = false;
                TryCleanup(DetachProductHandlers, failures);
                TryCleanup(() => ReleasePrimitiveSession("entry-failed"), failures);
                TryCleanup(() =>
                {
                    toggleRegistration?.Dispose();
                    toggleRegistration = null;
                }, failures);
                TryCleanup(nativeRuntime.RollbackFailedEntry, failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException("AutoFishing Entry failed and local rollback encountered additional cleanup failures.", failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            var failures = new List<Exception>();
            enabled = false;
            // Core has already moved the owner to Deactivating before invoking this
            // optional callback. Owner-bound event proxies correctly reject -= in that
            // state, so stop the product flag here and let Core's immediately following
            // Events.RemoveOwner pass sever all platform-owned handler roots.
            updateSubscribed = false;
            manualCancelEnabledAtUtc = DateTimeOffset.MinValue;
            nextCastAtUtc = DateTimeOffset.MinValue;
            lastActionSequence = -1;

            if (entryStarted)
            {
                if (primitivesInitialized)
                    TryCleanup(() => ResetPrimitiveLifecycleBoundary("OwnerDeactivation"), failures);
                else
                    TryCleanup(() => ReleasePrimitiveSession("OwnerDeactivation"), failures);
                TryCleanup(() =>
                {
                    toggleRegistration?.Dispose();
                    toggleRegistration = null;
                }, failures);
            }

            if (nativeRuntimeCreated)
                TryCleanup(() => nativeRuntime.DeactivateOwner("OwnerDeactivation"), failures);

            if (failures.Count > 0)
                throw new AggregateException("AutoFishing owner deactivation could not prove complete product-private cleanup.", failures);
            disposed = true;
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log("DTMAPI config menu API is not available.", LogLevel.Warn);
                return;
            }

            menu.Register(helper.ModManifest, ResetConfig, () =>
            {
                NormalizeConfig();
                RefreshInputCaches();
                toggleAwaitingRelease = false;
                toggleDuplicatePressLogged = false;
                toggleRegistration?.Update(DtmKeybindList.Parse(config.ToggleKey), DtmInputScope.Gameplay);
                helper.WriteConfig(config);
                if (enabled)
                    RefreshPrimitiveLeases("config-saved");
                helper.Monitor.Log("AutoFishing config saved through DTMAPI menu.");
            });

            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Auto fishing"));
            menu.AddParagraph(helper.ModManifest, () => string.Format(T("config.status.experimental", "{0} toggles the native auto-fishing loop. Current state: {1}."), FormatToggleKey(), FormatRuntimeStatus(menu)));
            menu.AddParagraph(helper.ModManifest, () => T("config.boundary.current", "Default loop: cast at the configured charge, wait for a native bite, reel, show and auto-complete the real minigame, collect the result, then recast. Bite waiting, cast charge, minigame skipping, and cast/pull animation speed are independent."));
            if (menu is IDtmConfigMenuKeybindDefaultsApi keybindDefaults)
                keybindDefaults.AddKeybindOption(helper.ModManifest, () => T("config.toggleKey.name", "Toggle key"), () => T("config.toggleKey.tooltip", "Click Capture, then press the next key. Reset restores F6; Escape, Backspace, or Delete clears the binding."), () => config.ToggleKey, value => config.ToggleKey = value, () => "F6");
            else
                menu.AddKeybindOption(helper.ModManifest, () => T("config.toggleKey.name", "Toggle key"), () => T("config.toggleKey.tooltip", "Click Capture, then press the next key. Escape, Backspace, or Delete clears the binding."), () => config.ToggleKey, value => config.ToggleKey = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.castChargeRatio.name", "Cast charge"), () => T("config.castChargeRatio.tooltip", "Set native cast charge from 0 (minimum distance) to 1 (full charge). This does not change animation speed."), () => config.CastChargeRatio, value => config.CastChargeRatio = value, 0, 1, 0.05);
            menu.AddBoolOption(helper.ModManifest, () => T("config.instantBite.name", "Instant bite"), () => T("config.instantBite.tooltip", "Skip the native waiting period after the hook reaches water, then reel into the selected native result path."), () => config.InstantBite, value => config.InstantBite = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.skipMinigame.name", "Skip minigame"), () => T("config.skipMinigame.tooltip", "Route bite-ready results through the native no-minigame result path. Native failure/success is preserved."), () => config.SkipMiniGame, value => config.SkipMiniGame = value);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.fastAnimations.name", "Fast fishing animations"), () => T("config.fastAnimations.tooltip", "Speed up the native backswing/charge animation, charge timing, cast hook flight, and pull phases without changing the configured charge target."), () => config.FastAnimations, value => config.FastAnimations = value, () => config.AnimationMultiplier, value => config.AnimationMultiplier = value, 1, 4, 0.5);
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (TryClosePendingInputFault())
                return;
            if (!enabled)
                return;
            if (!EnsurePrimitiveSession("update"))
            {
                SetAutomation(false, "update session unavailable");
                return;
            }

            primitives.RefreshNativeState();
            FishingPrimitiveSnapshot snapshot = session!.GetSnapshot();
            if (DateTimeOffset.UtcNow >= manualCancelEnabledAtUtc && ShouldCancelForMovement(snapshot, out string movementReason))
            {
                SetAutomation(false, movementReason);
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            FishingProductDecision decision = decisionEngine.Decide(snapshot, config.InstantBite, config.SkipMiniGame, now, nextCastAtUtc);
            if (decision.Action == FishingProductAction.None || lastActionSequence == snapshot.Sequence)
                return;

            lastActionSequence = snapshot.Sequence;
            FishingPrimitiveResult result = ExecuteDecision(snapshot, decision);
            lastReason = result.Status;
            if (!result.Applied && decision.Action == FishingProductAction.Cast)
            {
                lastActionSequence = -1;
                nextCastAtUtc = now + ResolveCastRetryDelay(result.Status);
            }
            if (!result.Applied && decision.Action != FishingProductAction.Cast)
                helper.Monitor.Log("AutoFishing action rejected action=" + decision.Action + " phase=" + snapshot.Phase + " status=" + result.Status + " message=" + result.Message, LogLevel.Warn);
        }

        private FishingPrimitiveResult ExecuteDecision(FishingPrimitiveSnapshot snapshot, FishingProductDecision decision)
        {
            switch (decision.Action)
            {
                case FishingProductAction.Cast:
                    nextCastAtUtc = DateTimeOffset.MaxValue;
                    return session!.TryCast(snapshot.Sequence, new FishingPrimitiveCastRequest(config.CastChargeRatio), decision.Reason);
                case FishingProductAction.PrepareNativeBite:
                    return session!.TryPrepareNativeBite(snapshot.Sequence, decision.Reason);
                case FishingProductAction.ReelSkipMiniGame:
                    return session!.TryReel(snapshot.Sequence, FishingPrimitiveReelMode.SkipMiniGameNativeResult, decision.Reason);
                case FishingProductAction.ReelVisibleMiniGame:
                    return session!.TryReel(snapshot.Sequence, FishingPrimitiveReelMode.NativeVisibleMiniGame, decision.Reason);
                default:
                    return new FishingPrimitiveResult(false, "no-action", decision.Reason, snapshot.Sequence);
            }
        }

        private void SetAutomation(bool value, string reason)
        {
            reason ??= string.Empty;
            if (value && TryClosePendingInputFault())
                return;
            if (enabled == value)
            {
                if (enabled)
                {
                    if (EnsurePrimitiveSession(reason))
                        SetUpdateSubscription(true);
                    else
                    {
                        enabled = false;
                        SetUpdateSubscription(false);
                    }
                }
                else if (session != null || primitives.HasActiveSession)
                {
                    SetUpdateSubscription(false);
                    ReleasePrimitiveSession(reason);
                    primitives.InvalidateEnvironment("automation-disable:" + reason);
                }
                return;
            }

            if (value)
            {
                primitives.InvalidateEnvironment("automation-enable:" + reason);
                enabled = true;
                manualCancelEnabledAtUtc = DateTimeOffset.UtcNow.AddSeconds(1);
                nextCastAtUtc = DateTimeOffset.UtcNow;
                lastActionSequence = -1;
                if (!EnsurePrimitiveSession(reason))
                {
                    enabled = false;
                    manualCancelEnabledAtUtc = DateTimeOffset.MinValue;
                    SetUpdateSubscription(false);
                    return;
                }
                SetUpdateSubscription(true);
            }
            else
            {
                enabled = false;
                SetUpdateSubscription(false);
                manualCancelEnabledAtUtc = DateTimeOffset.MinValue;
                nextCastAtUtc = DateTimeOffset.MinValue;
                lastActionSequence = -1;
                ReleasePrimitiveSession(reason);
                primitives.InvalidateEnvironment("automation-disable:" + reason);
            }
            helper.Monitor.Log("AutoFishing automation " + (enabled ? "enabled" : "disabled") + " reason=" + reason + ".");
        }

        private void SetUpdateSubscription(bool active)
        {
            if (updateSubscribed == active)
                return;
            if (active)
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            else
                helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            updateSubscribed = active;
        }

        private void DetachProductHandlers()
        {
            SetUpdateSubscription(false);
            helper.Events.Input.KeybindPressed -= OnKeybindPressed;
            helper.Events.Input.KeybindReleased -= OnKeybindReleased;
            helper.Events.Save.SaveLoaded -= OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle;
        }

        private bool EnsurePrimitiveSession(string reason)
        {
            if (session?.IsReleased == false)
                return true;
            session = primitives.StartSession(helper.ModManifest.UniqueID, OnFishingTransition);
            if (session == null)
            {
                helper.Monitor.Log("AutoFishing could not start its product-native fishing session.", LogLevel.Warn);
                lastReason = "session-unavailable";
                return false;
            }
            session.SetInputFaultHandler(OnFishingInputFault);
            RefreshPrimitiveLeases(reason);
            FishingPrimitiveSnapshot snapshot = session.GetSnapshot();
            lastPhase = snapshot.Phase;
            lastReason = "active";
            nextCastAtUtc = DateTimeOffset.UtcNow;
            return true;
        }

        private void RefreshPrimitiveLeases(string reason)
        {
            if (session?.IsReleased != false)
                return;
            session.ConfigureAutomation(
                config.SkipMiniGame ? null : decisionEngine.DecideMiniGameInput,
                config.FastAnimations
                    ? new FishingAnimationLeaseRequest(config.AnimationMultiplier, config.AnimationMultiplier, config.AnimationMultiplier)
                    : (FishingAnimationLeaseRequest?)null,
                reason);
        }

        private void ReleasePrimitiveSession(string reason)
        {
            if (session != null)
            {
                session.Release(reason);
                session = null;
            }
            lastPhase = FishingPrimitivePhase.Idle;
            lastReason = reason ?? string.Empty;
        }

        private void OnFishingTransition(FishingPrimitivePhase phase, FishingPrimitiveTransitionKind kind, string reason)
        {
            lastPhase = phase;
            lastReason = reason;
            lastActionSequence = -1;
            if (kind == FishingPrimitiveTransitionKind.PullExited)
                nextCastAtUtc = DateTimeOffset.UtcNow + RecastDelay;
            else if (kind == FishingPrimitiveTransitionKind.Interrupted)
                nextCastAtUtc = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2.5);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            helper.Monitor.Log("AutoFishing SaveLoaded boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown") + ".");
            if (TryClosePendingInputFault())
                return;
            ResetPrimitiveLifecycleBoundary("SaveLoaded");
            if (!enabled)
                return;
            nextCastAtUtc = DateTimeOffset.UtcNow;
            if (!EnsurePrimitiveSession("SaveLoaded"))
                SetAutomation(false, "SaveLoaded session unavailable");
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            toggleAwaitingRelease = false;
            toggleDuplicatePressLogged = false;
            bool wasEnabled = enabled;
            enabled = false;
            SetUpdateSubscription(false);
            manualCancelEnabledAtUtc = DateTimeOffset.MinValue;
            nextCastAtUtc = DateTimeOffset.MinValue;
            lastActionSequence = -1;
            ResetPrimitiveLifecycleBoundary("ReturnedToTitle");
            if (wasEnabled)
                helper.Monitor.Log("AutoFishing automation disabled reason=ReturnedToTitle.");
        }

        private void ResetPrimitiveLifecycleBoundary(string reason)
        {
            bool hadPendingInputFault = primitives.TryGetPendingInputFault(out string pendingInputFaultReason);
            primitives.ResetForLifecycleBoundary(reason ?? string.Empty);
            ReleasePrimitiveSession("lifecycle:" + (reason ?? string.Empty));
            if (hadPendingInputFault && session == null && !primitives.HasActiveSession)
                primitives.AcknowledgeInputFault(pendingInputFaultReason);
        }

        private bool TryClosePendingInputFault()
        {
            if (!primitives.TryGetPendingInputFault(out string reason))
                return false;
            if (OnFishingInputFault(reason))
                primitives.AcknowledgeInputFault(reason);
            return true;
        }

        private bool OnFishingInputFault(string reason)
        {
            try
            {
                SetAutomation(false, reason ?? "minigame-input-fault");
                return !enabled && session == null && !primitives.HasActiveSession;
            }
            catch (Exception ex)
            {
                lastReason = (reason ?? "minigame-input-fault") + ":cleanup-pending";
                enabled = true;
                try
                {
                    SetUpdateSubscription(true);
                }
                catch
                {
                    updateSubscribed = false;
                }
                helper.Monitor.Log(
                    "AutoFishing minigame-input fault cleanup remains pending error=" +
                    ex.GetType().Name + ": " + ex.Message,
                    LogLevel.Warn);
                return false;
            }
        }

        private bool ShouldCancelForMovement(FishingPrimitiveSnapshot snapshot, out string reason)
        {
            if (snapshot.HorizontalMoveFactorAvailable)
            {
                if (Math.Abs(snapshot.HorizontalMoveFactor) <= 0.001d)
                {
                    reason = string.Empty;
                    return false;
                }
                reason = "manual-move HorizontalMoveFactor=" + snapshot.HorizontalMoveFactor.ToString("0.###");
                return true;
            }

            if (!movementFallbackLogged)
            {
                movementFallbackLogged = true;
                helper.Monitor.Log("HorizontalMoveFactor is unavailable; AutoFishing movement cancel is using the legacy key snapshot fallback.", LogLevel.Warn);
            }
            foreach (DtmButton key in manualCancelKeyButtons)
            {
                if (helper.Input.WasPressed(key))
                {
                    reason = "manual-move fallback " + key.Id;
                    return true;
                }
            }
            reason = string.Empty;
            return false;
        }

        private string FormatRuntimeStatus(IDtmConfigMenuApi menu)
        {
            string bucket = FormatStateBucket(enabled, lastPhase);
            string status = bucket + " / phase=" + lastPhase + " / reason=" + lastReason;
            var conflicts = menu.GetKeybindConflicts(helper.ModManifest.UniqueID);
            if (conflicts.Count > 0)
                status += " / " + T("state.conflict", "按键冲突") + ": " + string.Join("; ", conflicts);
            return status;
        }

        private string FormatToggleKey()
        {
            string key = NormalizeKey(config.ToggleKey);
            return key.Equals("None", StringComparison.OrdinalIgnoreCase) ? T("state.hotkeyDisabled", "hotkey disabled") : key;
        }

        private string FormatStateBucket(bool isEnabled, FishingPrimitivePhase phase)
        {
            if (!isEnabled)
                return T("state.notEnabled", "未启用");
            if (phase == FishingPrimitivePhase.Idle || phase == FishingPrimitivePhase.ReadyEntered)
                return T("state.waitingFishing", "等待钓鱼");
            return T("state.experimental", "实验阶段");
        }

        private void RefreshInputCaches()
        {
            RefreshManualCancelButtonCache();
        }

        private void RefreshManualCancelButtonCache()
        {
            manualCancelKeyButtons = FallbackManualCancelKeys.Select(DtmButton.Parse).Where(button => button.IsBound).ToArray();
        }

        private void OnKeybindPressed(object sender, KeybindPressedEventArgs e)
        {
            if (!IsToggleEvent(e.OwnerId, e.KeybindId))
                return;
            if (toggleAwaitingRelease)
            {
                if (!toggleDuplicatePressLogged)
                {
                    toggleDuplicatePressLogged = true;
                    helper.Monitor.Log("AutoFishing ignored a duplicate toggle press before key release.", LogLevel.Warn);
                }
                return;
            }
            toggleAwaitingRelease = true;
            SetAutomation(!enabled, "hotkey " + e.TriggerButton);
        }

        private void OnKeybindReleased(object sender, KeybindReleasedEventArgs e)
        {
            if (!IsToggleEvent(e.OwnerId, e.KeybindId))
                return;
            toggleAwaitingRelease = false;
            toggleDuplicatePressLogged = false;
        }

        private bool IsToggleEvent(string ownerId, string keybindId)
        {
            return toggleRegistration != null &&
                ownerId.Equals(toggleRegistration.OwnerId, StringComparison.OrdinalIgnoreCase) &&
                keybindId.Equals(ToggleRegistrationId, StringComparison.OrdinalIgnoreCase);
        }

        private void ResetConfig()
        {
            config = new AutoFishingConfig();
            NormalizeConfig();
            RefreshInputCaches();
        }

        private void NormalizeConfig()
        {
            config.Normalize();
        }

        private static string NormalizeKey(string key) => DtmKeybindList.Parse(key).ToString();

        private static TimeSpan ResolveCastRetryDelay(string status)
        {
            if (status.Equals("no-water", StringComparison.OrdinalIgnoreCase) || status.Equals("no-selected-rod", StringComparison.OrdinalIgnoreCase))
                return TimeSpan.FromSeconds(1);
            if (status.Equals("cast-failed", StringComparison.OrdinalIgnoreCase))
                return TimeSpan.FromSeconds(2.5);
            return TimeSpan.FromSeconds(0.25);
        }

    }
}
