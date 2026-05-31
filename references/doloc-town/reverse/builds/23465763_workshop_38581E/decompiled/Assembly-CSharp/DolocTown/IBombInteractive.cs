using UnityEngine;

namespace DolocTown;

public interface IBombInteractive
{
	AttackableType attackableType { get; }

	void OnBomb(float damage, bool isCritical, Vector2 pos);
}
