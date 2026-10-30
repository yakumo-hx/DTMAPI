using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public interface IFixtureUnityCloneable
    {
        Object Clone(Transform parent);
    }

    public class Object
    {
        public bool IsDestroyed { get; private set; }

        public static Object Instantiate(
            Object original,
            Transform parent)
        {
            if (original is IFixtureUnityCloneable cloneable)
                return cloneable.Clone(parent);
            throw new InvalidOperationException(
                "Fixture Unity object is not cloneable.");
        }

        public static void Destroy(Object value)
        {
            if (value == null)
                return;
            value.IsDestroyed = true;
            if (value is GameObject gameObject)
                gameObject.transform.SetParent(null, false);
        }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; } = null!;

        public Transform transform =>
            this as Transform ?? gameObject.transform;
    }

    public sealed class GameObject : Object
    {
        private readonly List<Component> components =
            new List<Component>();

        public GameObject(string name, params Type[] componentTypes)
        {
            this.name = name ?? string.Empty;
            Type transformType = typeof(RectTransform);
            foreach (Type type in componentTypes ?? Array.Empty<Type>())
            {
                if (typeof(Transform).IsAssignableFrom(type))
                {
                    transformType = type;
                    break;
                }
            }
            var createdTransform =
                (Transform)(Activator.CreateInstance(transformType) ??
                throw new InvalidOperationException(
                    "Fixture transform creation failed."));
            Attach(createdTransform);
            transform = createdTransform;
            foreach (Type type in componentTypes ?? Array.Empty<Type>())
            {
                if (typeof(Transform).IsAssignableFrom(type))
                    continue;
                AddComponent(type);
            }
        }

        public string name { get; set; }

        public bool activeSelf { get; private set; } = true;

        public Transform transform { get; }

        public void SetActive(bool value) => activeSelf = value;

        public Component? GetComponent(Type type)
        {
            foreach (Component component in components)
            {
                if (type.IsAssignableFrom(component.GetType()))
                    return component;
            }
            return null;
        }

        public Component AddComponent(Type type)
        {
            var component =
                (Component)(Activator.CreateInstance(type) ??
                throw new InvalidOperationException(
                    "Fixture component creation failed: " +
                    type.FullName));
            Attach(component);
            return component;
        }

        public Component[] GetComponentsInChildren(
            Type type,
            bool includeInactive)
        {
            var result = new List<Component>();
            CollectComponents(
                transform,
                type,
                includeInactive,
                result);
            return result.ToArray();
        }

        public void Attach(Component component)
        {
            component.gameObject = this;
            components.Add(component);
        }

        private static void CollectComponents(
            Transform current,
            Type type,
            bool includeInactive,
            ICollection<Component> result)
        {
            if (includeInactive || current.gameObject.activeSelf)
            {
                Component? component =
                    current.gameObject.GetComponent(type);
                if (component != null)
                    result.Add(component);
                for (int index = 0;
                     index < current.childCount;
                     index++)
                {
                    CollectComponents(
                        current.GetChild(index),
                        type,
                        includeInactive,
                        result);
                }
            }
        }
    }

    public class Transform : Component
    {
        private readonly List<Transform> children =
            new List<Transform>();
        private Transform? currentParent;

        public Transform? parent => currentParent;

        public int childCount => children.Count;

        public Transform GetChild(int index) => children[index];

        public void SetParent(
            Transform? value,
            bool worldPositionStays)
        {
            currentParent?.children.Remove(this);
            currentParent = value;
            value?.children.Add(this);
        }

        public void SetParent(Transform? value) =>
            SetParent(value, false);

        public void SetAsLastSibling()
        {
            if (currentParent == null)
                return;
            currentParent.children.Remove(this);
            currentParent.children.Add(this);
        }
    }

    public sealed class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; } =
            new Vector2(0.5f, 0.5f);

        public Vector2 anchorMax { get; set; } =
            new Vector2(0.5f, 0.5f);

        public Vector2 anchoredPosition { get; set; }

        public Vector2 sizeDelta { get; set; }

        public Vector2 pivot { get; set; } =
            new Vector2(0.5f, 0.5f);

        public Vector2 offsetMin { get; set; }

        public Vector2 offsetMax { get; set; }

        public Rect rect
        {
            get
            {
                float parentWidth =
                    parent is RectTransform parentRect
                        ? parentRect.rect.width
                        : 0f;
                float parentHeight =
                    parent is RectTransform parentRect2
                        ? parentRect2.rect.height
                        : 0f;
                return new Rect(
                    sizeDelta.x +
                        parentWidth *
                        (anchorMax.x - anchorMin.x),
                    sizeDelta.y +
                        parentHeight *
                        (anchorMax.y - anchorMin.y));
            }
        }

        public void GetWorldCorners(Vector3[] corners)
        {
            if (corners == null || corners.Length < 4)
                throw new ArgumentException(nameof(corners));
            GetWorldBounds(
                out float minX,
                out float minY,
                out float maxX,
                out float maxY);
            corners[0] = new Vector3(minX, minY, 0f);
            corners[1] = new Vector3(minX, maxY, 0f);
            corners[2] = new Vector3(maxX, maxY, 0f);
            corners[3] = new Vector3(maxX, minY, 0f);
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
            GetWorldBounds(
                out float minX,
                out float minY,
                out float maxX,
                out float maxY);
            float width = Math.Max(0.0001f, rect.width);
            float height = Math.Max(0.0001f, rect.height);
            float scaleX = (maxX - minX) / width;
            float scaleY = (maxY - minY) / height;
            float worldOriginX =
                minX + pivot.x * (maxX - minX);
            float worldOriginY =
                minY + pivot.y * (maxY - minY);
            return new Vector3(
                (point.x - worldOriginX) / scaleX,
                (point.y - worldOriginY) / scaleY,
                point.z);
        }

        private void GetWorldBounds(
            out float minX,
            out float minY,
            out float maxX,
            out float maxY)
        {
            float parentMinX = 0f;
            float parentMinY = 0f;
            float parentMaxX = 0f;
            float parentMaxY = 0f;
            if (parent is RectTransform parentRect)
            {
                parentRect.GetWorldBounds(
                    out parentMinX,
                    out parentMinY,
                    out parentMaxX,
                    out parentMaxY);
            }
            float parentWidth = parentMaxX - parentMinX;
            float parentHeight = parentMaxY - parentMinY;
            float anchorX =
                parentMinX +
                parentWidth *
                ((anchorMin.x + anchorMax.x) / 2f);
            float anchorY =
                parentMinY +
                parentHeight *
                ((anchorMin.y + anchorMax.y) / 2f);
            float width = rect.width;
            float height = rect.height;
            float pivotX = anchorX + anchoredPosition.x;
            float pivotY = anchorY + anchoredPosition.y;
            minX = pivotX - pivot.x * width;
            minY = pivotY - pivot.y * height;
            maxX = minX + width;
            maxY = minY + height;
        }
    }

    public readonly struct Vector2
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x { get; }

        public float y { get; }
    }

    public readonly struct Vector3
    {
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x { get; }

        public float y { get; }

        public float z { get; }
    }

    public readonly struct Rect
    {
        public Rect(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public float width { get; }

        public float height { get; }
    }

    public readonly struct Color
    {
        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public float r { get; }

        public float g { get; }

        public float b { get; }

        public float a { get; }
    }

    public sealed class Sprite : Object
    {
    }

    public sealed class CanvasRenderer : Component
    {
    }

    public sealed class CanvasGroup : Component
    {
        public float alpha { get; set; }

        public bool interactable { get; set; }

        public bool blocksRaycasts { get; set; }
    }

    public static class Canvas
    {
        public static int ForceUpdateCount { get; private set; }

        public static void ForceUpdateCanvases() =>
            ForceUpdateCount++;
    }
}

