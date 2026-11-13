using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config.Monster;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static MonsterActionCapabilities DescribeActions(Template template)
        {
            // Read existing inputs only. These are the two movement mechanisms owned by
            // BeginMove; native inheritance can remain valid for other mover mechanisms.
            var attacks = template.Proto.AttackBehaviourProtos ?? Array.Empty<IMonsterAttackBehaviour>();
            bool movement = template.Proto.MoverProto is MonsterMoverProtoGround || template.Proto.MoverProto is MonsterMoverProtoCtxSteering;
            return new MonsterActionCapabilities(attacks
                .Where(a => a != null && a.AttackId != MonsterAttackId.Unset).Select(a => a.AttackId.ToString()),
                movement, movement ? "" : "This native mover can be inherited but is not owned by controlled MoveTo.",
                attacks.Length == 0
                    ? "No native attack behaviours. Target child break/reset and ordinary monster death are separate native paths; controlled Attack is unavailable."
                    : template.Prefab?.GetComponentInChildren<IMonsterSight>(true) != null
                    ? "Current-room active native locked target"
                    : attacks.Any(a => a is IChomperMimicry)
                        ? "ChomperMimicry is a self-action and needs no enemy target. Other attacks/GetTarget require native sight; controlled MoveTo remains unavailable."
                        : "TargetingUnsupported: template has no native sight provider for controlled targeting/Attack; native inheritance remains available.")
            {
                NativeAttacks = DescribeNativeAttacks(template),
                NativeParts = DescribeNativeParts(template),
                PhasedNativeActions = true,
                ActionAnimation = template.Prefab?.GetComponent<MonsterRenderer>() != null &&
                    template.Prefab.GetComponent<UnityEngine.Animator>() != null && template.Prefab.GetComponent<UnityEngine.SpriteRenderer>() != null &&
                    template.Prefab.GetComponent<SpriteOverrideHandler>() != null,
                Dash = SupportsDash(template),
                DashReason = SupportsDash(template) ? "" : "Dash requires native sight, a root body collider, and a Ground/Ground or Air/ContextSteering mover.",
                ScatterAttackIds = template.Prefab?.GetComponentInChildren<IMonsterSight>(true) == null ? Array.Empty<string>() :
                    attacks.Where(a => a != null && a.AttackId != MonsterAttackId.Unset && a is IMonsterAttackBehaviourBullet bullet &&
                        DolocAPI.assets.bulletMovers.QueryData(bullet.BulletMoverId, out var mover) && mover is BulletMoverProtoLinear)
                        .Select(a => a.AttackId.ToString()).Distinct().ToArray()
            };
        }
    }
}
