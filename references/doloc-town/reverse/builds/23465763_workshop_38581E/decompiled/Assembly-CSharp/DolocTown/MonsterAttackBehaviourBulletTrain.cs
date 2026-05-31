using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourBulletTrain : MonsterAttackBehaviourBullet
{
	private readonly IBulletTrain _params;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourBulletTrain(Transform host, IMonsterAttackBehaviourBullet proto, BulletManager bulletManager)
		: base(host, proto, bulletManager)
	{
		_params = (IBulletTrain)proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return true;
	}

	public override bool OnUpdate(float dt)
	{
		return _handle.Update(dt);
	}

	public override void Invoke()
	{
		Begin();
		if (_params.HasReadyAction)
		{
			DolocAPI.RaiseInstantGoEffects(_controller.position, InstantGoEffectsType.WARNING_LIGHT);
			_controller.RaiseDangerWarning01();
			_handle.SetTween(DoReadyAction(_params.ReadyDuration, _Invoke));
		}
		else
		{
			_Invoke();
		}
	}

	private void _Invoke()
	{
		float num = _params.ShootInterval * (float)_params.BulletShootTimes;
		float num2 = _params.BulletSpeed * num;
		float num3 = Mathf.Sign(DolocAPI.AgentPosition.x - _controller.position.x);
		_controller.FaceRight = num3 < 0f;
		Vector3 endValue = _controller.transform.position + Vector3.right * (num2 * num3) + Vector3.up * Random.Range(3, 5);
		Sequence sequence = DOTween.Sequence();
		sequence.Join(_controller.transform.DOMove(endValue, num).SetEase(Ease.Linear));
		Sequence sequence2 = DOTween.Sequence();
		for (int i = 0; i < _params.BulletShootTimes; i++)
		{
			sequence2.AppendInterval(_params.ShootInterval);
			sequence2.AppendCallback(Shoot);
		}
		sequence.Join(sequence2);
		_handle.SetFinalTween(sequence);
	}

	private void Shoot()
	{
		_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_ATTACK);
		DolocAPI.cameraController.ShakeScreen();
		foreach (Vector2 item in _params.InitialDirection.SplitIntoSector(_params.BulletCount, _params.SectorAngle))
		{
			GenBullet(_controller, item);
		}
	}
}
