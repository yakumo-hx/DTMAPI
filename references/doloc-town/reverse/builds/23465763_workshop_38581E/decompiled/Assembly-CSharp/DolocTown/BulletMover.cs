using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public abstract class BulletMover
{
	protected readonly float moveSpeed;

	protected readonly Transform transform;

	protected readonly BulletDirectionType directionType;

	public Vector2 MoveDir { get; protected set; }

	protected BulletMover(Transform transform, float moveSpeed, BulletDirectionType directionType)
	{
		this.moveSpeed = moveSpeed;
		this.transform = transform;
		this.directionType = directionType;
	}

	public abstract void Move(float deltaTime);

	public virtual void ConfigureFireInfo(Vector2 pos, Vector2 dir)
	{
		MoveDir = dir;
		transform.position = pos;
	}
}
