using System;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace Yuuka.DTMAPI.AutoFishing
{
    // Owns only the active player's fishing renderers and hook; global clocks stay untouched.
    internal sealed class FishingMotion : IDisposable
    {
        private static FishingMotion? current;
        private static readonly AccessTools.FieldRef<BodyController, Animator> BodyAnimator = AccessTools.FieldRefAccess<BodyController, Animator>("animator");
        private static readonly AccessTools.FieldRef<FishRodRenderer, Animator> RodAnimator = AccessTools.FieldRefAccess<FishRodRenderer, Animator>("_animator");
        private static readonly Action<FishRodHook, Collider2D> TouchPool = AccessTools.MethodDelegate<Action<FishRodHook, Collider2D>>(AccessTools.DeclaredMethod(typeof(FishRodHook), "OnTriggerEnter2D"));
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private Animator? actor, rod, hat;
        private FishRodHook? hook;
        private Rigidbody2D? rigidbody;
        private float actorSpeed, rodSpeed, hatSpeed;
        private float animationRate = 1f, rate = 1f, flightRate = 1f, gravity;
        private bool flying, gravityRestored, velocityScaled;
        private FishingPhase phase;
        private ContactFilter2D filter;

        public void Install(Harmony harmony)
        {
            current = this;
            harmony.Patch(AccessTools.DeclaredMethod(typeof(FishRodHook), "AddForce"), new HarmonyMethod(typeof(FishingMotion), nameof(Launch)));
            harmony.Patch(AccessTools.PropertySetter(typeof(FishRodHook), "Velocity"), new HarmonyMethod(typeof(FishingMotion), nameof(ReturnVelocity)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(FishRodHook), "FixedUpdate"), new HarmonyMethod(typeof(FishingMotion), nameof(SweepWater)));
        }

        public void Bind(BodyController body)
        {
            Restore();
            actor = BodyAnimator(body); rod = RodAnimator(body.fishRodRenderer);
            hat = body.HatRenderer.GetComponent<Animator>();
            actorSpeed = actor.speed; rodSpeed = rod.speed; hatSpeed = hat != null ? hat.speed : 1f;
            hook = body.fishRodRenderer.Hook; rigidbody = hook.GetComponent<Rigidbody2D>();
            filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(hook.gameObject.layer));
        }

        public void Apply(FishingPhase value, float multiplier)
        {
            phase = value; rate = multiplier;
            if (flying && phase != FishingPhase.Cast && phase != FishingPhase.Pull) RestoreFlight();
            float next = phase == FishingPhase.Ready || phase == FishingPhase.Cast || phase == FishingPhase.Pull ? multiplier : 1f;
            if (next == animationRate) return;
            if (actor != null) actor.speed = actorSpeed * next;
            if (rod != null) rod.speed = rodSpeed * next;
            if (hat != null) hat.speed = hatSpeed * next;
            animationRate = next;
        }

        private void BeginFlight()
        {
            RestoreFlight();
            if (rigidbody == null || rate <= 1f) return;
            gravity = rigidbody.gravityScale;
            flying = true; gravityRestored = false; velocityScaled = false; flightRate = rate;
            rigidbody.gravityScale = gravity * rate * rate;
        }
        private void RestoreFlight()
        {
            if (flying && rigidbody != null)
            {
                if (!gravityRestored) { rigidbody.gravityScale = gravity; gravityRestored = true; }
                if (velocityScaled) { rigidbody.velocity /= flightRate; velocityScaled = false; }
            }
            flying = false; gravityRestored = false; velocityScaled = false; flightRate = 1f;
        }
        private static void Launch(FishRodHook __instance, ref Vector2 force)
        {
            var self = current;
            if (self == null || !ReferenceEquals(self.hook, __instance) || self.phase != FishingPhase.Cast) return;
            self.BeginFlight(); force *= self.flightRate; self.velocityScaled = self.flying;
        }
        private static void ReturnVelocity(FishRodHook __instance, ref Vector2 value)
        {
            var self = current;
            if (self == null || !ReferenceEquals(self.hook, __instance) || self.phase != FishingPhase.Pull) return;
            self.BeginFlight(); value *= self.flightRate; self.velocityScaled = self.flying;
        }
        private static void SweepWater(FishRodHook __instance)
        {
            var self = current;
            if (self == null || !self.flying || self.phase != FishingPhase.Cast || !ReferenceEquals(self.hook, __instance) ||
                __instance.CurrentFishPool != null || self.rigidbody == null || !self.rigidbody.simulated) return;
            var rb = self.rigidbody;
            float dt = Time.fixedDeltaTime;
            Vector2 travel = (rb.velocity + Physics2D.gravity * (rb.gravityScale * dt)) * dt;
            float distance = travel.magnitude;
            if (distance <= 0f) return;
            Vector2 direction = travel / distance;
            int count = rb.Cast(direction, self.filter, self.hits, distance);
            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                var hit = self.hits[i];
                if (hit.collider == null || hit.collider.GetComponent<FishingPool>() == null) continue;
                if (nearest < 0 || hit.distance < self.hits[nearest].distance) nearest = i;
            }
            if (nearest < 0) return;
            var contact = self.hits[nearest];
            // Only a real water collider crossed by this physics step can enter the native handler.
            rb.position = contact.centroid + direction * 0.001f;
            TouchPool(__instance, contact.collider);
            if (__instance.CurrentFishPool != null) self.RestoreFlight();
        }
        public void Restore()
        {
            animationRate = 1f; rate = 1f; phase = FishingPhase.Other;
            Array.Clear(hits, 0, hits.Length);
            Cleanup.Run(
                () => { RestoreFlight(); hook = null; rigidbody = null; },
                () => { if (actor != null) actor.speed = actorSpeed; actor = null; },
                () => { if (rod != null) rod.speed = rodSpeed; rod = null; },
                () => { if (hat != null) hat.speed = hatSpeed; hat = null; });
        }
        public void Dispose()
        {
            try { Restore(); }
            finally { if (ReferenceEquals(current, this)) current = null; }
        }
    }
}
