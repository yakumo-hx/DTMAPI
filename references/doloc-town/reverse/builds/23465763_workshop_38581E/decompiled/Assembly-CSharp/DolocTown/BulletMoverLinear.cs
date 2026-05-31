using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class BulletMoverLinear : BulletMover
{
	private Vector2 _velocity;

	public BulletMoverLinear(Transform transform, float moveSpeed, BulletDirectionType dirType)
		: base(transform, moveSpeed, dirType)
	{
	}

	public override void Move(float deltaTime)
	{
		transform.Translate(_velocity * deltaTime);
	}

	public override void ConfigureFireInfo(Vector2 pos, Vector2 dir)
	{
		base.ConfigureFireInfo(pos, dir);
		switch (directionType)
		{
		case BulletDirectionType.ISOTROPIC:
			_velocity = dir.normalized * moveSpeed;
			break;
		case BulletDirectionType.ANISOTROPIC:
			_velocity = new Vector2(moveSpeed, 0f);
			transform.rotation = dir.GetRotation();
			break;
		}
	}
}
