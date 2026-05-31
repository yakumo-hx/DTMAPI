using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class BulletMoverRing : BulletMover
{
	private readonly float angleOffset;

	private float dirSign;

	private Vector2 currentDir;

	public BulletMoverRing(Transform transform, float moveSpeed, BulletDirectionType directionType, float angleOffset)
		: base(transform, moveSpeed, directionType)
	{
		this.angleOffset = angleOffset;
	}

	public override void Move(float deltaTime)
	{
		currentDir = currentDir.RotateDeg(angleOffset * dirSign);
		if (directionType == BulletDirectionType.ISOTROPIC)
		{
			transform.Translate(currentDir * (moveSpeed * deltaTime));
			return;
		}
		transform.rotation = currentDir.GetRotation();
		transform.Translate(Vector2.right * (moveSpeed * deltaTime));
	}

	public override void ConfigureFireInfo(Vector2 pos, Vector2 dir)
	{
		base.ConfigureFireInfo(pos, dir);
		currentDir = dir;
		dirSign = ((dir.x > 0f) ? 1 : (-1));
		if (directionType == BulletDirectionType.ANISOTROPIC)
		{
			transform.rotation = dir.GetRotation();
		}
	}
}
