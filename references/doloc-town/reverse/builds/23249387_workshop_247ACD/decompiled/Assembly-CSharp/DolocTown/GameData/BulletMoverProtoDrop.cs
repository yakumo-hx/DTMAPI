using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverProtoDrop : BulletMoverProto
{
	private readonly float gravity;

	public BulletMoverProtoDrop(string name, float gravity)
		: base(name)
	{
		this.gravity = gravity;
	}

	public override BulletMover CreateMover(Transform transform, BulletDirectionType directionType, float moveSpeed)
	{
		return new BulletMoverDrop(transform, moveSpeed, directionType, gravity);
	}
}
