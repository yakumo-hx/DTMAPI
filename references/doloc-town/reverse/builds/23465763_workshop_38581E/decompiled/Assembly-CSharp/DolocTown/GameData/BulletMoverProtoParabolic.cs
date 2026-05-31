using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverProtoParabolic : BulletMoverProto
{
	private readonly float Gravity;

	private readonly float speedY;

	public BulletMoverProtoParabolic(string name, float Gravity, float speedY)
		: base(name)
	{
		this.Gravity = Gravity;
		this.speedY = speedY;
	}

	public override BulletMover CreateMover(Transform transform, BulletDirectionType directionType, float moveSpeed)
	{
		return new BulletMoverParabolic(transform, moveSpeed, directionType, Gravity, speedY);
	}
}
