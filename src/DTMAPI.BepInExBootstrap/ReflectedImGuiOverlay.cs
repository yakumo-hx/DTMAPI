using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed class ReflectedImGuiOverlay
    {
        private readonly DtmApiRuntime runtime;
        private readonly IConfigMenuRuntime configMenu;
        private readonly DtmUiText text = new DtmUiText();
        private Type? rectType;
        private Type? guiType;
        private Type? screenType;
        private MethodInfo? boxMethod;
        private MethodInfo? labelMethod;
        private MethodInfo? buttonMethod;
        private MethodInfo? textFieldMethod;
        private string? selectedConfigModId;
        private string? capturingKeybindItemId;
        private string statusMessage = string.Empty;

        public ReflectedImGuiOverlay(DtmApiRuntime runtime, IConfigMenuRuntime configMenu)
        {
            this.runtime = runtime;
            this.configMenu = configMenu;
        }

        public bool IsCapturingKey => capturingKeybindItemId != null;

        public void Render()
        {
            if (!runtime.UI.IsOpen)
                return;
            if (!runtime.UI.CanDrawOverlay)
                return;
            if (!EnsureGui())
                return;

            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            float width = Math.Min(860, Math.Max(560, GetScreenWidth() - 40));
            float height = Math.Min(660, Math.Max(440, GetScreenHeight() - 40));
            float x = 20;
            float y = 20;
            Box(x, y, width, height, "DTMAPI");
            Label(x + 16, y + 8, width - 92, 24, "DTMAPI " + DtmApiRuntime.ApiVersion + "  |  dev diagnostics overlay  |  context=" + runtime.UI.InputContext);
            if (Button(x + width - 68, y + 8, 50, 24, "Close"))
                CloseOverlay();
            y += 38;

            DrawTabs(x + 16, y, width - 32);
            y += 34;

            switch (runtime.UI.CurrentPage)
            {
                case DtmOverlayPage.Status:
                    DrawStatus(snapshot, x + 16, y, width - 32);
                    break;
                case DtmOverlayPage.Mods:
                    DrawMods(snapshot, x + 16, y, width - 32);
                    break;
                case DtmOverlayPage.Config:
                    DrawConfig(snapshot, x + 16, y, width - 32, height - 86);
                    break;
                case DtmOverlayPage.Errors:
                    DrawErrors(snapshot, x + 16, y, width - 32);
                    break;
                case DtmOverlayPage.Hooks:
                    DrawHooks(snapshot, x + 16, y, width - 32);
                    break;
                case DtmOverlayPage.Logs:
                    DrawLogs(snapshot, x + 16, y, width - 32);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
                Label(x + 16, y + height - 116, width - 32, 22, Truncate(statusMessage, 160));
        }

        private void CloseOverlay()
        {
            string? configModId = selectedConfigModId;
            if (!string.IsNullOrWhiteSpace(configModId))
            {
                string nonNullConfigModId = configModId!;
                IConfigMenuPage? page = configMenu.GetPage(nonNullConfigModId);
                if (page != null && page.HasPendingChanges)
                    configMenu.Cancel(nonNullConfigModId);
            }
            capturingKeybindItemId = null;
            runtime.UI.Close();
        }

        private void DrawTabs(float x, float y, float width)
        {
            var tabs = new[]
            {
                Tuple.Create("Status", DtmOverlayPage.Status),
                Tuple.Create("Mods", DtmOverlayPage.Mods),
                Tuple.Create("Config", DtmOverlayPage.Config),
                Tuple.Create("Errors", DtmOverlayPage.Errors),
                Tuple.Create("Hooks", DtmOverlayPage.Hooks),
                Tuple.Create("Logs", DtmOverlayPage.Logs)
            };
            float buttonWidth = width / tabs.Length - 4;
            for (int i = 0; i < tabs.Length; i++)
            {
                if (Button(x + i * (buttonWidth + 4), y, buttonWidth, 26, tabs[i].Item1))
                    runtime.UI.SetPage(tabs[i].Item2);
            }
        }

        private void DrawStatus(RuntimeSnapshot snapshot, float x, float y, float width)
        {
            int line = 0;
            DrawLine(x, y, width, ref line, "Runtime: active since " + snapshot.StartedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            DrawLine(x, y, width, ref line, "Game path: " + snapshot.Paths.GamePath);
            DrawLine(x, y, width, ref line, "Mods discovered: " + snapshot.DiscoveredMods.Count + " | loaded: " + snapshot.LoadedMods.Count + " | errors: " + snapshot.Errors.Count);
            DrawLine(x, y, width, ref line, "Input boundary: gameplayHotkeys=" + runtime.UI.GameplayHotkeysAllowed + " | menuOpen=" + runtime.UI.IsOpen + " | draw=" + runtime.UI.CanDrawOverlay);
            DrawLine(x, y, width, ref line, "Workshop enablement remains official/Steam-owned; DTMAPI shows status, config, errors, hooks, and logs.");
        }

        private void DrawMods(RuntimeSnapshot snapshot, float x, float y, float width)
        {
            int line = 0;
            foreach (var mod in snapshot.DiscoveredMods.Take(18))
            {
                string state = snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == mod.Manifest.UniqueID) ? "loaded" : "not loaded";
                string enablement = mod.CanDtmApiToggle ? "local editable" : "locked to official/Steam";
                DrawLine(x, y, width, ref line, $"{mod.Manifest.UniqueID}  [{state}]  source={mod.Source}  enablement={enablement}");
            }
            if (snapshot.DiscoveredMods.Count == 0)
                DrawLine(x, y, width, ref line, "No DTMAPI manifests found in Mods or Workshop folders.");
        }

        private void DrawConfig(RuntimeSnapshot snapshot, float x, float y, float width, float height)
        {
            if (runtime.UI.RequestedConfigUniqueId != null && configMenu.GetPage(runtime.UI.RequestedConfigUniqueId) != null)
                selectedConfigModId = runtime.UI.RequestedConfigUniqueId;

            IConfigMenuPage[] pages = snapshot.ConfigPages.OrderBy(p => p.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase).ToArray();
            if (pages.Length == 0)
            {
                Label(x, y, width, 22, "No mods have registered config pages yet.");
                return;
            }

            if (selectedConfigModId == null || !pages.Any(p => p.Manifest.UniqueID.Equals(selectedConfigModId, StringComparison.OrdinalIgnoreCase)))
                selectedConfigModId = pages[0].Manifest.UniqueID;

            float listWidth = Math.Min(230, width * 0.33f);
            int pageLine = 0;
            foreach (IConfigMenuPage pageButton in pages.Take(16))
            {
                string label = pageButton.Manifest.UniqueID;
                if (pageButton.IsLocked)
                    label += " [locked]";
                if (Button(x, y + pageLine * 28, listWidth - 8, 24, Truncate(label, 34)))
                {
                    if (selectedConfigModId != null && selectedConfigModId != pageButton.Manifest.UniqueID)
                    {
                        IConfigMenuPage? oldPage = configMenu.GetPage(selectedConfigModId);
                        if (oldPage != null && oldPage.HasPendingChanges)
                            configMenu.Cancel(oldPage.Manifest.UniqueID);
                    }
                    selectedConfigModId = pageButton.Manifest.UniqueID;
                    statusMessage = string.Empty;
                    capturingKeybindItemId = null;
                }
                pageLine++;
            }

            IConfigMenuPage page = configMenu.GetPage(selectedConfigModId!) ?? pages[0];
            if (!page.IsEditing)
                configMenu.BeginEditing(page.Manifest.UniqueID);

            float contentX = x + listWidth;
            float contentWidth = width - listWidth;
            int line = 0;
            DrawLine(contentX, y, contentWidth, ref line, page.Manifest.Name + " (" + page.Manifest.UniqueID + ")" + (page.HasPendingChanges ? " *" : string.Empty));
            if (page.TitleScreenOnly)
                DrawLine(contentX, y, contentWidth, ref line, "Title-screen-only page.");
            if (page.IsLocked)
                DrawLine(contentX, y, contentWidth, ref line, "Locked: " + page.LockReason);

            float buttonY = y + line * 24;
            if (Button(contentX, buttonY, 72, 24, "Save"))
                TryPageAction(page, "Saved", () => configMenu.Save(page.Manifest.UniqueID));
            if (Button(contentX + 78, buttonY, 72, 24, "Reset"))
                TryPageAction(page, "Reset pending", () => configMenu.Reset(page.Manifest.UniqueID));
            if (Button(contentX + 156, buttonY, 72, 24, "Cancel"))
                TryPageAction(page, "Canceled", () => configMenu.Cancel(page.Manifest.UniqueID));
            line += 2;

            IDisposable? preview = configMenu.PreviewPendingValues(page);
            try
            {
                foreach (string conflict in configMenu.GetKeybindConflicts(page.Manifest.UniqueID).Take(3))
                    DrawLine(contentX, y, contentWidth, ref line, conflict);

                foreach (IConfigMenuItem item in page.Items.Take(Math.Max(1, (int)((height - 110) / 30))))
                    DrawConfigItem(page, item, contentX, y + line++ * 30, contentWidth);
            }
            finally
            {
                preview?.Dispose();
            }
        }

        private void DrawConfigItem(IConfigMenuPage page, IConfigMenuItem item, float x, float y, float width)
        {
            if (item.Kind == "Section")
            {
                Label(x, y, width, 24, item.Name);
                return;
            }
            if (item.Kind == "Paragraph")
            {
                Label(x + 10, y, width - 10, 24, Truncate(item.Name, 120));
                return;
            }

            Label(x, y, Math.Min(210, width * 0.42f), 24, Truncate(item.Name, 42));
            float controlX = x + Math.Min(220, width * 0.44f);
            float controlWidth = width - (controlX - x);

            if (page.IsLocked)
            {
                Label(controlX, y, controlWidth, 24, "Locked");
                return;
            }

            if (item.Kind == "Bool")
            {
                string next = item.PendingValue.Equals("true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
                if (Button(controlX, y, 90, 24, item.PendingValue.Equals("true", StringComparison.OrdinalIgnoreCase) ? "On" : "Off"))
                    SetPending(item, next);
            }
            else if (item.Kind == "Number")
            {
                double.TryParse(item.PendingValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
                double step = item.Interval ?? 1;
                if (Button(controlX, y, 28, 24, "-"))
                    SetPending(item, (value - step).ToString(CultureInfo.InvariantCulture));
                string changed = TextField(controlX + 32, y, 86, 24, item.PendingValue);
                if (changed != item.PendingValue)
                    SetPending(item, changed);
                if (Button(controlX + 122, y, 28, 24, "+"))
                    SetPending(item, (value + step).ToString(CultureInfo.InvariantCulture));
                Label(controlX + 156, y, controlWidth - 156, 24, $"[{item.MinValue:0.###}..{item.MaxValue:0.###}]");
            }
            else if (item.Kind == "Text")
            {
                string changed = TextField(controlX, y, Math.Min(220, controlWidth), 24, item.PendingValue);
                if (changed != item.PendingValue)
                    SetPending(item, changed);
            }
            else if (item.Kind == "Choice")
            {
                if (item.AllowedValues.Count == 0)
                {
                    Label(controlX, y, controlWidth, 24, "No choices");
                    return;
                }
                int current = Math.Max(0, item.AllowedValues.ToList().FindIndex(v => v.Equals(item.PendingValue, StringComparison.OrdinalIgnoreCase)));
                if (Button(controlX, y, 28, 24, "<"))
                    SetPending(item, item.AllowedValues[(current - 1 + item.AllowedValues.Count) % item.AllowedValues.Count]);
                Label(controlX + 34, y, Math.Min(140, controlWidth - 68), 24, text.DisplayLanguageName(item.PendingValue));
                if (Button(controlX + 178, y, 28, 24, ">"))
                    SetPending(item, item.AllowedValues[(current + 1) % item.AllowedValues.Count]);
            }
            else if (item.Kind == "InlineBoolNumber")
            {
                ParseInlineBoolNumber(item.PendingValue, out bool enabled, out double value);
                double step = item.Interval ?? 1;
                string numericText = value.ToString("0.###", CultureInfo.InvariantCulture);
                if (Button(controlX, y, 62, 24, enabled ? "On" : "Off"))
                    SetPending(item, (enabled ? "false" : "true") + "|" + value.ToString(CultureInfo.InvariantCulture));
                if (Button(controlX + 68, y, 28, 24, "-"))
                    SetPending(item, enabled.ToString().ToLowerInvariant() + "|" + (value - step).ToString(CultureInfo.InvariantCulture));
                string changed = TextField(controlX + 100, y, 72, 24, numericText);
                if (changed != numericText)
                    SetPending(item, enabled.ToString().ToLowerInvariant() + "|" + changed);
                if (Button(controlX + 176, y, 28, 24, "+"))
                    SetPending(item, enabled.ToString().ToLowerInvariant() + "|" + (value + step).ToString(CultureInfo.InvariantCulture));
                Label(controlX + 210, y, Math.Max(40, controlWidth - 210), 24, $"[{item.MinValue:0.###}..{item.MaxValue:0.###}]");
            }
            else if (item.Kind == "InlineBoolBool")
            {
                ParseInlineBoolBool(item.PendingValue, out bool enabled, out bool secondary);
                if (Button(controlX, y, 62, 24, enabled ? "On" : "Off"))
                    SetPending(item, (enabled ? "false" : "true") + "|" + secondary.ToString().ToLowerInvariant());
                bool showSecondary = enabled && (item.AllowedValues.Count < 3 || item.AllowedValues[2].Equals("true", StringComparison.OrdinalIgnoreCase));
                if (showSecondary)
                {
                    string secondaryName = item.AllowedValues.Count > 0 ? item.AllowedValues[0] : "Secondary";
                    Label(controlX + 72, y, 112, 24, Truncate(secondaryName, 16));
                    if (Button(controlX + 190, y, 62, 24, secondary ? "On" : "Off"))
                        SetPending(item, enabled.ToString().ToLowerInvariant() + "|" + (secondary ? "false" : "true"));
                }
            }
            else if (item.Kind == "Keybind")
            {
                bool capturing = item.ItemId == capturingKeybindItemId;
                if (capturing && ReflectedUnityInput.TryGetPressedKey(out string key))
                {
                    SetPending(item, key);
                    capturingKeybindItemId = null;
                }
                Label(controlX, y, 88, 24, capturing ? "Press key..." : item.PendingValue);
                if (Button(controlX + 94, y, 82, 24, capturing ? "Cancel" : "Capture"))
                    capturingKeybindItemId = capturing ? null : item.ItemId;
                if (Button(controlX + 182, y, 52, 24, "None"))
                    SetPending(item, "None");
            }
            else if (item.Kind == "Button")
            {
                if (Button(controlX, y, 110, 24, item.DisplayValue))
                    TryPageAction(page, "Action invoked", item.Invoke);
            }

            if (!string.IsNullOrWhiteSpace(item.ValidationError))
                Label(controlX + Math.Min(250, controlWidth * 0.62f), y, Math.Max(80, controlWidth * 0.38f), 24, item.ValidationError);
        }

        private void TryPageAction(IConfigMenuPage page, string success, Action action)
        {
            try
            {
                action();
                statusMessage = page.Manifest.UniqueID + ": " + success;
                capturingKeybindItemId = null;
            }
            catch (Exception ex)
            {
                statusMessage = page.Manifest.UniqueID + ": " + ex.Message;
            }
        }

        private void SetPending(IConfigMenuItem item, string value)
        {
            if (!item.TrySetPendingValue(value, out string error))
                statusMessage = item.Name + ": " + error;
            else
                statusMessage = string.Empty;
        }

        private static void ParseInlineBoolNumber(string value, out bool enabled, out double number)
        {
            enabled = false;
            number = 0;
            string[] parts = (value ?? string.Empty).Split('|');
            if (parts.Length > 0)
                bool.TryParse(parts[0], out enabled);
            if (parts.Length > 1)
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        private static void ParseInlineBoolBool(string value, out bool enabled, out bool secondary)
        {
            enabled = false;
            secondary = false;
            string[] parts = (value ?? string.Empty).Split('|');
            if (parts.Length > 0)
                bool.TryParse(parts[0], out enabled);
            if (parts.Length > 1)
                bool.TryParse(parts[1], out secondary);
        }

        private void DrawErrors(RuntimeSnapshot snapshot, float x, float y, float width)
        {
            int line = 0;
            foreach (IDtmErrorInfo error in snapshot.Errors.Take(16))
                DrawLine(x, y, width, ref line, $"{error.Time:HH:mm:ss} [{error.Owner}] {error.Message}");
            if (snapshot.Errors.Count == 0)
                DrawLine(x, y, width, ref line, "No DTMAPI errors recorded.");
        }

        private void DrawHooks(RuntimeSnapshot snapshot, float x, float y, float width)
        {
            int line = 0;
            foreach (IHookStatusInfo hook in snapshot.HookStatuses.Take(18))
                DrawLine(x, y, width, ref line, $"{hook.HookId}: {hook.Status} | {hook.Source}");
            if (snapshot.HookStatuses.Count == 0)
                DrawLine(x, y, width, ref line, "No hook statuses recorded.");
        }

        private void DrawLogs(RuntimeSnapshot snapshot, float x, float y, float width)
        {
            int line = 0;
            if (Button(x, y, 140, 26, "Export logs"))
                runtime.UI.ExportLogs();
            line += 2;
            DrawLine(x, y, width, ref line, "Latest log: " + runtime.Diagnostics.GetLatestLogPath());
            DrawLine(x, y, width, ref line, "Latest export: " + (string.IsNullOrWhiteSpace(snapshot.LastExportPath) ? "(none)" : snapshot.LastExportPath));
        }

        private void DrawLine(float x, float y, float width, ref int line, string text)
        {
            Label(x, y + line * 22, width, 22, Truncate(text, 150));
            line++;
        }

        private bool EnsureGui()
        {
            rectType ??= Type.GetType("UnityEngine.Rect, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Rect, UnityEngine");
            guiType ??= Type.GetType("UnityEngine.GUI, UnityEngine.IMGUIModule") ?? Type.GetType("UnityEngine.GUI, UnityEngine");
            screenType ??= Type.GetType("UnityEngine.Screen, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Screen, UnityEngine");
            if (rectType == null || guiType == null)
                return false;
            boxMethod ??= FindGuiMethod("Box");
            labelMethod ??= FindGuiMethod("Label");
            buttonMethod ??= FindGuiMethod("Button");
            textFieldMethod ??= FindGuiMethod("TextField");
            return boxMethod != null && labelMethod != null && buttonMethod != null && textFieldMethod != null;
        }

        private MethodInfo? FindGuiMethod(string name)
        {
            return guiType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return m.Name == name && p.Length == 2 && p[0].ParameterType == rectType && p[1].ParameterType == typeof(string);
                });
        }

        private void Box(float x, float y, float w, float h, string text) => boxMethod!.Invoke(null, new[] { Rect(x, y, w, h), text });
        private void Label(float x, float y, float w, float h, string text) => labelMethod!.Invoke(null, new[] { Rect(x, y, w, h), text });
        private bool Button(float x, float y, float w, float h, string text) => (bool)buttonMethod!.Invoke(null, new[] { Rect(x, y, w, h), text });
        private string TextField(float x, float y, float w, float h, string text) => (string)textFieldMethod!.Invoke(null, new[] { Rect(x, y, w, h), text ?? string.Empty });
        private object Rect(float x, float y, float w, float h) => Activator.CreateInstance(rectType!, x, y, w, h);

        private int GetScreenWidth() => GetScreenProperty("width", 1280);
        private int GetScreenHeight() => GetScreenProperty("height", 720);
        private int GetScreenProperty(string name, int fallback)
        {
            try
            {
                return screenType?.GetProperty(name)?.GetValue(null) is int value ? value : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static string Truncate(string text, int max)
        {
            text ??= string.Empty;
            if (text.Length <= max)
                return text;
            return text.Substring(0, max - 3) + "...";
        }
    }
}
