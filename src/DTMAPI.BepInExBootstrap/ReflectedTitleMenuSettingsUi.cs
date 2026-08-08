using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed class ReflectedTitleMenuSettingsUi
    {
        private const int ConfigListPageSize = 14;
        private const int ConfigItemPageSize = 12;
        private const int ManagerModsPageSize = 10;
        private const int ManagerDiagnosticPageSize = 15;
        private const int ManagerHookPageSize = 17;
        private const int ManagerFeaturePageSize = 14;
        private const int ManagerAdvancedPageSize = 14;

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
        private object? eventSystemComponent;
        private object? titleButtonRoot;
        private object? panelRoot;
        private object? panelContentRoot;
        private object? iconSprite;
        private string? selectedConfigModId;
        private string? capturingKeybindItemId;
        private bool keyCaptureArming;
        private int keyCaptureStartedFrame = -1;
        private string statusMessage = string.Empty;
        private bool initialized;
        private bool dirty = true;
        private bool wasTitleVisible;
        private bool buttonVisibleLogged;
        private bool menuOpenLogged;
        private bool eventSystemPendingLogged;
        private int eventSystemFailureCount;
        private string eventSystemFailureFingerprint = string.Empty;
        private DateTimeOffset nextEventSystemRetryAtUtc = DateTimeOffset.MinValue;
        private DtmOverlayPage renderedPage = (DtmOverlayPage)(-1);
        private string? renderedConfigUniqueId;
        private bool trackingRenderedObjects = true;
        private long renderedOverlaySessionSequence = -1;
        private bool buildingStaticUi;
        private int configListPageIndex;
        private int configItemPageIndex;
        private int managerModsPageIndex;
        private int managerErrorsPageIndex;
        private int managerHooksPageIndex;
        private int managerFeaturesPageIndex;
        private int managerAdvancedPageIndex;
        private string? selectedManagerModId;
        private bool managerShowingWarnings;
        private bool managerAdvancedShowingFeatures;

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

        public string GetLifecycleSummary()
        {
            return "initialized=" + initialized.ToString(CultureInfo.InvariantCulture) +
                "; rootAlive=" + IsAlive(root).ToString(CultureInfo.InvariantCulture) +
                "; root=" + FormatUnityObjectState(root) +
                "; titleButton=" + FormatUnityObjectState(titleButtonRoot) +
                "; panelRoot=" + FormatUnityObjectState(panelRoot) +
                "; panelContent=" + FormatUnityObjectState(panelContentRoot) +
                "; renderedObjects=" + renderedObjects.Count.ToString(CultureInfo.InvariantCulture) +
                "; renderedAlive=" + CountAliveUnityObjects(renderedObjects).ToString(CultureInfo.InvariantCulture) +
                "; eventBinders=" + eventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; staticEventBinders=" + staticEventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; inputValues=" + inputValues.Count.ToString(CultureInfo.InvariantCulture) +
                "; capturingKey=" + (capturingKeybindItemId != null).ToString(CultureInfo.InvariantCulture) +
                "; eventSystemRoot=" + FormatUnityObjectState(eventSystemRoot) +
                "; eventSystemComponent=" + FormatUnityObjectState(eventSystemComponent) +
                "; iconSprite=" + FormatUnityObjectState(iconSprite) +
                "; wasTitleVisible=" + wasTitleVisible.ToString(CultureInfo.InvariantCulture) +
                "; menuOpen=" + runtime.UI.IsOpen.ToString(CultureInfo.InvariantCulture);
        }

        public string GetOwnerObjectGraphSummary()
        {
            int eventSystems = IsAlive(eventSystemRoot) > 0 || IsAlive(eventSystemComponent) > 0 ? 1 : 0;
            int buttonCount = IsAlive(titleButtonRoot);
            int unityEventListeners = eventBinders.Count + staticEventBinders.Count;
            return "DTMAPI.TitleSettings={Canvas=" + IsAlive(root).ToString(CultureInfo.InvariantCulture) +
                "; EventSystem=" + eventSystems.ToString(CultureInfo.InvariantCulture) +
                "; Button=" + buttonCount.ToString(CultureInfo.InvariantCulture) +
                "; InputField=" + inputValues.Count.ToString(CultureInfo.InvariantCulture) +
                "; ScrollRect=0" +
                "; UnityEventListeners=" + unityEventListeners.ToString(CultureInfo.InvariantCulture) +
                "; DynamicBinders=" + eventBinders.Count.ToString(CultureInfo.InvariantCulture) +
                "; rootAlive=" + IsAlive(root).ToString(CultureInfo.InvariantCulture) + "}";
        }

        public void ResetForSaveBoundary(int? saveSlot, bool isNewGame)
        {
            ResetBoundaryState("SaveLoaded slot=" + (saveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown") + " isNewGame=" + isNewGame.ToString(CultureInfo.InvariantCulture));
        }

        public void ResetForTitleBoundary()
        {
            ResetBoundaryState("ReturnedToTitle");
        }

        public void Shutdown(string reason)
        {
            ResetBoundaryState("Shutdown " + (reason ?? string.Empty));
            ResetUiReferences("Shutdown " + (reason ?? string.Empty));
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
                EnsureEventSystem();
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
                ResetBoundaryState("title hidden");
                return;
            }

            if (capturingKeybindItemId != null && keyCaptureArming)
            {
                int currentFrame = ReflectedUnityInput.GetUnityFrameCount();
                if (keyCaptureStartedFrame < 0 || currentFrame < 0)
                {
                    // Conservative fallback when Unity frameCount is unavailable.
                    keyCaptureArming = false;
                    return;
                }
                if (currentFrame == keyCaptureStartedFrame)
                    return;
                keyCaptureArming = false;
            }
            if (capturingKeybindItemId != null && !keyCaptureArming && ReflectedUnityInput.TryGetPressedKey(out string key))
            {
                TrySetCapturingKey(key);
                capturingKeybindItemId = null;
                keyCaptureArming = false;
                keyCaptureStartedFrame = -1;
                dirty = true;
            }

            bool showPanel = runtime.UI.IsOpen;
            SetActive(panelRoot, showPanel);
            SetActive(titleButtonRoot, !showPanel);
            bool configRequestChanged = !string.Equals(renderedConfigUniqueId, runtime.UI.RequestedConfigUniqueId, StringComparison.OrdinalIgnoreCase);
            if (showPanel && (!menuOpenLogged || dirty || renderedPage != runtime.UI.CurrentPage || configRequestChanged || renderedOverlaySessionSequence != runtime.UI.OverlaySessionSequence))
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
            bool hasIcon = iconSprite != null;
            titleButtonRoot = CreateButton(root!, "DTMAPI.TitleSettings.Button", hasIcon ? string.Empty : "DTMAPI", () =>
            {
                OpenFromTitleButton();
            }, Color(0.10f, 0.12f, 0.14f, 0.92f), Color(1f, 0.92f, 0.78f, 1f));
            SetRect(titleButtonRoot, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(48, -38), Vector2(hasIcon ? 54 : 84, 54));
            if (hasIcon)
            {
                object icon = CreateImage(titleButtonRoot, "DTMAPI.TitleSettings.Button.Icon", iconSprite, Color(1f, 1f, 1f, 1f));
                SetRect(icon, Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0.5f, 0.5f), Vector2(0, 0), Vector2(38, 38));
            }
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
                if (eventSystemRoot != null)
                {
                    if (IsDestroyed(eventSystemRoot))
                    {
                        eventSystemRoot = null;
                        eventSystemComponent = null;
                    }
                    else
                    {
                        if (FindExternalEventSystem() != null)
                            DestroyOwnedEventSystem("native EventSystem detected");
                    else
                        return;
                }
            }

            if (FindObjectOfType(eventSystemType) != null)
            {
                if (eventSystemPendingLogged)
                {
                    eventSystemPendingLogged = false;
                    runtime.SetHookStatus("UI.TitleSettingsEventSystem", "verified", "Native EventSystem", "Reused scene EventSystem for title settings button input.");
                }
                return;
            }
            if (DateTimeOffset.UtcNow < nextEventSystemRetryAtUtc)
                return;

            if (!TrySelectEventSystemInputModule(out Type? inputModuleType, out string inputModuleSource))
            {
                if (!eventSystemPendingLogged)
                {
                    eventSystemPendingLogged = true;
                    runtime.RuntimeMonitor.Log("DTMAPI title settings UI delayed EventSystem creation: " + inputModuleSource + ".", LogLevel.Warn);
                    runtime.SetHookStatus("UI.TitleSettingsEventSystem", "pending", "Unity UI Canvas", inputModuleSource);
                }
                nextEventSystemRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(2);
                return;
            }

            if (TryCreateFallbackEventSystem(inputModuleType, inputModuleSource, out Exception rootCause))
                return;

            string message = rootCause.GetType().Name + ": " + rootCause.Message;
            bool inputSystemDeferred = inputSystemUiInputModuleType != null && !IsUnityInputSystemReady();
            if ((inputModuleType == inputSystemUiInputModuleType && IsInputSystemNotInitializedException(rootCause)) ||
                inputSystemDeferred)
            {
                if (inputModuleType != standaloneInputModuleType &&
                    standaloneInputModuleType != null &&
                    TryCreateFallbackEventSystem(standaloneInputModuleType, "StandaloneInputModule; InputSystemUIInputModule failed during startup and is deferred.", out rootCause))
                {
                    runtime.RuntimeMonitor.LogOnce(
                        "title-ui-eventsystem-input-deferred-standalone",
                        "DTMAPI title settings UI deferred InputSystemUIInputModule after startup failure and used StandaloneInputModule.",
                        LogLevel.Warn);
                    return;
                }

                message = rootCause.GetType().Name + ": " + rootCause.Message;
                runtime.RuntimeMonitor.LogOnce(
                    "title-ui-eventsystem-input-pending",
                    "DTMAPI title settings UI delayed EventSystem creation because Unity Input System is not initialized yet.",
                    LogLevel.Warn);
                runtime.SetHookStatus("UI.TitleSettingsEventSystem", "pending", inputModuleSource, message);
                nextEventSystemRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(2);
                return;
            }

            RecordEventSystemFallbackFailure(inputModuleSource, rootCause);
        }

        private bool TryCreateFallbackEventSystem(Type? inputModuleType, string inputModuleSource, out Exception rootCause)
        {
            object? candidateRoot = null;
            try
            {
                if (eventSystemType == null)
                {
                    rootCause = new InvalidOperationException("Unity EventSystem type is unavailable.");
                    return false;
                }

                candidateRoot = CreatePlainObject("DTMAPI.TitleSettings.EventSystem");
                eventSystemComponent = AddComponent(candidateRoot, eventSystemType);
                if (inputModuleType != null)
                    AddComponent(candidateRoot, inputModuleType);
                eventSystemRoot = candidateRoot;
                eventSystemPendingLogged = false;
                eventSystemFailureCount = 0;
                eventSystemFailureFingerprint = string.Empty;
                runtime.RuntimeMonitor.Log("DTMAPI title settings UI created an EventSystem for button input using " + inputModuleSource + ".");
                runtime.SetHookStatus("UI.TitleSettingsEventSystem", "verified", inputModuleSource, "Owned fallback EventSystem created for title settings button input; it is destroyed when title UI is hidden or a native EventSystem appears.");
                rootCause = new InvalidOperationException("No error.");
                return true;
            }
            catch (Exception ex)
            {
                if (candidateRoot != null && objectType != null && !IsDestroyed(candidateRoot))
                    Destroy(candidateRoot);
                eventSystemRoot = null;
                eventSystemComponent = null;

                rootCause = UnwrapReflectionException(ex);
                return false;
            }
        }

        private void RecordEventSystemFallbackFailure(string inputModuleSource, Exception rootCause)
        {
            string fingerprint = rootCause.GetType().FullName + "|" + rootCause.Message;
            if (!string.Equals(eventSystemFailureFingerprint, fingerprint, StringComparison.Ordinal))
            {
                eventSystemFailureFingerprint = fingerprint;
                eventSystemFailureCount = 0;
            }

            eventSystemFailureCount++;
            string message = rootCause.GetType().Name + ": " + rootCause.Message;
            if (eventSystemFailureCount <= 3)
                runtime.Diagnostics.RecordError("DTMAPI.TitleSettingsUI", "Title settings EventSystem fallback creation failed; retrying.", rootCause.ToString());
            else
                runtime.RuntimeMonitor.LogOnce("title-ui-eventsystem-retry-suppressed-" + rootCause.GetType().Name, "DTMAPI title settings UI suppressed repeated EventSystem fallback failures for " + rootCause.GetType().Name + "; retry remains active.", LogLevel.Warn);

            nextEventSystemRetryAtUtc = DateTimeOffset.UtcNow.AddSeconds(eventSystemFailureCount <= 3 ? 2 : 10);
            runtime.SetHookStatus("UI.TitleSettingsEventSystem", eventSystemFailureCount <= 3 ? "retrying" : "degraded", inputModuleSource, message + "; retryCount=" + eventSystemFailureCount.ToString(CultureInfo.InvariantCulture));
        }

        private object? FindExternalEventSystem()
        {
            if (eventSystemType == null || objectType == null)
                return null;

            try
            {
                MethodInfo? findObjects = objectType.GetMethod("FindObjectsOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                if (findObjects == null)
                    return null;
                var found = findObjects.Invoke(null, new object[] { eventSystemType }) as Array;
                if (found == null)
                    return null;

                foreach (object? candidate in found)
                {
                    if (candidate == null || IsDestroyed(candidate))
                        continue;
                    if (IsOwnedEventSystem(candidate))
                        continue;
                    return candidate;
                }
            }
            catch
            {
            }

            return null;
        }

        private bool IsOwnedEventSystem(object candidate)
        {
            if (eventSystemComponent != null && IsSameUnityObject(candidate, eventSystemComponent))
                return true;
            if (eventSystemRoot != null && IsSameUnityObject(candidate, eventSystemRoot))
                return true;
            if (eventSystemRoot == null)
                return false;
            try
            {
                object? gameObject = GetProperty(candidate, "gameObject");
                return gameObject != null && IsSameUnityObject(gameObject, eventSystemRoot);
            }
            catch
            {
                return false;
            }
        }

        private bool IsSameUnityObject(object left, object right)
        {
            if (ReferenceEquals(left, right))
                return true;
            try
            {
                return left.Equals(right);
            }
            catch
            {
                return false;
            }
        }

        private void DestroyOwnedEventSystem(string reason)
        {
            if (eventSystemRoot == null)
                return;

            object? rootToDestroy = eventSystemRoot;
            eventSystemRoot = null;
            eventSystemComponent = null;
            if (rootToDestroy != null && !IsDestroyed(rootToDestroy))
                Destroy(rootToDestroy);
            runtime.SetHookStatus("UI.TitleSettingsEventSystem", "released", "DTMAPI-owned EventSystem", reason ?? string.Empty);
        }

        private bool TrySelectEventSystemInputModule(out Type? inputModuleType, out string source)
        {
            inputModuleType = null;
            source = "No EventSystem input module type is available.";

            if (inputSystemUiInputModuleType != null && IsUnityInputSystemReady())
            {
                inputModuleType = inputSystemUiInputModuleType;
                source = "InputSystemUIInputModule";
                return true;
            }

            if (standaloneInputModuleType != null)
            {
                inputModuleType = standaloneInputModuleType;
                source = inputSystemUiInputModuleType == null
                    ? "StandaloneInputModule"
                    : "StandaloneInputModule; InputSystemUIInputModule is deferred until Unity Input System is initialized.";
                return true;
            }

            if (inputSystemUiInputModuleType != null)
                source = "InputSystemUIInputModule is available but Unity Input System is not initialized yet.";

            return false;
        }

        private static bool IsUnityInputSystemReady()
        {
            Type? inputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystemType == null)
                return false;

            try
            {
                PropertyInfo? settings = inputSystemType.GetProperty("settings", BindingFlags.Public | BindingFlags.Static);
                if (settings != null)
                    return settings.GetValue(null, null) != null;

                PropertyInfo? devices = inputSystemType.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
                if (devices != null)
                {
                    _ = devices.GetValue(null, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (IsInputSystemNotInitializedException(UnwrapReflectionException(ex)))
                    return false;
                return false;
            }

            return true;
        }

        private static bool IsInputSystemNotInitializedException(Exception ex)
        {
            string text = (ex.Message ?? string.Empty) + " " + ex.ToString();
            return text.IndexOf("Input System not yet initialized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("InputSystem not yet initialized", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Exception UnwrapReflectionException(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        private void RenderPanel()
        {
            if (panelContentRoot == null)
                return;

            ClearRenderedObjects();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            renderedPage = runtime.UI.CurrentPage;
            renderedOverlaySessionSequence = runtime.UI.OverlaySessionSequence;
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
                Tuple.Create(T("tab.advanced", "Advanced"), DtmOverlayPage.Features),
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
                case DtmOverlayPage.Features:
                    RenderFeatures(snapshot);
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
            int selectedIndex = Array.FindIndex(pages, p => p.Manifest.UniqueID.Equals(selectedConfigModId, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex >= 0 && !IsIndexOnPage(selectedIndex, configListPageIndex, ConfigListPageSize))
                configListPageIndex = selectedIndex / ConfigListPageSize;

            ClampPage(ref configListPageIndex, pages.Length, ConfigListPageSize);
            GetPageBounds(pages.Length, configListPageIndex, ConfigListPageSize, out int pageStart, out int pageEnd, out int pageCount);

            AddText(panelContentRoot!, "DTMAPI.Config.ListTitle", T("config.listTitle", "Enabled DTMAPI mods"), 16, Color(0.78f, 0.88f, 0.92f, 1f), TextAnchorMiddleLeft, 44, -126, 260, 24);
            if (pageCount > 1)
                RenderPager("DTMAPI.Config.ListPager", configListPageIndex, pageCount, pages.Length, pageStart, pageEnd, 172, -126, page =>
                {
                    configListPageIndex = page;
                    dirty = true;
                });

            for (int i = pageStart; i < pageEnd; i++)
            {
                IConfigMenuPage page = pages[i];
                bool active = page.Manifest.UniqueID.Equals(selectedConfigModId, StringComparison.OrdinalIgnoreCase);
                string label = Truncate(page.DisplayName, 24) + (page.IsLocked ? " [" + T("config.lockedBadge", "locked") + "]" : string.Empty);
                int row = i - pageStart;
                CreateButton(panelContentRoot!, "DTMAPI.Config.Mod." + page.Manifest.UniqueID, label, () =>
                {
                    SelectConfigPage(page.Manifest.UniqueID);
                }, active ? Color(0.18f, 0.42f, 0.50f, 1f) : Color(0.10f, 0.13f, 0.15f, 1f), Color(1f, 1f, 1f, 1f), 44, -158 - row * 32, 250, 28);
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

                IConfigMenuItem[] items = page.Items.ToArray();
                ClampPage(ref configItemPageIndex, items.Length, ConfigItemPageSize);
                GetPageBounds(items.Length, configItemPageIndex, ConfigItemPageSize, out int itemStart, out int itemEnd, out int itemPageCount);
                if (itemPageCount > 1)
                    RenderPager("DTMAPI.Config.ItemPager", configItemPageIndex, itemPageCount, items.Length, itemStart, itemEnd, x + 482, -178, pageIndex =>
                    {
                        configItemPageIndex = pageIndex;
                        capturingKeybindItemId = null;
                        dirty = true;
                    });

                float itemY = -226;
                for (int i = itemStart; i < itemEnd; i++)
                {
                    RenderConfigItem(page, items[i], x, itemY, 650);
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
                    if (capturing)
                    {
                        capturingKeybindItemId = null;
                        keyCaptureArming = false;
                        keyCaptureStartedFrame = -1;
                    }
                    else
                    {
                        keyCaptureStartedFrame = ReflectedUnityInput.BeginKeyCapture();
                        capturingKeybindItemId = item.ItemId;
                        keyCaptureArming = true;
                    }
                    dirty = true;
                }, capturing ? Color(0.43f, 0.24f, 0.18f, 1f) : Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), controlX + 106, y, 90, 26);
                if (item is IResettableKeybindConfigMenuItem resettable && resettable.HasDefaultValue)
                    CreateButton(panelContentRoot!, "DTMAPI.Item.KeyReset." + item.ItemId, T("config.reset", "Reset"), () => TryResetKeybind(item, resettable), Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), controlX + 204, y, 66, 26);
                else
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
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null)
            {
                AddText(panelContentRoot!, "DTMAPI.Mods.Unavailable", FormatManagerUnavailable(), 16, Color(1f, 0.82f, 0.58f, 1f), TextAnchorMiddleLeft, 52, -172, 900, 26);
                return;
            }

            ClampPage(ref managerModsPageIndex, manager.Mods.Count, ManagerModsPageSize);
            GetPageBounds(manager.Mods.Count, managerModsPageIndex, ManagerModsPageSize, out int modStart, out int modEnd, out int modPageCount);
            AddText(panelContentRoot!, "DTMAPI.Mods.Count", FormatShowingPage(T("mods.count", "Mods"), modStart, modEnd, manager.Mods.Count), 13, Color(0.72f, 0.82f, 0.86f, 1f), TextAnchorMiddleLeft, 52, -160, 620, 22);
            if (modPageCount > 1)
                RenderPager("DTMAPI.Mods.Pager", managerModsPageIndex, modPageCount, manager.Mods.Count, modStart, modEnd, 732, -160, page =>
                {
                    managerModsPageIndex = page;
                    dirty = true;
                });

            if (manager.Mods.Count == 0)
            {
                AddText(panelContentRoot!, "DTMAPI.Mods.Empty", T("mods.empty", "No discovered DTMAPI mods recorded."), 16, Color(0.88f, 1f, 0.88f, 1f), TextAnchorMiddleLeft, 52, -190, 600, 26);
                return;
            }

            if (selectedManagerModId == null ||
                !manager.Mods.Any(mod => mod.UniqueID.Equals(selectedManagerModId, StringComparison.OrdinalIgnoreCase)) ||
                !manager.Mods.Skip(modStart).Take(modEnd - modStart).Any(mod => mod.UniqueID.Equals(selectedManagerModId, StringComparison.OrdinalIgnoreCase)))
                selectedManagerModId = manager.Mods[modStart].UniqueID;

            int i = 0;
            for (int row = modStart; row < modEnd; row++)
            {
                ManagerModRow mod = manager.Mods[row];
                bool selected = mod.UniqueID.Equals(selectedManagerModId, StringComparison.OrdinalIgnoreCase);
                CreateButton(
                    panelContentRoot!,
                    "DTMAPI.Mods.Row." + i,
                    Truncate(FormatManagerModListRow(mod), 124),
                    () =>
                    {
                        selectedManagerModId = mod.UniqueID;
                        dirty = true;
                    },
                    selected ? Color(0.18f, 0.42f, 0.50f, 1f) : Color(0.12f, 0.15f, 0.18f, 1f),
                    GetModRowColor(mod),
                    52,
                    -190 - i * 30,
                    930,
                    24);
                i++;
            }

            ManagerModRow selectedMod = manager.Mods.First(mod => mod.UniqueID.Equals(selectedManagerModId, StringComparison.OrdinalIgnoreCase));
            string detailTitle = string.Format(
                CultureInfo.InvariantCulture,
                T("mods.detailsTitleFormat", "Selected Mod: {0} ({1})"),
                FirstManagerText(selectedMod.Name, T("mods.unknownName", "unnamed")),
                FirstManagerText(selectedMod.UniqueID, T("mods.unknownId", "no id")));
            AddText(panelContentRoot!, "DTMAPI.Mods.DetailTitle", detailTitle, 15, Color(0.78f, 0.88f, 0.92f, 1f), TextAnchorMiddleLeft, 52, -500, 900, 24);
            IReadOnlyList<string> detailLines = FormatManagerModDetailLines(selectedMod);
            for (int detailIndex = 0; detailIndex < detailLines.Count; detailIndex++)
                AddText(panelContentRoot!, "DTMAPI.Mods.Detail." + detailIndex, Truncate(detailLines[detailIndex], 136), 13, GetModRowColor(selectedMod), TextAnchorMiddleLeft, 52, -526 - detailIndex * 28, 930, 22);
        }

        private void RenderStatus(RuntimeSnapshot snapshot)
        {
            CreateButton(panelContentRoot!, "DTMAPI.Status.Refresh", T("status.refresh", "Refresh"), () =>
            {
                runtime.UI.RefreshDtmManagerModel();
                statusMessage = string.IsNullOrWhiteSpace(runtime.UI.LastManagerRefreshError)
                    ? T("status.refreshed", "Manager status refreshed.")
                    : T("status.refreshFailed", "Manager refresh failed: ") + runtime.UI.LastManagerRefreshError;
                dirty = true;
            }, Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), 52, -132, 120, 30);
            CreateButton(panelContentRoot!, "DTMAPI.Status.CopySummary", T("status.copySummary", "Copy Summary"), () =>
            {
                DtmManagerCopySummaryResult result = runtime.UI.CopyManagerSummary(TrySetClipboardText);
                statusMessage = result.Copied
                    ? T("status.summaryCopied", "Manager summary copied.")
                    : T("status.summaryCopyUnavailable", "Copy unavailable; summary written to runtime log.");
                dirty = true;
            }, Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), 184, -132, 148, 30);

            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            string[] lines = manager == null
                ? new[]
                {
                    FormatManagerUnavailable(),
                    T("status.officialEnablement", "Official enablement remains source-owned; DTMAPI config editing does not hot-unload DLLs or override official state."),
                    T("status.advancedHint", "Use Advanced for registry, compatibility, Hook, and feature evidence.")
                }
                : new[]
            {
                T("status.managerOverall", "Overall status: ") + TranslateManagerStatus(manager.Summary.OverallStatus),
                string.Format(CultureInfo.InvariantCulture, T("status.managerMods", "Mods loaded: {0} | blocked: {1} | disabled: {2}"), manager.Summary.LoadedModCount, manager.Summary.BlockedModCount, manager.Summary.DisabledModCount),
                string.Format(CultureInfo.InvariantCulture, T("status.managerDependencies", "Dependency issues: {0} | restart required now: {1}"), manager.Summary.DependencyIssueModCount, manager.Summary.RestartRequiredModCount),
                string.Format(CultureInfo.InvariantCulture, T("status.managerDiagnostics", "Diagnostics errors: {0} | warnings: {1}"), manager.Summary.ErrorCount, manager.Summary.WarningCount),
                string.IsNullOrWhiteSpace(runtime.UI.LastManagerRefreshError)
                    ? T("status.managerRefreshOk", "Manager refresh: refreshed")
                    : T("status.managerRefreshFailed", "Manager model refresh failed: ") + runtime.UI.LastManagerRefreshError,
                T("status.officialEnablement", "Official enablement remains source-owned; DTMAPI config editing does not hot-unload DLLs or override official state."),
                T("status.advancedHint", "Use Advanced for registry, compatibility, Hook, and feature evidence.")
            };
            for (int i = 0; i < lines.Length; i++)
                AddText(panelContentRoot!, "DTMAPI.Status.Row." + i, Truncate(lines[i], 140), 15, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -176 - i * 32, 930, 26);
        }

        private string FormatManagerPathStatus(string label, bool hasPath, bool exists, string path)
        {
            string status = !hasPath
                ? T("status.pathUnavailable", "unavailable")
                : exists ? T("status.pathPresent", "present") : T("status.pathMissing", "missing");
            return label + ": " + status + (hasPath ? " | " + path : string.Empty);
        }

        private void RenderPager(string id, int pageIndex, int pageCount, int total, int start, int end, float x, float y, Action<int> setPage)
        {
            pageCount = Math.Max(1, pageCount);
            string label = total <= 0
                ? "0/0"
                : string.Format(CultureInfo.InvariantCulture, T("pager.page", "{0}-{1}/{2}  Page {3}/{4}"), start + 1, end, total, pageIndex + 1, pageCount);
            AddText(panelContentRoot!, id + ".Label", label, 12, Color(0.72f, 0.82f, 0.86f, 1f), TextAnchorMiddleLeft, x, y, 160, 22);
            CreateButton(panelContentRoot!, id + ".Prev", "<", () =>
            {
                setPage(Math.Max(0, pageIndex - 1));
            }, pageIndex <= 0 ? Color(0.12f, 0.13f, 0.14f, 1f) : Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), x + 166, y, 30, 22);
            CreateButton(panelContentRoot!, id + ".Next", ">", () =>
            {
                setPage(Math.Min(pageCount - 1, pageIndex + 1));
            }, pageIndex >= pageCount - 1 ? Color(0.12f, 0.13f, 0.14f, 1f) : Color(0.22f, 0.25f, 0.29f, 1f), Color(1f, 1f, 1f, 1f), x + 202, y, 30, 22);
        }

        private static bool IsIndexOnPage(int index, int pageIndex, int pageSize)
        {
            int start = Math.Max(0, pageIndex) * Math.Max(1, pageSize);
            return index >= start && index < start + pageSize;
        }

        private static void ClampPage(ref int pageIndex, int total, int pageSize)
        {
            pageIndex = ManagerPagination.Create(total, pageIndex, pageSize).PageIndex;
        }

        private static void GetPageBounds(int total, int pageIndex, int pageSize, out int start, out int end, out int pageCount)
        {
            ManagerPageWindow page = ManagerPagination.Create(total, pageIndex, pageSize);
            start = page.Start;
            end = page.End;
            pageCount = page.PageCount;
        }

        private string FormatShowingPage(string label, int start, int end, int total)
        {
            if (total <= 0)
                return label + ": 0/0";
            return string.Format(CultureInfo.InvariantCulture, T("pager.showing", "{0}: showing {1}-{2} of {3}"), label, start + 1, end, total);
        }

        private void RenderErrors(RuntimeSnapshot snapshot)
        {
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null)
            {
                AddText(panelContentRoot!, "DTMAPI.Errors.Unavailable", FormatManagerUnavailable(), 16, Color(1f, 0.82f, 0.58f, 1f), TextAnchorMiddleLeft, 52, -144, 900, 26);
                return;
            }

            CreateButton(panelContentRoot!, "DTMAPI.Errors.SelectErrors", T("errors.label", "Errors") + " (" + manager.Errors.Count.ToString(CultureInfo.InvariantCulture) + ")", () =>
            {
                managerShowingWarnings = false;
                managerErrorsPageIndex = 0;
                dirty = true;
            }, !managerShowingWarnings ? Color(0.48f, 0.22f, 0.20f, 1f) : Color(0.12f, 0.15f, 0.18f, 1f), Color(1f, 1f, 1f, 1f), 52, -136, 140, 28);
            CreateButton(panelContentRoot!, "DTMAPI.Errors.SelectWarnings", T("warnings.label", "Warnings") + " (" + manager.Warnings.Count.ToString(CultureInfo.InvariantCulture) + ")", () =>
            {
                managerShowingWarnings = true;
                managerErrorsPageIndex = 0;
                dirty = true;
            }, managerShowingWarnings ? Color(0.48f, 0.38f, 0.16f, 1f) : Color(0.12f, 0.15f, 0.18f, 1f), Color(1f, 1f, 1f, 1f), 202, -136, 160, 28);

            IReadOnlyList<ManagerDiagnosticRow> rows = managerShowingWarnings ? manager.Warnings : manager.Errors;
            string label = managerShowingWarnings ? T("warnings.label", "Warnings") : T("errors.label", "Errors");
            ClampPage(ref managerErrorsPageIndex, rows.Count, ManagerDiagnosticPageSize);
            GetPageBounds(rows.Count, managerErrorsPageIndex, ManagerDiagnosticPageSize, out int start, out int end, out int pageCount);
            AddText(panelContentRoot!, "DTMAPI.Errors.Count", FormatShowingPage(label, start, end, rows.Count), 13, Color(0.72f, 0.82f, 0.86f, 1f), TextAnchorMiddleLeft, 390, -139, 300, 22);
            if (pageCount > 1)
                RenderPager("DTMAPI.Errors.Pager", managerErrorsPageIndex, pageCount, rows.Count, start, end, 732, -139, page =>
            {
                managerErrorsPageIndex = page;
                dirty = true;
            });

            if (rows.Count == 0)
            {
                AddText(panelContentRoot!, "DTMAPI.Errors.EmptySelected", managerShowingWarnings
                    ? T("warnings.noneSelected", "No warnings recorded.")
                    : T("errors.noneSelected", "No errors recorded."), 15, Color(0.88f, 1f, 0.88f, 1f), TextAnchorMiddleLeft, 52, -184, 720, 24);
                return;
            }

            for (int row = start; row < end; row++)
            {
                ManagerDiagnosticRow diagnostic = rows[row];
                AddText(
                    panelContentRoot!,
                    "DTMAPI.Errors.Row." + row,
                    Truncate(ManagerPageRowFormatter.FormatDiagnosticRow(diagnostic) +
                        (string.IsNullOrWhiteSpace(diagnostic.Details) ? string.Empty : " | " + diagnostic.Details), 132),
                    13,
                    managerShowingWarnings ? Color(1f, 0.88f, 0.62f, 1f) : Color(1f, 0.76f, 0.70f, 1f),
                    TextAnchorMiddleLeft,
                    52,
                    -180 - (row - start) * 30,
                    930,
                    24);
            }
        }

        private void RenderHooks(RuntimeSnapshot snapshot)
        {
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null)
            {
                AddText(panelContentRoot!, "DTMAPI.Hooks.Unavailable", FormatManagerUnavailable(), 16, Color(1f, 0.82f, 0.58f, 1f), TextAnchorMiddleLeft, 52, -144, 900, 26);
                return;
            }

            if (manager.Hooks.Count == 0)
            {
                AddText(panelContentRoot!, "DTMAPI.Hooks.Empty", T("hooks.empty", "No hook statuses recorded."), 16, Color(0.88f, 1f, 0.88f, 1f), TextAnchorMiddleLeft, 52, -144, 600, 26);
                return;
            }
            AddText(panelContentRoot!, "DTMAPI.Hooks.Title", T("hooks.title", "Hooks"), 15, Color(0.78f, 0.88f, 0.92f, 1f), TextAnchorMiddleLeft, 52, -132, 900, 24);
            ClampPage(ref managerHooksPageIndex, manager.Hooks.Count, ManagerHookPageSize);
            GetPageBounds(manager.Hooks.Count, managerHooksPageIndex, ManagerHookPageSize, out int hookStart, out int hookEnd, out int hookPageCount);
            AddText(panelContentRoot!, "DTMAPI.Hooks.Count", FormatShowingPage(T("hooks.title", "Hooks"), hookStart, hookEnd, manager.Hooks.Count), 13, Color(0.72f, 0.82f, 0.86f, 1f), TextAnchorMiddleLeft, 52, -158, 620, 22);
            if (hookPageCount > 1)
                RenderPager("DTMAPI.Hooks.Pager", managerHooksPageIndex, hookPageCount, manager.Hooks.Count, hookStart, hookEnd, 732, -158, page =>
                {
                    managerHooksPageIndex = page;
                    dirty = true;
                });
            for (int i = hookStart; i < hookEnd; i++)
            {
                ManagerHookRow hook = manager.Hooks[i];
                AddText(panelContentRoot!, "DTMAPI.Hooks.Row." + i, Truncate(ManagerPageRowFormatter.FormatHookRow(hook), 132), 13, GetHookRowColor(hook), TextAnchorMiddleLeft, 52, -188 - (i - hookStart) * 28, 930, 22);
            }
        }

        private void RenderFeatures(RuntimeSnapshot snapshot)
        {
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null)
            {
                AddText(panelContentRoot!, "DTMAPI.Features.Unavailable", FormatManagerUnavailable(), 16, Color(1f, 0.82f, 0.58f, 1f), TextAnchorMiddleLeft, 52, -144, 900, 26);
                return;
            }

            AddText(panelContentRoot!, "DTMAPI.Advanced.Title", T("advanced.title", "Advanced diagnostics"), 16, Color(0.78f, 0.88f, 0.92f, 1f), TextAnchorMiddleLeft, 52, -132, 300, 24);
            AddText(panelContentRoot!, "DTMAPI.Advanced.Summary", Truncate(FormatAdvancedSummary(manager.AdvancedDiagnostics), 136), 13, Color(0.86f, 0.92f, 1f, 1f), TextAnchorMiddleLeft, 52, -160, 930, 22);
            CreateButton(panelContentRoot!, "DTMAPI.Advanced.SelectRegistry", T("advanced.registry", "Registry evidence") + " (" + manager.AdvancedDiagnostics.Rows.Count.ToString(CultureInfo.InvariantCulture) + ")", () =>
            {
                managerAdvancedShowingFeatures = false;
                managerAdvancedPageIndex = 0;
                dirty = true;
            }, !managerAdvancedShowingFeatures ? Color(0.18f, 0.42f, 0.50f, 1f) : Color(0.12f, 0.15f, 0.18f, 1f), Color(1f, 1f, 1f, 1f), 52, -190, 210, 28);
            CreateButton(panelContentRoot!, "DTMAPI.Advanced.SelectFeatures", T("advanced.features", "Feature evidence") + " (" + manager.Features.Count.ToString(CultureInfo.InvariantCulture) + ")", () =>
            {
                managerAdvancedShowingFeatures = true;
                managerFeaturesPageIndex = 0;
                dirty = true;
            }, managerAdvancedShowingFeatures ? Color(0.18f, 0.42f, 0.50f, 1f) : Color(0.12f, 0.15f, 0.18f, 1f), Color(1f, 1f, 1f, 1f), 272, -190, 200, 28);

            if (managerAdvancedShowingFeatures)
            {
                ClampPage(ref managerFeaturesPageIndex, manager.Features.Count, ManagerFeaturePageSize);
                GetPageBounds(manager.Features.Count, managerFeaturesPageIndex, ManagerFeaturePageSize, out int featureStart, out int featureEnd, out int featurePageCount);
                AddText(panelContentRoot!, "DTMAPI.Advanced.Count", FormatShowingPage(T("advanced.features", "Feature evidence"), featureStart, featureEnd, manager.Features.Count), 13, Color(0.72f, 0.82f, 0.86f, 1f), TextAnchorMiddleLeft, 500, -193, 220, 22);
                if (featurePageCount > 1)
                    RenderPager("DTMAPI.Features.Pager", managerFeaturesPageIndex, featurePageCount, manager.Features.Count, featureStart, featureEnd, 732, -193, page =>
                    {
                        managerFeaturesPageIndex = page;
                        dirty = true;
                    });
                if (manager.Features.Count == 0)
                {
                    AddText(panelContentRoot!, "DTMAPI.Advanced.Empty", T("advanced.featuresEmpty", "No feature status evidence recorded."), 15, Color(0.88f, 1f, 0.88f, 1f), TextAnchorMiddleLeft, 52, -232, 700, 24);
                    return;
                }

                for (int row = featureStart; row < featureEnd; row++)
                {
                    ManagerFeatureRow feature = manager.Features[row];
                    AddText(panelContentRoot!, "DTMAPI.Features.Row." + row, Truncate(ManagerPageRowFormatter.FormatFeatureRow(feature), 132), 13, GetFeatureRowColor(feature), TextAnchorMiddleLeft, 52, -232 - (row - featureStart) * 28, 930, 22);
                }
                return;
            }

            IReadOnlyList<ManagerAdvancedDiagnosticRow> advancedRows = manager.AdvancedDiagnostics.Rows;
            ClampPage(ref managerAdvancedPageIndex, advancedRows.Count, ManagerAdvancedPageSize);
            GetPageBounds(advancedRows.Count, managerAdvancedPageIndex, ManagerAdvancedPageSize, out int advancedStart, out int advancedEnd, out int advancedPageCount);
            AddText(panelContentRoot!, "DTMAPI.Advanced.Count", FormatShowingPage(T("advanced.registry", "Registry evidence"), advancedStart, advancedEnd, advancedRows.Count), 13, Color(0.72f, 0.82f, 0.86f, 1f), TextAnchorMiddleLeft, 500, -193, 220, 22);
            if (advancedPageCount > 1)
                RenderPager("DTMAPI.Advanced.Pager", managerAdvancedPageIndex, advancedPageCount, advancedRows.Count, advancedStart, advancedEnd, 732, -193, page =>
            {
                managerAdvancedPageIndex = page;
                dirty = true;
            });
            if (advancedRows.Count == 0)
            {
                string empty = manager.AdvancedDiagnostics.RegistryAvailable
                    ? T("advanced.registryClean", "Registry checks are clean; no sampled diagnostics or legacy diffs.")
                    : T("advanced.registryUnavailable", "Content/manifest registry evidence is not available yet.");
                AddText(panelContentRoot!, "DTMAPI.Advanced.Empty", empty, 15, Color(0.88f, 1f, 0.88f, 1f), TextAnchorMiddleLeft, 52, -232, 820, 24);
                return;
            }

            for (int row = advancedStart; row < advancedEnd; row++)
            {
                ManagerAdvancedDiagnosticRow advanced = advancedRows[row];
                object color = advanced.Severity.Equals("error", StringComparison.OrdinalIgnoreCase)
                    ? Color(1f, 0.76f, 0.70f, 1f)
                    : advanced.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase)
                        ? Color(1f, 0.88f, 0.62f, 1f)
                        : Color(0.86f, 0.92f, 1f, 1f);
                AddText(panelContentRoot!, "DTMAPI.Advanced.Row." + row, Truncate(ManagerPageRowFormatter.FormatAdvancedDiagnosticRow(advanced), 132), 13, color, TextAnchorMiddleLeft, 52, -232 - (row - advancedStart) * 28, 930, 22);
            }
        }

        private void RenderLogs(RuntimeSnapshot snapshot)
        {
            CreateButton(panelContentRoot!, "DTMAPI.Logs.Export", T("logs.export", "Export logs"), () =>
            {
                string path = runtime.UI.ExportLogs();
                DtmManagerReportExportResult? export = runtime.UI.LastManagerReportExport;
                ManagerLogsPageState logsState = ManagerLogsPageState.From(export, runtime.UI.CurrentManagerModel);
                statusMessage = logsState.ExportStatus == "export-failed"
                    ? T("logs.exportFailed", "Export failed: ") + logsState.ExportErrorMessage
                    : T("logs.exported", "Exported logs: ") + (string.IsNullOrWhiteSpace(path) ? logsState.ExportStatus : path);
                dirty = true;
            }, Color(0.18f, 0.34f, 0.42f, 1f), Color(1f, 1f, 1f, 1f), 52, -144, 148, 30);
            ManagerLogsPageState state = ManagerLogsPageState.From(runtime.UI.LastManagerReportExport, runtime.UI.CurrentManagerModel);
            string latestLogPath = string.IsNullOrWhiteSpace(state.LatestLogPath) ? runtime.Diagnostics.GetLatestLogPath() : state.LatestLogPath;
            string exportedPath = string.IsNullOrWhiteSpace(state.ExportedReportPath) ? snapshot.LastExportPath : state.ExportedReportPath;
            AddText(panelContentRoot!, "DTMAPI.Logs.Latest", T("logs.latest", "Latest log: ") + Truncate(latestLogPath, 120), 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -192, 930, 24);
            string exportStatusText = state.ExportStatus == "export-failed"
                ? T("logs.exportFailedClear", "Export status: export-failed; see error below")
                : T("logs.exportStatus", "Export status: ") + state.ExportStatus;
            AddText(panelContentRoot!, "DTMAPI.Logs.ExportStatus", exportStatusText, 14, GetLogsStatusColor(state), TextAnchorMiddleLeft, 52, -222, 930, 24);
            AddText(panelContentRoot!, "DTMAPI.Logs.ExportPath", T("logs.latestExport", "Exported path: ") + Truncate(string.IsNullOrWhiteSpace(exportedPath) ? T("common.none", "(none)") : exportedPath, 120), 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -252, 930, 24);
            AddText(panelContentRoot!, "DTMAPI.Logs.SnapshotReport", T("logs.snapshotReport", "Snapshot report: ") + Truncate(string.IsNullOrWhiteSpace(state.SnapshotLatestReportPath) ? T("common.none", "(none)") : state.SnapshotLatestReportPath, 120), 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -282, 930, 24);
            AddText(panelContentRoot!, "DTMAPI.Logs.PathMatch", T("logs.pathMatch", "Path match: ") + state.PathMatchStatus + T("logs.snapshotReportStatus", " | snapshot report: ") + state.SnapshotReportStatus, 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -312, 930, 24);
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager != null)
                AddText(panelContentRoot!, "DTMAPI.Logs.InstallState", T("logs.installState", "Install state: ") + Truncate(ManagerPageRowFormatter.FormatInstallState(manager.InstallState), 120), 14, Color(0.92f, 0.95f, 0.96f, 1f), TextAnchorMiddleLeft, 52, -342, 930, 24);
            if (state.ExportStatus == "export-failed")
                AddText(panelContentRoot!, "DTMAPI.Logs.ExportError", T("logs.exportError", "Export error: ") + Truncate(state.ExportErrorMessage, 120), 14, Color(1f, 0.76f, 0.70f, 1f), TextAnchorMiddleLeft, 52, -372, 930, 24);
        }

        private object GetModRowColor(ManagerModRow mod)
        {
            if (mod.IsBlocked)
                return Color(1f, 0.76f, 0.70f, 1f);
            if (mod.IsWarning || mod.IsDisabled)
                return Color(1f, 0.88f, 0.62f, 1f);
            if (mod.IsLoaded)
                return Color(0.88f, 1f, 0.88f, 1f);
            return Color(0.86f, 0.92f, 1f, 1f);
        }

        private object GetHookRowColor(ManagerHookRow hook)
        {
            if (hook.IsFailed)
                return Color(1f, 0.76f, 0.70f, 1f);
            if (hook.IsMissing)
                return Color(1f, 0.88f, 0.62f, 1f);
            if (hook.Status.Equals("verified", StringComparison.OrdinalIgnoreCase) || hook.Status.Equals("ready", StringComparison.OrdinalIgnoreCase))
                return Color(0.88f, 1f, 0.88f, 1f);
            return Color(0.86f, 0.92f, 1f, 1f);
        }

        private object GetFeatureRowColor(ManagerFeatureRow feature)
        {
            if (feature.IsFailed)
                return Color(1f, 0.76f, 0.70f, 1f);
            if (feature.IsDegraded)
                return Color(1f, 0.88f, 0.62f, 1f);
            return Color(0.88f, 1f, 0.88f, 1f);
        }

        private object GetLogsStatusColor(ManagerLogsPageState state)
        {
            if (state.ExportStatus.Equals("export-failed", StringComparison.OrdinalIgnoreCase) ||
                state.PathMatchStatus.Equals("report-path-mismatch", StringComparison.OrdinalIgnoreCase) ||
                state.PathMatchStatus.Equals("missing-export-path", StringComparison.OrdinalIgnoreCase))
                return Color(1f, 0.76f, 0.70f, 1f);

            if (state.ExportStatus.Equals("not-exported", StringComparison.OrdinalIgnoreCase))
                return Color(1f, 0.88f, 0.62f, 1f);

            return Color(0.88f, 1f, 0.88f, 1f);
        }

        private void SelectConfigPage(string uniqueId)
        {
            string? cancelError = null;
            if (!string.Equals(selectedConfigModId, uniqueId, StringComparison.OrdinalIgnoreCase) && selectedConfigModId != null)
            {
                IConfigMenuPage? oldPage = configMenu.GetPage(selectedConfigModId);
                if (oldPage != null && oldPage.HasPendingChanges)
                {
                    try
                    {
                        configMenu.Cancel(oldPage.Manifest.UniqueID);
                    }
                    catch (Exception ex)
                    {
                        cancelError = oldPage.Manifest.UniqueID + ": " + ex.Message;
                    }
                }
            }
            if (cancelError != null)
            {
                statusMessage = cancelError;
                dirty = true;
                return;
            }
            selectedConfigModId = uniqueId;
            runtime.UI.OpenConfigPage(uniqueId);
            statusMessage = string.Empty;
            capturingKeybindItemId = null;
            configItemPageIndex = 0;
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
            string cancelResult = CancelSelectedConfigPendingChanges("CloseMenu");
            if (cancelResult.StartsWith("failed:", StringComparison.OrdinalIgnoreCase))
            {
                statusMessage = cancelResult.Substring("failed:".Length);
                dirty = true;
                return;
            }
            capturingKeybindItemId = null;
            inputValues.Clear();
            CloseOwnedRuntimeMenu();
        }

        private void ResetBoundaryState(string reason)
        {
            string normalizedReason = reason ?? string.Empty;
            bool forceLog = normalizedReason.StartsWith("SaveLoaded", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.StartsWith("ReturnedToTitle", StringComparison.OrdinalIgnoreCase) ||
                normalizedReason.StartsWith("Shutdown", StringComparison.OrdinalIgnoreCase);
            bool hadTransientState = IsOwnedRuntimeMenuOpen() ||
                renderedObjects.Count > 0 ||
                eventBinders.Count > 0 ||
                inputValues.Count > 0 ||
                capturingKeybindItemId != null ||
                eventSystemRoot != null ||
                menuOpenLogged ||
                buttonVisibleLogged ||
                wasTitleVisible ||
                renderedPage != (DtmOverlayPage)(-1) ||
                renderedConfigUniqueId != null;
            string cancelResult = CancelSelectedConfigPendingChanges(normalizedReason);
            CloseOwnedRuntimeMenu();
            SetActive(panelRoot, false);
            SetActive(titleButtonRoot, true);
            DestroyOwnedEventSystem(normalizedReason);
            ClearRenderedObjects();
            inputValues.Clear();
            capturingKeybindItemId = null;
            statusMessage = string.Empty;
            dirty = true;
            wasTitleVisible = false;
            menuOpenLogged = false;
            buttonVisibleLogged = false;
            renderedPage = (DtmOverlayPage)(-1);
            renderedOverlaySessionSequence = -1;
            renderedConfigUniqueId = null;
            configListPageIndex = 0;
            configItemPageIndex = 0;
            managerModsPageIndex = 0;
            managerErrorsPageIndex = 0;
            managerHooksPageIndex = 0;
            managerFeaturesPageIndex = 0;
            managerAdvancedPageIndex = 0;
            selectedManagerModId = null;
            managerShowingWarnings = false;
            managerAdvancedShowingFeatures = false;
            if (forceLog || hadTransientState || !cancelResult.Equals("none", StringComparison.OrdinalIgnoreCase))
                runtime.RuntimeMonitor.Log("DTMAPI title settings UI boundary reset reason=" + normalizedReason + " pendingConfig=" + cancelResult + ".");
        }

        private bool IsOwnedRuntimeMenuOpen()
        {
            return runtime.UI.IsOpen && runtime.UI.ActiveMenuId.Equals(
                "DTMAPI." + runtime.UI.CurrentPage,
                StringComparison.OrdinalIgnoreCase);
        }

        private void CloseOwnedRuntimeMenu()
        {
            if (IsOwnedRuntimeMenuOpen())
                runtime.UI.Close();
        }

        private string CancelSelectedConfigPendingChanges(string reason)
        {
            if (selectedConfigModId == null)
                return "none";

            IConfigMenuPage? page = configMenu.GetPage(selectedConfigModId);
            if (page == null || !page.HasPendingChanges)
                return "none";

            try
            {
                configMenu.Cancel(page.Manifest.UniqueID);
                return "cancelled:" + page.Manifest.UniqueID;
            }
            catch (Exception ex)
            {
                string message = page.Manifest.UniqueID + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.TitleSettings", "Failed to cancel pending config changes at title UI boundary.", "reason=" + (reason ?? string.Empty) + "; " + ex);
                return "failed:" + message;
            }
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

        private void TryResetKeybind(IConfigMenuItem item, IResettableKeybindConfigMenuItem resettable)
        {
            if (!resettable.TryResetPendingValue(out string error))
                statusMessage = item.Name + ": " + error;
            else
                statusMessage = string.Empty;
            inputValues[item.ItemId] = item.PendingValue;
            capturingKeybindItemId = null;
            keyCaptureArming = false;
            keyCaptureStartedFrame = -1;
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

        private void DestroyIconSprite()
        {
            object? sprite = iconSprite;
            iconSprite = null;
            if (sprite == null)
                return;

            object? texture = null;
            try
            {
                texture = GetProperty(sprite, "texture");
            }
            catch
            {
                texture = null;
            }

            Destroy(sprite);
            if (texture != null)
                Destroy(texture);
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

        private string FormatUnityObjectState(object? value)
        {
            if (value == null)
                return "null";
            if (IsDestroyed(value))
                return "destroyed";

            PropertyInfo? activeSelf = value.GetType().GetProperty("activeSelf", BindingFlags.Public | BindingFlags.Instance);
            if (activeSelf != null && activeSelf.GetValue(value, null) is bool active)
                return active ? "alive-active" : "alive-inactive";
            return "alive";
        }

        private int IsAlive(object? value)
        {
            return value != null && !IsDestroyed(value) ? 1 : 0;
        }

        private int CountAliveUnityObjects(IEnumerable<object> values)
        {
            int count = 0;
            foreach (object value in values)
            {
                if (value != null && !IsDestroyed(value))
                    count++;
            }
            return count;
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
            DestroyOwnedEventSystem("title UI rebuild");
            if (root != null && objectType != null && !IsDestroyed(root))
                Destroy(root);
            root = null;
            titleButtonRoot = null;
            panelRoot = null;
            panelContentRoot = null;
            DestroyIconSprite();
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
        private string FormatManagerUnavailable()
        {
            return string.IsNullOrWhiteSpace(runtime.UI.LastManagerRefreshError)
                ? T("manager.modelUnavailable", "Manager model unavailable.")
                : T("manager.modelRefreshFailed", "Manager model refresh failed: ") + runtime.UI.LastManagerRefreshError;
        }

        private string FormatManagerModListRow(ManagerModRow mod)
        {
            int issueCount = mod.Dependencies.Count(dependency => dependency.IsIssue);
            string line = "[" + TranslateManagerStatus(FirstManagerText(mod.StatusCode, mod.Status, "unknown")) + "] " +
                FirstManagerText(mod.Name, T("mods.unknownName", "unnamed")) +
                "  " + FirstManagerText(mod.Version, T("mods.unknownVersion", "no version")) +
                " | " + FirstManagerText(mod.UniqueID, T("mods.unknownId", "no id")) +
                " | " + TranslateManagerSource(mod.Source);
            if (mod.Dependencies.Count > 0)
            {
                line += " | " + T("mods.dependencies", "Dependencies") + "=" +
                    mod.Dependencies.Count.ToString(CultureInfo.InvariantCulture) + " (" +
                    (issueCount > 0
                        ? string.Format(CultureInfo.InvariantCulture, T("mods.dependencyIssue", "{0} issue(s)"), issueCount)
                        : T("mods.dependencyOk", "ok")) + ")";
            }
            if (mod.RequiresRestart)
                line += " | " + T("mods.restartRequired", "restart required");
            return line;
        }

        private IReadOnlyList<string> FormatManagerModDetailLines(ManagerModRow mod)
        {
            string state = T("mods.stateLabel", "State") + ": " +
                TranslateManagerStatus(FirstManagerText(mod.StatusCode, mod.Status, "unknown")) +
                " | " + (mod.Loaded ? T("mods.loadedYes", "loaded") : T("mods.loadedNo", "not loaded")) +
                " | " + (mod.OfficialEnablementManaged
                    ? mod.OfficialEnabled
                        ? T("mods.officialEnabled", "official enabled")
                        : T("mods.officialDisabled", "official disabled")
                    : T("mods.officialUnmanaged", "not officially managed"));
            if (!string.IsNullOrWhiteSpace(mod.Reason))
                state += " | " + text.TranslateEnablementReason(mod.Reason);

            string identity = T("mods.identityLabel", "Identity") + ": " +
                TranslateManagerIdentity(FirstManagerText(mod.ManagedIdentity, mod.Type, T("mods.unknownIdentity", "unknown identity"))) +
                " | " + T("mods.placementLabel", "placement") + "=" +
                TranslateManagerPlacement(FirstManagerText(mod.ManagedPlacement, T("common.none", "none"))) +
                " | " + T("mods.compatibilityLabel", "compatibility") + "=" +
                TranslateManagerCompatibility(FirstManagerText(mod.GameCompatibility, T("common.none", "none")));
            string dependencies = T("mods.dependencies", "Dependencies") + ": " +
                (mod.Dependencies.Count == 0
                    ? T("mods.dependenciesNone", "none declared")
                    : string.Join("; ", mod.Dependencies.Select(FormatManagerDependencyRow).ToArray()));
            string restart = T("mods.restartLabel", "Restart") + ": " +
                (string.IsNullOrWhiteSpace(mod.RestartHint)
                    ? T("mods.restartNone", "not currently required")
                    : TranslateManagerRestartHint(mod.RestartHint));
            return new[] { state, identity, dependencies, restart };
        }

        private string FormatManagerDependencyRow(ManagerDependencyRow dependency)
        {
            return FirstManagerText(dependency.UniqueID, T("mods.unknownId", "no id")) +
                (string.IsNullOrWhiteSpace(dependency.MinimumVersion) ? string.Empty : ">=" + dependency.MinimumVersion) +
                " [" + (dependency.Required
                    ? T("mods.dependencyRequired", "required")
                    : T("mods.dependencyOptional", "optional")) +
                "/" + TranslateDependencyStatus(dependency.Status) + "]";
        }

        private string FormatAdvancedSummary(ManagerAdvancedDiagnostics advanced)
        {
            if (!advanced.RegistryAvailable)
                return T("advanced.unavailable", "Content/manifest registry unavailable; refresh after discovery completes.");
            return string.Format(
                CultureInfo.InvariantCulture,
                T("advanced.summary", "registry {0} | manifest issues {1} | dependency errors/warnings {2}/{3} | Advanced receipts {4}/{5} | restart required {6} | Hook/feature issues {7}/{8}"),
                advanced.RegistryRowCount,
                advanced.ManifestIssueCount,
                advanced.DependencyErrorCount,
                advanced.DependencyWarningCount,
                advanced.AdvancedReferenceVerifiedCount,
                advanced.AdvancedModCount,
                advanced.RestartRequiredModCount,
                advanced.FailedOrMissingHookCount,
                advanced.FailedOrDegradedFeatureCount);
        }

        private string TranslateManagerSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return T("mods.unknownSource", "unknown source");
            if (source.IndexOf("Workshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf("Official", StringComparison.OrdinalIgnoreCase) >= 0)
                return T("mods.source.official", "official/Steam managed");
            if (source.IndexOf("Local", StringComparison.OrdinalIgnoreCase) >= 0)
                return T("mods.source.local", "local DTMAPI file");
            return source;
        }

        private string TranslateManagerIdentity(string identity)
        {
            string normalized = (identity ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
            switch (normalized)
            {
                case "strictcodemod":
                case "strict":
                    return T("mods.identity.strict", "Strict CodeMod");
                case "advancedcodemod":
                case "advanced":
                    return T("mods.identity.advanced", "Advanced CodeMod");
                case "contentpack":
                    return T("mods.identity.contentPack", "ContentPack");
                default:
                    return FirstManagerText(identity, T("mods.unknownIdentity", "unknown identity"));
            }
        }

        private string TranslateManagerPlacement(string placement)
        {
            switch ((placement ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "managed-local":
                    return T("mods.placement.local", "managed local");
                case "managed-official-local":
                    return T("mods.placement.officialLocal", "managed official local");
                case "managed-workshop-native-verified":
                    return T("mods.placement.workshopVerified", "native-verified Workshop");
                case "managed-workshop-compatibility":
                    return T("mods.placement.workshopCompatibility", "compatibility Workshop");
                default:
                    return FirstManagerText(placement, T("common.none", "none"));
            }
        }

        private string TranslateManagerCompatibility(string compatibility)
        {
            string value = (compatibility ?? string.Empty).Trim();
            if (value.Equals("unverified", StringComparison.OrdinalIgnoreCase))
                return T("mods.compatibilityUnverified", "unverified");
            if (value.Equals("not-applicable", StringComparison.OrdinalIgnoreCase))
                return T("mods.compatibilityNotApplicable", "not applicable");
            string[] contextParts = value.Split(';').Select(part => part.Trim()).ToArray();
            if (contextParts.Length > 0 &&
                (contextParts[0].Equals("Exact", StringComparison.OrdinalIgnoreCase) ||
                 contextParts[0].Equals("Drift", StringComparison.OrdinalIgnoreCase) ||
                 contextParts[0].Equals("Unknown", StringComparison.OrdinalIgnoreCase)))
            {
                string contextLabel = contextParts[0].Equals("Exact", StringComparison.OrdinalIgnoreCase)
                    ? T("mods.compatibilityExact", "exact")
                    : contextParts[0].Equals("Drift", StringComparison.OrdinalIgnoreCase)
                        ? T("mods.compatibilityDrift", "drift")
                        : T("mods.compatibilityUnknown", "unknown context");
                var contextValues = contextParts
                    .Skip(1)
                    .Select(part => part.Split(new[] { '=' }, 2))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);
                contextValues.TryGetValue("compiledBuild", out string? compiledBuild);
                contextValues.TryGetValue("installedBuild", out string? installedBuild);
                contextValues.TryGetValue("references", out string? references);
                return contextLabel +
                    (string.IsNullOrWhiteSpace(compiledBuild)
                        ? string.Empty
                        : " | " + T("mods.compiledBuildLabel", "compiled baseline") + "=" + compiledBuild) +
                    (string.IsNullOrWhiteSpace(installedBuild)
                        ? string.Empty
                        : " | " + T("mods.installedBuildLabel", "installed build") + "=" + installedBuild) +
                    (string.IsNullOrWhiteSpace(references)
                        ? string.Empty
                        : " | " + T("mods.referenceMatchLabel", "reference match") + "=" + references);
            }
            const string verifiedPrefix = "verified build=";
            if (!value.StartsWith(verifiedPrefix, StringComparison.OrdinalIgnoreCase))
                return FirstManagerText(value, T("common.none", "none"));

            string build = value.Substring(verifiedPrefix.Length);
            string hash = string.Empty;
            int separator = build.IndexOf(';');
            if (separator >= 0)
            {
                string remainder = build.Substring(separator + 1).Trim();
                build = build.Substring(0, separator).Trim();
                const string hashPrefix = "gameAssemblySha256=";
                hash = remainder.StartsWith(hashPrefix, StringComparison.OrdinalIgnoreCase)
                    ? remainder.Substring(hashPrefix.Length).Trim()
                    : remainder;
            }
            return T("mods.compatibilityVerified", "verified") +
                " | " + T("mods.gameBuildLabel", "game build") + "=" + build +
                (string.IsNullOrWhiteSpace(hash)
                    ? string.Empty
                    : " | " + T("mods.gameAssemblyLabel", "game assembly SHA-256") + "=" + hash);
        }

        private string TranslateManagerRestartHint(string hint)
        {
            string value = (hint ?? string.Empty).Trim();
            if (value.IndexOf("before this Mod can run again", StringComparison.OrdinalIgnoreCase) >= 0)
                return T("mods.restartRunAgain", value);
            if (value.IndexOf("unload this disabled CodeMod", StringComparison.OrdinalIgnoreCase) >= 0)
                return T("mods.restartUnloadDisabled", value);
            if (value.IndexOf("Assembly updates", StringComparison.OrdinalIgnoreCase) >= 0 &&
                value.IndexOf("take effect after restart", StringComparison.OrdinalIgnoreCase) >= 0)
                return T("mods.restartAssemblyChanges", value);
            return text.TranslateEnablementReason(value);
        }

        private string TranslateManagerStatus(string? status)
        {
            switch ((status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "healthy":
                case "ok":
                case "verified":
                case "active":
                    return T("status.state.healthy", "healthy");
                case "loaded":
                    return T("mods.state.loaded", "loaded");
                case "loaded-disabled":
                    return T("mods.state.loadedDisabled", "loaded; restart to stop");
                case "restart-required":
                case "dependency-blocked":
                case "blocked":
                    return T("mods.state.restart", "restart/dependency required");
                case "warning":
                case "warnings":
                case "attention-required":
                    return T("status.state.attention", "attention required");
                case "degraded":
                    return T("status.state.degraded", "degraded");
                case "failed":
                case "error":
                    return T("status.state.failed", "failed");
                default:
                    return FirstManagerText(status, T("common.none", "none"));
            }
        }

        private string TranslateDependencyStatus(string? status)
        {
            switch ((status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "satisfied":
                case "ok":
                case "loaded":
                    return T("mods.dependencyOk", "ok");
                case "required-missing":
                    return T("mods.dependencyRequiredMissing", "required missing");
                case "optional-missing":
                    return T("mods.dependencyOptionalMissing", "optional missing");
                case "version-too-old":
                    return T("mods.dependencyVersionOld", "version too old");
                default:
                    return FirstManagerText(status, T("common.none", "none"));
            }
        }

        private static string FirstManagerText(params string?[] values)
        {
            foreach (string? value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value!;
            }
            return string.Empty;
        }

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

        private bool TrySetClipboardText(string value)
        {
            value ??= string.Empty;
            if (TrySetUnityClipboardText(value))
                return true;

            return TrySetWindowsFormsClipboardText(value);
        }

        private bool TrySetUnityClipboardText(string value)
        {
            foreach (string typeName in new[] { "UnityEngine.GUIUtility, UnityEngine", "UnityEngine.GUIUtility, UnityEngine.CoreModule" })
            {
                try
                {
                    Type? guiUtility = Type.GetType(typeName);
                    PropertyInfo? systemCopyBuffer = guiUtility?.GetProperty("systemCopyBuffer", BindingFlags.Public | BindingFlags.Static);
                    if (systemCopyBuffer == null || !systemCopyBuffer.CanWrite)
                        continue;

                    systemCopyBuffer.SetValue(null, value, null);
                    return true;
                }
                catch
                {
                    // Fall through to the next clipboard provider.
                }
            }

            return false;
        }

        private bool TrySetWindowsFormsClipboardText(string value)
        {
            try
            {
                Type? clipboard = Type.GetType("System.Windows.Forms.Clipboard, System.Windows.Forms");
                MethodInfo? setText = clipboard?.GetMethod("SetText", new[] { typeof(string) });
                if (setText == null)
                    return false;

                setText.Invoke(null, new object[] { value });
                return true;
            }
            catch
            {
                return false;
            }
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
