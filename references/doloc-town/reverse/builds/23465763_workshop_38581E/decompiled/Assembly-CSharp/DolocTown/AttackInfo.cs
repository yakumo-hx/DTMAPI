using UnityEngine;

namespace DolocTown;

public readonly struct AttackInfo
{
	public readonly Vector2 position;

	public readonly Vector2 direction;

	public AttackInfo(Vector2 position, Vector2 direction)
	{
		this.position = position;
		this.direction = direction;
	}
}
