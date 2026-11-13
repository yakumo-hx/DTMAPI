using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using DTMAPI.MonsterContent;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private readonly Dictionary<BulletManager, List<Shot>> managerShots = new Dictionary<BulletManager, List<Shot>>();
        private sealed class Shot
        {
            internal readonly InstanceScope Scope;
            internal readonly BulletManager Manager;
            internal readonly MonsterActionHandle? ActionHandle;
            private Action<Bullet, Collider2D>? hit;
            private Action<Bullet>? created;
            internal Bullet? Bullet;
            private readonly long generation;
            internal bool Cancelled;
            internal bool Fired;
            internal bool HitObserved;
            internal bool PoolPending;
            internal Shot(InstanceScope scope, BulletManager manager, Action<Bullet, Collider2D> hit, Action<Bullet>? created)
            { Scope = scope; Manager = manager; this.hit = hit; this.created = created; generation = scope.Handle.Generation; ActionHandle = scope.Action?.Handle; }
            internal bool Valid => !Cancelled && Scope.Active && Scope.Handle.Generation == generation;
            internal void OnHit(Bullet bullet, Collider2D collider) { if (Valid) { HitObserved = true; hit?.Invoke(bullet, collider); } }
            internal void OnCreated(Bullet bullet)
            {
                Bullet = bullet;
                if (Valid) created?.Invoke(bullet);
                else { bullet.callbackOnHit = null!; bullet.Recycle(); }
            }
            internal void Release()
            {
                Cancelled = true; hit = null; created = null; Scope.Shots.Remove(this);
                if (current != null && current.managerShots.TryGetValue(Manager, out var shots))
                { shots.Remove(this); if (shots.Count == 0) current.managerShots.Remove(Manager); }
            }
        }
        private void InstallShotHooks()
        {
            Patch(AccessTools.Method(typeof(BulletManager), "InvokeBullet"), prefix: nameof(EnqueueShot));
            Patch(AccessTools.Method(typeof(BulletManager), "_Fire"), prefix: nameof(FireShot));
            Patch(AccessTools.Method(typeof(BulletManager), "OnFixedUpdate"), postfix: nameof(ShotsAdvanced));
        }
        private static bool EnqueueShot(BulletManager __instance, ref Action<Bullet, Collider2D> cb, ref Action<Bullet> bulletHandle)
        {
            if (current == null || !(cb?.Target is MonsterAttackBehaviour attack)) return true;
            var controller = NativeAccess.Get<MonsterController>(attack, "_controller");
            if (!current.controllers.TryGetValue(controller, out var scope))
            { current.WrapNativeProjectile(controller, ref bulletHandle); return true; }
            if (!scope.Active) return false;
            var shot = current.TrackShot(scope, __instance, cb, bulletHandle);
            cb = shot.OnHit; bulletHandle = shot.OnCreated;
            return true;
        }
        private Shot TrackShot(InstanceScope scope, BulletManager manager, Action<Bullet, Collider2D> hit, Action<Bullet>? created)
        {
            var shot = new Shot(scope, manager, hit, created);
            if (!managerShots.TryGetValue(manager, out var shots)) managerShots.Add(manager, shots = new List<Shot>());
            shots.Add(shot); scope.Shots.Add(shot);
            return shot;
        }
        private static bool FireShot(Action<Bullet, Collider2D> callback)
        { if (!(callback?.Target is Shot shot)) return true; if (shot.Valid) { shot.Fired = true; return true; } shot.Release(); return false; }
        private static void ShotsAdvanced(BulletManager __instance)
        {
            if (current == null) return;
             {
            if (current.managerShots.TryGetValue(__instance, out var shots))
            {
                var bullets = NativeAccess.Get<IList>(__instance, "bullets");
                for (int i = shots.Count - 1; i >= 0; i--)
                {
                    var shot = shots[i];
                    if (!shot.Cancelled && shot.Fired)
                    {
                        using var trace = OptimizationTrace.Begin(15);
                        if (shot.Bullet == null || !bullets.Contains(shot.Bullet)) shot.Release();
                    }
                }
            }
            }
        }
        private static void FilterQueue(object queue, Func<object, bool> keep)
        {
            var items = ((IEnumerable)queue).Cast<object>().Where(keep).ToArray();
            queue.GetType().GetMethod("Clear")!.Invoke(queue, null);
            var enqueue = queue.GetType().GetMethod("Enqueue")!;
            foreach (object item in items) enqueue.Invoke(queue, new[] { item });
        }
        private void CancelShots(InstanceScope scope, MonsterActionHandle? action = null)
        {
            var shots = scope.Shots.Where(s => action == null || s.ActionHandle.HasValue && s.ActionHandle.Value.Equals(action.Value)).ToArray();
            var selected = new HashSet<Shot>(shots);
            var errors = new List<Exception>();
            foreach (var shot in shots) shot.Cancelled = true;
            foreach (var manager in shots.Select(s => s.Manager).Distinct())
            {
                Attempt(errors, () => {
                FilterQueue(NativeAccess.Get(manager, "fireBuffer")!, item => !(NativeAccess.Get(item, "callback") is Delegate callback) || !(callback.Target is Shot shot) || !selected.Contains(shot));
                var bullets = NativeAccess.Get<IList>(manager, "bullets");
                var owned = new HashSet<Bullet>(shots.Where(s => ReferenceEquals(s.Manager, manager) && s.Bullet != null).Select(s => s.Bullet!));
                FilterQueue(NativeAccess.Get(manager, "recycleBuffer")!, item => !owned.Contains((Bullet)item));
                var pool = NativeAccess.Get(manager, "pool")!;
                foreach (var shot in shots.Where(s => ReferenceEquals(s.Manager, manager)))
                {
                    Attempt(errors, () => {
                    var bullet = shot.Bullet;
                    if (bullet == null) { shot.Release(); return; }
                    bullet.callbackOnHit = null!;
                    NativeAccess.Set(bullet, "_m_hasBeenRecycled", true);
                    if (bullets.Contains(bullet)) { shot.PoolPending = true; bullets.Remove(bullet); }
                    if (shot.PoolPending)
                    {
                        Fault("ShotRecycle");
                        pool.GetType().GetMethod("Recycle", new[] { typeof(BulletEntity) })!.Invoke(pool, new object[] { bullet.entity });
                        shot.PoolPending = false;
                    }
                    shot.Release();
                    });
                }
                });
            }
            ThrowCleanup(errors);
        }
    }
}
