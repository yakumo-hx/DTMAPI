using UnityEngine;

namespace DolocTown;

public interface IAttackable
{
	AttackableType attackableType { get; }

	bool OnAttacked(float attack, bool isCritical, Vector2 position, out bool isDead);

	bool OnSwordAttack(float atk, bool isCritical, Vector2 position, out bool isDead);

	bool OnAttacked(float atk, bool ctr, Vector2 pos)
	{
		bool isDead;
		return OnAttacked(atk, ctr, pos, out isDead);
	}
}
