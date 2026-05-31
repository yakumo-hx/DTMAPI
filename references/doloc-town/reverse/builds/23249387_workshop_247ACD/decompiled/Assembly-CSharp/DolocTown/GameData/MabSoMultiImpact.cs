using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoMultiImpact : MabSoImpact, IMultiImpact, IImpact, IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction, IHasDash
{
	[SerializeField]
	private Vector2Int timesRange = new Vector2Int(2, 4);

	public Vector2Int TimesRange => timesRange;
}
