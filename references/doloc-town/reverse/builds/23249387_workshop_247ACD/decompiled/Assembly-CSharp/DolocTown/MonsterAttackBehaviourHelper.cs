using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public static class MonsterAttackBehaviourHelper
{
	public static MonsterAttackBehaviour CreateMonsterAttackBehaviour(BattleSystem battleSystem, Transform host, IMonsterAttackBehaviour proto)
	{
		if (!(proto is IMonsterAttackBehaviourBullet protoBullet))
		{
			if (!(proto is IMonsterAttackBehaviourPhysical proto2))
			{
				if (proto is IMonsterAttackBehaviourSkill proto3)
				{
					return _Skill(battleSystem, host, proto3);
				}
				return _Other(battleSystem, host, proto);
			}
			return _Physical(host, proto2);
		}
		return _Bullet(battleSystem, host, protoBullet);
	}

	private static MonsterAttackBehaviour _Bullet(BattleSystem battleSystem, Transform host, IMonsterAttackBehaviourBullet protoBullet)
	{
		BulletManager bulletManager = battleSystem.CreateBulletManager(protoBullet.BulletId, protoBullet.BulletMoverId);
		if (bulletManager == null)
		{
			Debug.LogError("无效的子弹ID:" + protoBullet.BulletId);
			return null;
		}
		if (!(protoBullet is IShoot proto))
		{
			if (!(protoBullet is IMultiShoot proto2))
			{
				if (!(protoBullet is ISalvoShoot proto3))
				{
					if (!(protoBullet is IDragonShoot proto4))
					{
						if (!(protoBullet is IScatteringShoot proto5))
						{
							if (!(protoBullet is IScatteringThrow proto6))
							{
								if (!(protoBullet is IBombingDive proto7))
								{
									if (!(protoBullet is IAngryThrow proto8))
									{
										if (!(protoBullet is IFungusShoot proto9))
										{
											if (!(protoBullet is IRandomShoot proto10))
											{
												if (protoBullet is IBulletTrain proto11)
												{
													return new MonsterAttackBehaviourBulletTrain(host, proto11, bulletManager);
												}
												return null;
											}
											return new MonsterAttackBehaviourRandomShoot(host, proto10, bulletManager);
										}
										return new MonsterAttackBehaviourFungusShoot(host, proto9, bulletManager);
									}
									return new MonsterAttackBehaviourAngryThrow(host, proto8, bulletManager);
								}
								return new MonsterAttackBehaviourBombingDive(host, proto7, bulletManager);
							}
							return new MonsterAttackBehaviourScatteringThrow(host, proto6, bulletManager);
						}
						return new MonsterAttackBehaviourScatteringShoot(host, proto5, bulletManager);
					}
					return new MonsterAttackBehaviourDragonShoot(host, proto4, bulletManager);
				}
				return new MonsterAttackBehaviourSalvoShoot(host, proto3, bulletManager);
			}
			return new MonsterAttackBehaviourMultiShoot(host, proto2, bulletManager);
		}
		return new MonsterAttackBehaviourShoot(host, proto, bulletManager);
	}

	private static MonsterAttackBehaviour _Physical(Transform host, IMonsterAttackBehaviourPhysical proto)
	{
		if (!(proto is ISting proto2))
		{
			if (!(proto is IChomperBite proto3))
			{
				if (!(proto is IMultiImpact proto4))
				{
					if (!(proto is IImpact proto5))
					{
						if (proto is IAmoebaJumpAttack proto6)
						{
							return new MonsterAttackBehaviourAmoebaJumpAttack(host, proto6);
						}
						return null;
					}
					return new MonsterAttackBehaviourImpact(host, proto5);
				}
				return new MonsterAttackBehaviourMultiImpact(host, proto4);
			}
			return new MonsterAttackBehaviourChomperBite(host, proto3);
		}
		return new MonsterAttackBehaviourSting(host, proto2);
	}

	private static MonsterAttackBehaviour _Skill(BattleSystem battleSystem, Transform host, IMonsterAttackBehaviourSkill proto)
	{
		SkillManager skillManager = battleSystem.CreateSkillManager(proto.SkillId);
		if (skillManager == null)
		{
			return null;
		}
		if (!(proto is IBomb proto2))
		{
			if (!(proto is ITimeBomb proto3))
			{
				if (!(proto is ILandmine proto4))
				{
					if (proto is IChomperMimicry proto5)
					{
						return new MonsterAttackBehaviourSkillChomperMimicry(host, proto5, skillManager);
					}
					return null;
				}
				return new MonsterAttackBehaviourSkillLandmine(host, proto4, skillManager);
			}
			return new MonsterAttackBehaviourSkillTimeBomb(host, proto3, skillManager);
		}
		return new MonsterAttackBehaviourSkillBomb(host, proto2, skillManager);
	}

	private static MonsterAttackBehaviour _Other(BattleSystem battleSystem, Transform host, IMonsterAttackBehaviour proto)
	{
		if (proto is IMonsterAttackBehaviourCall proto2)
		{
			return new MonsterAttackBehaviourCall(host, proto2);
		}
		return null;
	}
}
