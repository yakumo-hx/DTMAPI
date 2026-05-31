using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown.GameData;

public class MabSoBombingDive : MonsterAttackBehaviourSOBullet, IBombingDive, IMonsterAttackBehaviourBullet, IMonsterAttackBehaviour, IHasDamage, IHasBullet, IHasReadyAction, IHasDash
{
	[SerializeField]
	[Range(0f, 30f)]
	private float validDistance = 15f;

	[SerializeField]
	[Range(0f, 30f)]
	private float distance = 10f;

	[SerializeField]
	[Range(0f, 20f)]
	private float speed = 10f;

	[SerializeField]
	private Vector2Int bulletCountRange = new Vector2Int(3, 5);

	[SerializeField]
	private Ease ease;

	[SerializeField]
	private string dashSound;

	[SerializeField]
	private bool hasAfterImage;

	public float ValidDistance => validDistance;

	public float DashDistance => distance;

	public float DashSpeed => speed;

	public Vector2Int BulletCountRange => bulletCountRange;

	public Ease DashEase => ease;

	public string DashSound => dashSound;

	public bool HasAfterImage => hasAfterImage;
}
