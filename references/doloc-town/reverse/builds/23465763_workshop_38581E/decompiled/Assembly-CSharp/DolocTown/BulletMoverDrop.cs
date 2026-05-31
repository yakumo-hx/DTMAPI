using DolocTown.GameData;
using RedSaw.Physical;
using UnityEngine;

namespace DolocTown;

public class BulletMoverDrop : BulletMover
{
	private readonly float gravity;

	private PhysicalSimulator _simulator;

	public BulletMoverDrop(Transform transform, float moveSpeed, BulletDirectionType directionType, float gravity)
		: base(transform, moveSpeed, directionType)
	{
		this.gravity = gravity;
	}

	public override void Move(float deltaTime)
	{
		Vector2 vector = transform.position;
		transform.position = _simulator.Update(deltaTime);
		Vector2 vector2 = transform.position;
		if (directionType == BulletDirectionType.ANISOTROPIC)
		{
			transform.rotation = (vector2 - vector).GetRotation();
		}
	}

	public override void ConfigureFireInfo(Vector2 pos, Vector2 dir)
	{
		base.ConfigureFireInfo(pos, dir);
		_simulator = new PhysicalSimulator(pos, 1f);
		_simulator.SetGravity(new Vector2(0f, gravity));
		_simulator.SetVelocity(dir * moveSpeed);
	}
}
