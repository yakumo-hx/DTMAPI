using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleUi
    {
        private void Rebuild()
        {
            if (layout == null)
                return;
            ReleaseItemTooltip();
            Destroy(panelRoot);
            eventBinders.Clear();
            inputFields.Clear();
            catalogChromeBinders.Clear();
            catalogPoolBinders.Clear();
            worldBinders.Clear();
            advancedBinders.Clear();
            catalogInputFields.Clear();
            activeBinderSink = null;
            activeInputSink = null;
            catalogCells.Clear();
            headerRoot = null;
            catalogRoot = null;
            catalogRegionHost = null;
            catalogChromeRoot = null;
            catalogGridRoot = null;
            worldRoot = null;
            worldRegionHost = null;
            worldContentRoot = null;
            advancedRoot = null;
            advancedRegionHost = null;
            advancedContentRoot = null;
            catalogTabBackground = null;
            worldTabBackground = null;
            advancedTabBackground = null;
            panelRoot = CreateUiObject("DTMAPI.DebugConsole.Panel", root);
            object image = AddComponent(panelRoot, imageType!);
            SetProperty(image, "color", Color(0.035f, 0.04f, 0.046f, 0.96f));
            SetRect(
                panelRoot,
                Vector2(layout.AnchorMinX, layout.AnchorMinY),
                Vector2(layout.AnchorMaxX, layout.AnchorMaxY),
                Vector2(0.5f, 0.5f),
                Vector2(0, 0),
                Vector2(0, 0));

            float logicalWidth = (float)layout.LogicalWidth;
            float logicalHeight = (float)layout.LogicalHeight;
            headerRoot = CreateUiObject("DTMAPI.DebugConsole.Header", panelRoot);
            SetRect(headerRoot, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(0, 0));
            AddText(headerRoot, "DTMAPI.DebugConsole.Title", T("debug.title.y", "Y-Key Console") + "  " + runtime.ProductVersion, 29, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 28, -30, Math.Max(420, logicalWidth - 250), 42);
            AddText(headerRoot, "DTMAPI.DebugConsole.Subtitle", T("debug.subtitle", "Experimental in-save tools"), 16, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleLeft, 28, -64, Math.Max(420, logicalWidth - 250), 28);
            float closeSize = (float)Math.Max(64d, layout.MinimumClickSize);
            CreateButton(headerRoot, "DTMAPI.DebugConsole.Close", T("ui.close", "Close"), () => Close(ownerManifest!, "button"), Color(0.34f, 0.16f, 0.16f, 1f), Color(1f, 1f, 1f, 1f), logicalWidth - closeSize - 28, -34, closeSize, Math.Max(44, closeSize * 0.65f));

            if (layout.Breakpoint != DebugConsoleBreakpoint.Wide)
                BuildTabs(headerRoot, logicalWidth);

            if (layout.Breakpoint == DebugConsoleBreakpoint.Wide || activeTab == DebugConsoleTab.Catalog)
                CreateCatalogRegion();
            if (layout.Breakpoint == DebugConsoleBreakpoint.Wide)
                CreateWorldRegion(combined: true);
            else if (activeTab == DebugConsoleTab.World)
                CreateWorldRegion(combined: false);
            else if (activeTab == DebugConsoleTab.Advanced)
                CreateAdvancedRegion();

            statusTextObject = AddText(panelRoot, "DTMAPI.DebugConsole.Status", FirstText(statusMessage, T("debug.status.ready", "Ready")), 14, Color(0.78f, 0.86f, 0.86f, 1f), TextAnchorMiddleLeft, 28, -logicalHeight + 56, Math.Max(120f, logicalWidth - 56f), 48, stretch: false);
            renderedLanguage = text.Language;
            dirtyRegions = UiDirtyRegion.None;
        }

        private object CreateRegion(string name, float offsetY)
        {
            object region = CreateUiObject(name, panelRoot);
            SetRect(
                region,
                Vector2(0, 0),
                Vector2(1, 1),
                Vector2(0.5f, 0.5f),
                Vector2(0, offsetY),
                Vector2(0, 0));
            return region;
        }

        private void CreateCatalogRegion()
        {
            if (layout == null || catalogRoot != null)
                return;
            float catalogOffset = layout.Breakpoint ==
                    DebugConsoleBreakpoint.Compact
                ? -10f
                : 0f;
            catalogRoot = CreateResponsiveRegion(
                "DTMAPI.DebugConsole.Region.Catalog",
                catalogOffset,
                1100f,
                out catalogRegionHost);
            catalogGridRoot = CreateUiObject(
                "DTMAPI.DebugConsole.Region.Catalog.Grid",
                catalogRoot);
            SetRect(
                catalogGridRoot,
                Vector2(0, 0),
                Vector2(1, 1),
                Vector2(0.5f, 0.5f),
                Vector2(0, 0),
                Vector2(0, 0));
            RefreshCatalogRegion();
        }

        private void CreateWorldRegion(bool combined)
        {
            if (worldRoot != null)
                return;
            worldRoot = CreateResponsiveRegion(
                combined
                    ? "DTMAPI.DebugConsole.Region.WorldAdvanced"
                    : "DTMAPI.DebugConsole.Region.World",
                combined ? 0f : -52f,
                820f,
                out worldRegionHost,
                allowCompactScroll: false);
            RefreshWorldContent();
        }

        private void CreateAdvancedRegion()
        {
            if (layout == null || advancedRoot != null)
                return;
            float offsetY = layout.Breakpoint ==
                    DebugConsoleBreakpoint.Compact
                ? 598f
                : 604f;
            advancedRoot = CreateResponsiveRegion(
                "DTMAPI.DebugConsole.Region.Advanced",
                offsetY,
                500f,
                out advancedRegionHost,
                allowCompactScroll: false);
            RefreshAdvancedContent();
        }

        private object CreateResponsiveRegion(
            string name,
            float contentOffsetY,
            float contentHeight,
            out object host,
            bool allowCompactScroll = true)
        {
            DebugConsoleLayout? currentLayout = layout;
            if (currentLayout == null ||
                !allowCompactScroll ||
                currentLayout.Breakpoint != DebugConsoleBreakpoint.Compact ||
                scrollRectType == null ||
                rectMask2DType == null)
            {
                host = CreateRegion(name, contentOffsetY);
                return host;
            }

            const float topInset = 138f;
            const float bottomInset = 56f;
            host = CreateUiObject(name + ".Scroll", panelRoot);
            SetRect(
                host,
                Vector2(0, 0),
                Vector2(1, 1),
                Vector2(0.5f, 0.5f),
                Vector2(0, (bottomInset - topInset) * 0.5f),
                Vector2(0, -(topInset + bottomInset)));
            object hitTarget = AddComponent(host, imageType!);
            SetProperty(hitTarget, "color", Color(0f, 0f, 0f, 0.001f));
            SetProperty(hitTarget, "raycastTarget", true);
            AddComponent(host, rectMask2DType);

            object content = CreateUiObject(name + ".Content", host);
            float minimumContentHeight = Math.Max(
                1f,
                (float)(currentLayout.LogicalHeight - topInset - bottomInset));
            SetRect(
                content,
                Vector2(0, 1),
                Vector2(1, 1),
                Vector2(0.5f, 1),
                Vector2(0, 0),
                Vector2(0, Math.Max(contentHeight, minimumContentHeight)));

            object surface = CreateUiObject(name, content);
            SetRect(
                surface,
                Vector2(0, 0),
                Vector2(1, 1),
                Vector2(0.5f, 0.5f),
                Vector2(0, topInset + contentOffsetY),
                Vector2(0, 0));

            object scroll = AddComponent(host, scrollRectType);
            SetProperty(scroll, "content", GetComponent(content, rectTransformType!));
            SetProperty(scroll, "viewport", GetComponent(host, rectTransformType!));
            SetProperty(scroll, "horizontal", false);
            SetProperty(scroll, "vertical", true);
            SetProperty(scroll, "scrollSensitivity", 40f);
            return surface;
        }

        private void RefreshCatalogRegion()
        {
            if (catalogRoot == null || catalogGridRoot == null)
                return;
            ReleaseCatalogChrome();
            catalogChromeRoot = CreateUiObject(
                "DTMAPI.DebugConsole.Region.Catalog.Chrome",
                catalogRoot);
            SetRect(catalogChromeRoot, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(0, 0));
            BuildInRegion(
                catalogChromeRoot,
                catalogChromeBinders,
                catalogInputFields,
                BuildItemsTab);
        }

        private void RefreshWorldRegion()
        {
            if (layout == null || panelRoot == null)
                return;
            bool combined = layout.Breakpoint == DebugConsoleBreakpoint.Wide;
            if (worldRoot == null || IsDestroyed(worldRoot))
            {
                worldRoot = null;
                worldRegionHost = null;
                CreateWorldRegion(combined);
                return;
            }
            RefreshWorldContent();
        }

        private void RefreshWorldContent()
        {
            if (layout == null || worldRoot == null)
                return;
            ReleaseOwnedBinders(worldBinders);
            Destroy(worldContentRoot);
            worldContentRoot = CreateDynamicRegionContent(
                "DTMAPI.DebugConsole.Region.World.Dynamic",
                worldRoot);
            bool combined = layout.Breakpoint == DebugConsoleBreakpoint.Wide;
            float x = combined
                ? Math.Max(1000f, (float)layout.LogicalWidth - 520f)
                : 28f;
            BuildInRegion(
                worldContentRoot,
                worldBinders,
                null,
                () => BuildDebugSidePanel(x, true, combined));
        }

        private void RefreshAdvancedRegion()
        {
            if (layout == null || panelRoot == null)
                return;
            if (layout.Breakpoint == DebugConsoleBreakpoint.Wide)
            {
                RefreshWorldRegion();
                return;
            }
            if (advancedRoot == null || IsDestroyed(advancedRoot))
            {
                advancedRoot = null;
                advancedRegionHost = null;
                CreateAdvancedRegion();
                return;
            }
            RefreshAdvancedContent();
        }

        private void RefreshAdvancedContent()
        {
            if (advancedRoot == null)
                return;
            ReleaseOwnedBinders(advancedBinders);
            Destroy(advancedContentRoot);
            advancedContentRoot = CreateDynamicRegionContent(
                "DTMAPI.DebugConsole.Region.Advanced.Dynamic",
                advancedRoot);
            BuildInRegion(
                advancedContentRoot,
                advancedBinders,
                null,
                () => BuildDebugSidePanel(28f, false, true));
        }

        private object CreateDynamicRegionContent(string name, object parent)
        {
            object content = CreateUiObject(name, parent);
            SetRect(
                content,
                Vector2(0, 0),
                Vector2(1, 1),
                Vector2(0.5f, 0.5f),
                Vector2(0, 0),
                Vector2(0, 0));
            return content;
        }

        private void ReleaseCatalogChrome()
        {
            HideItemTooltip();
            foreach (object input in catalogInputFields)
                inputFields.Remove(input);
            catalogInputFields.Clear();
            ReleaseOwnedBinders(catalogChromeBinders);
            Destroy(catalogChromeRoot);
            catalogChromeRoot = null;
        }

        private void PrepareRetainedUiForClose()
        {
            ReleaseItemTooltip();
            ReleaseCatalogChrome();
            ReleaseOwnedBinders(worldBinders);
            Destroy(worldContentRoot);
            worldContentRoot = null;
            ReleaseOwnedBinders(advancedBinders);
            Destroy(advancedContentRoot);
            advancedContentRoot = null;
            ClearRetainedCatalogCells();
            itemSpriteCache.Clear();
            unifiedCatalogSources = null;
            statusMessage = string.Empty;
            if (statusTextObject != null && !IsDestroyed(statusTextObject))
                SetProperty(statusTextObject, "text", string.Empty);
            dirtyRegions = (dirtyRegions & UiDirtyRegion.Layout) |
                UiDirtyRegion.Dynamic;
        }

        private void BuildInRegion(
            object region,
            List<object>? binderSink,
            List<object>? inputSink,
            Action build)
        {
            object? shell = panelRoot;
            List<object>? previousBinderSink = activeBinderSink;
            List<object>? previousInputSink = activeInputSink;
            panelRoot = region;
            activeBinderSink = binderSink;
            activeInputSink = inputSink;
            try
            {
                build();
            }
            finally
            {
                panelRoot = shell;
                activeBinderSink = previousBinderSink;
                activeInputSink = previousInputSink;
            }
        }

        private void BuildTabs(object parent, float logicalWidth)
        {
            float width = Math.Max(132f, Math.Min(220f, (logicalWidth - 84f) / 3f));
            float x = 28f;
            CreateTabButton(parent, DebugConsoleTab.Catalog, T("debug.tab.catalog", "Catalog"), x, width);
            CreateTabButton(parent, DebugConsoleTab.World, T("debug.tab.world", "World"), x + width + 10f, width);
            CreateTabButton(parent, DebugConsoleTab.Advanced, T("debug.tab.advanced", "Advanced"), x + (width + 10f) * 2f, width);
        }

        private void CreateTabButton(
            object parent,
            DebugConsoleTab tab,
            string label,
            float x,
            float width)
        {
            bool selected = activeTab == tab;
            object buttonRoot = CreateButton(
                parent,
                "DTMAPI.DebugConsole.Tab." + tab,
                label,
                () =>
                {
                    ActivateTab(tab);
                },
                selected
                    ? Color(0.10f, 0.36f, 0.34f, 1f)
                    : Color(0.13f, 0.15f, 0.17f, 1f),
                Color(1f, 1f, 1f, 1f),
                x,
                -92,
                width,
                (float)Math.Max(44d, layout?.MinimumClickSize ?? 44d));
            object? background = imageType == null
                ? null
                : GetComponent(buttonRoot, imageType);
            SetTabBackground(tab, background);
        }

        private void ActivateTab(DebugConsoleTab tab)
        {
            if (layout == null ||
                layout.Breakpoint == DebugConsoleBreakpoint.Wide ||
                activeTab == tab)
            {
                return;
            }

            activeTab = tab;
            if (tab == DebugConsoleTab.Catalog)
            {
                if (catalogRoot == null)
                    CreateCatalogRegion();
                else
                    RefreshCatalogRegion();
                dirtyRegions &= ~UiDirtyRegion.Catalog;
            }
            else if (tab == DebugConsoleTab.World)
            {
                if (worldRoot == null)
                    CreateWorldRegion(combined: false);
                else
                    RefreshWorldRegion();
                dirtyRegions &= ~(UiDirtyRegion.World |
                    UiDirtyRegion.Weather |
                    UiDirtyRegion.Teleport);
            }
            else
            {
                if (advancedRoot == null)
                    CreateAdvancedRegion();
                else
                    RefreshAdvancedRegion();
                dirtyRegions &= ~UiDirtyRegion.Advanced;
            }

            SetActive(
                catalogRegionHost ?? catalogRoot,
                tab == DebugConsoleTab.Catalog);
            SetActive(
                worldRegionHost ?? worldRoot,
                tab == DebugConsoleTab.World);
            SetActive(
                advancedRegionHost ?? advancedRoot,
                tab == DebugConsoleTab.Advanced);
            RefreshTabSelection();
        }

        private void SetTabBackground(
            DebugConsoleTab tab,
            object? background)
        {
            if (tab == DebugConsoleTab.Catalog)
                catalogTabBackground = background;
            else if (tab == DebugConsoleTab.World)
                worldTabBackground = background;
            else
                advancedTabBackground = background;
        }

        private void RefreshTabSelection()
        {
            SetTabBackgroundColor(
                catalogTabBackground,
                activeTab == DebugConsoleTab.Catalog);
            SetTabBackgroundColor(
                worldTabBackground,
                activeTab == DebugConsoleTab.World);
            SetTabBackgroundColor(
                advancedTabBackground,
                activeTab == DebugConsoleTab.Advanced);
        }

        private void SetTabBackgroundColor(object? background, bool selected)
        {
            if (background == null || IsDestroyed(background))
                return;
            SetProperty(
                background,
                "color",
                selected
                    ? Color(0.10f, 0.36f, 0.34f, 1f)
                    : Color(0.13f, 0.15f, 0.17f, 1f));
        }

    }
}
