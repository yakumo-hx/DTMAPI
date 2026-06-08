using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed class ReflectedTitleMenuSettingsUi
    {
        private readonly DtmApiRuntime runtime;
        private readonly IConfigMenuRuntime configMenu;
        private readonly DtmUiText text = new DtmUiText();
        private readonly string[] iconCandidates;
        private readonly List<object> renderedObjects = new List<object>();
        private readonly List<object> eventBinders = new List<object>();
        private readonly List<object> staticEventBinders = new List<object>();
        private readonly Dictionary<string, string> inputValues = new Dictionary<string, string>(StringComparer.Ordinal);

        private Type? gameObjectType;
        private Type? transformType;
        private Type? rectTransformType;
        private Type? objectType;
        private Type? canvasType;
        private Type? canvasScalerType;
        private Type? graphicRaycasterType;
        private Type? imageType;
        private Type? buttonType;
        private Type? textType;
        private Type? fontType;
        private Type? resourcesType;
        private Type? colorType;
        private Type? vector2Type;
        private Type? rectType;
        private Type? texture2DType;
        private Type? imageConversionType;
        private Type? spriteType;
        private Type? inputFieldType;
        private Type? eventSystemType;
        private Type? standaloneInputModuleType;
        private Type? inputSystemUiInputModuleType;
        private Type? unityActionType;
        private Type? unityActionStringType;

        private object? root;
        private object? eventSystemRoot;
        private object? titleButtonRoot;
        private object? panelRoot;
        private object? panelContentRoot;
        private object? iconSprite;
        private string? selectedConfigModId;
        private string? capturingKeybindItemId;
        private string statusMessage = string.Empty;
        private bool initialized;
        private bool dirty = true;
        private bool wasTitleVisible;
        private bool buttonVisibleLogged;
        private bool menuOpenLogged;
        private DtmOverlayPage renderedPage = (DtmOverlayPage)(-1);
        private string? renderedConfigUniqueId;
        private bool trackingRenderedObjects = true;
        private bool buildingStaticUi;

        public ReflectedTitleMenuSettingsUi(DtmApiRuntime runtime, IConfigMenuRuntime configMenu)
        {
            this.runtime = runtime;
            this.configMenu = configMenu;
            iconCandidates = new[]
            {
                Path.Combine(runtime.Paths.PluginPath, "DTMAPI", "assets", "branding", "dtmapi-icon.png"),
                Path.Combine(runtime.Paths.PluginPath, "assets", "branding", "dtmapi-icon.png"),
                Path.Combine(runtime.Paths.GamePath, "DTMAPI", "assets", "branding", "dtmapi-icon.png")
            };
        }

        public bool IsCapturingKey => capturingKeybindItemId != null;

        public bool ClickTitleButtonForSmoke()
        {
            if (!EnsureInitialized())
                return false;
            if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                return false;

            OpenFromTitleButton();
            return true;
        }

        public void Update()
        {
            if (!EnsureInitialized())
                return;
            if (!EnsureMountedUiObjects() && !EnsureInitialized())
                return;

            bool titleVisible = runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase);
            SetActive(root, titleVisible);
            if (titleVisible)
            {
                if (!wasTitleVisible)
                    dirty = true;
                if (!buttonVisibleLogged)
                {
                    buttonVisibleLogged = true;
                    runtime.RuntimeMonitor.Log("DTMAPI title settings button visible on HomePageUiState.");
                    runtime.SetHookStatus("UI.TitleSettingsEntry", "verified", "Unity UI Canvas", "DTMAPI Settings button is drawn only on the Doloc Town title homepage.");
                }
            }
            wasTitleVisible = titleVisible;

            if (!titleVisible)
            {
                if (runtime.UI.IsOpen)
                    runtime.UI.Close();
                SetActive(panelRoot, false);
                capturingKeybindItemId = null;
                buttonVisibleLogged = false;
                menuOpenLogged = false;
                return;
            }

            if (capturingKeybindItemId != null && ReflectedUnityInput.TryGetPressedKey(out string key))
            {
                TrySetCapturingKey(key);
                capturingKeybindItemId = null;
                dirty = true;
            }

            bool showPanel = runtime.UI.IsOpen;
            SetActive(panelRoot, showPanel);
            SetActive(titleButtonRoot, !showPanel);
            bool configRequestChanged = !string.Equals(renderedConfigUniqueId, runtime.UI.RequestedConfigUniqueId, StringComparison.OrdinalIgnoreCase);
            if (showPanel && (!menuOpenLogged || dirty || renderedPage != runtime.UI.CurrentPage || configRequestChanged))
            {
                if (!menuOpenLogged)
                {
                    menuOpenLogged = true;
                    runtime.RuntimeMonitor.Log("DTMAPI title settings menu opened.");
                    runtime.SetHookStatus("UI.TitleSettingsMenu", "verified", "Unity UI Canvas", "DTMAPI title settings menu opened from the homepage button.");
                }
                RenderPanel();
            }
        }

        private bool EnsureInitialized()
        {
            if (initialized)
                return true;
            if (!ResolveTypes())
                return false;

            root = CreateUiObject("DTMAPI.TitleSettings.Canvas", null);
            object canvas = AddComponent(root, canvasType!);
            SetEnumProperty(canvas, "renderMode", 0);
            SetProperty(canvas, "sortingOrder", 32000);
            AddComponent(root, canvasScalerType!);
            AddComponent(root, graphicRaycasterType!);

            object? scaler = GetComponent(root, canvasScalerType!);
            if (scaler != null)
            {
                SetEnumProperty(scaler, "uiScaleMode", 1);
                SetProperty(scaler, "referenceResolution", Vector2(1920, 1080));
                SetProperty(scaler, "matchWidthOrHeight", 0.5f);
            }

            DontDestroyOnLoad(root);
            EnsureEventSystem();
            iconSprite = LoadIconSprite();
            CreateTitleButton();
            CreatePanelRoot();
            SetActive(root, false);
            initialized = true;
            runtime.RuntimeMonitor.Log("DTMAPI reflected title settings UI initialized.");
            return true;
        }

        private bool EnsureMountedUiObjects()
        {
            if (!initialized)
                return false;
            if (root == null || titleButtonRoot == null || panelRoot == null || panelContentRoot == null)
            {
                ResetUiReferences("missing mounted Unity UI object");
                return false;
            }
            if (IsDestroyed(root) || IsDestroyed(titleButtonRoot) || IsDestroyed(panelRoot) || IsDestroyed(panelContentRoot))
            {
                ResetUiReferences("destroyed mounted Unity UI object");
                return false;
            }
            return true;
        }

        private bool ResolveTypes()
        {
            gameObjectType ??= Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.GameObject, UnityEngine");
            transformType ??= Type.GetType("UnityEngine.Transform, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Transform, UnityEngine");
            rectTransformType ??= Type.GetType("UnityEngine.RectTransform, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.RectTransform, UnityEngine");
            objectType ??= Type.GetType("UnityEngine.Object, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Object, UnityEngine");
            canvasType ??= Type.GetType("UnityEngine.Canvas, UnityEngine.UIModule") ?? Type.GetType("UnityEngine.Canvas, UnityEngine");
            canvasScalerType ??= Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            graphicRaycasterType ??= Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
            imageType ??= Type.GetType("UnityEngine.UI.Image, UnityEngine.UI");
            buttonType ??= Type.GetType("UnityEngine.UI.Button, UnityEngine.UI");
            textType ??= Type.GetType("UnityEngine.UI.Text, UnityEngine.UI");
            fontType ??= Type.GetType("UnityEngine.Font, UnityEngine.TextRenderingModule") ?? Type.GetType("UnityEngine.Font, UnityEngine");
            resourcesType ??= Type.GetType("UnityEngine.Resources, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Resources, UnityEngine");
            colorType ??= Type.GetType("UnityEngine.Color, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Color, UnityEngine");
            vector2Type ??= Type.GetType("UnityEngine.Vector2, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Vector2, UnityEngine");
            rectType ??= Type.GetType("UnityEngine.Rect, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Rect, UnityEngine");
            texture2DType ??= Type.GetType("UnityEngine.Texture2D, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Texture2D, UnityEngine");
            imageConversionType ??= Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule") ?? Type.GetType("UnityEngine.ImageConversion, UnityEngine");
            spriteType ??= Type.GetType("UnityEngine.Sprite, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Sprite, UnityEngine");
            inputFieldType ??= Type.GetType("UnityEngine.UI.InputField, UnityEngine.UI");
            eventSystemType ??= Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
            standaloneInputModuleType ??= Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");
            inputSystemUiInputModuleType ??= Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            unityActionType ??= Type.GetType("UnityEngine.Events.UnityAction, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Events.UnityAction, UnityEngine");
            Type? unityActionGeneric = Type.GetType("UnityEngine.Events.UnityAction`1, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Events.UnityAction`1, UnityEngine");
            unityActionStringType ??= unityActionGeneric?.MakeGenericType(typeof(string));

            bool ok = gameObjectType != null && rectTransformType != null && objectType != null && canvasType != null &&
                canvasScalerType != null && graphicRaycasterType != null && imageType != null && buttonType != null &&
                textType != null && fontType != null && resourcesType != null && colorType != null && vector2Type != null &&
                rectType != null && unityActionType != null;
            if (!ok)
                runtime.RuntimeMonitor.LogOnce("title-ui-types-missing", "DTMAPI title settings UI could not resolve Unity UI types.", LogLevel.Warn);
            return ok;
        }

        private void CreateTitleButton()
        {
            buildingStaticUi = true;
            trackingRenderedObjects = false;
            titleButtonRoot = CreateButton(root!, "DTMAPI.TitleSettings.Button", "DTMAPI", () =>
            {
                OpenFromTitleButton();
            }, Color(0.11f, 0.17f, 0.22f, 0.92f), Color(1f, 1f, 1f, 1f));
            SetRect(titleButtonRoot, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(48, -38), Vector2(172, 54));

            object icon = CreateImage(titleButtonRoot, "DTMAPI.TitleSettings.Button.Icon", iconSprite, Color(1f, 1f, 1f, 1f));
            SetRect(icon, Vector2(0, 0.5f), Vector2(0, 0.5f), Vector2(0, 0.5f), Vector2(28, 0), Vector2(34, 34));
            trackingRenderedObjects = true;
            buildingStaticUi = false;
        }

        private void CreatePanelRoot()
        {
            trackingRenderedObjects = false;
            panelRoot = CreateUiObject("DTMAPI.TitleSettings.Panel", root);
            CreateImage(panelRoot, "DTMAPI.TitleSettings.Panel.Background", null, Color(0.06f, 0.08f, 0.10f, 0.96f), stretch: true);
            SetRect(panelRoot, Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(1060, 720));
            panelContentRoot = CreateUiObject("DTMAPI.TitleSettings.Panel.Content", panelRoot);
            SetRect(panelContentRoot, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(0, 0));
            SetActive(panelRoot, false);
            trackingRenderedObjects = true;
        }

        private void EnsureEventSystem()
        {
            if (eventSystemType == null)
                return;
            if (FindObjectOfType(eventSystemType) != null)
                return;

            eventSystemRoot = CreatePlainObject("DTMAPI.TitleSettings.EventSystem");
            AddComponent(eventSystemRoot, eventSystemType);
            if (inputSystemUiInputModuleType != null)
                AddComponent(eventSystemRoot, inputSystemUiInputModuleType);
            else if (standaloneInputModuleType != null)
                AddComponent(eventSystemRoot, standaloneInputModuleType);
            DontDestroyOnLoad(eventSystemRoot);
            runtime.RuntimeMonitor.Log("DTMAPI title settings UI created an EventSystem for button input.");
        }

        private void RenderPanel()
        {
            if (panelContentRoot == null)
                return;

            ClearRenderedObjects();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            renderedPage = runtime.UI.CurrentPage;
            renderedConfigUniqueId = runtime.UI.RequestedConfigUniqueId;

            AddText(panelContentRoot, "DTMAPI.Title", T("ui.title", "DTMAPI Settings") + "  " + DtmApiRuntime.ApiVersion, 22, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 40, -36, 520, 32);
            CreateButton(panelContentRoot, "DTMAPI.Close", T("ui.close", "Close"), () =>
            {
                CloseMenu();
                dirty = true;
            }, Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), 930, -38, 86, 30);

            var tabs = new[]
            {
                Tuple.Create(T("tab.config", "Config"), DtmOverlayPage.Config),
                Tuple.Create(T("tab.mods", "Mods"), DtmOverlayPage.Mods),
                Tuple.Create(T("tab.status", "Status"), DtmOverlayPage.Status),
                Tuple.Create(T("tab.errors", "Errors"), DtmOverlayPage.Errors),
                Tuple.Create(T("tab.hooks", "Hooks"), DtmOverlayPage.Hooks),
                Tuple.Create(T("tab.logs", "Logs"), DtmOverlayPage.Logs)
            };
            for (int i = 0; i < tabs.Length; i++)
            {
                DtmOverlayPage page = tabs[i].Item2;
                bool active = page == runtime.UI.CurrentPage;
                CreateButton(panelContentRoot, "DTMAPI.Tab." + page, tabs[i].Item1, () =>
                {
                    runtime.UI.SetPage(page);
                    dirty = true;
                }, active ? Color(0.18f, 0.42f, 0.50f, 1f) : Color(0.12f, 0.15f, 0.18f, 1f), Color(1f, 1f, 1f, 1f), 40 + i * 118, -84, 108, 30);
            }

            switch (runtime.UI.CurrentPage)
            {
                case DtmOverlayPage.Config:
                    RenderConfig(snapshot);
                    break;
                case DtmOverlayPage.Mods:
                    RenderMods(snapshot);
                    break;
                case DtmOverlayPage.Status:
                    RenderStatus(snapshot);
                    break;
                case DtmOverlayPage.Errors:
                    RenderErrors(snapshot);
                    break;
                case DtmOverlayPage.Hooks:
                    RenderHooks(snapshot);
                    break;
                case DtmOverlayPage.Logs:
                    RenderLogs(snapshot);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
                AddText(panelContentRoot, "DTMAPI.StatusMessage", Truncate(statusMessage, 150), 15, Color(0.88f, 0.95f, 1f, 1f), TextAnchorMiddleLeft, 40, -676, 940, 24);

            dirty = false;
        }

        private void RenderConfig(RuntimeSnapshot snapshot)
        {
            IConfigMenuPage[] pages = snapshot.ConfigPages.OrderBy(p => p.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase).ToArray();
            if (pages.Length == 0)
            {
                AddText(panelContentRoot!, "DTMAPI.Config.Empty", T("config.empty", "No enabled DTMAPI mods have registered config pages."), 18, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 52, -146, 850, 28);
                return;
            }

            if (runtime.UI.RequestedConfigUniqueId != null && pages.Any(p => p.Manifest.UniqueID.Equals(runtime.UI.RequestedConfigUniqueId, StringComparison.OrdinalIgnoreCase)))
                selectedConfigModId = runtime.UI.RequestedConfigUniqueId;
            if (selectedConfigModId == null || !pages.Any(p => p.Manifest.UniqueID.Equals(selectedConfigModId, StringComparison.OrdinalIgnoreCase)))
                selectedConfigModId = pages[0].Manifest.UniqueID;

            AddText(panelContentRoot!, "DTMAPI.Config.ListTitle", T("config.listTitle", "Enabled DTMAPI mods"), 16, Color(0.78f, 0.88f, 0.92f, 1f), TextAnchorMiddleLeft, 44, -126, 260, 24);
            for (int i = 0; i < Math.Min(15, pages.Length); i++)
            {
                IConfigMenuPage page = pages[i];
                bool active = page.Manifest.UniqueID.Equals(selectedConfigModId, StringComparison.OrdinalIgnoreCase);
                string label = Truncate(page.DisplayName, 24) + (page.IsLocked ? " [" + T("config.lockedBadge", "locked") + "]" : string.Empty);
                CreateButton(panelContentRoot!, "DTMAPI.Config.Mod." + page.Manifest.UniqueID, label, () =>
                {
                    SelectConfigPage(page.Manifest.UniqueID);
                }, active ? Color(0.18f, 0.42f, 0.50f, 1f) : Color(0.10f, 0.13f, 0.15f, 1f), Color(1f, 1f, 1f, 1f), 44, -158 - i * 32, 250, 28);
            }

            IConfigMenuPage selectedPage = configMenu.GetPage(selectedConfigModId!) ?? pages[0];
            if (!selectedPage.IsEditing)
                configMenu.BeginEditing(selectedPage.Manifest.UniqueID);
            RenderConfigPage(selectedPage);
        }

        private void RenderConfigPage(IConfigMenuPage page)
        {
            IDisposable? preview = configMenu.PreviewPendingValues(page);
            try
            {
                float x = 326;
                float y = -126;
                AddText(panelContentRoot!, "DTMAPI.Config.PageTitle", page.DisplayName + " (" + page.Manifest.UniqueID + ")" + (page.HasPendingChanges ? " *" : string.Empty), 18, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, x, y, 600, 28);
                if (page.IsLocked)
                    AddText(panelContentRoot!, "DTMAPI.Config.Locked", T("config.lockedPrefix", "Locked: ") + text.TranslateEnablementReason(page.LockReason), 14, Color(1f, 0.70f, 0.42f, 1f), TextAnchorMiddleLeft, x, y - 26, 650, 24);

                CreateButton(panelContentRoot!, "DTMAPI.Config.Save", T("config.save", "Save"), () => TryPageAction(page, T("config.saved", "Saved"), () => configMenu.Save(page.Manifest.UniqueID)), page.IsLocked ? Color(0.20f, 0.20f, 0.20f, 1f) : Color(0.12f, 0.44f, 0.32f, 1f), Color(1f, 1f, 1f, 1f), x, -178, 82, 28);
                CreateButton(panelContentRoot!, "DTMAPI.Config.Reset", T("config.reset", "Reset"), () => TryPageAction(page, T("config.resetPending", "Reset pending"), () => configMenu.Reset(page.Manifest.UniqueID)), page.IsLocked ? Color(0.20f, 0.20f, 0.20f, 1f) : Color(0.42f, 0.28f, 0.13f, 1f), Color(1f, 1f, 1f, 1f), x + 92, -178, 82, 28);
                CreateButton(panelContentRoot!, "DTMAPI.Config.Cancel", T("config.cancel", "Cancel"), () => TryPageAction(page, T("config.canceled", "Canceled"), () => configMenu.Cancel(page.Manifest.UniqueID)), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), x + 184, -178, 82, 28);

                int conflictLine = 0;
                foreach (string conflict in configMenu.GetKeybindConflicts(page.Manifest.UniqueID).Take(2))
                {
                    AddText(panelContentRoot!, "DTMAPI.Config.Conflict." + conflictLine, conflict, 13, Color(1f, 0.66f, 0.62f, 1f), TextAnchorMiddleLeft, x + 286, -179 - conflictLine * 20, 420, 20);
                    conflictLine++;
                }

                float itemY = -226;
                foreach (IConfigMenuItem item in page.Items.Take(13))
                {
                    RenderConfigItem(page, item, x, itemY, 650);
                    itemY -= 34;
                }
            }
            finally
            {
                preview?.Dispose();
            }
        }

        private void RenderConfigItem(IConfigMenuPage page, IConfigMenuItem item, float x, float y, float width)
        {
            if (item.Kind == "Section")
            {
                AddText(panelContentRoot!, "DTMAPI.Item." + item.ItemId, item.Name, 16, Color(0.76f, 0.92f, 0.98f, 1f), TextAnchorMiddleLeft, x, y, width, 26);
                return;
            }
            if (item.Kind == "Paragraph")
            {
                AddText(panelContentRoot!, "DTMAPI.Item." + item.ItemId, Truncate(item.Name, 90), 13, Color(0.82f, 0.84f, 0.86f, 1f), TextAnchorMiddleLeft, x + 10, y, width - 10, 24);
                return;
            }

            AddText(panelContentRoot!, "DTMAPI.Item.Label." + item.ItemId, Truncate(item.Name, 36), 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, x, y, 230, 26);
            float controlX = x + 244;
            if (page.IsLocked)
            {
                AddText(panelContentRoot!, "DTMAPI.Item.Lock." + item.ItemId, T("item.locked", "Locked"), 14, Color(1f, 0.70f, 0.42f, 1f), TextAnchorMiddleLeft, controlX, y, 220, 26);
                return;
            }
            if (!item.CanEdit && !item.Kind.Equals("Button", StringComparison.OrdinalIgnoreCase))
            {
                AddText(panelContentRoot!, "DTMAPI.Item.ItemLock." + item.ItemId, FirstText(item.DisplayValue, T("item.locked", "Locked")), 14, Color(0.68f, 0.72f, 0.75f, 1f), TextAnchorMiddleLeft, controlX, y, 260, 26);
                return;
            }

            if (item.Kind == "Bool")
            {
                string next = item.PendingValue.Equals("true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
                CreateButton(panelContentRoot!, "DTMAPI.Item.Bool." + item.ItemId, item.PendingValue.Equals("true", StringComparison.OrdinalIgnoreCase) ? T("item.on", "On") : T("item.off", "Off"), () => TrySetPending(item, next), item.PendingValue.Equals("true", StringComparison.OrdinalIgnoreCase) ? Color(0.10f, 0.43f, 0.32f, 1f) : Color(0.28f, 0.31f, 0.34f, 1f), Color(1f, 1f, 1f, 1f), controlX, y, 88, 26);
            }
            else if (item.Kind == "InlineBoolNumber")
            {
                ParseInlineBoolNumber(item.PendingValue, out bool enabled, out double value);
                double step = item.Interval ?? 1;
                string next = enabled ? "false" : "true";
                CreateButton(panelContentRoot!, "DTMAPI.Item.InlineBool." + item.ItemId, enabled ? T("item.on", "On") : T("item.off", "Off"), () => TrySetPending(item, next + "|" + value.ToString(CultureInfo.InvariantCulture)), enabled ? Color(0.10f, 0.43f, 0.32f, 1f) : Color(0.28f, 0.31f, 0.34f, 1f), Color(1f, 1f, 1f, 1f), controlX, y, 70, 26);
                CreateButton(panelContentRoot!, "DTMAPI.Item.InlineMinus." + item.ItemId, "-", () => TrySetPending(item, enabled.ToString().ToLowerInvariant() + "|" + (value - step).ToString(CultureInfo.InvariantCulture)), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX + 80, y, 30, 26);
                CreateInput(panelContentRoot!, "DTMAPI.Item.InlineNumInput." + item.ItemId, value.ToString("0.###", CultureInfo.InvariantCulture), v => TrySetPending(item, enabled.ToString().ToLowerInvariant() + "|" + v), controlX + 116, y, 78, 26);
                CreateButton(panelContentRoot!, "DTMAPI.Item.InlinePlus." + item.ItemId, "+", () => TrySetPending(item, enabled.ToString().ToLowerInvariant() + "|" + (value + step).ToString(CultureInfo.InvariantCulture)), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX + 200, y, 30, 26);
                AddText(panelContentRoot!, "DTMAPI.Item.InlineRange." + item.ItemId, "[" + item.MinValue?.ToString("0.###", CultureInfo.InvariantCulture) + ".." + item.MaxValue?.ToString("0.###", CultureInfo.InvariantCulture) + "]", 12, Color(0.68f, 0.72f, 0.75f, 1f), TextAnchorMiddleLeft, controlX + 240, y, 110, 24);
            }
            else if (item.Kind == "InlineBoolBool")
            {
                ParseInlineBoolBool(item.PendingValue, out bool enabled, out bool secondary);
                string next = enabled ? "false" : "true";
                CreateButton(panelContentRoot!, "DTMAPI.Item.InlineBoolPrimary." + item.ItemId, enabled ? T("item.on", "On") : T("item.off", "Off"), () => TrySetPending(item, next + "|" + secondary.ToString().ToLowerInvariant()), enabled ? Color(0.10f, 0.43f, 0.32f, 1f) : Color(0.28f, 0.31f, 0.34f, 1f), Color(1f, 1f, 1f, 1f), controlX, y, 70, 26);
                bool showSecondary = enabled && (item.AllowedValues.Count < 3 || item.AllowedValues[2].Equals("true", StringComparison.OrdinalIgnoreCase));
                if (showSecondary)
                {
                    string secondaryName = item.AllowedValues.Count > 0 ? item.AllowedValues[0] : T("item.secondary", "Secondary");
                    string secondaryNext = secondary ? "false" : "true";
                    AddText(panelContentRoot!, "DTMAPI.Item.InlineBoolSecondaryLabel." + item.ItemId, Truncate(secondaryName, 16), 13, Color(0.82f, 0.86f, 0.88f, 1f), TextAnchorMiddleLeft, controlX + 88, y, 126, 24);
                    CreateButton(panelContentRoot!, "DTMAPI.Item.InlineBoolSecondary." + item.ItemId, secondary ? T("item.on", "On") : T("item.off", "Off"), () => TrySetPending(item, enabled.ToString().ToLowerInvariant() + "|" + secondaryNext), secondary ? Color(0.10f, 0.43f, 0.32f, 1f) : Color(0.28f, 0.31f, 0.34f, 1f), Color(1f, 1f, 1f, 1f), controlX + 222, y, 78, 26);
                }
            }
            else if (item.Kind == "Number")
            {
                double.TryParse(item.PendingValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
                double step = item.Interval ?? 1;
                CreateButton(panelContentRoot!, "DTMAPI.Item.NumMinus." + item.ItemId, "-", () => TrySetPending(item, (value - step).ToString(CultureInfo.InvariantCulture)), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX, y, 30, 26);
                CreateInput(panelContentRoot!, "DTMAPI.Item.NumInput." + item.ItemId, item.PendingValue, v => TrySetPending(item, v), controlX + 36, y, 94, 26);
                CreateButton(panelContentRoot!, "DTMAPI.Item.NumPlus." + item.ItemId, "+", () => TrySetPending(item, (value + step).ToString(CultureInfo.InvariantCulture)), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX + 136, y, 30, 26);
                AddText(panelContentRoot!, "DTMAPI.Item.NumRange." + item.ItemId, "[" + item.MinValue?.ToString("0.###", CultureInfo.InvariantCulture) + ".." + item.MaxValue?.ToString("0.###", CultureInfo.InvariantCulture) + "]", 12, Color(0.68f, 0.72f, 0.75f, 1f), TextAnchorMiddleLeft, controlX + 176, y, 150, 24);
            }
            else if (item.Kind == "Text")
            {
                CreateInput(panelContentRoot!, "DTMAPI.Item.TextInput." + item.ItemId, item.PendingValue, v => TrySetPending(item, v), controlX, y, 240, 26);
            }
            else if (item.Kind == "Choice")
            {
                if (item.AllowedValues.Count == 0)
                {
                    AddText(panelContentRoot!, "DTMAPI.Item.NoChoice." + item.ItemId, T("item.noChoices", "No choices"), 14, Color(0.85f, 0.85f, 0.85f, 1f), TextAnchorMiddleLeft, controlX, y, 180, 26);
                    return;
                }
                int current = Math.Max(0, item.AllowedValues.ToList().FindIndex(v => v.Equals(item.PendingValue, StringComparison.OrdinalIgnoreCase)));
                CreateButton(panelContentRoot!, "DTMAPI.Item.ChoicePrev." + item.ItemId, "<", () => TrySetPending(item, item.AllowedValues[(current - 1 + item.AllowedValues.Count) % item.AllowedValues.Count]), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX, y, 30, 26);
                AddText(panelContentRoot!, "DTMAPI.Item.ChoiceValue." + item.ItemId, FormatChoiceDisplayValue(item.PendingValue), 14, Color(1f, 1f, 1f, 1f), TextAnchorMiddleCenter, controlX + 36, y, 128, 26);
                CreateButton(panelContentRoot!, "DTMAPI.Item.ChoiceNext." + item.ItemId, ">", () => TrySetPending(item, item.AllowedValues[(current + 1) % item.AllowedValues.Count]), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX + 170, y, 30, 26);
            }
            else if (item.Kind == "ColorPreset")
            {
                int swatch = 0;
                foreach (string value in item.AllowedValues.Take(8))
                {
                    string[] parts = value.Split('|');
                    string id = parts.Length > 0 ? parts[0] : value;
                    string hex = parts.Length > 2 ? parts[2] : "FFFFFF";
                    bool selected = id.Equals(item.PendingValue, StringComparison.OrdinalIgnoreCase);
                    bool custom = id.Equals("Custom", StringComparison.OrdinalIgnoreCase);
                    string label = custom ? (selected ? "*" : "+") : (selected ? "*" : string.Empty);
                    object background = custom
                        ? Color(selected ? 1f : 0.88f, selected ? 1f : 0.92f, selected ? 1f : 0.96f, 0.94f)
                        : ColorFromHex(hex, selected ? 1f : 0.86f);
                    object textColor = selected || custom
                        ? Color(0f, 0f, 0f, 1f)
                        : Color(1f, 1f, 1f, 1f);
                    CreateButton(panelContentRoot!, "DTMAPI.Item.ColorPreset." + item.ItemId + "." + swatch, label, () => TrySetPending(item, id), background, textColor, controlX + swatch * 34, y, 26, 26);
                    swatch++;
                }
            }
            else if (item.Kind == "Keybind")
            {
                bool capturing = item.ItemId == capturingKeybindItemId;
                AddText(panelContentRoot!, "DTMAPI.Item.KeyValue." + item.ItemId, capturing ? T("item.pressKey", "Press key...") : item.PendingValue, 14, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, controlX, y, 100, 26);
                CreateButton(panelContentRoot!, "DTMAPI.Item.KeyCapture." + item.ItemId, capturing ? T("config.cancel", "Cancel") : T("item.capture", "Capture"), () =>
                {
                    capturingKeybindItemId = capturing ? null : item.ItemId;
                    dirty = true;
                }, capturing ? Color(0.43f, 0.24f, 0.18f, 1f) : Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), controlX + 106, y, 90, 26);
                CreateButton(panelContentRoot!, "DTMAPI.Item.KeyNone." + item.ItemId, T("item.none", "None"), () => TrySetPending(item, "None"), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX + 204, y, 66, 26);
            }
            else if (item.Kind == "Button")
            {
                CreateButton(panelContentRoot!, "DTMAPI.Item.Button." + item.ItemId, item.DisplayValue, () => TryPageAction(page, T("item.actionInvoked", "Action invoked"), item.Invoke), Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), controlX, y, 120, 26);
            }

            if (!string.IsNullOrWhiteSpace(item.ValidationError))
                AddText(panelContentRoot!, "DTMAPI.Item.Error." + item.ItemId, item.ValidationError, 12, Color(1f, 0.66f, 0.62f, 1f), TextAnchorMiddleLeft, controlX + 300, y, 230, 24);
        }

        private void RenderMods(RuntimeSnapshot snapshot)
        {
            AddText(panelContentRoot!, "DTMAPI.Mods.Title", T("mods.notice", "DTMAPI shows status only. Enable, disable, and ordering stay with Doloc Town or Steam Workshop."), 15, Color(0.78f, 0.88f, 0.92f, 1f), TextAnchorMiddleLeft, 46, -132, 900, 26);
            int i = 0;
            foreach (var mod in snapshot.DiscoveredMods.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase).Take(16))
            {
                bool loaded = snapshot.LoadedMods.Any(m => m.Manifest.UniqueID.Equals(mod.Manifest.UniqueID, StringComparison.OrdinalIgnoreCase));
                string state = loaded && !mod.OfficialEnabled
                    ? T("mods.state.loadedDisabled", "loaded; restart to stop")
                    : loaded ? T("mods.state.loaded", "loaded") : (mod.OfficialEnabled ? T("mods.state.restart", "restart/dependency required") : T("mods.state.locked", "locked by official path"));
                string manage = mod.CanDtmApiToggle ? T("mods.source.local", "local DTMAPI file") : T("mods.source.official", "official/Steam managed");
                string reason = loaded && !mod.OfficialEnabled
                    ? text.TranslateEnablementReason("此 Mod 已经加载；官方禁用会在重启游戏后完全停用。")
                    : loaded ? string.Empty : text.TranslateEnablementReason(mod.EnablementReason);
                string line = mod.Manifest.UniqueID + "  [" + state + "]  " + T("mods.sourceLabel", "source") + "=" + mod.Source + "  " + manage + (string.IsNullOrWhiteSpace(reason) ? string.Empty : "  " + reason);
                AddText(panelContentRoot!, "DTMAPI.Mods.Row." + i, Truncate(line, 130), 14, loaded ? Color(0.88f, 1f, 0.88f, 1f) : Color(1f, 0.82f, 0.58f, 1f), TextAnchorMiddleLeft, 52, -172 - i * 30, 920, 24);
                i++;
            }
        }

        private void RenderStatus(RuntimeSnapshot snapshot)
        {
            string[] lines =
            {
                T("status.runtimeActiveSince", "Runtime active since ") + snapshot.StartedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                T("status.gamePath", "Game path: ") + snapshot.Paths.GamePath,
                string.Format(CultureInfo.InvariantCulture, T("status.modsSummary", "Mods discovered: {0} | loaded: {1} | config pages: {2}"), snapshot.DiscoveredMods.Count, snapshot.LoadedMods.Count, snapshot.ConfigPages.Count),
                T("status.titleEntry", "Title settings entry: Unity UI Canvas, visible only on the unobstructed title homepage, anchored top-left."),
                T("status.officialEnablement", "Official enablement remains source-owned; DTMAPI config editing does not hot-unload DLLs or override official state."),
                T("status.diagnosticsHotkey", "F8 diagnostics overlay is disabled by default; use the title-page DTMAPI Settings entry.")
            };
            for (int i = 0; i < lines.Length; i++)
                AddText(panelContentRoot!, "DTMAPI.Status.Row." + i, lines[i], 15, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -140 - i * 32, 930, 26);
        }

        private void RenderErrors(RuntimeSnapshot snapshot)
        {
            if (snapshot.Errors.Count == 0)
            {
                AddText(panelContentRoot!, "DTMAPI.Errors.Empty", T("errors.empty", "No DTMAPI errors recorded."), 16, Color(0.88f, 1f, 0.88f, 1f), TextAnchorMiddleLeft, 52, -144, 600, 26);
                return;
            }
            for (int i = 0; i < Math.Min(16, snapshot.Errors.Count); i++)
            {
                IDtmErrorInfo error = snapshot.Errors[i];
                AddText(panelContentRoot!, "DTMAPI.Errors.Row." + i, Truncate(error.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " [" + error.Owner + "] " + error.Message, 120), 13, Color(1f, 0.76f, 0.70f, 1f), TextAnchorMiddleLeft, 52, -140 - i * 30, 930, 24);
            }
        }

        private void RenderHooks(RuntimeSnapshot snapshot)
        {
            if (snapshot.HookStatuses.Count == 0)
            {
                AddText(panelContentRoot!, "DTMAPI.Hooks.Empty", T("hooks.empty", "No hook statuses recorded."), 16, Color(0.88f, 1f, 0.88f, 1f), TextAnchorMiddleLeft, 52, -144, 600, 26);
                return;
            }
            for (int i = 0; i < Math.Min(17, snapshot.HookStatuses.Count); i++)
            {
                IHookStatusInfo hook = snapshot.HookStatuses[i];
                AddText(panelContentRoot!, "DTMAPI.Hooks.Row." + i, Truncate(hook.HookId + ": " + hook.Status + " | " + hook.Source, 120), 13, Color(0.86f, 0.92f, 1f, 1f), TextAnchorMiddleLeft, 52, -140 - i * 28, 930, 22);
            }
        }

        private void RenderLogs(RuntimeSnapshot snapshot)
        {
            CreateButton(panelContentRoot!, "DTMAPI.Logs.Export", T("logs.export", "Export logs"), () =>
            {
                string path = runtime.UI.ExportLogs();
                statusMessage = T("logs.exported", "Exported logs: ") + path;
                dirty = true;
            }, Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), 52, -144, 148, 30);
            AddText(panelContentRoot!, "DTMAPI.Logs.Latest", T("logs.latest", "Latest log: ") + runtime.Diagnostics.GetLatestLogPath(), 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -192, 930, 24);
            AddText(panelContentRoot!, "DTMAPI.Logs.ExportPath", T("logs.latestExport", "Latest export: ") + (string.IsNullOrWhiteSpace(snapshot.LastExportPath) ? T("common.none", "(none)") : snapshot.LastExportPath), 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -222, 930, 24);
        }

        private void SelectConfigPage(string uniqueId)
        {
            if (!string.Equals(selectedConfigModId, uniqueId, StringComparison.OrdinalIgnoreCase) && selectedConfigModId != null)
            {
                IConfigMenuPage? oldPage = configMenu.GetPage(selectedConfigModId);
                if (oldPage != null && oldPage.HasPendingChanges)
                    configMenu.Cancel(oldPage.Manifest.UniqueID);
            }
            selectedConfigModId = uniqueId;
            runtime.UI.OpenConfigPage(uniqueId);
            statusMessage = string.Empty;
            capturingKeybindItemId = null;
            inputValues.Clear();
            dirty = true;
        }

        private void OpenFromTitleButton()
        {
            runtime.UI.OpenConfigPage(selectedConfigModId);
            statusMessage = string.Empty;
            dirty = true;
            runtime.RuntimeMonitor.Log("DTMAPI title settings button clicked.");
        }

        private void CloseMenu()
        {
            if (selectedConfigModId != null)
            {
                IConfigMenuPage? page = configMenu.GetPage(selectedConfigModId);
                if (page != null && page.HasPendingChanges)
                    configMenu.Cancel(page.Manifest.UniqueID);
            }
            capturingKeybindItemId = null;
            inputValues.Clear();
            runtime.UI.Close();
        }

        private void TryPageAction(IConfigMenuPage page, string success, Action action)
        {
            try
            {
                action();
                inputValues.Clear();
                statusMessage = page.Manifest.UniqueID + ": " + success;
                capturingKeybindItemId = null;
            }
            catch (Exception ex)
            {
                statusMessage = page.Manifest.UniqueID + ": " + ex.Message;
            }
            dirty = true;
        }

        private void TrySetPending(IConfigMenuItem item, string value)
        {
            if (!item.TrySetPendingValue(value, out string error))
                statusMessage = item.Name + ": " + error;
            else
                statusMessage = string.Empty;
            inputValues[item.ItemId] = item.PendingValue;
            dirty = true;
        }

        private string FormatChoiceDisplayValue(string value)
        {
            return text.DisplayLanguageName(value);
        }

        private void TrySetCapturingKey(string key)
        {
            if (selectedConfigModId == null || capturingKeybindItemId == null)
                return;
            IConfigMenuItem? item = configMenu.GetPage(selectedConfigModId)?.Items.FirstOrDefault(i => i.ItemId == capturingKeybindItemId);
            if (item != null)
                TrySetPending(item, key);
        }

        private object CreateButton(object parent, string name, string label, Action onClick, object background, object textColor, float x = 0, float y = 0, float w = 100, float h = 28)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", background);
            object button = AddComponent(go, buttonType!);
            SetProperty(button, "targetGraphic", image);
            AddButtonListener(button, onClick);
            AddText(go, name + ".Text", label, 14, textColor, TextAnchorMiddleCenter, 0, 0, 0, 0, stretch: true);
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            if (trackingRenderedObjects)
                renderedObjects.Add(go);
            return go;
        }

        private object CreateInput(object parent, string name, string value, Action<string> onEdited, float x, float y, float w, float h)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", Color(0.04f, 0.05f, 0.06f, 1f));
            object text = AddText(go, name + ".Text", inputValues.TryGetValue(name, out string pending) ? pending : value, 14, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 8, 0, -16, 0, stretch: true);
            if (inputFieldType != null && unityActionStringType != null)
            {
                object input = AddComponent(go, inputFieldType);
                SetProperty(input, "targetGraphic", image);
                SetProperty(input, "textComponent", text);
                SetProperty(input, "text", value);
                AddStringListener(GetProperty(input, "onEndEdit"), v =>
                {
                    inputValues[name] = v;
                    onEdited(v);
                });
            }
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            if (trackingRenderedObjects)
                renderedObjects.Add(go);
            return go;
        }

        private object AddText(object parent, string name, string value, int fontSize, object color, int alignment, float x, float y, float w, float h, bool stretch = false)
        {
            object go = CreateUiObject(name, parent);
            object text = AddComponent(go, textType!);
            SetProperty(text, "text", value ?? string.Empty);
            SetProperty(text, "font", GetBuiltinFont());
            SetProperty(text, "fontSize", fontSize);
            SetProperty(text, "color", color);
            SetEnumProperty(text, "alignment", alignment);
            SetProperty(text, "raycastTarget", false);
            if (stretch)
                SetRect(go, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(x, y), Vector2(w, h));
            else
                SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            if (trackingRenderedObjects)
                renderedObjects.Add(go);
            return text;
        }

        private object CreateImage(object? parent, string name, object? sprite, object color, bool stretch = false)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            if (sprite != null)
                SetProperty(image, "sprite", sprite);
            SetProperty(image, "color", color);
            if (stretch)
                SetRect(go, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(0, 0));
            if (trackingRenderedObjects)
                renderedObjects.Add(go);
            return go;
        }

        private object CreateUiObject(string name, object? parent)
        {
            object go;
            ConstructorInfo? ctor = gameObjectType!.GetConstructor(new[] { typeof(string), typeof(Type[]) });
            if (ctor != null)
                go = ctor.Invoke(new object[] { name, new[] { rectTransformType! } });
            else
                go = Activator.CreateInstance(gameObjectType!, name);

            if (parent != null)
            {
                object? transform = GetProperty(go, "transform");
                object? parentTransform = GetProperty(parent, "transform");
                transform?.GetType().GetMethod("SetParent", new[] { transformType!, typeof(bool) })?.Invoke(transform, new[] { parentTransform, false });
            }
            return go;
        }

        private object CreatePlainObject(string name)
        {
            return Activator.CreateInstance(gameObjectType!, name);
        }

        private object AddComponent(object go, Type componentType)
        {
            return gameObjectType!.GetMethod("AddComponent", new[] { typeof(Type) })!.Invoke(go, new object[] { componentType });
        }

        private object? GetComponent(object go, Type componentType)
        {
            return gameObjectType!.GetMethod("GetComponent", new[] { typeof(Type) })?.Invoke(go, new object[] { componentType });
        }

        private object? FindObjectOfType(Type componentType)
        {
            try
            {
                object? found = objectType?.GetMethod("FindObjectOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null)?.Invoke(null, new object[] { componentType });
                return found != null && !IsDestroyed(found) ? found : null;
            }
            catch
            {
                return null;
            }
        }

        private void AddButtonListener(object button, Action action)
        {
            object? onClick = GetProperty(button, "onClick");
            if (onClick == null || unityActionType == null)
                return;
            var binder = new ActionBinder(action);
            if (buildingStaticUi)
                staticEventBinders.Add(binder);
            else
                eventBinders.Add(binder);
            Delegate del = Delegate.CreateDelegate(unityActionType, binder, nameof(ActionBinder.Invoke));
            onClick.GetType().GetMethod("AddListener", new[] { unityActionType })?.Invoke(onClick, new object[] { del });
        }

        private void AddStringListener(object? unityEvent, Action<string> action)
        {
            if (unityEvent == null || unityActionStringType == null)
                return;
            var binder = new StringActionBinder(action);
            if (buildingStaticUi)
                staticEventBinders.Add(binder);
            else
                eventBinders.Add(binder);
            Delegate del = Delegate.CreateDelegate(unityActionStringType, binder, nameof(StringActionBinder.Invoke));
            unityEvent.GetType().GetMethod("AddListener", new[] { unityActionStringType })?.Invoke(unityEvent, new object[] { del });
        }

        private void SetRect(object go, object anchorMin, object anchorMax, object pivot, object anchoredPosition, object sizeDelta)
        {
            object? rect = GetComponent(go, rectTransformType!);
            if (rect == null)
                return;
            SetProperty(rect, "anchorMin", anchorMin);
            SetProperty(rect, "anchorMax", anchorMax);
            SetProperty(rect, "pivot", pivot);
            SetProperty(rect, "anchoredPosition", anchoredPosition);
            SetProperty(rect, "sizeDelta", sizeDelta);
        }

        private object? LoadIconSprite()
        {
            if (texture2DType == null || imageConversionType == null || spriteType == null || rectType == null || vector2Type == null)
                return null;
            string? iconPath = iconCandidates.FirstOrDefault(File.Exists);
            if (iconPath == null)
            {
                runtime.RuntimeMonitor.Log("Startup segment IconLoad elapsedMs=0 result=missing; DTMAPI title settings icon was not found, using text-only button.", LogLevel.Warn);
                return null;
            }

            try
            {
                DateTimeOffset started = DateTimeOffset.Now;
                byte[] bytes = File.ReadAllBytes(iconPath);
                object texture = Activator.CreateInstance(texture2DType, 2, 2);
            MethodInfo? loadImage = imageConversionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "LoadImage")
                .OrderBy(m => m.GetParameters().Length)
                .FirstOrDefault(m => m.GetParameters().Length == 2 || m.GetParameters().Length == 3);
            if (loadImage == null)
                return null;
            object?[] args = loadImage.GetParameters().Length == 2
                ? new object?[] { texture, bytes }
                : new object?[] { texture, bytes, false };
            loadImage.Invoke(null, args);
                int width = (int)(texture2DType.GetProperty("width")?.GetValue(texture) ?? 32);
                int height = (int)(texture2DType.GetProperty("height")?.GetValue(texture) ?? 32);
                object rect = Activator.CreateInstance(rectType, 0f, 0f, (float)width, (float)height);
                object pivot = Vector2(0.5f, 0.5f);
                object? sprite = spriteType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "Create" && m.GetParameters().Length == 3)
                    .Invoke(null, new[] { texture, rect, pivot });
                runtime.RuntimeMonitor.Log("Startup segment IconLoad elapsedMs=" + (long)(DateTimeOffset.Now - started).TotalMilliseconds + " result=loaded path=" + iconPath + ".");
                return sprite;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.TitleSettings", "Failed to load DTMAPI title icon.", ex.ToString());
                return null;
            }
        }

        private void ClearRenderedObjects()
        {
            foreach (object go in renderedObjects.ToArray())
            {
                if (ReferenceEquals(go, titleButtonRoot) || ReferenceEquals(go, panelRoot))
                    continue;
                Destroy(go);
            }
            renderedObjects.Clear();
            eventBinders.Clear();
        }

        private void Destroy(object? go)
        {
            if (go == null || objectType == null)
                return;
            objectType.GetMethod("Destroy", new[] { objectType })?.Invoke(null, new[] { go });
        }

        private void DontDestroyOnLoad(object go)
        {
            objectType?.GetMethod("DontDestroyOnLoad", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType! }, null)?.Invoke(null, new[] { go });
        }

        private static object? GetProperty(object target, string name) => target.GetType().GetProperty(name)?.GetValue(target, null);

        private static void SetProperty(object target, string name, object? value)
        {
            PropertyInfo? property = target.GetType().GetProperty(name);
            property?.SetValue(target, value, null);
        }

        private static void SetEnumProperty(object target, string name, int value)
        {
            PropertyInfo? property = target.GetType().GetProperty(name);
            if (property == null)
                return;
            property.SetValue(target, Enum.ToObject(property.PropertyType, value), null);
        }

        private bool SetActive(object? go, bool active)
        {
            if (go == null || IsDestroyed(go))
            {
                InvalidateIfMountedObject(go);
                return false;
            }

            try
            {
                go.GetType().GetMethod("SetActive", new[] { typeof(bool) })?.Invoke(go, new object[] { active });
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is NullReferenceException)
            {
                InvalidateIfMountedObject(go);
                return false;
            }
        }

        private bool IsDestroyed(object go)
        {
            if (objectType == null || !objectType.IsAssignableFrom(go.GetType()))
                return false;

            MethodInfo? equality = objectType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType, objectType }, null);
            if (equality == null)
                return false;

            try
            {
                return equality.Invoke(null, new object?[] { go, null }) is bool destroyed && destroyed;
            }
            catch
            {
                return false;
            }
        }

        private void InvalidateIfMountedObject(object? go)
        {
            if (go != null &&
                !ReferenceEquals(go, root) &&
                !ReferenceEquals(go, titleButtonRoot) &&
                !ReferenceEquals(go, panelRoot) &&
                !ReferenceEquals(go, panelContentRoot))
                return;

            ResetUiReferences("SetActive saw destroyed mounted Unity UI object");
        }

        private void ResetUiReferences(string reason)
        {
            if (root != null && objectType != null && !IsDestroyed(root))
                Destroy(root);
            root = null;
            eventSystemRoot = null;
            titleButtonRoot = null;
            panelRoot = null;
            panelContentRoot = null;
            initialized = false;
            dirty = true;
            wasTitleVisible = false;
            renderedObjects.Clear();
            eventBinders.Clear();
            staticEventBinders.Clear();
            runtime.RuntimeMonitor.Log("DTMAPI title settings UI invalidated for rebuild: " + reason + ".");
        }

        private object GetBuiltinFont()
        {
            MethodInfo? getBuiltin = resourcesType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetBuiltinResource" && m.GetParameters().Length == 2);
            return getBuiltin?.Invoke(null, new object[] { fontType!, "Arial.ttf" }) ?? throw new InvalidOperationException("Unity built-in font Arial.ttf was not found.");
        }

        private object Color(float r, float g, float b, float a) => Activator.CreateInstance(colorType!, r, g, b, a);
        private object Vector2(float x, float y) => Activator.CreateInstance(vector2Type!, x, y);
        private string T(string key, string fallback) => text.Get(key, fallback);

        private object ColorFromHex(string hex, float alpha)
        {
            hex = (hex ?? string.Empty).Trim().TrimStart('#');
            if (hex.Length != 6 ||
                !int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) ||
                !int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) ||
                !int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
            {
                return Color(1f, 1f, 1f, alpha);
            }

            return Color(r / 255f, g / 255f, b / 255f, alpha);
        }

        private static void ParseInlineBoolNumber(string value, out bool enabled, out double number)
        {
            enabled = false;
            number = 1;
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

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private const int TextAnchorMiddleLeft = 3;
        private const int TextAnchorMiddleCenter = 4;

        private static string Truncate(string text, int max)
        {
            text ??= string.Empty;
            return text.Length <= max ? text : text.Substring(0, Math.Max(0, max - 3)) + "...";
        }

        private sealed class ActionBinder
        {
            private readonly Action action;
            public ActionBinder(Action action) => this.action = action;
            public void Invoke() => action();
        }

        private sealed class StringActionBinder
        {
            private readonly Action<string> action;
            public StringActionBinder(Action<string> action) => this.action = action;
            public void Invoke(string value) => action(value);
        }
    }
}
