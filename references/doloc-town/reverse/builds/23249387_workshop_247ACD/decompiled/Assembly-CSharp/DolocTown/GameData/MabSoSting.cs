using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoSting : MonsterAttackBehaviourSOPhysical, ISting, IMonsterAttackBehaviourPhysical, IMonsterAttackBehaviour, IHasDamage, IHasReadyAction, IHasDash
{
	[SerializeField]
	private float moveSpeed = 17f;

	[SerializeField]
	private float moveDistance;

	[SerializeField]
	private Ease ease;

	[SerializeField]
	private string dashSound;

	[SerializeField]
	private bool hasAfterImage;

	public float DashSpeed => moveSpeed;

	public Ease DashEase => ease;

	public string DashSound => dashSound;

	public float DashDistance => moveDistance;

	public bool HasAfterImage => hasAfterImage;
}
