using DTMAPI.Abstractions;
using System;
using System.IO;
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
                ExerciseConfigPage(helper, menu, uniqueId);
        }

        private void ExerciseConfigPage(IDtmHelper helper, IDtmConfigMenuApi menu, string uniqueId)
        {
            IConfigMenuPage? page = menu.GetPage(uniqueId);
            if (page == null)
            {
                helper.Monitor.Log("HookProbe ConfigPage MISSING " + uniqueId, LogLevel.Warn);
                return;
            }
            if (page.IsLocked)
            {
                helper.Monitor.Log("HookProbe ConfigPage LOCKED " + uniqueId + " reason=" + page.LockReason, LogLevel.Warn);
                return;
            }

            string configPath = helper.Config.GetConfigPath(page.Manifest);
            string original = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
            bool hadOriginal = File.Exists(configPath);
            try
            {
                string kinds = string.Join(",", page.Items.Where(item => item.Kind != "Section" && item.Kind != "Paragraph").Select(item => item.Kind).Distinct().OrderBy(value => value));
                helper.Monitor.Log("HookProbe ConfigPage Visible OK " + uniqueId + " kinds=" + kinds);

                IConfigMenuItem? item = page.Items.FirstOrDefault(candidate => candidate.CanEdit && candidate.Kind != "Button" && candidate.Kind != "Section" && candidate.Kind != "Paragraph");
                if (item == null)
                {
                    helper.Monitor.Log("HookProbe ConfigPage Editable MISSING " + uniqueId, LogLevel.Warn);
                    return;
                }

                page.BeginEditing();
                string beforeCancel = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
                if (!TrySetAlternateValue(item, out string cancelError))
                {
                    helper.Monitor.Log("HookProbe ConfigPage Edit MISSING " + uniqueId + " error=" + cancelError, LogLevel.Warn);
                    return;
                }
                page.Cancel();
                string afterCancel = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
                if (afterCancel == beforeCancel)
                    helper.Monitor.Log("HookProbe ConfigPage CancelNoWrite OK " + uniqueId);
                else
                    helper.Monitor.Log("HookProbe ConfigPage CancelNoWrite MISMATCH " + uniqueId, LogLevel.Warn);

                page.BeginEditing();
                item = page.Items.FirstOrDefault(candidate => candidate.CanEdit && candidate.Kind != "Button" && candidate.Kind != "Section" && candidate.Kind != "Paragraph");
                string saveError = string.Empty;
                if (item == null || !TrySetAlternateValue(item, out saveError))
                {
                    helper.Monitor.Log("HookProbe ConfigPage SaveEdit MISSING " + uniqueId + " error=" + saveError, LogLevel.Warn);
                    return;
                }
                page.Save();
                string afterSave = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
                if (afterSave != beforeCancel)
                    helper.Monitor.Log("HookProbe ConfigPage SaveWrite OK " + uniqueId);
                else
                    helper.Monitor.Log("HookProbe ConfigPage SaveWrite MISMATCH " + uniqueId, LogLevel.Warn);

                page.Reset();
                page.Save();
                string afterReset = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
                if (afterReset != afterSave)
                    helper.Monitor.Log("HookProbe ConfigPage ResetDefault OK " + uniqueId);
                else
                    helper.Monitor.Log("HookProbe ConfigPage ResetDefault MISMATCH " + uniqueId, LogLevel.Warn);
            }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? ".");
                if (hadOriginal)
                    File.WriteAllText(configPath, original);
                else if (File.Exists(configPath))
                    File.Delete(configPath);
            }
        }

        private static bool TrySetAlternateValue(IConfigMenuItem item, out string error)
        {
            string alternate;
            if (item.Kind.Equals("Bool", StringComparison.OrdinalIgnoreCase))
                alternate = item.PendingValue.Equals("true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
            else if (item.Kind.Equals("Number", StringComparison.OrdinalIgnoreCase))
            {
                double max = item.MaxValue ?? 10;
                double min = item.MinValue ?? 0;
                alternate = (max > 1 ? max : min).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (item.Kind.Equals("Choice", StringComparison.OrdinalIgnoreCase))
                alternate = item.AllowedValues.FirstOrDefault(value => !value.Equals(item.PendingValue, StringComparison.OrdinalIgnoreCase)) ?? item.PendingValue;
            else if (item.Kind.Equals("Keybind", StringComparison.OrdinalIgnoreCase))
                alternate = item.PendingValue.Equals("F12", StringComparison.OrdinalIgnoreCase) ? "F7" : "F12";
            else
                alternate = item.PendingValue + "-dtmapi-smoke";

            return item.TrySetPendingValue(alternate, out error);
        }
    }
}
