using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourChomperBite : MonsterAttackBehaviourPhysical
{
	private new readonly IChomperBite _proto;

	private readonly MonsterAttackHelperAnim _helperAnimController;

	public MonsterAttackBehaviourChomperBite(Transform host, IChomperBite proto)
		: base(host, proto)
	{
		_proto = proto;
		_helperAnimController = new MonsterAttackHelperAnim(host.GetComponent<Animator>());
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return _controller.DistanceToTarget(target) <= _proto.AttackRange;
	}

	public override void Invoke()
	{
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_helperAnimController.Invoke(_proto.ReadyDuration, _proto.AnimName, delegate
			{
				SetAttackParams(_proto.Damage, _proto.CriticalRate);
			});
		}
		else
		{
			SetAttackParams(_proto.Damage, _proto.CriticalRate);
			_helperAnimController.Invoke(_proto.AnimName);
		}
	}

	public override bool OnUpdate(float dt)
	{
		return _helperAnimController.Update(dt);
	}
}
