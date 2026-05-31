using DolocTown.GameData;
using RedSaw.Physical;
using UnityEngine;

namespace DolocTown;

public class BulletMoverParabolic : BulletMover
{
	private readonly float Gravity;

	private readonly float speedY;

	private SimpleSimulator simulator;

	public BulletMoverParabolic(Transform transform, float moveSpeed, BulletDirectionType directionType, float Gravity, float speedY)
		: base(transform, moveSpeed, directionType)
	{
		this.speedY = speedY;
		this.Gravity = Gravity;
	}

	public override void Move(float dt)
	{
		Vector2 vector = transform.position;
		transform.position = simulator.OnUpdate(dt);
		Vector2 vector2 = transform.position;
		if (directionType == BulletDirectionType.ANISOTROPIC)
		{
			transform.rotation = (vector2 - vector).GetRotation();
		}
	}

	public override void ConfigureFireInfo(Vector2 pos, Vector2 dir)
	{
		base.ConfigureFireInfo(pos, dir);
		simulator.CurrentPosition = pos;
		simulator.SetGravity(new Vector2(0f, Gravity));
		simulator.SetVelocity(dir * new Vector2(moveSpeed, speedY));
	}
}
