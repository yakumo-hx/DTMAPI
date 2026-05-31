using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoImpact : MonsterAttackBehaviourSOPhysical, IImpact, IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction, IHasDash
{
	[SerializeField]
	private float distance;

	[SerializeField]
	private float speed;

	[SerializeField]
	private Ease ease;

	[SerializeField]
	private string dashSound;

	[SerializeField]
	private bool hasAfterImage;

	public float DashDistance => distance;

	public float DashSpeed => speed;

	public Ease DashEase => ease;

	public string DashSound => dashSound;

	public bool HasAfterImage => hasAfterImage;
}
