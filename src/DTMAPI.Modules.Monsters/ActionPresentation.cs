using DolocTown;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static void RestoreActionPresentation(InstanceScope scope, bool moving)
        {
            // Mechanism adapters: no attack-end callback, projectile, damage or landing effect.
            // Exit cleanup rebinds the Animator separately; only a live cancellation needs recovery.
            var controller = scope.Controller;
            if (!scope.Active || controller == null) return;
            var renderer = controller.Renderer;
            NativeAccess.Set(renderer, "onAnimationDone", null);
            NativeAccess.Set(renderer, "shouldCheckAnimationDone", false);
            if (!moving && scope.Bastion != null)
            {
                CancelBastionAttack(scope.Bastion);
                renderer.PlayAnimation("idle");
            }
            else if (!moving && renderer.AttackBehaviourRenderer is AttackBehaviourRendererBallDrone)
                renderer.PlayAnimation("resume");
            else if ((moving || scope.Action?.Attack is MonsterAttackBehaviourAmoebaJumpAttack)
                && controller.GetComponent<MonsterMoverGroundRendererAmoeba>() != null && !HurtLocked(controller))
                renderer.PlayAnimation("idle"); // touch_ground contains a landing sound event.
        }
    }
}
