using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public interface IMonsterAttackBehaviourCall : IMonsterAttackBehaviour, IHasReadyAction
{
	string MonsterId { get; }

	Vector2Int CountRange { get; }

	int Limitation { get; }
}
