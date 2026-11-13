using System;
using System.Reflection;
using UnityEngine;
using Yuuka.DTMAPI.AutoFishing;

namespace DTMAPI.UnitTests
{
    internal static class MotionRecoveryTests
    {
        internal static void RunAll()
        {
            var motion = new FishingMotion();
            var actor = new Animator { speed = 3, FailNextWrite = true };
            var rod = new Animator { speed = 6 };
            var hat = new Animator { speed = 9 };
            var rb = new Rigidbody2D { gravityScale = 9, velocity = new Vector2(6, 3), FailVelocityWrite = true };
            Set("actor", actor); Set("actorSpeed", 1f); Set("rod", rod); Set("rodSpeed", 2f);
            Set("hat", hat); Set("hatSpeed", 3f); Set("rigidbody", rb);
            Set("gravity", 1f); Set("flightRate", 3f); Set("flying", true); Set("velocityScaled", true);
            try { motion.Restore(); throw new Exception("Expected independent motion failures"); }
            catch (AggregateException ex) { if (ex.InnerExceptions.Count != 2) throw new Exception("Lost restoration failure"); }
            Require(rod.speed == 2 && hat.speed == 3, "actor/flight failures must not block other renderers");
            Require(actor.speed == 3 && rb.gravityScale == 1, "failed restoration remains pending");
            motion.Restore();
            Require(actor.speed == 1 && rb.gravityScale == 1 && rb.velocity.x == 2 && rb.velocity.y == 1, "retry restores original animation and flight exactly once");
            actor.speed = 0.5f; rb.velocity = new Vector2(7, 8); motion.Restore();
            Require(actor.speed == 0.5f && rb.velocity.x == 7, "completed restoration releases ownership");
            Set("rigidbody", rb); Set("rate", 3f); rb.FailGravityWrite = true;
            try { typeof(FishingMotion).GetMethod("BeginFlight", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(motion, null); throw new Exception("Expected launch setter failure"); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { }
            motion.Restore();
            Require(rb.gravityScale == 1 && rb.velocity.x == 7 && rb.velocity.y == 8, "failed gravity application must not divide an unscaled velocity");
            void Set(string field, object value) => typeof(FishingMotion).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(motion, value);
        }
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    }
}

// Minimal native property boundaries for the linked production motion owner, with transient setters.
// No fishing, collision, timing or game behavior is simulated here.
namespace UnityEngine
{
    public class Component
    {
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : new() => new T();
    }
    public class GameObject { public int layer; }
    public class Animator : Component
    {
        private float value;
        public bool FailNextWrite;
        public float speed { get => value; set { if (FailNextWrite) { FailNextWrite = false; throw new InvalidOperationException("animation setter"); } this.value = value; } }
    }
    public class Collider2D : Component { }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public static Vector2 operator *(Vector2 a, float n) => new Vector2(a.x * n, a.y * n);
        public static Vector2 operator /(Vector2 a, float n) => new Vector2(a.x / n, a.y / n);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
    }
    public class Rigidbody2D : Component
    {
        private float gravity;
        public bool FailGravityWrite;
        public float gravityScale { get => gravity; set { if (FailGravityWrite) { FailGravityWrite = false; throw new InvalidOperationException("gravity setter"); } gravity = value; } }
        public bool simulated, FailVelocityWrite;
        public Vector2 position;
        private Vector2 value;
        public Vector2 velocity { get => value; set { if (FailVelocityWrite) { FailVelocityWrite = false; throw new InvalidOperationException("velocity setter"); } this.value = value; } }
        public int Cast(Vector2 direction, ContactFilter2D filter, RaycastHit2D[] hits, float distance) => 0;
    }
    public struct ContactFilter2D { public bool useTriggers; public void SetLayerMask(int mask) { } }
    public struct RaycastHit2D { public Collider2D collider; public float distance; public Vector2 centroid; }
    public static class Physics2D { public static Vector2 gravity; public static int GetLayerCollisionMask(int layer) => 0; }
    public static class Time { public static float fixedDeltaTime; }
}
namespace DolocTown
{
    public class BodyController { public FishRodRenderer fishRodRenderer = new FishRodRenderer(); public UnityEngine.Component HatRenderer = new UnityEngine.Component(); }
    public class FishRodRenderer { public FishRodHook Hook = new FishRodHook(); }
    public class FishRodHook : UnityEngine.Component { public FishingPool? CurrentFishPool; }
    public class FishingPool : UnityEngine.Component { }
}
namespace HarmonyLib
{
    public class Harmony { public void Patch(MethodInfo? target, HarmonyMethod prefix) { } }
    public class HarmonyMethod { public HarmonyMethod(Type type, string name) { } }
    public static class AccessTools
    {
        public delegate ref F FieldRef<T, F>(T value);
        private static class Cell<F> { internal static F Value = default!; }
        public static FieldRef<T, F> FieldRefAccess<T, F>(string name) => (T value) => ref Cell<F>.Value;
        public static T MethodDelegate<T>(MethodInfo? method) => default!;
        public static MethodInfo? DeclaredMethod(Type type, string name) => null;
        public static MethodInfo? PropertySetter(Type type, string name) => null;
    }
}
