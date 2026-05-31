using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverProtoLinear : BulletMoverProto
{
	public BulletMoverProtoLinear(string name = "linear")
		: base(name)
	{
	}

	public override BulletMover CreateMover(Transform transform, BulletDirectionType directionType, float moveSpeed)
	{
		return new BulletMoverLinear(transform, moveSpeed, directionType);
	}
}
