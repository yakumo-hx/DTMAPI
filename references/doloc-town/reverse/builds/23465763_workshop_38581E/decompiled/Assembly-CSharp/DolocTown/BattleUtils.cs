using RedSaw;
using UnityEngine;
using UnityEngine.Tilemaps;
using XLua;

namespace DolocTown;

public static class BattleUtils
{
	private static bool _isBattleConfigLoaded;

	private static LuaFunction _damageFormula;

	public static void LoadBattleConfig()
	{
		Debug.Log("加载战斗配置脚本");
		try
		{
			if (!(DolocAPI.luaEnv.DoString(DolocLuaLoader.ReadLua("BattleConfig"))[0] is LuaTable luaTable))
			{
				Debug.LogError("战斗配置脚本加载失败\"未找到配置脚本\"");
				return;
			}
			_damageFormula = luaTable.Get<LuaFunction>("DamageFormula");
			if (_damageFormula == null)
			{
				Debug.Log("战斗配置脚本加载失败\"未定义DamageFormula函数\"");
				return;
			}
			object[] array = _damageFormula.Call(10, 10, 0.1f);
			Debug.Log("伤害计算结果验证" + array[0]);
			_isBattleConfigLoaded = true;
		}
		catch (LuaException exception)
		{
			Debug.Log("战斗系统配置脚本加载失败..");
			Debug.LogException(exception);
		}
	}

	public static AttackHitInfo OnBulletHitEnemy(BulletManager bulletManager, int attack, float critical, Bullet bullet, Collider2D other)
	{
		if (IsWall(other))
		{
			bulletManager.RaiseHitEffects(bullet.positionWS, bullet.MoveDir);
			return AttackHitInfo.HitWall(bullet.positionWS, other);
		}
		IAttackable component = other.GetComponent<IAttackable>();
		if (component == null)
		{
			return AttackHitInfo.HitInvalidObject(bullet.positionWS);
		}
		AttackableType attackableType = component.attackableType;
		if (attackableType == AttackableType.None || attackableType == AttackableType.Enemy)
		{
			bool isCritical = RandomUtils.Dice(critical);
			if (!component.OnAttacked(attack, isCritical, bullet.positionWS, out var isDead))
			{
				return AttackHitInfo.HitInvalidObject(bullet.positionWS, component);
			}
			bulletManager.RaiseHitEffects(bullet.positionWS, bullet.MoveDir);
			return AttackHitInfo.HitEnemy(bullet.positionWS, isSuccess: true, isCritical, isDead, component, other);
		}
		return AttackHitInfo.HitInvalidObject(bullet.positionWS, component, other);
	}

	public static void OnBulletHitPlayer(BulletManager bulletManager, float attack, float critical, Bullet bullet, Collider2D other)
	{
		if (IsWall(other))
		{
			bulletManager.RaiseHitEffects(bullet.positionWS, bullet.MoveDir);
			bullet.Recycle();
			return;
		}
		IAttackable component = other.GetComponent<IAttackable>();
		if (component == null)
		{
			return;
		}
		bool isDead;
		switch (component.attackableType)
		{
		case AttackableType.Enemy:
		case AttackableType.Unattackable:
		{
			BodyController component2 = other.GetComponent<BodyController>();
			if (component2 != null && component2.IsPerfectDodge)
			{
				DolocAPI.PerfectDodgeEffects();
			}
			break;
		}
		case AttackableType.None:
			if (component.OnAttacked(attack, RandomUtils.Dice(critical), bullet.positionWS, out isDead))
			{
				bulletManager.RaiseHitEffects(bullet.positionWS, bullet.MoveDir);
				bullet.Recycle();
			}
			break;
		case AttackableType.Player:
			component.OnAttacked(attack, RandomUtils.Dice(critical), bullet.positionWS, out isDead);
			bulletManager.RaiseHitEffects(bullet.positionWS, bullet.MoveDir);
			bullet.Recycle();
			break;
		}
	}

	public static bool IsWall(GameObject obj)
	{
		Collider2D component = obj.GetComponent<Collider2D>();
		if (component == null)
		{
			return false;
		}
		if (component.isTrigger)
		{
			return false;
		}
		return RSUtils.LayerMaskCheck(obj, DolocAPI.gameConfig.groundMask);
	}

	public static bool IsWalkable(GameObject obj)
	{
		Collider2D component = obj.GetComponent<Collider2D>();
		if (component == null)
		{
			return false;
		}
		if (component.isTrigger)
		{
			return true;
		}
		return !RSUtils.LayerMaskCheck(obj, DolocAPI.gameConfig.walkableMask);
	}

	public static bool IsWall(Collider2D other)
	{
		if (other.isTrigger)
		{
			return false;
		}
		return RSUtils.LayerMaskCheck(other.gameObject, DolocAPI.gameConfig.groundMask);
	}

	public static bool IsRealWall(Collider2D other)
	{
		if (!IsWall(other))
		{
			return false;
		}
		return other.GetComponent<Tilemap>() != null;
	}

	private static int CalcDamageDefault(float attack, float defend, float criticalRate)
	{
		float num = 1f - defend / (defend + DolocAPI.GlobalParameter.DefendAdjust);
		if (RandomUtils.Dice(criticalRate))
		{
			num *= DolocAPI.GlobalParameter.CriticalDamageRate;
		}
		return Mathf.Max((int)(attack * num), 0);
	}

	private static int CalcDamageFormula2(float atk, float def, bool ctr)
	{
		if (ctr)
		{
			atk *= 1.5f;
		}
		return Mathf.Max(Mathf.RoundToInt(atk - def), 1);
	}

	public static int CalcDamage(float attack, float defend, bool isCritical)
	{
		return CalcDamageFormula2(attack, defend, isCritical);
	}
}
