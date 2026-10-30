using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DolocTown;

namespace DTMAPI.MoreSaves
{
    // Enter ends text editing only. This disk operation requires the explicit pointer/button click.
    internal sealed class MoreSavesRenameButton : Button
    {
        public override void OnSubmit(BaseEventData eventData) { }
    }

    internal sealed partial class MoreSavesNativePage
    {
        private GameObject? renameRoot;
        private GameObject? renameFooterSpace;
        private InputField? nameInput;
        private Button? renameButton;

        public void ResetRenameInput(MoreSavesArchive? archive)
        {
            if (nameInput == null || renameButton == null) return;
            nameInput.DeactivateInputField();
            nameInput.text = string.Empty;
            bool enabled = archive?.Availability == ArchiveAvailability.Ready;
            nameInput.interactable = enabled;
            renameButton.interactable = enabled;
            ((Text)nameInput.placeholder).text = enabled ? ((BaseArchiveData)archive!.Data!).GetPlayerName() : T("rename.placeholder", "Name");
        }

        private void EnsureRenameInput()
        {
            if (renameRoot != null) return;
            Text style = Panel.btnConfirm.GetComponentInChildren<Text>(true);
            renameRoot = new GameObject("DTMAPI.MoreSaves.Rename", typeof(RectTransform), typeof(LayoutElement));
            RectTransform root = (RectTransform)renameRoot.transform;
            root.SetParent(Panel.btnDelete.transform.parent, false);
            root.SetSiblingIndex(Panel.btnDelete.transform.GetSiblingIndex());
            root.sizeDelta = new Vector2(300, 36);
            var layout = renameRoot.GetComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 300; layout.minHeight = layout.preferredHeight = 36;

            // Reserve a separate footer for the bottom-right pager after widening the native button row.
            // An owned layout child lets AutoSizeUIPanel restore its original size when we retire.
            renameFooterSpace = new GameObject("DTMAPI.MoreSaves.RenameFooterSpace", typeof(RectTransform));
            var footer = (RectTransform)renameFooterSpace.transform;
            footer.SetParent(Panel.transform, false);
            footer.SetSiblingIndex(Panel.btnDelete.transform.parent.GetSiblingIndex() + 1);
            footer.sizeDelta = new Vector2(0, 24);

            var entry = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            var rect = (RectTransform)entry.transform;
            rect.SetParent(root, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(210, 36);
            var background = entry.GetComponent<Image>(); background.color = new Color(0.1f, 0.13f, 0.16f, 0.9f);
            nameInput = entry.GetComponent<InputField>(); nameInput.targetGraphic = background;
            Text text = MakeText("Text", style, rect, new Vector2(8, 0), new Vector2(194, 36));
            text.alignment = TextAnchor.MiddleLeft; text.supportRichText = false; text.color = Color.white;
            Text placeholder = MakeText("Placeholder", style, rect, new Vector2(8, 0), new Vector2(194, 36));
            placeholder.alignment = TextAnchor.MiddleLeft; placeholder.supportRichText = false; placeholder.color = new Color(0.8f, 0.8f, 0.8f, 1);
            nameInput.textComponent = text; nameInput.placeholder = placeholder;
            nameInput.lineType = InputField.LineType.SingleLine; nameInput.characterLimit = NameLimit;
            nameInput.navigation = new Navigation { mode = Navigation.Mode.None };

            var action = new GameObject("Rename", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MoreSavesRenameButton));
            var actionRect = (RectTransform)action.transform;
            actionRect.SetParent(root, false); actionRect.anchorMin = actionRect.anchorMax = actionRect.pivot = Vector2.zero;
            actionRect.anchoredPosition = new Vector2(220, 0); actionRect.sizeDelta = new Vector2(80, 36);
            Image image = action.GetComponent<Image>();
            if (Panel.btnConfirm.targetGraphic is Image source) { image.sprite = source.sprite; image.type = source.type; image.color = source.color; }
            renameButton = (Button)action.GetComponent(typeof(MoreSavesRenameButton)); renameButton.targetGraphic = image; renameButton.colors = Panel.btnConfirm.colors;
            renameButton.navigation = new Navigation { mode = Navigation.Mode.None };
            renameButton.onClick.AddListener(() => { Session.EditName(nameInput.text); Session.Rename(); });
            MakeText("Label", style, actionRect, Vector2.zero, actionRect.sizeDelta).text = T("rename.button", "Rename");
        }
    }
}
