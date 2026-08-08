using DTMAPI.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HookProbeMod
{
    public sealed class ModEntry : DtmMod
    {
        private bool updateLogged;
        private bool secondLogged;
        private bool inputLogged;
        private bool uiLogged;

        public override void Entry(IDtmHelper helper)
        {
            helper.Monitor.Log("HookProbe Entry OK");
            helper.Events.GameLoop.GameLaunched += (_, __) =>
            {
                helper.Monitor.Log("HookProbe GameLaunched OK");
                VerifyInputRegistrations(helper);
                ExerciseConfigMenu(helper);
            };
            helper.Events.GameLoop.UpdateTicked += (_, e) =>
            {
                if (updateLogged)
                    return;
                updateLogged = true;
                helper.Monitor.Log("HookProbe UpdateTicked OK tick=" + e.Tick);
            };
            helper.Events.GameLoop.OneSecondUpdateTicked += (_, e) =>
            {
                if (secondLogged)
                    return;
                secondLogged = true;
                helper.Monitor.Log("HookProbe OneSecondUpdateTicked OK second=" + e.Second);
            };
            helper.Events.Save.SaveLoaded += (_, e) =>
            {
                helper.Monitor.Log($"HookProbe SaveLoaded OK slot={e.SaveSlot?.ToString() ?? "unknown"} isNewGame={e.IsNewGame}");
                helper.Diagnostics.RecordEvidence("HOOK-SAVELOADED", "HookProbe SaveLoaded event reached.");
                ExerciseUi(helper);
            };
            helper.Events.Save.SaveSaving += (_, e) => helper.Monitor.Log("HookProbe SaveSaving OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Events.Save.SaveSaved += (_, e) => helper.Monitor.Log("HookProbe SaveSaved OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Events.Workshop.ModListChanged += (_, e) => helper.Monitor.Log("HookProbe WorkshopModListChanged OK count=" + e.ModCount);
            helper.Events.Diagnostics.HookStatusChanged += (_, e) => helper.Monitor.Log("HookProbe HookStatusChanged OK " + e.HookId + "=" + e.Status);
        }

        private void VerifyInputRegistrations(IDtmHelper helper)
        {
            if (inputLogged)
                return;
            inputLogged = true;

            string[] required = { "F6", "F9", "F10", "F11" };
            string[] registered = helper.Input.GetRegisteredButtons().ToArray();
            string[] missing = required
                .Where(expected => !registered.Any(actual => actual.Equals(expected, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (missing.Length == 0)
                helper.Monitor.Log("HookProbe InputRegistrations OK keys=" + string.Join(",", required));
            else
                helper.Monitor.Log("HookProbe InputRegistrations MISSING keys=" + string.Join(",", missing), LogLevel.Warn);
        }

        private void ExerciseUi(IDtmHelper helper)
        {
            if (uiLogged)
                return;
            uiLogged = true;

            helper.UI.OpenDtmApiStatusPage();
            helper.Monitor.Log("HookProbe UI Status OK");
            helper.UI.OpenModListPage();
            helper.Monitor.Log("HookProbe UI Mods OK");
            helper.UI.OpenConfigPage();
            helper.Monitor.Log("HookProbe UI Config OK");
            helper.UI.OpenErrorPage();
            helper.Monitor.Log("HookProbe UI Errors OK");
            helper.UI.OpenHookStatusPage();
            helper.Monitor.Log("HookProbe UI Hooks OK");
            try
            {
                string report = helper.UI.ExportLogs();
                helper.Monitor.Log("HookProbe UI ExportLogs OK " + report);
                helper.Diagnostics.RecordEvidence("UI-SMOKE", "HookProbe exercised DTMAPI UI status/mod/config/error/hook pages and exported logs.");
            }
            catch (System.Exception ex)
            {
                helper.Monitor.LogException(ex, "HookProbe UI ExportLogs failed.");
                throw;
            }
        }

        private void ExerciseConfigMenu(IDtmHelper helper)
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log("HookProbe ConfigMenu API MISSING", LogLevel.Warn);
                return;
            }

            string[] targets =
            {
                "Yuuka.DTMAPI.ActionSpeed",
                "Yuuka.DTMAPI.AutoFishing",
                "Yuuka.DTMAPI.OneActionComplete"
            };

            foreach (string uniqueId in targets)
                ProbeConfigRegistration(helper, menu, uniqueId);
        }

        private void ProbeConfigRegistration(IDtmHelper helper, IDtmConfigMenuApi menu, string uniqueId)
        {
            IReadOnlyList<string> conflicts = menu.GetKeybindConflicts(uniqueId);
            helper.Monitor.Log("HookProbe ConfigMenu PublicApi OK " + uniqueId + " conflicts=" + conflicts.Count);
        }
    }
}
