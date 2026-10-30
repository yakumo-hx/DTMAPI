using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed partial class MoreEquipmentSlotsNativeRuntime
    {
        // The equipment widget owns the reopen entry; ConfigMenu owns dialog presentation.
        private object? choiceRoot;
        private readonly List<UiLease> choiceListeners = new List<UiLease>();
        private string choiceUiRevision = "";
        private bool choiceStatusDismissed;
        private IDtmConfigMenuDialogsApi? configDialogs;
        private IManifest? dialogOwner;
        private IConfigMenuDialogSession? choiceDialog;

        internal void ConfigureDialogs(IDtmConfigMenuDialogsApi dialogs, IManifest owner)
        { configDialogs = dialogs; dialogOwner = owner; }

        private void ClearEmbeddedChoiceUi(UiSession? session)
        {
            choiceDialog?.Dispose();
            choiceDialog = null;
            foreach (var lease in choiceListeners) lease.ClearListeners();
            choiceListeners.Clear();
            DestroyUnityObject(choiceRoot);
            choiceRoot = null;
            choiceUiRevision = "";
        }

        private void RefreshEmbeddedChoiceUi(UiSession session)
        {
            var choice = embeddedSession;
            bool show = choice != null && session.Visible && !session.LayoutBlocked && !choiceStatusDismissed &&
                (choice.State != EquipmentSlotEmbeddedState.Ready ||
                 ((choice.Source != null || choice.SourceProblem.Length > 0) && !choiceStatusDismissed));
            if (!show) { ClearEmbeddedChoiceUi(session); return; }
            string revision = choice!.State + ":" + choice.Deferred + ":" + choice.Message + ":" + choice.Source?.Path;
            if (choiceUiRevision == revision && (choiceDialog?.IsOpen == true || choiceRoot != null)) return;
            ClearEmbeddedChoiceUi(session);
            bool selecting = choice.State == EquipmentSlotEmbeddedState.NeedsChoice && !choice.Deferred;
            if (choice.Deferred)
            {
                CreateChoiceReopenEntry(session);
                choiceUiRevision = revision;
                return;
            }
            DtmConfigDialog dialog;
            if (selecting)
            {
                string body = DescribeChoiceSource(choice.Source) + "\n\n当前存档：" + DescribeGroup(choice.Current) +
                    "\n\n此电脑旧版：" + DescribeGroup(choice.Source?.Slots) +
                    "\n\n" + EquipmentSlotEmbeddedSession.AdoptionWarning +
                    "\n不合并、不返还替换掉的装备。确认后仍需正常保存；未保存退出会回到原存档。" +
                    "\n选择保留当前装备，将清理本次明确放弃的旧文件。";
                dialog = new DtmConfigDialog("旧装备选择", body, new[] {
                    new DtmConfigDialogButton("current", "保留当前存档装备", choice.Current != null),
                    new DtmConfigDialogButton("legacy", "确认采用旧版装备"),
                    new DtmConfigDialogButton("defer", "暂不处理") }, "defer");
            }
            else
            {
                dialog = new DtmConfigDialog("旧装备选择", choice.Message,
                    new[] { new DtmConfigDialogButton("dismiss", "知道了") }, "dismiss");
            }
            bool opened = configDialogs != null && dialogOwner != null && configDialogs.TryShowDialog(dialogOwner, dialog, id =>
            {
                if (!ReferenceEquals(embeddedSession, choice) || !ReferenceEquals(uiSession, session)) return;
                object bar = session.AccessoriesBar;
                int count = session.OfficialPassiveCount;
                try
                {
                    if (id == "current") ChooseEmbedded(false);
                    else if (id == "legacy") ChooseEmbedded(true);
                    else if (id == "defer") DeferEmbeddedChoice();
                    else choiceStatusDismissed = true;
                }
                finally { RenderAccessoriesBar(bar, count, "config dialog choice: " + id); }
            }, out choiceDialog);
            if (!opened) CreateChoiceReopenEntry(session);
            choiceUiRevision = revision;
        }

        private void CreateChoiceReopenEntry(UiSession session)
        {
            choiceRoot = CreateChoiceRect(session.WidgetRoot!, "DTMAPI.MoreEquipmentSlots.ChoiceEntry", 320, 48, 0, -280);
            SetIgnoreLayout(choiceRoot);
            AddChoiceButton(session, RequireRectTransform(choiceRoot), GetChoiceFont(), "重新打开旧装备选择", 0, 0, 320, () =>
            {
                choiceStatusDismissed = false;
                if (embeddedSession?.Deferred == true) ReopenEmbeddedChoice();
                else choiceUiRevision = "";
            });
        }

        private static string DescribeChoiceSource(EquipmentSlotLegacySource? source)
        {
            if (source == null) return "旧来源：没有可用旧文件。";
            // The captured source admits only the canonical scoped/global paths.
            // Describe that bound path, never the current archive or a new scan.
            string directory = Path.GetDirectoryName(source.Path) ?? "";
            string folder = Path.GetFileName(directory);
            string kind = "此电脑的全局旧装备（不限定存档槽）";
            if (Path.GetFileName(Path.GetDirectoryName(directory)) == "equipment-slots" &&
                folder.StartsWith("slot-", StringComparison.Ordinal) &&
                int.TryParse(folder.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                kind = "此电脑的分槽旧装备 · 存档 " + ((long)index + 1).ToString(CultureInfo.InvariantCulture) +
                    "（文件夹 " + folder + "）";
            return "旧来源：" + kind + "\n文件夹：" + directory + "\n文件名：" + Path.GetFileName(source.Path);
        }

        private void AddChoiceButton(UiSession session, object parent, object font, string label,
            float x, float y, float width, Action action, bool enabled = true)
        {
            object root = CreateChoiceRect(parent, label, width, 48, x, y);
            object background = AddChoiceImage(root, .22f, .30f, .33f, 1f);
            object button = AddComponent(root, RequireRuntimeType("UnityEngine.UI.Button"))!;
            SetMember(button, "targetGraphic", background);
            SetMember(button, "interactable", enabled);
            AddChoiceText(RequireRectTransform(root), font, label, width - 12, 40, 0, 0, 20, true);
            var lease = new UiLease(button);
            choiceListeners.Add(lease);
            var displayedChoice = embeddedSession;
            BindVoidEvent(Read(button, "onClick"), () =>
            {
                if (!ReferenceEquals(embeddedSession, displayedChoice) || !ReferenceEquals(uiSession, session))
                {
                    monitor.Log("MES ignored a button from an expired choice UI.");
                    return;
                }
                // Choice/reopen disposes the old widget roots. Keep only the
                // native widget identity and rebuild against the new session.
                object bar = session.AccessoriesBar;
                int count = session.OfficialPassiveCount;
                try { action(); }
                catch (Exception ex)
                {
                    lastMessage = ex.GetBaseException().Message;
                    monitor.Log("MES choice UI: " + lastMessage, LogLevel.Warn);
                }
                RenderAccessoriesBar(bar, count, "embedded choice button: " + label);
            }, lease);
        }

        private static object CreateChoiceRect(object parent, string name, float width, float height, float x, float y)
        {
            object root = CreateProductRowRoot(parent);
            SetMember(root, "name", name);
            SetRectTransform(RequireRectTransform(root), .5f, .5f, .5f, .5f, width, height, x, y);
            return root;
        }
        private static object AddChoiceImage(object root, float r, float g, float b, float a)
        {
            object image = AddComponent(root, RequireRuntimeType("UnityEngine.UI.Image"))!;
            SetMember(image, "color", CreateUnityValue("UnityEngine.Color", r, g, b, a));
            return image;
        }
        private static object GetChoiceFont()
        {
            Type textType = RequireRuntimeType("TMPro.TextMeshProUGUI");
            var find = RequireRuntimeType("UnityEngine.Resources").GetMethod("FindObjectsOfTypeAll",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null)!;
            foreach (object text in (Array)find.Invoke(null, new object[] { textType })!)
            {
                object? font = Read(text, "font");
                if (font == null) continue;
                // Quantity labels can use a digits-only atlas. Prefer the font
                // already rendering Chinese in the active native UI.
                string label = Read(text, "text") as string ?? "";
                if (Read(Read(text, "gameObject"), "activeInHierarchy") is bool active && active)
                    foreach (char value in label)
                        if (value >= '\u4e00' && value <= '\u9fff') return font;
            }
            foreach (object text in (Array)find.Invoke(null, new object[] { textType })!)
            {
                object? font = Read(text, "font");
                if (Read(font, "characterLookupTable") is IDictionary glyphs && glyphs.Contains((uint)'旧')) return font!;
            }
            throw new InvalidOperationException("Native UI font is unavailable.");
        }
        private static void AddChoiceText(object parent, object font, string value, float width, float height,
            float x, float y, float size, bool centered = false)
        {
            object root = CreateChoiceRect(parent, "Text", width, height, x, y);
            object text = AddComponent(root, RequireRuntimeType("TMPro.TextMeshProUGUI"))!;
            SetMember(text, "font", font);
            SetMember(text, "fontSize", size);
            SetMember(text, "text", value);
            SetMember(text, "richText", false);
            SetMember(text, "enableWordWrapping", true);
            SetMember(text, "raycastTarget", false);
            SetMember(text, "color", CreateUnityValue("UnityEngine.Color", .97f, .97f, .93f, 1f));
            SetMember(text, "alignment", Enum.Parse(RequireRuntimeType("TMPro.TextAlignmentOptions"), centered ? "Center" : "TopLeft"));
        }
    }
}
