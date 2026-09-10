using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<UiMemberKey, MemberInfo>
            UiMemberCache =
                new ConcurrentDictionary<UiMemberKey, MemberInfo>();
        private static readonly ConcurrentDictionary<UiMethodKey, MethodInfo>
            UiMethodCache =
                new ConcurrentDictionary<UiMethodKey, MethodInfo>();
        private static readonly object BoxedTrue = true;
        private static readonly object BoxedFalse = false;

        private ConstructorInfo? gameObjectRectConstructor;
        private ConstructorInfo? gameObjectNameConstructor;
        private ConstructorInfo? colorConstructor;
        private ConstructorInfo? vector2Constructor;
        private MethodInfo? gameObjectAddComponentMethod;
        private MethodInfo? gameObjectSetActiveMethod;
        private MethodInfo? transformSetParentMethod;
        private MethodInfo? unityObjectDestroyMethod;
        private MethodInfo? unityObjectDontDestroyOnLoadMethod;
        private MethodInfo? unityObjectEqualityMethod;
        private MethodInfo? unityObjectFindObjectOfTypeMethod;
        private MethodInfo? unityObjectFindObjectsOfTypeMethod;
        private PropertyInfo? gameObjectTransformProperty;
        private PropertyInfo? gameObjectActiveSelfProperty;
        private Type[]? rectTransformComponentTypes;
        private bool uiReflectionMetadataResolved;

        private readonly object?[] createUiObjectArguments = new object?[2];
        private readonly object?[] createPlainObjectArguments = new object?[1];
        private readonly object?[] addComponentArguments = new object?[1];
        private readonly object?[] getComponentArguments = new object?[1];
        private readonly object?[] setParentArguments = new object?[2];
        private readonly object?[] findObjectArguments = new object?[1];
        private readonly object?[] findObjectsArguments = new object?[1];
        private readonly object?[] destroyArguments = new object?[1];
        private readonly object?[] dontDestroyArguments = new object?[1];
        private readonly object?[] setActiveArguments = new object?[1];
        private readonly object?[] equalityArguments = new object?[2];
        private readonly object?[] colorArguments = new object?[4];
        private readonly object?[] vector2Arguments = new object?[2];
        private object? colorWhite;
        private object? colorMuted;
        private object? colorDisabled;
        private object? colorWarning;
        private object? colorPrimaryText;
        private object? colorButton;
        private object? colorSelected;
        private object? vectorZero;
        private object? vectorOne;
        private object? vectorTopLeft;
        private object? vectorBottomRight;
        private object? vectorCenter;

        private object CreateButton(object parent, string name, string label, Action onClick, object background, object textColor, float x, float y, float w, float h)
        {
            float minimum = (float)(layout?.MinimumClickSize ?? 44d);
            w = Math.Max(w, minimum);
            h = Math.Max(h, minimum);
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", background);
            object button = AddComponent(go, buttonType!);
            SetProperty(button, "targetGraphic", image);
            AddButtonListener(button, onClick);
            object labelText = AddText(go, name + ".Text", label, 13, textColor, TextAnchorMiddleCenter, 4, -3, -8, -6, stretch: true);
            SetProperty(labelText, "resizeTextForBestFit", false);
            SetEnumProperty(labelText, "horizontalOverflow", 0);
            SetEnumProperty(labelText, "verticalOverflow", 0);
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return go;
        }

        private object CreateInput(object parent, string name, string value, Action<string> onEdited, float x, float y, float w, float h)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "color", Color(0.02f, 0.025f, 0.03f, 1f));
            object textObject = AddText(go, name + ".Text", FirstText(value, T("debug.items.search", "Search")), 14, Color(1f, 1f, 1f, 1f), TextAnchorMiddleLeft, 8, 0, -16, 0, stretch: true);
            if (inputFieldType != null && unityActionStringType != null)
            {
                object input = AddComponent(go, inputFieldType);
                SetProperty(input, "targetGraphic", image);
                SetProperty(input, "textComponent", textObject);
                SetProperty(input, "text", value ?? string.Empty);
                inputFields.Add(input);
                activeInputSink?.Add(input);
                AddStringListener(GetProperty(input, "onEndEdit"), onEdited);
            }
            SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return go;
        }

        internal bool IsAnyTextInputFocused()
        {
            foreach (object input in inputFields)
            {
                if (input == null || IsDestroyed(input))
                    continue;
                if (ReadBoolProperty(input, "isFocused"))
                    return true;
            }

            object? currentSelected = GetCurrentSelectedGameObject();
            if (currentSelected == null || IsDestroyed(currentSelected))
                return false;
            foreach (object input in inputFields)
            {
                object? inputGameObject = GetProperty(input, "gameObject");
                if (ReferenceEquals(inputGameObject, currentSelected))
                    return true;
            }

            ResolveTextInputFocusMetadata();
            return IsFocusedTextInputComponent(
                    currentSelected,
                    inputFieldType,
                    gameObjectGetComponentByTypeMethod,
                    inputFieldIsFocusedProperty) ||
                IsFocusedTextInputComponent(
                    currentSelected,
                    tmpInputFieldType,
                    gameObjectGetComponentByTypeMethod,
                    tmpInputFieldIsFocusedProperty);
        }

        private object? GetCurrentSelectedGameObject()
        {
            try
            {
                ResolveTextInputFocusMetadata();
                object? current = eventSystemCurrentProperty?.GetValue(null, null);
                return current == null
                    ? null
                    : eventSystemSelectedGameObjectProperty?.GetValue(current, null);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsFocusedTextInputComponent(
            object gameObject,
            Type? componentType,
            MethodInfo? getComponentByType,
            PropertyInfo? isFocusedProperty)
        {
            if (gameObject == null ||
                componentType == null ||
                getComponentByType == null ||
                isFocusedProperty == null)
            {
                return false;
            }

            try
            {
                object? component = getComponentByType.Invoke(
                    gameObject,
                    new object[] { componentType });
                return component != null &&
                    isFocusedProperty.GetValue(component, null) is bool focused &&
                    focused;
            }
            catch
            {
                return false;
            }
        }

        private static bool ReadBoolProperty(object target, string name)
        {
            try
            {
                object? value = GetProperty(target, name);
                return value is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        private object AddText(object parent, string name, string value, int fontSize, object color, int alignment, float x, float y, float w, float h, bool stretch = false)
        {
            object go = CreateUiObject(name, parent);
            object textObject = AddComponent(go, textType!);
            SetProperty(textObject, "text", value ?? string.Empty);
            SetProperty(textObject, "font", GetBuiltinFont());
            SetProperty(textObject, "fontSize", fontSize);
            SetProperty(textObject, "color", color);
            SetEnumProperty(textObject, "alignment", alignment);
            SetProperty(textObject, "resizeTextForBestFit", false);
            SetEnumProperty(textObject, "horizontalOverflow", 0);
            SetEnumProperty(textObject, "verticalOverflow", 1);
            SetProperty(textObject, "raycastTarget", false);
            if (stretch)
                SetRect(go, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(x, y), Vector2(w, h));
            else
                SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return textObject;
        }

        private object AddImage(object parent, string name, object sprite, float x, float y, float w, float h, bool stretch = false)
        {
            object go = CreateUiObject(name, parent);
            object image = AddComponent(go, imageType!);
            SetProperty(image, "sprite", sprite);
            SetProperty(image, "preserveAspect", true);
            SetProperty(image, "raycastTarget", false);
            SetProperty(image, "color", Color(1f, 1f, 1f, 1f));
            if (stretch)
                SetRect(go, Vector2(0, 0), Vector2(1, 1), Vector2(0.5f, 0.5f), Vector2(x, y), Vector2(w, h));
            else
                SetRect(go, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(x, y), Vector2(w, h));
            return image;
        }

        private object? ResolveItemSprite(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;
            if (itemSpriteCache.TryGetValue(itemId, out object? cached))
                return cached;
            try
            {
                object? sprite = getItemSpriteMethod?.Invoke(null, new object[] { itemId });
                itemSpriteCache[itemId] = sprite;
                return sprite;
            }
            catch (Exception ex)
            {
                runtime.RecordError("DTMAPI.DebugConsole", "Debug console item sprite lookup failed for " + itemId + ".", ex.ToString());
                itemSpriteCache[itemId] = null;
                return null;
            }
        }

        private object CreateUiObject(string name, object? parent)
        {
            ResolveUiReflectionMetadata();
            object go;
            if (gameObjectRectConstructor != null &&
                rectTransformComponentTypes != null)
            {
                createUiObjectArguments[0] = name;
                createUiObjectArguments[1] = rectTransformComponentTypes;
                go = gameObjectRectConstructor.Invoke(createUiObjectArguments);
                createUiObjectArguments[0] = null;
                createUiObjectArguments[1] = null;
            }
            else if (gameObjectNameConstructor != null)
            {
                createPlainObjectArguments[0] = name;
                go = gameObjectNameConstructor.Invoke(
                    createPlainObjectArguments);
                createPlainObjectArguments[0] = null;
            }
            else
                go = Activator.CreateInstance(gameObjectType!, name) ??
                    throw new InvalidOperationException(
                        "Unity GameObject constructor returned null.");

            if (parent != null && transformSetParentMethod != null)
            {
                object? transform =
                    gameObjectTransformProperty?.GetValue(go, null);
                object? parentTransform =
                    gameObjectTransformProperty?.GetValue(parent, null);
                if (transform != null && parentTransform != null)
                {
                    setParentArguments[0] = parentTransform;
                    setParentArguments[1] = BoxedFalse;
                    transformSetParentMethod.Invoke(
                        transform,
                        setParentArguments);
                    setParentArguments[0] = null;
                    setParentArguments[1] = null;
                }
            }
            return go;
        }

        private object CreatePlainObject(string name)
        {
            ResolveUiReflectionMetadata();
            if (gameObjectNameConstructor == null)
                return Activator.CreateInstance(gameObjectType!, name) ??
                    throw new InvalidOperationException(
                        "Unity GameObject constructor returned null.");

            createPlainObjectArguments[0] = name;
            object value = gameObjectNameConstructor.Invoke(
                createPlainObjectArguments);
            createPlainObjectArguments[0] = null;
            return value;
        }

        private object AddComponent(object go, Type componentType)
        {
            ResolveUiReflectionMetadata();
            MethodInfo method = gameObjectAddComponentMethod ??
                throw new MissingMethodException(
                    gameObjectType?.FullName,
                    "AddComponent(Type)");
            addComponentArguments[0] = componentType;
            object? value = method.Invoke(go, addComponentArguments);
            addComponentArguments[0] = null;
            return value ??
                throw new InvalidOperationException(
                    "Unity GameObject.AddComponent returned null for " +
                    componentType.FullName + ".");
        }

        private object? GetComponent(object go, Type componentType)
        {
            ResolveTextInputFocusMetadata();
            getComponentArguments[0] = componentType;
            object? value = gameObjectGetComponentByTypeMethod?.Invoke(
                go,
                getComponentArguments);
            getComponentArguments[0] = null;
            return value;
        }

        private object? FindObjectOfType(Type componentType)
        {
            try
            {
                ResolveUiReflectionMetadata();
                findObjectArguments[0] = componentType;
                object? found = unityObjectFindObjectOfTypeMethod?.Invoke(
                    null,
                    findObjectArguments);
                findObjectArguments[0] = null;
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
            RetainBinder(binder);
            Delegate del = Delegate.CreateDelegate(unityActionType, binder, nameof(ActionBinder.Invoke));
            ResolveCachedMethod(
                    onClick.GetType(),
                    "AddListener",
                    unityActionType)
                ?.Invoke(onClick, new object[] { del });
        }

        private ActionBinder AddButtonRelay(object button)
        {
            var binder = new ActionBinder(NoOp);
            object? onClick = GetProperty(button, "onClick");
            if (onClick == null || unityActionType == null)
                return binder;
            RetainBinder(binder);
            Delegate relay = Delegate.CreateDelegate(
                unityActionType,
                binder,
                nameof(ActionBinder.Invoke));
            ResolveCachedMethod(
                    onClick.GetType(),
                    "AddListener",
                    unityActionType)
                ?.Invoke(onClick, new object[] { relay });
            return binder;
        }

        private void AddStringListener(object? unityEvent, Action<string> action)
        {
            if (unityEvent == null || unityActionStringType == null)
                return;
            var binder = new StringActionBinder(action);
            RetainBinder(binder);
            Delegate del = Delegate.CreateDelegate(unityActionStringType, binder, nameof(StringActionBinder.Invoke));
            ResolveCachedMethod(
                    unityEvent.GetType(),
                    "AddListener",
                    unityActionStringType)
                ?.Invoke(unityEvent, new object[] { del });
        }

        private PointerActionBinder AddPointerRelay(
            object trigger,
            string eventName,
            out bool bound)
        {
            var binder = new PointerActionBinder(NoOpPointer);
            bound = false;
            if (eventTriggerEntryType == null ||
                eventTriggerTypeEnum == null ||
                unityActionBaseEventDataType == null)
            {
                return binder;
            }
            try
            {
                object? entry = Activator.CreateInstance(eventTriggerEntryType);
                if (entry == null)
                    return binder;
                SetProperty(entry, "eventID", Enum.Parse(eventTriggerTypeEnum, eventName));
                object? callback = GetProperty(entry, "callback");
                MethodInfo? addListener = callback == null
                    ? null
                    : ResolveCachedMethod(
                        callback.GetType(),
                        "AddListener",
                        unityActionBaseEventDataType);
                object? triggers = GetProperty(trigger, "triggers");
                MethodInfo? addTrigger = triggers == null
                    ? null
                    : ResolveCachedMethod(
                        triggers.GetType(),
                        "Add",
                        eventTriggerEntryType);
                if (callback == null || addListener == null || triggers == null || addTrigger == null)
                    return binder;
                addListener.Invoke(
                    callback,
                    new object[] { CreatePointerActionDelegate(binder) });
                addTrigger.Invoke(triggers, new[] { entry });
                RetainBinder(binder);
                bound = true;
                return binder;
            }
            catch (Exception error)
            {
                runtime.RuntimeMonitor.LogOnce(
                    "debug-console-pointer-relay-" + eventName,
                    "Debug console catalog pointer relay failed for " +
                    eventName + ": " + error.GetType().Name + ": " + error.Message,
                    LogLevel.Warn);
                return binder;
            }
        }

        private Delegate CreatePointerActionDelegate(PointerActionBinder binder)
        {
            if (baseEventDataType == null || unityActionBaseEventDataType == null)
                throw new InvalidOperationException("Unity BaseEventData action type is not available.");

            MethodInfo invoke = pointerInvokeClosedMethod
                ?? throw new MissingMethodException(
                    nameof(PointerActionBinder),
                    nameof(PointerActionBinder.InvokeTyped));
            return Delegate.CreateDelegate(
                unityActionBaseEventDataType,
                binder,
                invoke);
        }

        private void RetainBinder(object binder)
        {
            eventBinders.Add(binder);
            activeBinderSink?.Add(binder);
        }

        private void ReleaseOwnedBinders(List<object> binders)
        {
            foreach (object binder in binders)
                eventBinders.Remove(binder);
            binders.Clear();
        }

        private static bool IsRightClick(object? eventData)
        {
            object? value = eventData == null ? null : GetProperty(eventData, "button");
            if (value == null)
                return false;
            string? text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text) && text.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
            }
            catch
            {
                return false;
            }
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

        private void Destroy(object? go)
        {
            if (go == null || objectType == null)
                return;
            ResolveUiReflectionMetadata();
            destroyArguments[0] = go;
            unityObjectDestroyMethod?.Invoke(null, destroyArguments);
            destroyArguments[0] = null;
        }

        private void DontDestroyOnLoad(object go)
        {
            ResolveUiReflectionMetadata();
            dontDestroyArguments[0] = go;
            unityObjectDontDestroyOnLoadMethod?.Invoke(
                null,
                dontDestroyArguments);
            dontDestroyArguments[0] = null;
        }

        private bool SetActive(object? go, bool active)
        {
            if (go == null || IsDestroyed(go))
                return false;
            try
            {
                ResolveUiReflectionMetadata();
                if (gameObjectActiveSelfProperty?.GetValue(go, null) is
                        bool current &&
                    current == active)
                {
                    return true;
                }

                if (gameObjectSetActiveMethod == null)
                    return false;
                setActiveArguments[0] = active ? BoxedTrue : BoxedFalse;
                gameObjectSetActiveMethod.Invoke(go, setActiveArguments);
                setActiveArguments[0] = null;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsDestroyed(object go)
        {
            if (objectType == null || !objectType.IsAssignableFrom(go.GetType()))
                return false;
            ResolveUiReflectionMetadata();
            if (unityObjectEqualityMethod == null)
                return false;
            try
            {
                equalityArguments[0] = go;
                equalityArguments[1] = null;
                bool destroyed = unityObjectEqualityMethod.Invoke(
                        null,
                        equalityArguments) is bool result &&
                    result;
                equalityArguments[0] = null;
                return destroyed;
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

            ResolveUiReflectionMetadata();
            if (gameObjectActiveSelfProperty != null &&
                gameObjectType != null &&
                gameObjectType.IsAssignableFrom(value.GetType()) &&
                gameObjectActiveSelfProperty.GetValue(value, null) is bool active)
            {
                return active ? "alive-active" : "alive-inactive";
            }
            return "alive";
        }

        private int IsAlive(object? value)
        {
            return value != null && !IsDestroyed(value) ? 1 : 0;
        }

        private static string SanitizeMetricKey(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            var builder = new System.Text.StringBuilder(text.Length);
            bool previousUnderscore = false;
            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                {
                    builder.Append(ch);
                    previousUnderscore = false;
                    continue;
                }

                if (!previousUnderscore)
                {
                    builder.Append('_');
                    previousUnderscore = true;
                }
            }

            return builder.ToString().Trim('_');
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

        private void ResolveUiReflectionMetadata()
        {
            if (uiReflectionMetadataResolved ||
                gameObjectType == null ||
                transformType == null ||
                rectTransformType == null ||
                objectType == null)
            {
                return;
            }

            gameObjectRectConstructor = gameObjectType.GetConstructor(
                new[] { typeof(string), typeof(Type[]) });
            gameObjectNameConstructor = gameObjectType.GetConstructor(
                new[] { typeof(string) });
            gameObjectAddComponentMethod = gameObjectType.GetMethod(
                "AddComponent",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(Type) },
                null);
            gameObjectGetComponentByTypeMethod ??= gameObjectType.GetMethod(
                "GetComponent",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(Type) },
                null);
            gameObjectSetActiveMethod = gameObjectType.GetMethod(
                "SetActive",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(bool) },
                null);
            gameObjectTransformProperty = gameObjectType.GetProperty(
                "transform",
                BindingFlags.Public | BindingFlags.Instance);
            gameObjectActiveSelfProperty = gameObjectType.GetProperty(
                "activeSelf",
                BindingFlags.Public | BindingFlags.Instance);
            transformSetParentMethod = transformType.GetMethod(
                "SetParent",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { transformType, typeof(bool) },
                null);
            unityObjectDestroyMethod = objectType.GetMethod(
                "Destroy",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { objectType },
                null);
            unityObjectDontDestroyOnLoadMethod = objectType.GetMethod(
                "DontDestroyOnLoad",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { objectType },
                null);
            unityObjectEqualityMethod = objectType.GetMethod(
                "op_Equality",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { objectType, objectType },
                null);
            unityObjectFindObjectOfTypeMethod = objectType.GetMethod(
                "FindObjectOfType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Type) },
                null);
            unityObjectFindObjectsOfTypeMethod = objectType.GetMethod(
                "FindObjectsOfType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Type) },
                null);
            colorConstructor = colorType?.GetConstructor(
                new[] { typeof(float), typeof(float), typeof(float), typeof(float) });
            vector2Constructor = vector2Type?.GetConstructor(
                new[] { typeof(float), typeof(float) });
            rectTransformComponentTypes = new[] { rectTransformType };
            uiReflectionMetadataResolved = true;
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
            string text = (ex.Message ?? string.Empty) + " " + ex;
            return text.IndexOf("Input System not yet initialized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("InputSystem not yet initialized", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Exception UnwrapReflectionException(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        private static Type? ResolveRuntimeType(string typeName)
        {
            Type? type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type != null)
                return type;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static object? GetProperty(object target, string name)
        {
            Type type = target.GetType();
            MemberInfo? member = ResolveCachedMember(type, name);
            if (member is PropertyInfo property)
                return property.GetValue(target, null);
            return (member as FieldInfo)?.GetValue(target);
        }

        private static void SetProperty(object target, string name, object? value)
        {
            Type type = target.GetType();
            MemberInfo? member = ResolveCachedMember(type, name);
            if (member is PropertyInfo property)
            {
                property.SetValue(target, value, null);
                return;
            }

            (member as FieldInfo)?.SetValue(target, value);
        }

        private static void SetEnumProperty(object target, string name, int value)
        {
            PropertyInfo? property =
                ResolveCachedMember(target.GetType(), name) as PropertyInfo;
            if (property == null)
                return;
            property.SetValue(target, Enum.ToObject(property.PropertyType, value), null);
        }

        private static MemberInfo? ResolveCachedMember(
            Type type,
            string name)
        {
            var key = new UiMemberKey(type, name);
            if (UiMemberCache.TryGetValue(key, out MemberInfo? member))
                return member;

            const BindingFlags Flags = BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;
            member = type.GetProperty(name, Flags) ??
                (MemberInfo?)type.GetField(name, Flags);
            if (member != null)
                UiMemberCache.TryAdd(key, member);
            return member;
        }

        private static MethodInfo? ResolveCachedMethod(
            Type type,
            string name,
            Type parameterType)
        {
            var key = new UiMethodKey(type, name, parameterType);
            if (UiMethodCache.TryGetValue(key, out MethodInfo? method))
                return method;

            method = type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance,
                null,
                new[] { parameterType },
                null);
            if (method != null)
                UiMethodCache.TryAdd(key, method);
            return method;
        }

        private object GetBuiltinFont()
        {
            builtInFont ??= getBuiltinResourceMethod?.Invoke(
                null,
                new object[] { fontType!, "Arial.ttf" });
            return builtInFont ??
                throw new InvalidOperationException(
                    "Unity built-in font Arial.ttf was not found.");
        }

        private object Color(float r, float g, float b, float a)
        {
            if (r == 1f && g == 1f && b == 1f && a == 1f)
                return colorWhite ??= CreateColorValue(r, g, b, a);
            if (r == 0.70f && g == 0.78f && b == 0.80f && a == 1f)
                return colorMuted ??= CreateColorValue(r, g, b, a);
            if (r == 0.55f && g == 0.60f && b == 0.62f && a == 1f)
                return colorDisabled ??= CreateColorValue(r, g, b, a);
            if (r == 1f && g == 0.72f && b == 0.55f && a == 1f)
                return colorWarning ??= CreateColorValue(r, g, b, a);
            if (r == 0.90f && g == 0.96f && b == 0.96f && a == 1f)
                return colorPrimaryText ??= CreateColorValue(r, g, b, a);
            if (r == 0.13f && g == 0.15f && b == 0.17f && a == 1f)
                return colorButton ??= CreateColorValue(r, g, b, a);
            if (r == 0.10f && g == 0.36f && b == 0.34f && a == 1f)
                return colorSelected ??= CreateColorValue(r, g, b, a);
            return CreateColorValue(r, g, b, a);
        }

        private object CreateColorValue(float r, float g, float b, float a)
        {
            ResolveUiReflectionMetadata();
            if (colorConstructor == null)
                return Activator.CreateInstance(colorType!, r, g, b, a) ??
                    throw new InvalidOperationException(
                        "Unity Color constructor returned null.");
            colorArguments[0] = r;
            colorArguments[1] = g;
            colorArguments[2] = b;
            colorArguments[3] = a;
            return colorConstructor.Invoke(colorArguments);
        }

        private object Vector2(float x, float y)
        {
            if (x == 0f && y == 0f)
                return vectorZero ??= CreateVector2Value(x, y);
            if (x == 1f && y == 1f)
                return vectorOne ??= CreateVector2Value(x, y);
            if (x == 0f && y == 1f)
                return vectorTopLeft ??= CreateVector2Value(x, y);
            if (x == 1f && y == 0f)
                return vectorBottomRight ??= CreateVector2Value(x, y);
            if (x == 0.5f && y == 0.5f)
                return vectorCenter ??= CreateVector2Value(x, y);
            return CreateVector2Value(x, y);
        }

        private object CreateVector2Value(float x, float y)
        {
            ResolveUiReflectionMetadata();
            if (vector2Constructor == null)
                return Activator.CreateInstance(vector2Type!, x, y) ??
                    throw new InvalidOperationException(
                        "Unity Vector2 constructor returned null.");
            vector2Arguments[0] = x;
            vector2Arguments[1] = y;
            return vector2Constructor.Invoke(vector2Arguments);
        }
        private string T(string key, string fallback) => text.Get(key, fallback);

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private static string FirstText(string value1, string value2) =>
            FirstTextCore(value1) ??
            FirstTextCore(value2) ??
            string.Empty;

        private static string FirstText(
            string value1,
            string value2,
            string value3) =>
            FirstTextCore(value1) ??
            FirstTextCore(value2) ??
            FirstTextCore(value3) ??
            string.Empty;

        private static string FirstText(
            string value1,
            string value2,
            string value3,
            string value4) =>
            FirstTextCore(value1) ??
            FirstTextCore(value2) ??
            FirstTextCore(value3) ??
            FirstTextCore(value4) ??
            string.Empty;

        private static string FirstText(
            string value1,
            string value2,
            string value3,
            string value4,
            string value5) =>
            FirstTextCore(value1) ??
            FirstTextCore(value2) ??
            FirstTextCore(value3) ??
            FirstTextCore(value4) ??
            FirstTextCore(value5) ??
            string.Empty;

        private static string? FirstTextCore(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string BreadcrumbValue(string? value, int max)
        {
            value ??= string.Empty;
            string normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (normalized.Length == 0)
                return "-";
            return normalized.Length <= max ? normalized : normalized.Substring(0, Math.Max(0, max - 14)) + "...[truncated]";
        }

        private static string Truncate(string value, int max)
        {
            value ??= string.Empty;
            return value.Length <= max ? value : value.Substring(0, Math.Max(0, max - 1)) + "...";
        }

        private static string FormatLifecycleValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "<empty>" : value;
        }

        private const int TextAnchorMiddleLeft = 3;
        private const int TextAnchorMiddleCenter = 4;
        private const int TextAnchorUpperLeft = 0;

        private readonly struct UiMemberKey : IEquatable<UiMemberKey>
        {
            internal UiMemberKey(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            private Type Type { get; }
            private string Name { get; }

            public bool Equals(UiMemberKey other) =>
                ReferenceEquals(Type, other.Type) &&
                string.Equals(Name, other.Name, StringComparison.Ordinal);

            public override bool Equals(object? obj) =>
                obj is UiMemberKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Type.GetHashCode() * 397) ^
                        StringComparer.Ordinal.GetHashCode(Name);
                }
            }
        }

        private readonly struct UiMethodKey : IEquatable<UiMethodKey>
        {
            internal UiMethodKey(
                Type type,
                string name,
                Type parameterType)
            {
                Type = type;
                Name = name;
                ParameterType = parameterType;
            }

            private Type Type { get; }
            private string Name { get; }
            private Type ParameterType { get; }

            public bool Equals(UiMethodKey other) =>
                ReferenceEquals(Type, other.Type) &&
                ReferenceEquals(ParameterType, other.ParameterType) &&
                string.Equals(Name, other.Name, StringComparison.Ordinal);

            public override bool Equals(object? obj) =>
                obj is UiMethodKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (Type.GetHashCode() * 397) ^
                        ParameterType.GetHashCode();
                    return (hash * 397) ^
                        StringComparer.Ordinal.GetHashCode(Name);
                }
            }
        }

        private sealed class ActionBinder
        {
            private Action action;
            public ActionBinder(Action action) { this.action = action; }
            public void Invoke() => action();
            public void Bind(Action next) => action = next;
        }

        private sealed class StringActionBinder
        {
            private readonly Action<string> action;
            public StringActionBinder(Action<string> action) { this.action = action; }
            public void Invoke(string value) => action(value);
        }

        private sealed class PointerActionBinder
        {
            private Action<object?> action;
            public PointerActionBinder(Action<object?> action) { this.action = action; }
            public void Invoke(object? value) => action(value);
            public void InvokeTyped<T>(T value) => action(value);
            public void Bind(Action<object?> next) => action = next;
        }

        private sealed class CatalogCell
        {
            internal CatalogCell(
                DebugConsoleUi owner,
                object root,
                object background,
                ActionBinder click,
                object iconRoot,
                object iconImage,
                object fallbackText,
                object? fallbackRoot,
                object labelText,
                object? labelRoot,
                object sourceText,
                object? sourceRoot,
                object statusText,
                object? statusRoot,
                PointerActionBinder pointerDown,
                PointerActionBinder pointerEnter,
                PointerActionBinder pointerExit)
            {
                this.owner = owner;
                Root = root;
                Background = background;
                Click = click;
                IconRoot = iconRoot;
                IconImage = iconImage;
                FallbackText = fallbackText;
                FallbackRoot = fallbackRoot;
                LabelText = labelText;
                LabelRoot = labelRoot;
                SourceText = sourceText;
                SourceRoot = sourceRoot;
                StatusText = statusText;
                StatusRoot = statusRoot;
                PointerDown = pointerDown;
                PointerEnter = pointerEnter;
                PointerExit = pointerExit;
                Click.Bind(HandleClick);
                PointerDown.Bind(HandlePointerDown);
                PointerEnter.Bind(HandlePointerEnter);
                PointerExit.Bind(HandlePointerExit);
            }

            private readonly DebugConsoleUi owner;
            private CatalogCellKind kind;
            private InventoryDebugItem? item;
            private SpawnCatalogOption? monster;
            private AnimalCatalogOption? animal;
            private string hover = string.Empty;
            private float tooltipX;
            private float tooltipY;

            internal object Root { get; }
            internal object Background { get; }
            internal ActionBinder Click { get; }
            internal object IconRoot { get; }
            internal object IconImage { get; }
            internal object FallbackText { get; }
            internal object? FallbackRoot { get; }
            internal object LabelText { get; }
            internal object? LabelRoot { get; }
            internal object SourceText { get; }
            internal object? SourceRoot { get; }
            internal object StatusText { get; }
            internal object? StatusRoot { get; }
            internal PointerActionBinder PointerDown { get; }
            internal PointerActionBinder PointerEnter { get; }
            internal PointerActionBinder PointerExit { get; }
            internal object? LastSprite { get; set; }
            internal object? LastBackground { get; set; }
            internal object? LastTextColor { get; set; }
            internal string LastFallback { get; set; } = "-";
            internal string LastLabel { get; set; } = string.Empty;
            internal string LastSource { get; set; } = string.Empty;
            internal string LastStatus { get; set; } = string.Empty;
            internal bool LayoutAssigned { get; set; }
            internal float LastX { get; set; }
            internal float LastY { get; set; }
            internal float LastSide { get; set; }

            internal void BindItem(
                InventoryDebugItem next,
                string nextHover,
                float nextTooltipX,
                float nextTooltipY)
            {
                ClearContext();
                kind = CatalogCellKind.Item;
                item = next;
                hover = nextHover;
                tooltipX = nextTooltipX;
                tooltipY = nextTooltipY;
            }

            internal void BindMonster(
                SpawnCatalogOption next,
                string nextHover)
            {
                ClearContext();
                kind = CatalogCellKind.Monster;
                monster = next;
                hover = nextHover;
            }

            internal void BindAnimal(
                AnimalCatalogOption next,
                string nextHover)
            {
                ClearContext();
                kind = CatalogCellKind.Animal;
                animal = next;
                hover = nextHover;
            }

            internal void ClearContext()
            {
                kind = CatalogCellKind.None;
                item = null;
                monster = null;
                animal = null;
                hover = string.Empty;
                tooltipX = 0f;
                tooltipY = 0f;
            }

            private void HandleClick()
            {
                if (kind == CatalogCellKind.Item && item != null)
                {
                    if (item.CanGive)
                        owner.GiveItem(item, 1);
                    else
                        owner.SetStatusMessage(hover, false);
                }
                else if (kind == CatalogCellKind.Monster && monster != null)
                    owner.SpawnMonster(monster, 1);
                else if (kind == CatalogCellKind.Animal && animal != null)
                    owner.SpawnAnimal(animal, 1);
            }

            private void HandlePointerDown(object? eventData)
            {
                if (!IsRightClick(eventData))
                    return;
                if (kind == CatalogCellKind.Item && item != null)
                {
                    if (item.CanGive)
                    {
                        owner.TryGiveRightClickItem(
                            item,
                            "pointer-down");
                    }
                    else
                        owner.SetStatusMessage(hover, false);
                }
                else if (kind == CatalogCellKind.Monster && monster != null)
                    owner.SpawnMonster(monster, 10);
                else if (kind == CatalogCellKind.Animal && animal != null)
                    owner.SpawnAnimal(animal, 10);
            }

            private void HandlePointerEnter(object? eventData)
            {
                if (kind == CatalogCellKind.None)
                    return;
                owner.SetStatusMessage(hover, false);
                if (kind == CatalogCellKind.Item && item != null)
                    owner.ShowItemTooltip(item, tooltipX, tooltipY);
            }

            private void HandlePointerExit(object? eventData)
            {
                if (owner.statusMessage.Equals(
                        hover,
                        StringComparison.Ordinal))
                {
                    owner.SetStatusMessage(string.Empty, false);
                }
                if (kind == CatalogCellKind.Item)
                    owner.HideItemTooltip();
            }

            private enum CatalogCellKind
            {
                None,
                Item,
                Monster,
                Animal
            }
        }
    }
}
