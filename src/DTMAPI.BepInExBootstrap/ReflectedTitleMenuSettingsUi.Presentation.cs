using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Services;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed partial class ReflectedTitleMenuSettingsUi
    {
        private const int DetailLinesPerPage = 16;
        private string? detailTitle;
        private string detailBody = string.Empty;
        private string? detailReturnFocus;
        private int detailPageIndex;
        private float contentOffsetY;

        private readonly List<NavigationTarget> configActionTargets = new List<NavigationTarget>();

        // Enable actions while typing without rebuilding the focused InputField before pointer-up.
        private void RefreshConfigActionAvailability(bool inputChanged)
        {
            IConfigMenuPage? page = selectedConfigModId == null ? null : configMenu.GetPage(selectedConfigModId);
            bool pending = inputChanged || page?.HasPendingChanges == true;
            foreach (NavigationTarget target in configActionTargets)
            {
                bool enabled = pending && (target.Name != "DTMAPI.Config.Save" || page?.IsLocked != true);
                SetProperty(target.Control, "interactable", enabled);
                navigationTargets.RemoveAll(existing => existing.Name == target.Name);
                if (enabled) navigationTargets.Add(target);
            }
        }

        internal static IConfigMenuPage[] OrderConfigPages(IEnumerable<IConfigMenuPage> pages) => pages
            .OrderBy(page => page.Manifest.UniqueID.Equals("DTMAPI.TitleSettings", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(page => page.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase).ToArray();

        private static bool IsDiagnosticPage(DtmOverlayPage page) => page != DtmOverlayPage.Config && page != DtmOverlayPage.Mods;

        private void NavigateTo(DtmOverlayPage page)
        {
            ClearDetail();
            statusMessage = string.Empty;
            runtime.UI.SetPage(page);
            dirty = true;
        }

        private void RenderDiagnosticNavigation()
        {
            var tabs = new[] {
                Tuple.Create(T("diagnostics.overview", "Overview"), DtmOverlayPage.Status),
                Tuple.Create(T("diagnostics.issues", "Issues"), DtmOverlayPage.Errors),
                Tuple.Create(T("tab.logs", "Logs"), DtmOverlayPage.Logs),
                Tuple.Create(T("diagnostics.hooks", "Technical: Hooks"), DtmOverlayPage.Hooks),
                Tuple.Create(T("diagnostics.registry", "Technical: Registry / features"), DtmOverlayPage.Features)
            };
            for (int i = 0; i < tabs.Length; i++)
            {
                DtmOverlayPage page = tabs[i].Item2;
                CreateButton(panelContentRoot!, "DTMAPI.Diagnostics." + page, tabs[i].Item1, () => NavigateTo(page),
                    runtime.UI.CurrentPage == page ? Color(0.18f, 0.42f, 0.50f, 1f) : Color(0.12f, 0.15f, 0.18f, 1f),
                    Color(1f, 1f, 1f, 1f), 40 + i * 188, -126, 178, 28);
            }
        }

        private void OpenDetail(string title, string body, string returnFocus)
        {
            detailTitle = title;
            detailBody = body;
            detailReturnFocus = returnFocus;
            detailPageIndex = 0;
            SetNativeSelection(null);
            focusedName = "DTMAPI.Detail.Back";
            statusMessage = string.Empty;
            dirty = true;
        }

        private void ClearDetail()
        {
            detailTitle = null;
            detailBody = string.Empty;
            detailReturnFocus = null;
            detailPageIndex = 0;
        }

        private bool TryCloseDetail()
        {
            if (detailTitle == null) return false;
            SetNativeSelection(null);
            focusedName = detailReturnFocus;
            ClearDetail();
            statusMessage = string.Empty;
            dirty = true;
            return true;
        }

        private void RenderDetail()
        {
            AddText(panelContentRoot!, "DTMAPI.Detail.Title", Truncate(detailTitle!, 90), 18, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 52, -132, 930, 28);
            CreateButton(panelContentRoot!, "DTMAPI.Detail.Back", T("detail.back", "Back"), () => TryCloseDetail(),
                Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), 52, -168, 100, 28);
            CreateButton(panelContentRoot!, "DTMAPI.Detail.Copy", T("detail.copy", "Copy full text"), () => {
                statusMessage = TrySetClipboardText(detailBody) ? T("detail.copied", "Full text copied.") : T("detail.copyFailed", "Clipboard unavailable.");
                dirty = true;
            }, Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), 164, -168, 160, 28);
            IReadOnlyList<string> lines = WrapDetailText(detailBody);
            ClampPage(ref detailPageIndex, lines.Count, DetailLinesPerPage);
            GetPageBounds(lines.Count, detailPageIndex, DetailLinesPerPage, out int start, out int end, out int count);
            if (count > 1)
                RenderPager("DTMAPI.Detail.Pager", detailPageIndex, count, lines.Count, start, end, 732, -168, page => { detailPageIndex = page; dirty = true; });
            for (int i = start; i < end; i++)
            {
                object line = AddText(panelContentRoot!, "DTMAPI.Detail.Line." + i, lines[i], 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -208 - (i - start) * 26, 940, 24);
                SetProperty(line, "supportRichText", false);
            }
        }

        // Bound each rendered line without discarding the end of long paths or splitting Unicode text elements.
        internal static IReadOnlyList<string> WrapDetailText(string body, int lineWidth = 100)
        {
            var lines = new List<string>();
            foreach (string paragraph in body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = new StringBuilder();
                int width = 0;
                TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(paragraph.Replace("\t", "    "));
                while (elements.MoveNext())
                {
                    string element = elements.GetTextElement();
                    int size = element[0] > 0x7f || char.IsUpper(element[0]) || "wm@%#".IndexOf(element[0]) >= 0 ? 2 : 1;
                    if (width + size > lineWidth) { lines.Add(line.ToString()); line.Clear(); width = 0; }
                    line.Append(element);
                    width += size;
                }
                lines.Add(line.ToString());
            }
            return lines;
        }

        private void RenderDetailRow(string id, string label, string body, object color, float y)
        {
            object row = CreateButton(panelContentRoot!, id, string.Empty, () => OpenDetail(label, body, id),
                Color(0.12f, 0.15f, 0.18f, 1f), color, 52, y, 930, 24);
            AddText(row, id + ".Label", Truncate(label, 106), 13, color, TextAnchorMiddleLeft, 10, 0, 872, 24);
            AddText(row, id + ".Open", ">", 14, color, TextAnchorMiddleCenter, 894, 0, 26, 24);
        }

        internal static string CompactDiagnosticMessage(string message)
        {
            const string discovery = "Manifest discovery failed: ";
            int start = message.StartsWith(discovery, StringComparison.OrdinalIgnoreCase) ? discovery.Length : 0;
            int end = message.IndexOf("manifest.json:", start, StringComparison.OrdinalIgnoreCase);
            if (end >= 0 && (start > 0 || message.StartsWith("/", StringComparison.Ordinal) || message.StartsWith("\\", StringComparison.Ordinal) || message.Length > 2 && message[1] == ':'))
            {
                string path = message.Substring(start, end - start).TrimEnd('/', '\\');
                string[] segments = path.Replace('\\', '/').Split('/');
                int modRoot = Array.FindLastIndex(segments, part => part.Equals("MODS", StringComparison.OrdinalIgnoreCase));
                string owner = modRoot >= 0 && modRoot + 1 < segments.Length ? segments[modRoot + 1] : segments.LastOrDefault() ?? path;
                return owner + " — " + message.Substring(end + "manifest.json:".Length).Trim();
            }
            return message;
        }

        private string DiagnosticLabel(string owner, string message, string details = "")
        {
            if (owner == "DTMAPI.ModScanner" && !string.IsNullOrWhiteSpace(details)) message = details;
            string compact = CompactDiagnosticMessage(message);
            int duplicate = compact.IndexOf("Duplicate UniqueID ", StringComparison.OrdinalIgnoreCase);
            if (duplicate >= 0)
            {
                string id = compact.Substring(duplicate + "Duplicate UniqueID ".Length).Split(' ')[0];
                string name = runtime.UI.CurrentManagerModel?.Mods.FirstOrDefault(mod => mod.UniqueID.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id;
                return name + " — " + T("diagnostics.duplicate", "Duplicate sources; open to see the selected source");
            }
            int marker = compact.IndexOf("advanced-package-marker-invalid:", StringComparison.OrdinalIgnoreCase);
            if (marker >= 0)
                compact = compact.Substring(0, marker) + T("diagnostics.invalidPackage", "Advanced package receipt does not match; open for details.");
            return compact == message && !string.IsNullOrWhiteSpace(owner) ? owner + " — " + compact : compact;
        }

        private string ModDetail(ManagerModRow mod) => mod.Name + "\n" + mod.UniqueID + "\n" +
            T("mods.version", "Version") + ": " + mod.Version + "\n" + string.Join("\n", FormatManagerModDetailLines(mod)) +
            "\n\n" + T("detail.raw", "Raw diagnostic fields") + "\n" +
            "status=" + mod.Status + "\nstatusCode=" + mod.StatusCode + "\nreason=" + mod.Reason +
            "\nsource=" + mod.Source + "\nmanifest=" + mod.ManifestPath + "\nroot=" + mod.RootPath +
            "\nidentity=" + mod.ManagedIdentity + "\nplacement=" + mod.ManagedPlacement +
            "\ncompatibility=" + mod.GameCompatibility + "\nrestart=" + mod.RestartHint;
    }
}
