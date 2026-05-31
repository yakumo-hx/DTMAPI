using System;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class BulletMoverTracking : BulletMover
{
	private Func<Vector2, float, IBulletTrackingObject> trackingObjectGetter;

	private bool shouldRefindTrackingObject;

	private readonly float turningSpeed;

	private Vector2 _velocity;

	private IBulletTrackingObject target;

	public BulletMoverTracking(Transform transform, float moveSpeed, BulletDirectionType directionType, float turningSpeed)
		: base(transform, moveSpeed, directionType)
	{
		this.turningSpeed = turningSpeed;
	}

	public void BindTrackingGetter(Func<Vector2, float, IBulletTrackingObject> trackingObjectGetter)
	{
		this.trackingObjectGetter = trackingObjectGetter;
		target = trackingObjectGetter?.Invoke(transform.position, 100f);
		shouldRefindTrackingObject = target != null;
	}

	public override void Move(float deltaTime)
	{
		Tracking(deltaTime);
		if (directionType == BulletDirectionType.ISOTROPIC)
		{
			transform.Translate(_velocity * deltaTime);
		}
		else
		{
			transform.Translate(Vector2.right * (moveSpeed * deltaTime));
		}
	}

	private void Tracking(float dt)
	{
		if (target == null)
		{
			if (shouldRefindTrackingObject)
			{
				shouldRefindTrackingObject = false;
				target = trackingObjectGetter?.Invoke(transform.position, 100f);
				shouldRefindTrackingObject = target != null;
			}
		}
		else if (!target.isValid)
		{
			target = null;
		}
		else
		{
			_Tracking(dt, target.transform);
		}
	}

	private void _Tracking(float dt, Transform target)
	{
		Vector2 b = (target.position - transform.position).normalized;
		base.MoveDir = Vector2.Lerp(base.MoveDir, b, turningSpeed * dt);
		if (directionType == BulletDirectionType.ISOTROPIC)
		{
			_velocity = base.MoveDir * moveSpeed;
		}
		else
		{
			transform.rotation = base.MoveDir.GetRotation();
		}
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
