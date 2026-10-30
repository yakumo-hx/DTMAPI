using System;
using DolocTown.UI;
using global::DTMAPI.Abstractions;
using UnityEngine;
using UnityEngine.UI;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal sealed class AnimalHusbandryView : IDisposable
    {
        private readonly ProgressBar template;
        private readonly Transform parent;
        private readonly IReflectionHelper reflection;
        private readonly AnimalOverlaySessionState<Row> pool = new AnimalOverlaySessionState<Row>();
        private GameObject? root;
        private RectTransform content = null!;
        private ScrollRect scroll = null!;
        private LayoutElement layout = null!;
        private readonly float rowHeight;
        private readonly float spacing;
        internal AnimalHusbandryView(ProgressBar template, IReflectionHelper reflection)
        {
            this.template = template; this.parent = template.transform.parent; this.reflection = reflection;
            try
            {
                root = new GameObject("DTMAPI.AnimalProduceProgress", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(LayoutElement));
                root.SetActive(false);
                var rect = (RectTransform)root.transform;
                var original = (RectTransform)template.transform;
                rowHeight = Mathf.Max(24, original.rect.height);
                spacing = parent.GetComponent<VerticalLayoutGroup>()?.spacing ?? 24;
                rect.SetParent(parent, false);
                // The native spacer must stay after every information row.
                rect.SetSiblingIndex(original.GetSiblingIndex() + 1);
                rect.localPosition = original.localPosition + new Vector3(0, -rowHeight - spacing, 0);
                rect.localScale = original.localScale;
                rect.pivot = new Vector2(original.pivot.x, 1);
                rect.sizeDelta = new Vector2(Mathf.Max(200, original.rect.width), rowHeight);
                // Participate in the native vertical layout. ScrollRect alone
                // reports no preferred height, so its parent collapses it to zero.
                layout = root.GetComponent<LayoutElement>();
                layout.minHeight = rowHeight;
                layout.preferredHeight = rowHeight;
                layout.flexibleHeight = 0;
                var background = root.GetComponent<Image>();
                background.color = Color.clear;
                content = (RectTransform)new GameObject("Rows", typeof(RectTransform)).transform;
                content.SetParent(rect, false);
                content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
                scroll = root.GetComponent<ScrollRect>();
                scroll.viewport = rect; scroll.content = content;
                scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = rowHeight + spacing;
                scroll.movementType = ScrollRect.MovementType.Clamped;
            }
            catch { Dispose(); throw; }
        }
        internal bool IsAlive => root && parent && template;
        internal bool Matches(ProgressBar value) => IsAlive && ReferenceEquals(template, value) && ReferenceEquals(parent, value.transform.parent);
        internal void Bind(AnimalProgressRow[] rows, string colorHex, bool resetScroll)
        {
            if (!IsAlive) throw new InvalidOperationException("Animal viewer native parent is no longer available.");
            ColorUtility.TryParseHtmlString("#" + colorHex, out Color color);
            pool.EnsureCount(rows.Length, index => new Row(template, content, reflection, index));
            float height = 0, viewportHeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                Row row = pool.Tracked[i];
                if (i < rows.Length)
                {
                    if (i > 0) height += spacing;
                    height += row.Bind(rows[i], color, height, rowHeight);
                    if (i < 3) viewportHeight = height;
                }
                row.Object.SetActive(i < rows.Length);
            }
            content.sizeDelta = new Vector2(0, height);
            layout.minHeight = viewportHeight; layout.preferredHeight = viewportHeight;
            var viewport = (RectTransform)root!.transform;
            viewport.sizeDelta = new Vector2(viewport.sizeDelta.x, viewportHeight);
            if (resetScroll) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
            root!.SetActive(rows.Length > 0);
        }
        public void Dispose()
        {
            if (root) root!.SetActive(false);
            pool.Clear(row => row.Dispose());
            if (root) UnityEngine.Object.Destroy(root);
            root = null;
        }
        private sealed class Row : IDisposable
        {
            internal readonly GameObject Object;
            private readonly ProgressBar bar;
            private readonly Text title;
            private readonly Text progress;
            private readonly Image fill;
            private readonly RectTransform rect;
            internal Row(ProgressBar template, RectTransform parent, IReflectionHelper reflection, int index)
            {
                Object = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
                try
                {
                    Object.name = "DTMAPI.AnimalProduceProgress." + index;
                    Object.SetActive(false);
                    foreach (var localization in Object.GetComponentsInChildren<UILocalization>(true))
                    { localization.enabled = false; UnityEngine.Object.Destroy(localization); }
                    bar = Object.GetComponent<ProgressBar>();
                    using (var field = reflection.GetField<Text>(bar, "txtTitle")) title = field.GetValue();
                    using (var field = reflection.GetField<Text>(bar, "txtProgress")) progress = field.GetValue();
                    using (var field = reflection.GetField<Image>(bar, "progressMask")) fill = field.GetValue();
                    RectTransform background;
                    using (var field = reflection.GetField<RectTransform>(bar, "progressBackground")) background = field.GetValue();
                    rect = (RectTransform)Object.transform;
                    rect.anchorMin = new Vector2(0.5f, 1); rect.anchorMax = rect.anchorMin; rect.pivot = new Vector2(0.5f, 1);
                    // A stretched native row may have a negative sizeDelta.x.
                    // Once anchored to a fixed scroll row, use its actual width.
                    float width = Mathf.Max(200, ((RectTransform)template.transform).rect.width);
                    rect.sizeDelta = new Vector2(width, Mathf.Max(24, ((RectTransform)template.transform).rect.height));
                    // Keep the native bar, number, sprites and hierarchy intact.
                    // Bound only the label to the native space before the bar.
                    float labelWidth = Mathf.Max(60, RectTransformUtility.CalculateRelativeRectTransformBounds(rect, background).min.x - rect.rect.xMin - 8);
                    var labelRect = title.rectTransform;
                    labelRect.anchorMin = new Vector2(0, 0); labelRect.anchorMax = new Vector2(0, 1);
                    labelRect.pivot = new Vector2(0, 0.5f); labelRect.anchoredPosition = Vector2.zero;
                    labelRect.sizeDelta = new Vector2(labelWidth, 0);
                    title.alignment = TextAnchor.MiddleLeft;
                    title.horizontalOverflow = HorizontalWrapMode.Wrap;
                    title.verticalOverflow = VerticalWrapMode.Truncate;
                    title.resizeTextForBestFit = true;
                    title.resizeTextMinSize = Math.Min(12, title.fontSize);
                    title.resizeTextMaxSize = title.fontSize;
                    title.raycastTarget = false; progress.raycastTarget = false;
                }
                catch { UnityEngine.Object.Destroy(Object); throw; }
            }
            internal float Bind(AnimalProgressRow row, Color color, float offset, float nativeHeight)
            {
                if (!Object) throw new InvalidOperationException("Owned animal row was destroyed.");
                title.text = row.Title;
                var settings = title.GetGenerationSettings(new Vector2(title.rectTransform.rect.width, 0));
                settings.resizeTextForBestFit = false; settings.fontSize = title.resizeTextMinSize;
                float height = Mathf.Max(nativeHeight, Mathf.Ceil(title.cachedTextGeneratorForLayout.GetPreferredHeight(row.Title, settings) / title.pixelsPerUnit));
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
                rect.anchoredPosition = new Vector2(0, -offset);
                bar.SetTitle(row.Title); bar.SetProgress(row.Fill, row.Text);
                title.text = row.Title; progress.text = row.Text; fill.color = color;
                return height;
            }
            public void Dispose()
            {
                if (!Object) return;
                Object.SetActive(false);
                UnityEngine.Object.Destroy(Object);
            }
        }
    }
}
