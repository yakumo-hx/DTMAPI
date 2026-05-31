using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourMultiImpact : MonsterAttackBehaviourPhysical
{
	private new readonly IMultiImpact _proto;

	private readonly MonsterAttackHelperTween _handle;

	private readonly RSTimer ghostShadowTimer = new RSTimer(0.05f);

	public MonsterAttackBehaviourMultiImpact(Transform host, IMultiImpact proto)
		: base(host, proto)
	{
		_proto = proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool OnUpdate(float dt)
	{
		return _handle.Update(dt);
	}

	public override void Invoke()
	{
		Begin();
		ReadyForImpact(_proto.TimesRange.DiceCount());
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return _controller.DistanceToTarget(target) <= _proto.DashDistance;
	}

	private void ReadyForImpact(int times)
	{
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN);
			if (RandomUtils.Dice(0.5f))
			{
				DolocAPI.RaiseEmotion(_controller.transform, EmotionName.ANGRY);
			}
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, delegate
			{
				DoImpact(times);
			}, _controller.IsDirectional));
		}
		else
		{
			DoImpact(times);
		}
	}

	private void DoImpact(int times)
	{
		EnableDamageBox(_proto.Damage, _proto.CriticalRate);
		_controller.PostSoundEvent(SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN);
		_controller.PostSoundEvent(_proto.DashSound);
		Vector2 dirToTarget = base.DirToTarget;
		Vector2 vector = (Vector2)host.transform.position + dirToTarget * _proto.DashDistance;
		host.transform.localScale = MonsterUtils.GetScaleFromShootDir(dirToTarget.x);
		Sprite sprite = host.GetComponent<SpriteRenderer>().sprite;
		TweenerCore<Vector3, Vector3, VectorOptions> tweenerCore = host.DOMove(vector, _proto.DashDuration).OnUpdate(delegate
		{
			if (_proto.HasAfterImage && ghostShadowTimer.Tick(Time.fixedDeltaTime))
			{
				DolocAPI.RaiseGhostShadow(host.transform.position, host.transform.localScale, sprite);
			}
		}).SetEase(_proto.DashEase);
		if (times <= 0)
		{
			_handle.SetFinalTween(tweenerCore, base.DisableDamageBox);
			return;
		}
		tweenerCore.OnComplete(delegate
		{
			DolocAPI.cameraController.ShakeScreen();
			ReadyForImpact(times - 1);
		});
		_handle.SetTween(tweenerCore);
	}
}
