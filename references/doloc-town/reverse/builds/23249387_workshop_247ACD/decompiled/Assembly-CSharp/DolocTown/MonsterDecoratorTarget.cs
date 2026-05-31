using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorTarget : MonsterDecorator
{
	private readonly RSTimer _timer = new RSTimer(3f);

	private bool _hasTargetLoaded;

	public override AttackableType attackableType => AttackableType.Unattackable;

	public override bool ShieldBullet => !_hasTargetLoaded;

	public override void OnMonsterLoaded(Monster monster)
	{
		_ResetTarget(monster.proto);
	}

	public override void OnUpdate(float dt)
	{
		if (!_hasTargetLoaded && _timer.Tick(dt))
		{
			_ResetTarget(base.Monster.proto);
		}
	}

	private void _ResetTarget(MonsterProto proto)
	{
		_hasTargetLoaded = true;
		Target componentInChildren = GetComponentInChildren<Target>(includeInactive: true);
		componentInChildren.ProtoName = proto.Name;
		componentInChildren.ResetTarget(proto.Health, proto.Defense);
		componentInChildren.CallbackOnBroken(delegate
		{
			_hasTargetLoaded = false;
		});
	}

	public override bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		return false;
	}
}
