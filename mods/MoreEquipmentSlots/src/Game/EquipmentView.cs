using System;
using System.Collections.Generic;
using DolocTown.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class EquipmentView
    {
        private readonly EquipmentGame game;
        private readonly List<AccessorySlot> slots = new List<AccessorySlot>(3);
        private AccessoriesBar? bar;
        private GameObject? root;
        private Button? accessButton;
        private TextMeshProUGUI? accessLabel;

        internal EquipmentView(EquipmentGame game) => this.game = game;

        internal void Render(AccessoriesBar current, int nativeCount)
        {
            AccessorySlot last = nativeCount > 0 ? current.GetPassiveSlotByIndex(nativeCount - 1) :
                current.positiveItem;
            AccessorySlot previous = nativeCount > 1 ? current.GetPassiveSlotByIndex(nativeCount - 2) :
                nativeCount == 1 ? current.positiveItem : current.hatItem;
            if (bar != current || root == null)
            {
                Clear();
                bar = current;
                root = new GameObject("MoreEquipmentSlots", typeof(RectTransform), typeof(LayoutElement));
                root.layer = current.gameObject.layer;
                root.transform.SetParent(current.transform.parent, false);
                root.GetComponent<LayoutElement>().ignoreLayout = true;
                for (int i = 0; i < 3; i++)
                {
                    AccessorySlot clone = Object.Instantiate(last, root.transform, false);
                    clone.name = "ExtraEquipment" + (i + 1);
                    clone.gameObject.SetActive(true);
                    clone.Init();
                    var button = clone.button;
                    clone.ClearAllClickCallbacks();
                    button.onSelect.RemoveAllListeners();
                    button.onDeselect.RemoveAllListeners();
                    button.onPointerEnter.RemoveAllListeners();
                    button.onPointerExit.RemoveAllListeners();
                    button.onLeftContinuesClick = null;
                    button.onRightContinuesClick = null;
                    int index = i;
                    button.onLeftClick.AddListener(() => game.Click(index));
                    button.onSelect.AddListener(() => Hover(index));
                    button.onPointerEnter.AddListener(() => Hover(index));
                    button.onDeselect.AddListener(DolocAPI.HideHoverBox);
                    button.onPointerExit.AddListener(DolocAPI.HideHoverBox);
                    slots.Add(clone);
                }
                CreateAccessButton();
            }

            Canvas.ForceUpdateCanvases();
            RectTransform parent = (RectTransform)root.transform.parent;
            Vector3[] corners = new Vector3[4];
            ((RectTransform)last.transform).GetWorldCorners(corners);
            Vector3 min = parent.InverseTransformPoint(corners[0]);
            Vector3 max = parent.InverseTransformPoint(corners[2]);
            ((RectTransform)previous.transform).GetWorldCorners(corners);
            float gap = min.x - parent.InverseTransformPoint(corners[2]).x;
            float width = max.x - min.x;
            float height = max.y - min.y;
            var row = (RectTransform)root.transform;
            row.anchorMin = row.anchorMax = parent.pivot;
            row.pivot = Vector2.zero;
            row.sizeDelta = new Vector2(width * 3 + gap * 2, height);
            row.anchoredPosition = new Vector2(max.x + gap, min.y);
            for (int i = 0; i < slots.Count; i++)
            {
                var rect = (RectTransform)slots[i].transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2((width + gap) * i, 0);
            }
            root.SetActive(true);
            Refresh();
            EquipmentBarPanel panel = current.GetComponentInParent<EquipmentBarPanel>();
            if (panel != null) panel.RebuildNavigation();
        }

        internal void Refresh()
        {
            if (root == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                EquipmentItem? item = game.ItemAt(i);
                slots[i].Render(item == null ? null : DolocAPI.GetItemSprite(item.Item.name));
            }
            accessButton!.gameObject.SetActive(!game.CanEdit);
            accessLabel!.text = game.AccessPrompt;
        }

        private void Hover(int index) => game.HandleGameCall(() =>
        {
            string text = game.CanEdit ? "额外属性装备槽 " + (index + 1) : game.AccessPrompt;
            slots[index].ShowEquipmentItemViewer(game.ItemAt(index)?.Item, text);
        });

        internal Selectable[] Selectables(AccessoriesBar current, Selectable[] native)
        {
            if (bar != current || root == null || !root.activeSelf) return native;
            var result = new List<Selectable>(native);
            foreach (AccessorySlot slot in slots) result.Add(slot.button);
            if (accessButton!.gameObject.activeSelf) result.Add(accessButton);
            return result.ToArray();
        }

        internal void Hide(AccessoriesBar current)
        {
            if (bar != current || root == null) return;
            root.SetActive(false);
            DolocAPI.HideHoverBox();
        }

        internal void Clear()
        {
            if (root != null)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
            root = null;
            bar = null;
            accessButton = null;
            accessLabel = null;
            slots.Clear();
        }

        private void CreateAccessButton()
        {
            var buttonRoot = new GameObject("EquipmentMessage", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonRoot.layer = root!.layer;
            buttonRoot.transform.SetParent(root.transform, false);
            var rect = (RectTransform)buttonRoot.transform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(0, 36);
            rect.anchoredPosition = new Vector2(0, -8);
            Image image = buttonRoot.GetComponent<Image>();
            image.color = new Color(.22f, .30f, .33f, 1);
            accessButton = buttonRoot.GetComponent<Button>();
            accessButton.targetGraphic = image;
            accessButton.onClick.AddListener(game.RequestAccess);
            var labelRoot = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelRoot.layer = root.layer;
            labelRoot.transform.SetParent(buttonRoot.transform, false);
            var labelRect = (RectTransform)labelRoot.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            accessLabel = labelRoot.GetComponent<TextMeshProUGUI>();
            accessLabel.font = FindFont();
            accessLabel.fontSize = 20;
            accessLabel.alignment = TextAlignmentOptions.Center;
            accessLabel.richText = false;
            accessLabel.raycastTarget = false;
            accessLabel.color = Color.white;
        }

        private static TMP_FontAsset FindFont()
        {
            foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
                if (text.font != null && text.gameObject.activeInHierarchy && text.text != null)
                    foreach (char c in text.text)
                        if (c >= '\u4e00' && c <= '\u9fff') return text.font;
            return TMP_Settings.defaultFontAsset;
        }

    }
}
