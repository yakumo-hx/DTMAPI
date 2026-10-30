using System;
using System.Globalization;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleNativeActions
    {
        private object? sessionArchive;
        private object? sessionAgentData;
        private bool sessionActive;
        private bool godEnabled;
        private string godOwnerId = string.Empty;
        private object? timeScaleOwner;
        private object? timeScaleSession;
        private double timeScaleMultiplier = 1d;
        private double timeScaleAppliedMultiplier = 1d;
        private double timeScalePriorMultiplier = 1d;
        private bool timeScalePriorWasOwned;
        private string timeScaleLeaseOwnerId = string.Empty;
        private bool timeScaleLeaseActive;
        private bool timeScaleMutationUncertain;
        private bool writingTimeScale;

        internal static object? CurrentAgentData => Native.Read(Native.Read(Native.Archive, "farmData"), "agentData");
        private static object? CurrentTimeScaleOwner => Native.Read(Native.Resolve("DolocAPI, Assembly-CSharp"), "timeScaleManager");
        private bool SameSaveSession => sessionAgentData != null &&
            ReferenceEquals(sessionArchive, Native.Archive) && ReferenceEquals(sessionAgentData, CurrentAgentData);
        internal bool IsCurrentSaveSession => sessionActive && SameSaveSession;

        internal void BeginSaveSession()
        {
            sessionArchive = Native.Archive;
            sessionAgentData = CurrentAgentData ?? throw new InvalidOperationException("Loaded player data is unavailable.");
            sessionActive = true;
            DebugConsoleMovementHooks.SessionIsCurrent = () => IsCurrentSaveSession;
        }

        internal void EndSaveSession(string reason)
        {
            sessionActive = false;
            // Disable effects first; restoration debt can outlive activation.
            RestoreTransientState(reason);
            sessionArchive = null;
            sessionAgentData = null;
        }

        public bool GodModeEnabled => godEnabled && IsCurrentSaveSession;

        public DebugCommandResult SetGodMode(IManifest owner, bool enabled)
        {
            var result = new DebugCommandResult();
            try
            {
                if (enabled)
                {
                    if (!IsCurrentSaveSession)
                        return Fail(result, "no-current-save-session", "Load a playable save before enabling god mode.");
                    RefillGodVitals();
                    godOwnerId = owner.UniqueID;
                    godEnabled = true;
                }
                else
                    StopGodMode();
                result.Success = true;
                result.Message = enabled ? "God mode enabled; current health, energy and spirit are full." : "God mode disabled; native damage and consumption resume.";
            }
            catch (Exception error)
            {
                StopGodMode();
                runtime.Error("god-mode", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
            return result;
        }

        private void StopGodMode()
        {
            godEnabled = false;
            godOwnerId = string.Empty;
        }

        private void RefillGodVitals()
        {
            if (!IsCurrentSaveSession)
                return;
            bool changed = FillVital("health", "MaxHealth");
            changed |= FillVital("energy", "MaxEnergy");
            changed |= FillVital("spirit", "MaxSpirit");
            if (!changed)
                return;
            object? bar = Native.Read(Native.Read(Native.Resolve("DolocAPI, Assembly-CSharp"), "uiSystem"), "agentStatusBar");
            if (bar == null)
                return;
            Native.Method(bar.GetType(), "UpdateHealth", 0, false)?.Invoke(bar, Array.Empty<object>());
            Native.Method(bar.GetType(), "UpdateEnergy", 0, false)?.Invoke(bar, Array.Empty<object>());
            Native.Method(bar.GetType(), "UpdateSpirit", 0, false)?.Invoke(bar, Array.Empty<object>());
        }

        private bool FillVital(string value, string maximum)
        {
            object? max = Native.Read(sessionAgentData, maximum);
            if (max == null)
                throw new InvalidOperationException("Player maximum is unavailable: " + maximum);
            if (Equals(Native.Read(sessionAgentData, value), max))
                return false;
            if (!Native.Write(sessionAgentData, value, max))
                throw new InvalidOperationException("Could not fill player " + value + " to its current maximum.");
            return true;
        }

        internal bool ProtectArchive(object archive, out int health)
        {
            health = 0;
            if (!GodModeEnabled || !ReferenceEquals(archive, sessionArchive))
                return false;
            RefillGodVitals();
            health = Native.Int(sessionAgentData, "MaxHealth", 0);
            return true;
        }

        internal bool ProtectSpirit(object data)
        {
            if (!GodModeEnabled || !ReferenceEquals(data, sessionAgentData))
                return false;
            RefillGodVitals();
            return true;
        }

        internal void ObserveNativeTimeScaleWrite(object manager)
        {
            if (!writingTimeScale && timeScaleLeaseActive && ReferenceEquals(manager, timeScaleOwner))
                ClearTimeScaleLease(); // Native control, including a same-value write, supersedes this lease.
        }

        public TimeScaleDebugResult SetTimeScale(IManifest owner, double multiplier)
        {
            var result = new TimeScaleDebugResult { RequestedMultiplier = multiplier, BeforeMultiplier = timeScaleMultiplier };
            if (!IsCurrentSaveSession)
                return Fail(result, "no-current-save-session", "Load a playable save before changing time scale.");
            if (double.IsNaN(multiplier) || multiplier < 1 || multiplier > 20 || multiplier != Math.Truncate(multiplier))
                return Fail(result, "invalid-time-scale", "Time scale must be an integer from 1 through 20.");
            if (timeScaleMutationUncertain && multiplier == 1)
                return ResetTimeScaleCore("explicit retry");
            if (timeScaleMutationUncertain)
                return Fail(result, "time-scale-restore-pending", "Restore the retained time-scale responsibility before applying another value.");
            try
            {
                object manager = CurrentTimeScaleOwner ?? throw new InvalidOperationException("Native time-scale manager is unavailable.");
                double current = ReadTimeScale(manager);
                object? loop = Native.Read(Native.Resolve("DolocAPI, Assembly-CSharp"), "gameLoop");
                if (current <= 0 || Native.Read(loop, "IsPaused") is bool paused && paused || Native.Read(loop, "IsGlobalPaused") is bool globalPaused && globalPaused)
                    return Fail(result, "native-paused", "Resume native gameplay before changing time scale.");
                bool previousOwned = timeScaleLeaseActive && ReferenceEquals(manager, timeScaleOwner) &&
                    ReferenceEquals(timeScaleSession, sessionAgentData) && NearlyEqual(current, timeScaleAppliedMultiplier);
                timeScaleOwner = manager;
                timeScaleSession = sessionAgentData;
                timeScaleLeaseOwnerId = owner.UniqueID;
                timeScalePriorMultiplier = current;
                timeScalePriorWasOwned = previousOwned;
                timeScaleAppliedMultiplier = multiplier;
                timeScaleLeaseActive = true;
                timeScaleMutationUncertain = true;
                WriteTimeScale(manager, multiplier, true);
                if (!NearlyEqual(ReadTimeScale(manager), multiplier))
                    throw new InvalidOperationException("Native time scale did not retain the requested value.");
                timeScaleMultiplier = multiplier;
                timeScaleMutationUncertain = false;
                if (multiplier == 1)
                    ClearTimeScaleLease();
                result.AfterMultiplier = multiplier;
                result.Success = true;
                result.Message = "Current session time scale=" + multiplier.ToString(CultureInfo.InvariantCulture) + ".";
            }
            catch (Exception error)
            {
                runtime.Error("time-scale-set", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
            return result;
        }

        public TimeScaleDebugResult ResetTimeScale(IManifest owner, string reason) => ResetTimeScaleCore(reason);

        private TimeScaleDebugResult ResetTimeScaleCore(string reason)
        {
            var result = new TimeScaleDebugResult { RequestedMultiplier = 1, BeforeMultiplier = timeScaleMultiplier };
            try
            {
                if (timeScaleLeaseActive)
                {
                    // A dead/replaced holder or archive is no longer ours to write. Native load
                    // entry ends the old session before replacement, including same-slot reloads.
                    if (!ReferenceEquals(timeScaleOwner, CurrentTimeScaleOwner) || !ReferenceEquals(timeScaleSession, CurrentAgentData))
                        ClearTimeScaleLease();
                    else
                    {
                        double current = ReadTimeScale(timeScaleOwner!);
                        bool ours = NearlyEqual(current, timeScaleAppliedMultiplier) ||
                            timeScaleMutationUncertain && timeScalePriorWasOwned && NearlyEqual(current, timeScalePriorMultiplier);
                        if (ours && !NearlyEqual(current, 1))
                        {
                            timeScaleMutationUncertain = true;
                            WriteTimeScale(timeScaleOwner!, 1, false);
                            if (!NearlyEqual(ReadTimeScale(timeScaleOwner!), 1))
                                throw new InvalidOperationException("Normal time scale restoration was not observed.");
                        }
                        ClearTimeScaleLease(); // A later native value also ends our responsibility.
                    }
                }
                result.Success = true;
                result.AfterMultiplier = 1;
                result.Message = "Session time control released: " + reason + ".";
            }
            catch (Exception error)
            {
                runtime.Error("time-scale-restore", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
            return result;
        }

        private static double ReadTimeScale(object manager) => Convert.ToDouble(
            Native.Read(manager, "currentTimeScale") ?? throw new InvalidOperationException("Native time-scale value is unavailable."), CultureInfo.InvariantCulture);

        private void WriteTimeScale(object manager, double value, bool showTip)
        {
            writingTimeScale = true;
            try
            {
                var method = Native.Method(manager.GetType(), "SetTimeScale", 2, false)
                    ?? throw new MissingMethodException(manager.GetType().FullName, "SetTimeScale");
                method.Invoke(manager, new object[] { (float)value, showTip });
            }
            finally { writingTimeScale = false; }
        }

        private void ClearTimeScaleLease()
        {
            timeScaleOwner = null;
            timeScaleSession = null;
            timeScaleLeaseOwnerId = string.Empty;
            timeScaleMultiplier = timeScaleAppliedMultiplier = timeScalePriorMultiplier = 1;
            timeScaleLeaseActive = timeScaleMutationUncertain = timeScalePriorWasOwned = false;
        }
    }
}
