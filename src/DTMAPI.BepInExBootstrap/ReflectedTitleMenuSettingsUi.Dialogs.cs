using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown.Native;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed partial class ReflectedTitleMenuSettingsUi
    {
        private ConfigMenuDialogService.Session? dialogView;
        private object? dialogRoot, dialogPanel, dialogScaler, dialogNativeParent;
        private readonly List<ActionBinder> dialogEventBinders = new List<ActionBinder>();
        private readonly List<NavigationTarget> dialogTargets = new List<NavigationTarget>();
        private readonly MenuNavigationCycle dialogNavigation = new MenuNavigationCycle();
        private bool buildingDialogUi;
        private int dialogPage;
        private string? dialogFocus;

        private bool ShowConfigDialog(ConfigMenuDialogService.Session session)
        {
            if (!EnsureInitialized()) return false;
            nativeMenuInput ??= new NativeMenuInputAdapter(runtime);
            if (!nativeMenuInput.BeginDialog(out dialogNativeParent)) return false;
            dialogView = session;
            dialogPage = 0;
            dialogFocus = "DTMAPI.Dialog.Button." + session.Dialog.DefaultButtonId;
            dialogNavigation.Reset();
            EnsureEventSystem();
            AcquireNavigationLease();
            RenderConfigDialog();
            return true;
        }

        private void ClearDialogDrawing()
        {
            foreach (ActionBinder binder in dialogEventBinders) binder.Clear();
            dialogEventBinders.Clear();
            dialogTargets.Clear();
            SetActive(dialogRoot, false);
            Destroy(dialogRoot);
            dialogRoot = dialogPanel = dialogScaler = null;
        }

        private void HideConfigDialog(ConfigMenuDialogService.Session session)
        {
            if (!ReferenceEquals(dialogView, session)) return;
            dialogView = null;
            dialogNativeParent = null;
            ClearDialogDrawing();
            nativeMenuInput?.EndDialog();
            dialogNavigation.Reset();
            SetNativeSelection(null);
            dirty = true;
        }

        private void RenderConfigDialog()
        {
            ConfigMenuDialogService.Session? session = dialogView;
            if (session == null) return;
            ClearDialogDrawing();
            bool previousTracking = trackingRenderedObjects;
            trackingRenderedObjects = false;
            buildingDialogUi = true;
            try
            {
                dialogRoot = CreateUiObject("DTMAPI.ConfigDialog.Canvas", null);
                object canvas = AddComponent(dialogRoot, canvasType!);
                SetEnumProperty(canvas, "renderMode", 0);
                SetProperty(canvas, "sortingOrder", 32760);
                dialogScaler = AddComponent(dialogRoot, canvasScalerType!);
                SetEnumProperty(dialogScaler, "uiScaleMode", 0);
                SetProperty(dialogScaler, "scaleFactor", DialogScale(session.Dialog.Size));
                AddComponent(dialogRoot, graphicRaycasterType!);
                CreateImage(dialogRoot, "DTMAPI.Dialog.Shade", null, Color(0, 0, 0, .62f), stretch: true);
                dialogPanel = CreateImage(dialogRoot, "DTMAPI.Dialog.Panel", null, Color(.07f, .10f, .12f, 1));
                float panelWidth = session.Dialog.Size.Width, panelHeight = session.Dialog.Size.Height;
                float contentWidth = panelWidth - 52;
                SetRect(dialogPanel, Vector2(.5f, .5f), Vector2(.5f, .5f), Vector2(.5f, .5f), Vector2(0, 0), Vector2(panelWidth, panelHeight));
                AddText(dialogPanel, "DTMAPI.Dialog.Title", session.Dialog.Title, 22, Color(1, 1, 1, 1), TextAnchorMiddleLeft, 26, -22, contentWidth, 36);
                // Paginate full text and button lists; no caller-supplied text is silently truncated.
                IReadOnlyList<string> lines = WrapDetailText(session.Dialog.Message, Math.Max(1, (int)(contentWidth / 8.7f)));
                int columns = Math.Min(3, Math.Max(1, (int)((contentWidth + 12) / 212)));
                int buttonsPerPage = columns * (panelHeight >= 300 ? 2 : 1);
                int rows = Math.Min(2, (Math.Min(buttonsPerPage, session.Dialog.Buttons.Count) + columns - 1) / columns);
                float buttonTop = panelHeight - 26 - rows * 42;
                float bodyTop = panelHeight < 220 ? 64 : 74;
                int linesPerPage = Math.Max(1, (int)((buttonTop - bodyTop - 40) / 22));
                int textPages = Math.Max(1, (lines.Count + linesPerPage - 1) / linesPerPage);
                int buttonPages = (session.Dialog.Buttons.Count + buttonsPerPage - 1) / buttonsPerPage;
                int pages = Math.Max(textPages, buttonPages);
                dialogPage = Math.Min(dialogPage, pages - 1);
                string body = string.Join("\n", lines.Skip(Math.Min(dialogPage, textPages - 1) * linesPerPage).Take(linesPerPage));
                object bodyText = AddText(dialogPanel, "DTMAPI.Dialog.Body", body, 16, Color(.94f, .96f, .98f, 1), 0, 26, -bodyTop, contentWidth, linesPerPage * 22);
                SetProperty(bodyText, "supportRichText", false);
                if (pages > 1)
                {
                    AddText(dialogPanel, "DTMAPI.Dialog.Page", (dialogPage + 1) + " / " + pages, 14, Color(1, 1, 1, 1), TextAnchorMiddleCenter, panelWidth / 2 - 70, 40 - buttonTop, 140, 28);
                    if (dialogPage > 0) CreateButton(dialogPanel, "DTMAPI.Dialog.Previous", "<", () => { dialogPage--; RenderConfigDialog(); }, Color(.22f, .30f, .35f, 1), Color(1, 1, 1, 1), panelWidth / 2 - 134, 40 - buttonTop, 50, 28);
                    if (dialogPage + 1 < pages) CreateButton(dialogPanel, "DTMAPI.Dialog.Next", ">", () => { dialogPage++; RenderConfigDialog(); }, Color(.22f, .30f, .35f, 1), Color(1, 1, 1, 1), panelWidth / 2 + 84, 40 - buttonTop, 50, 28);
                }
                var buttons = session.Dialog.Buttons.Skip(Math.Min(dialogPage, buttonPages - 1) * buttonsPerPage).Take(buttonsPerPage).ToArray();
                columns = Math.Min(columns, buttons.Length);
                float width = (contentWidth - (columns - 1) * 12) / columns;
                for (int i = 0; i < buttons.Length; i++)
                {
                    DtmConfigDialogButton button = buttons[i];
                    string name = "DTMAPI.Dialog.Button." + button.Id;
                    object go = CreateButton(dialogPanel, name, button.Label, () => session.Select(button.Id),
                        Color(.20f, .32f, .37f, 1), Color(1, 1, 1, 1), 26 + (i % columns) * (width + 12), -buttonTop - (i / columns) * 42, width, 34);
                    SetProperty(GetComponent(go, buttonType!)!, "interactable", button.Enabled);
                    if (!button.Enabled) dialogTargets.RemoveAll(t => t.Name == name);
                }
                SetDialogFocus(dialogTargets.Find(t => t.Name == dialogFocus) ?? dialogTargets.FirstOrDefault());
            }
            finally { buildingDialogUi = false; trackingRenderedObjects = previousTracking; }
        }

        private void SetDialogFocus(NavigationTarget? target)
        {
            if (target == null) return;
            NavigationTarget? previous = dialogTargets.Find(t => t.Name == dialogFocus);
            if (previous != null) SetProperty(previous.Image, "color", previous.Background);
            dialogFocus = target.Name;
            SetProperty(target.Image, "color", Color(.28f, .55f, .67f, 1));
            SetNativeSelection(target.Go);
        }

        private void UpdateConfigDialog()
        {
            runtime.ConfigDialogs.Validate();
            ConfigMenuDialogService.Session? session = dialogView;
            if (session != null && !ReferenceEquals(dialogNativeParent, nativeMenuInput?.CurrentNativeMenu)) session.Close();
            if (nativeMenuInput?.DialogBlocking != true) return;
            MenuInput sample = nativeMenuInput.Read();
            session = dialogView;
            if (session == null)
            {
                if (!nativeMenuInput.DialogBlocking)
                {
                    if (IsOwnedRuntimeMenuOpen()) RestoreNavigationFocus();
                    else ReleaseNavigationLease();
                }
                return;
            }
            if (dialogScaler != null) SetProperty(dialogScaler, "scaleFactor", DialogScale(session.Dialog.Size));
            int move = dialogNavigation.Advance(sample, Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency, out bool confirm, out bool cancel);
            if (cancel) { session.Select(session.Dialog.CancelButtonId); return; }
            int index = dialogTargets.FindIndex(t => t.Name == dialogFocus);
            if (index < 0) { SetDialogFocus(dialogTargets.FirstOrDefault()); return; }
            if (confirm) { dialogTargets[index].Action?.Invoke(); return; }
            if (ReflectedUnityInput.SampleButtonCached("Tab").PressedEdge)
            {
                int direction = ReflectedUnityInput.GetKey("LeftShift") || ReflectedUnityInput.GetKey("RightShift") ? -1 : 1;
                SetDialogFocus(dialogTargets[(index + direction + dialogTargets.Count) % dialogTargets.Count]);
            }
            else if (move != 0)
            {
                int next = FindDirectionalTarget(dialogTargets, index, move);
                if (next >= 0) SetDialogFocus(dialogTargets[next]);
            }
        }

        private float DialogScale(DtmConfigDialogSize size) => Math.Min(CalculateViewportScale(viewportWidth, viewportHeight),
            Math.Min(viewportWidth / (size.Width + 40f), viewportHeight / (size.Height + 40f)));
    }
}
