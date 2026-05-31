using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourBombingDive : MonsterAttackBehaviourBullet
{
	private new readonly IBombingDive _proto;

	private Tween _tween;

	public MonsterAttackBehaviourBombingDive(Transform host, IBombingDive proto, BulletManager bm)
		: base(host, proto, bm)
	{
		_proto = proto;
	}

	public override bool OnUpdate(float dt)
	{
		if (_tween == null)
		{
			return true;
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		if (target.position.y > host.position.y)
		{
			return false;
		}
		if (_controller.DistanceToTarget(target) > _proto.ValidDistance)
		{
			return false;
		}
		return Physics2D.Raycast(direction: new Vector2(Mathf.Sign(target.position.x - host.position.x), 0f), origin: host.transform.position, distance: _proto.DashDistance, layerMask: DolocAPI.gameConfig.groundMask).collider == null;
	}

	public override void Invoke()
	{
		if (_proto.HasReadyAction)
		{
			Vector2 shootDir = new Vector2(Mathf.Sign(base.target.position.x - host.position.x), 0f);
			_tween = DoReadyAction(shootDir, _proto.ReadyDuration, StartBombingDive, base.IsDirectional);
		}
		else
		{
			StartBombingDive();
		}
	}

	private void StartBombingDive()
	{
		Vector2 vector = host.position;
		Vector2 vector2 = new Vector2(Mathf.Sign(base.target.position.x - vector.x), 0f);
		Vector2 pos = vector + vector2 * _proto.DashDistance;
		Sequence sequence = DOTween.Sequence();
		float dashDuration = _proto.DashDuration;
		sequence.Append(MonsterUtils.MoveTo(host, pos, dashDuration, _proto.DashEase));
		int num = _proto.BulletCountRange.DiceCount();
		float interval = dashDuration / (float)num;
		Sequence sequence2 = DOTween.Sequence();
		for (int i = 0; i < num; i++)
		{
			sequence2.AppendCallback(delegate
			{
				GenBullet(_controller, Vector2.up);
			});
			sequence2.AppendInterval(interval);
		}
		sequence.Join(sequence2);
		sequence.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		sequence.OnComplete(Stop);
		_tween = sequence;
	}

	private void Stop()
	{
		_tween?.Kill();
		_tween = null;
		End();
	}
}