namespace UnityEngine.UI
{
    public struct Navigation
    {
        public enum Mode
        {
            None = 0,
            Automatic = 3
        }

        public Mode mode { get; set; }
    }

    public class Selectable : UnityEngine.Component
    {
        public bool interactable { get; set; } = true;

        public Navigation navigation { get; set; } =
            new Navigation
            {
                mode = Navigation.Mode.Automatic
            };
    }

    public class Graphic : UnityEngine.Component
    {
        public bool raycastTarget { get; set; } = true;
    }

    public sealed class Image : Graphic
    {
        public UnityEngine.Color color { get; set; }
    }

    public sealed class LayoutElement : UnityEngine.Component
    {
        public bool ignoreLayout { get; set; }

        public float preferredWidth { get; set; }

        public float preferredHeight { get; set; }
    }

    public sealed class FixtureUnityEvent
    {
        private readonly List<Action> listeners =
            new List<Action>();

        public int ListenerCount => listeners.Count;

        public void AddListener(Action listener) =>
            listeners.Add(listener);

        public void RemoveAllListeners() => listeners.Clear();

        public void Invoke()
        {
            foreach (Action listener in listeners.ToArray())
                listener();
        }
    }

    public sealed class Button : Selectable
    {
        public FixtureUnityEvent onClick { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onSelect { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onDeselect { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onPointerEnter { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onPointerExit { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onPointerDown { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onPointerUp { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onLeftClick { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onLeftLongClick { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onRightClick { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onRightLongClick { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onAssistLeftClick { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onAssistRightClick { get; } =
            new FixtureUnityEvent();
        public FixtureUnityEvent onMove { get; } =
            new FixtureUnityEvent();

        public Func<bool>? onLeftContinuesClick { get; set; }

        public Func<bool>? onRightContinuesClick { get; set; }

        public void Select() => onSelect.Invoke();
    }
}

namespace DolocTown.UI
{
    public sealed class AccessorySlot :
        UnityEngine.Component,
        UnityEngine.IFixtureUnityCloneable
    {
        public static int CloneCount { get; private set; }

        public readonly UnityEngine.UI.FixtureUnityEvent onClick =
            new UnityEngine.UI.FixtureUnityEvent();
        public readonly UnityEngine.UI.FixtureUnityEvent onSelect =
            new UnityEngine.UI.FixtureUnityEvent();
        public readonly UnityEngine.UI.FixtureUnityEvent onDeselect =
            new UnityEngine.UI.FixtureUnityEvent();
        public readonly UnityEngine.UI.FixtureUnityEvent onPointerEnter =
            new UnityEngine.UI.FixtureUnityEvent();
        public readonly UnityEngine.UI.FixtureUnityEvent onPointerExit =
            new UnityEngine.UI.FixtureUnityEvent();
        public readonly UnityEngine.UI.FixtureUnityEvent onMove =
            new UnityEngine.UI.FixtureUnityEvent();

        public UnityEngine.UI.Button button { get; private set; } = null!;

        private UnityEngine.RectTransform? runtimeRectTransform;

        public UnityEngine.RectTransform rectTransform
        {
            get => runtimeRectTransform!;
            private set => runtimeRectTransform = value;
        }

        public int index { get; set; }

        public UnityEngine.Sprite? RenderedSprite { get; private set; }

        public string LastHint { get; private set; } = string.Empty;

        public bool ClickCallbacksCleared { get; private set; }

        public int TotalSlotListenerCount =>
            onClick.ListenerCount +
            onSelect.ListenerCount +
            onDeselect.ListenerCount +
            onPointerEnter.ListenerCount +
            onPointerExit.ListenerCount +
            onMove.ListenerCount;

        public int TotalButtonListenerCount =>
            button.onClick.ListenerCount +
            button.onSelect.ListenerCount +
            button.onDeselect.ListenerCount +
            button.onPointerEnter.ListenerCount +
            button.onPointerExit.ListenerCount +
            button.onPointerDown.ListenerCount +
            button.onPointerUp.ListenerCount +
            button.onLeftClick.ListenerCount +
            button.onLeftLongClick.ListenerCount +
            button.onRightClick.ListenerCount +
            button.onRightLongClick.ListenerCount +
            button.onAssistLeftClick.ListenerCount +
            button.onAssistRightClick.ListenerCount +
            button.onMove.ListenerCount;

        public static AccessorySlot Create(
            UnityEngine.Transform parent,
            string name,
            float x,
            float y)
        {
            var gameObject = new UnityEngine.GameObject(
                name,
                typeof(UnityEngine.RectTransform));
            gameObject.transform.SetParent(parent, false);
            var slot = new AccessorySlot();
            gameObject.Attach(slot);
            slot.rectTransform =
                (UnityEngine.RectTransform)gameObject.transform;
            slot.button =
                (UnityEngine.UI.Button)gameObject.AddComponent(
                    typeof(UnityEngine.UI.Button));
            UnityEngine.RectTransform rect = slot.rectTransform;
            rect.anchorMin = new UnityEngine.Vector2(0f, 0f);
            rect.anchorMax = new UnityEngine.Vector2(0f, 0f);
            rect.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
            rect.sizeDelta = new UnityEngine.Vector2(112f, 112f);
            rect.anchoredPosition = new UnityEngine.Vector2(x, y);
            slot.SeedInheritedListeners();
            return slot;
        }

        public UnityEngine.Object Clone(
            UnityEngine.Transform parent)
        {
            CloneCount++;
            AccessorySlot clone = Create(
                parent,
                gameObject.name + " clone",
                rectTransform.anchoredPosition.x,
                rectTransform.anchoredPosition.y);
            clone.runtimeRectTransform = null;
            return clone;
        }

        public void Render(UnityEngine.Sprite? sprite) =>
            RenderedSprite = sprite;

        public void ShowEquipmentItemViewer(
            object? item,
            string text)
        {
            if (rectTransform == null)
            {
                throw new InvalidOperationException(
                    "fixture cloned AccessorySlot runtime RectTransform was not initialized");
            }
            LastHint = text ?? string.Empty;
        }

        public void Select() => button.Select();

        public void ClearAllClickCallbacks() =>
            ClickCallbacksCleared = true;

        private void SeedInheritedListeners()
        {
            Action inherited = () => { };
            onClick.AddListener(inherited);
            onSelect.AddListener(inherited);
            onDeselect.AddListener(inherited);
            onPointerEnter.AddListener(inherited);
            onPointerExit.AddListener(inherited);
            onMove.AddListener(inherited);
            button.onClick.AddListener(inherited);
            button.onSelect.AddListener(inherited);
            button.onDeselect.AddListener(inherited);
            button.onPointerEnter.AddListener(inherited);
            button.onPointerExit.AddListener(inherited);
            button.onPointerDown.AddListener(inherited);
            button.onPointerUp.AddListener(inherited);
            button.onLeftClick.AddListener(inherited);
            button.onLeftLongClick.AddListener(inherited);
            button.onRightClick.AddListener(inherited);
            button.onRightLongClick.AddListener(inherited);
            button.onAssistLeftClick.AddListener(inherited);
            button.onAssistRightClick.AddListener(inherited);
            button.onMove.AddListener(inherited);
            button.onLeftContinuesClick = () => true;
            button.onRightContinuesClick = () => true;
        }
    }

    public sealed class EquipmentBarPanel : UnityEngine.Component
    {
        public int RebuildNavigationCount { get; private set; }

        public void RebuildNavigation() =>
            RebuildNavigationCount++;
    }

    public sealed class AccessoriesBar : UnityEngine.Component
    {
        private readonly List<AccessorySlot> passiveSlots =
            new List<AccessorySlot>();
        private readonly UnityEngine.RectTransform accessoriesRoot;

        public AccessoriesBar()
        {
            var panelObject = new UnityEngine.GameObject(
                "equipment_panel",
                typeof(UnityEngine.RectTransform),
                typeof(EquipmentBarPanel));
            ((UnityEngine.RectTransform)panelObject.transform).sizeDelta =
                new UnityEngine.Vector2(1400f, 900f);
            var widgetObject = new UnityEngine.GameObject(
                "EquipmentBarWidget",
                typeof(UnityEngine.RectTransform));
            widgetObject.transform.SetParent(
                panelObject.transform,
                false);
            ((UnityEngine.RectTransform)widgetObject.transform).sizeDelta =
                new UnityEngine.Vector2(1200f, 700f);

            var characterObject = new UnityEngine.GameObject(
                "charactar_panel",
                typeof(UnityEngine.RectTransform));
            characterObject.transform.SetParent(
                widgetObject.transform,
                false);
            ((UnityEngine.RectTransform)characterObject.transform).sizeDelta =
                new UnityEngine.Vector2(606f, 364f);

            var accessoriesObject = new UnityEngine.GameObject(
                "accessories",
                typeof(UnityEngine.RectTransform));
            accessoriesObject.transform.SetParent(
                characterObject.transform,
                false);
            accessoriesRoot =
                (UnityEngine.RectTransform)accessoriesObject.transform;
            accessoriesRoot.anchorMin =
                new UnityEngine.Vector2(0f, 0f);
            accessoriesRoot.anchorMax =
                new UnityEngine.Vector2(0f, 0f);
            accessoriesRoot.pivot =
                new UnityEngine.Vector2(0f, 0f);
            accessoriesRoot.sizeDelta =
                new UnityEngine.Vector2(606f, 112f);
            accessoriesObject.Attach(this);

            hatItem = AccessorySlot.Create(
                accessoriesRoot,
                "hat",
                56f,
                56f);
            positiveItem = AccessorySlot.Create(
                accessoriesRoot,
                "positive",
                180f,
                56f);

            var droneObject = new UnityEngine.GameObject(
                "drone_panel",
                typeof(UnityEngine.RectTransform));
            droneObject.transform.SetParent(
                widgetObject.transform,
                false);
            UnityEngine.RectTransform droneRect =
                (UnityEngine.RectTransform)droneObject.transform;
            droneRect.sizeDelta =
                new UnityEngine.Vector2(580f, 236f);
            droneRect.anchoredPosition =
                new UnityEngine.Vector2(700f, 200f);
            var nativeControl = new UnityEngine.GameObject(
                "native_drone_control",
                typeof(UnityEngine.RectTransform),
                typeof(UnityEngine.UI.Button),
                typeof(UnityEngine.UI.Image));
            nativeControl.transform.SetParent(
                droneObject.transform,
                false);
            DroneSelectable =
                (UnityEngine.UI.Button)(
                    nativeControl.GetComponent(
                        typeof(UnityEngine.UI.Button)) ??
                    throw new InvalidOperationException());
            DroneGraphic =
                (UnityEngine.UI.Image)(
                    nativeControl.GetComponent(
                        typeof(UnityEngine.UI.Image)) ??
                    throw new InvalidOperationException());
            Panel =
                (EquipmentBarPanel)(
                    panelObject.GetComponent(
                        typeof(EquipmentBarPanel)) ??
                    throw new InvalidOperationException());
        }

        public AccessorySlot hatItem { get; }

        public AccessorySlot positiveItem { get; }

        public UnityEngine.UI.Button DroneSelectable { get; }

        public UnityEngine.UI.Image DroneGraphic { get; }

        public EquipmentBarPanel Panel { get; }

        public UnityEngine.UI.Selectable[] allSelectablesArray
        {
            get
            {
                var result = new List<UnityEngine.UI.Selectable>
                {
                    hatItem.button,
                    positiveItem.button
                };
                foreach (AccessorySlot slot in passiveSlots)
                    result.Add(slot.button);
                return result.ToArray();
            }
        }

        public void __Init()
        {
        }

        public void OnStartShow()
        {
        }

        public AccessorySlot GetPassiveSlotByIndex(int index) =>
            passiveSlots[index];

#if !DTMAPI_LEGACY_237
        public void RenderPassiveItems(UnityEngine.Sprite[] icons)
        {
            while (passiveSlots.Count < icons.Length)
            {
                int index = passiveSlots.Count;
                passiveSlots.Add(
                    AccessorySlot.Create(
                        accessoriesRoot,
                        "passive-" + index,
                        304f + index * 124f,
                        56f));
            }
            for (int index = 0;
                 index < passiveSlots.Count;
                 index++)
            {
                passiveSlots[index].gameObject.SetActive(
                    index < icons.Length);
                if (index < icons.Length)
                    passiveSlots[index].Render(icons[index]);
            }
        }
#endif

        public void ClearCallBack()
        {
        }
    }
}
