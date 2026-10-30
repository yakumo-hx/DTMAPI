// DTMAPI-authored signature fixture. No game implementations or assets.
using System;
using System.Runtime.CompilerServices;
namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object? value) => value != null && !value.Destroyed;
    }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } }
    public class Transform : Object
    {
        private Vector3 value = new Vector3(1, 1, 1);
        public bool FailWrite;
        public int Writes;
        public Vector3 localScale { get => value; set { if (FailWrite) throw new InvalidOperationException("scale write failure"); this.value = value; ++Writes; } }
        public Vector3 localPosition;
    }
    public class Camera : Object
    {
        private float size = 10;
        public bool FailWrite, FailRead;
        public int Writes;
        public float orthographicSize { get { if (FailRead) throw new InvalidOperationException("camera read failure"); return size; } set { if (FailWrite) throw new InvalidOperationException("camera write failure"); size = value; ++Writes; } }
        public float aspect { get; set; } = 16f / 9f;
    }
    public class Sprite : Object { }
    public class SpriteRenderer : Object
    {
        public Sprite sprite { get; set; } = new Sprite();
        public Transform transform { get; } = new Transform();
        public object material = new object();
        public int color = 42;
        public bool enabled = true;
    }
}
namespace DolocTown.GameData { public class EnvBackgroundSO : UnityEngine.Object { } }
namespace DolocTown
{
    public class CameraController : UnityEngine.Object
    {
        public Action? DuringRefresh;
        public float Observed;
        public int Calls;
        [MethodImpl(MethodImplOptions.NoInlining)] public void RefreshResolution() { ++Calls; Observed = DolocAPI.mainCamera.orthographicSize; DuringRefresh?.Invoke(); }
    }
    public class BackgroundLayerRenderer : UnityEngine.Object { public UnityEngine.SpriteRenderer SpriteRenderer { get; } = new UnityEngine.SpriteRenderer(); }
    public class BackgroundRenderer : UnityEngine.Object
    {
        private BackgroundLayerRenderer skyRenderer = new BackgroundLayerRenderer();
        private BackgroundLayerRenderer[] _layers = { new BackgroundLayerRenderer() };
        public GameData.EnvBackgroundSO CurrentPreset { get; private set; } = new GameData.EnvBackgroundSO();
        public BackgroundLayerRenderer Sky => skyRenderer;
        public BackgroundLayerRenderer Layer => _layers[0];
        [MethodImpl(MethodImplOptions.NoInlining)] public void LoadPreset(GameData.EnvBackgroundSO bg) { CurrentPreset = bg; }
    }
    public class DepthFogController : UnityEngine.Object
    {
        private UnityEngine.SpriteRenderer[] fogRenderers = { new UnityEngine.SpriteRenderer(), new UnityEngine.SpriteRenderer() };
        public UnityEngine.SpriteRenderer[] Sprites => fogRenderers;
        [MethodImpl(MethodImplOptions.NoInlining)] protected void __Init() { }
    }
    public class DepthFogControllerBuilding : UnityEngine.Object
    {
        private UnityEngine.SpriteRenderer skyRenderer = new UnityEngine.SpriteRenderer();
        private UnityEngine.SpriteRenderer remoteRenderer = new UnityEngine.SpriteRenderer();
        public UnityEngine.SpriteRenderer Sky => skyRenderer;
        public UnityEngine.SpriteRenderer Remote => remoteRenderer;
        [MethodImpl(MethodImplOptions.NoInlining)] protected void __Init() { }
    }
    public class EnvCovariantController
    {
        private readonly DepthFogController _depthFogController = new DepthFogController();
        private readonly DepthFogControllerBuilding _depthFogControllerBuilding = new DepthFogControllerBuilding();
        public DepthFogController Outdoor => _depthFogController;
        public DepthFogControllerBuilding Indoor => _depthFogControllerBuilding;
    }
}
public static class DolocAPI
{
    public static UnityEngine.Camera mainCamera { get; set; } = new UnityEngine.Camera();
    public static DolocTown.CameraController cameraController { get; set; } = new DolocTown.CameraController();
    public static DolocTown.BackgroundRenderer envBackgroundEx { get; set; } = new DolocTown.BackgroundRenderer();
    public static DolocTown.EnvCovariantController EnvCovariantController { get; set; } = new DolocTown.EnvCovariantController();
    [MethodImpl(MethodImplOptions.NoInlining)] public static void SetEnvCamera(UnityEngine.Vector2 a, UnityEngine.Vector2 b, bool c, bool d, bool e) { }
}
