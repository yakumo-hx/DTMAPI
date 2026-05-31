using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoCall : MonsterAttackBehaviourSO, IMonsterAttackBehaviourCall, IMonsterAttackBehaviour, IHasReadyAction
{
	[SerializeField]
	private bool hasReadyAction;

	[SerializeField]
	[Range(0f, 3f)]
	private float readyDuration = 0.5f;

	[SerializeField]
	private string monsterId = "drone";

	[SerializeField]
	private Vector2Int countRange;

	[SerializeField]
	[Min(1f)]
	private int limitation = 6;

	public bool HasReadyAction => hasReadyAction;

	public float ReadyDuration => readyDuration;

	public string MonsterId => monsterId;

	public Vector2Int CountRange => countRange;

	public int Limitation => limitation;
}
