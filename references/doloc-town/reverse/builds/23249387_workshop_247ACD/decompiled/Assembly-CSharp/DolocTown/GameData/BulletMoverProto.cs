using UnityEngine;

namespace DolocTown.GameData;

public abstract class BulletMoverProto
{
	public readonly string name;

	protected BulletMoverProto(string name)
	{
		this.name = name;
	}

	public abstract BulletMover CreateMover(Transform transform, BulletDirectionType directionType, float moveSpeed);
}
