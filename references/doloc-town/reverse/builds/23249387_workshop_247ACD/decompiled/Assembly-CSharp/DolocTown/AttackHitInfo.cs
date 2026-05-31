using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public struct AttackHitInfo
{
	public Vector2 position;

	public bool isSuccess;

	public bool isCritical;

	public bool isDead;

	public bool isWall;

	public IAttackable attackableObj;

	public MonsterProto monsterProto;

	public Collider2D collider;

	public bool IsHitEnemy
	{
		get
		{
			IAttackable attackable = attackableObj;
			if (attackable == null)
			{
				return false;
			}
			return attackable.attackableType == AttackableType.Enemy;
		}
	}

	public static AttackHitInfo HitWall(Vector2 position, Collider2D collider2D)
	{
		return new AttackHitInfo(position, collider2D);
	}

	public static AttackHitInfo HitInvalidObject(Vector2 position, IAttackable target = null, Collider2D other = null)
	{
		return new AttackHitInfo(position, isSuccess: false, isCritical: false, isDead: false, isWall: false, target, other);
	}

	public static AttackHitInfo HitEnemy(Vector2 position, bool isSuccess, bool isCritical, bool isDead, IAttackable attackableObj, Collider2D collider)
	{
		AttackHitInfo result = new AttackHitInfo(position, isSuccess, isCritical, isDead, isWall: false, attackableObj, collider);
		if (attackableObj is MonsterController monsterController)
		{
			result.monsterProto = monsterController.MonsterProto;
		}
		return result;
	}

	public AttackHitInfo(Vector2 position, Collider2D collider)
	{
		this.position = position;
		isCritical = false;
		isDead = false;
		isWall = true;
		attackableObj = null;
		isSuccess = true;
		this.collider = collider;
		monsterProto = default(MonsterProto);
	}

	public AttackHitInfo(Vector2 position, bool isSuccess, bool isCritical, bool isDead, bool isWall, IAttackable attackableObj, Collider2D collider)
	{
		this.position = position;
		this.isSuccess = isSuccess;
		this.isCritical = isCritical;
		this.isDead = isDead;
		this.isWall = isWall;
		this.attackableObj = attackableObj;
		this.collider = collider;
		monsterProto = default(MonsterProto);
	}
}
