using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverProtoRing : BulletMoverProto
{
	private readonly float angleOffset;

	public BulletMoverProtoRing(string name, float angleOffset)
		: base(name)
	{
		this.angleOffset = angleOffset;
	}

	public override BulletMover CreateMover(Transform transform, BulletDirectionType dirType, float moveSpeed)
	{
		return new BulletMoverRing(transform, moveSpeed, dirType, angleOffset);
	}
}
