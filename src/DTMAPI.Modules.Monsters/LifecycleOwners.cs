using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DolocTown;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private readonly Dictionary<MonsterMoverJump, InstanceScope> jumpOwners = new Dictionary<MonsterMoverJump, InstanceScope>();

        private void InstallLifecycleHooks()
        {
            Patch(AccessTools.PropertySetter(typeof(ContinuesParticleEffects), "positionGetter"), prefix: nameof(EffectPositionAssigned));
            Patch(AccessTools.Method(typeof(ContinuesParticleEffects), "OnReuse"), prefix: nameof(EffectReusing));
            Patch(AccessTools.Method(typeof(MonsterMoverHelper), "CreateMover"), postfix: nameof(MoverCreated));
            Patch(AccessTools.Method(typeof(MonsterMoverJump), "StartJump"), postfix: nameof(JumpStarted));
            InstallGroupHooks();
        }
        private static void MoverCreated(MonsterEnv env, UnityEngine.Transform transform, DolocTown.GameData.MonsterMoverProto proto, DolocTown.Config.Monster.MonsterGenType genType, ref MonsterMover __result)
        {
            if (__result == null && current != null) __result = current.FixedBastionMover(env, transform, proto, genType)!;
            if (__result == null || current == null) return;
            var controller = NativeAccess.Get<MonsterController>(__result, "_controller");
            if (!current.controllers.TryGetValue(controller, out var scope)) return;
            scope.AcquiredMover = __result;
            if (__result is MonsterMoverGround)
            {
                scope.JumpHelper = NativeAccess.Get<MonsterMoverJump>(__result, "_jumpHelper");
                current.jumpOwners[scope.JumpHelper] = scope;
            }
            current.Fault("MoverAcquired");
        }
        private static void JumpStarted(MonsterMoverJump __instance)
        {
            if (current == null || !current.jumpOwners.TryGetValue(__instance, out var scope)) return;
            scope.MovementTweens.RemoveWhere(t => !t.IsActive());
            if (NativeAccess.Get(__instance, "_tween") is Tween tween)
            {
                scope.MovementTweens.Add(tween);
                if (!scope.Active) CancelTween(tween);
            }
        }
        private static void CancelTween(Tween tween)
        {
            // No completion/kill/update callback may re-enter a retiring execution.
            tween.onComplete = null; tween.onKill = null; tween.onUpdate = null;
            tween.onPlay = null; tween.onPause = null;
            tween.onStepComplete = null; tween.onRewind = null;
            tween.Kill(false);
        }
        private void CancelMovement(InstanceScope scope)
        {
            var mover = scope.AcquiredMover ?? scope.Controller?.mover;
            if (mover == null) return;
            NativeAccess.Set(mover, "callback", null);
            // The helper is an exact per-mover resource, never a recursive native graph walk.
            var jump = scope.JumpHelper;
            if (jump != null)
            {
                if (NativeAccess.Get(jump, "_tween") is Tween active) scope.MovementTweens.Add(active);
                NativeAccess.Set(jump, "_isReady", false);
                NativeAccess.Set(jump, "_isOver", true);
                var errors = new List<Exception>();
                foreach (var tween in scope.MovementTweens.ToArray())
                    Attempt(errors, () => { CancelTween(tween); scope.MovementTweens.Remove(tween); });
                ThrowCleanup(errors);
                NativeAccess.Set(jump, "_tween", null);
            }
            CancelTweens(mover);
            mover.StopMove();
        }
        private void ForgetMovement(InstanceScope scope)
        { if (scope.JumpHelper != null) jumpOwners.Remove(scope.JumpHelper); }
    }
}
