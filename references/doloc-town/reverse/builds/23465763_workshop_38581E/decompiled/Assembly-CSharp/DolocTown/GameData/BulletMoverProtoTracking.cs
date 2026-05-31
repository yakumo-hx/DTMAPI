using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverProtoTracking : BulletMoverProto
{
	public readonly float turningSpeed;

	public BulletMoverProtoTracking(string name, float turningSpeed)
		: base(name)
	{
		this.turningSpeed = turningSpeed;
	}

	public override BulletMover CreateMover(Transform transform, BulletDirectionType directionType, float moveSpeed)
	{
		return new BulletMoverTracking(transform, moveSpeed, directionType, turningSpeed);
	}
}
