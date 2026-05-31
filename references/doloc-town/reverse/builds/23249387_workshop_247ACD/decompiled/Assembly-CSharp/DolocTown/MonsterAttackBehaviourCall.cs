using DG.Tweening;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourCall : MonsterAttackBehaviour
{
	private new readonly IMonsterAttackBehaviourCall _proto;

	private Tween _tween;

	public MonsterAttackBehaviourCall(Transform host, IMonsterAttackBehaviourCall proto)
		: base(host, proto)
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

	public override void Invoke()
	{
		Begin();
		DolocAPI.RaiseInstantAnimEffects(_controller.EmotionPos, InstAnimEffectType.SIGNAL, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		_tween = DoReadyAction(Vector2.one.normalized, _proto.ReadyDuration, CallMonster, shouldFaceTarget: false);
	}

	private void CallMonster()
	{
		Debug.Log("召唤怪物:" + _proto.MonsterId);
		MonsterController component = host.GetComponent<MonsterController>();
		int num = _proto.CountRange.DiceCount();
		int num2 = Mathf.Max(0, _proto.Limitation - component.Env.Count);
		if (num2 < num)
		{
			num = num2;
		}
		if (num <= 0)
		{
			DolocAPI.RaiseEmotion(host, EmotionName.CONFUSE);
			Debug.Log("召唤:超出房间的怪物数量限制");
		}
		component.Env.CallMonsters(_proto.MonsterId, num);
		Stop();
	}

	private void Stop()
	{
		_tween?.Kill();
		_tween = null;
		End();
	}

	public override void Dispose(BattleSystem bs)
	{
	}
}
