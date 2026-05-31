using DG.Tweening;
using UnityEngine;

namespace DolocTown.GameData;

public class MonsterMoverSODoTween : MonsterMoverSO, IMonsterMoverProtoDoTween, IMonsterMoverProto
{
	[SerializeField]
	private float moveDuration;

	[SerializeField]
	private Ease moveEase;

	float IMonsterMoverProtoDoTween.MoveSpeed => moveDuration;

	Ease IMonsterMoverProtoDoTween.MoveEase => moveEase;

	public override bool CreateProto(out MonsterMoverProto proto)
	{
		if (!_pathFinder.CreateProto(out var proto2))
		{
			proto = null;
			return false;
		}
		proto = new MonsterMoverProtoDoTween(moveDuration, moveEase, proto2, isDirectional, _aroundRange);
		return true;
	}
}
