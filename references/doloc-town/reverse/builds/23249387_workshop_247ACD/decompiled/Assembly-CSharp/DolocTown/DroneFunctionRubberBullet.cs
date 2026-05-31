using System;
using System.Collections.Generic;
using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionRubberBullet : DroneFunction
{
	private readonly DroneFunctionProtoRubberBullet protoRubberBullet;

	private readonly Dictionary<Bullet, int> bulletReboundCount = new Dictionary<Bullet, int>();

	public DroneFunctionRubberBullet(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		protoRubberBullet = (DroneFunctionProtoRubberBullet)proto;
	}

	private bool CheckReboundCount(Bullet bullet)
	{
		if (bulletReboundCount.TryGetValue(bullet, out var value))
		{
			if (value >= protoRubberBullet.MaxReboundTimes)
			{
				return false;
			}
			bulletReboundCount[bullet] = value + 1;
			return true;
		}
		bulletReboundCount[bullet] = 1;
		return true;
	}

	public override bool HandleCollideWithEnemy(Bullet bullet, AttackHitInfo hitInfo)
	{
		if (CheckReboundCount(bullet) && ReboundRandomDir(bullet, hitInfo))
		{
			return true;
		}
		bulletReboundCount.Remove(bullet);
		bullet.Recycle();
		return true;
	}

	public override void HandleCollideWithWall(Bullet bullet, AttackHitInfo hitInfo)
	{
		if (bullet.collideWithWall && (!CheckReboundCount(bullet) || !ReboundBulletReflectDir(bullet, hitInfo)))
		{
			bulletReboundCount.Remove(bullet);
			bullet.Recycle();
		}
	}

	private Vector2 GetNormal(Vector2 moveDir)
	{
		float num = Vector2.SignedAngle(Vector2.right, moveDir);
		if (num < 0f)
		{
			num += 360f;
		}
		int num2 = Mathf.RoundToInt(num / 45f);
		return new Vector2(Mathf.Cos((float)(num2 * 45) * (MathF.PI / 180f)), Mathf.Sin((float)(num2 * 45) * (MathF.PI / 180f)));
	}

	private bool ReboundRandomDir(Bullet bullet, AttackHitInfo hitInfo)
	{
		int num = UnityEngine.Random.Range(0, 360);
		Vector2 normal = new Vector2(Mathf.Cos((float)num * (MathF.PI / 180f)), Mathf.Sin((float)num * (MathF.PI / 180f)));
		bullet.Rebound(normal);
		return true;
	}

	private bool ReboundBulletReflectDir(Bullet bullet, AttackHitInfo hitInfo)
	{
		Vector3 center = hitInfo.collider.bounds.center;
		Vector3 size = hitInfo.collider.bounds.size;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(distance: Mathf.Max(size.x, size.y) + 0.1f, origin: center, direction: bullet.MoveDir, layerMask: DolocAPI.gameConfig.groundMask);
		if (raycastHit2D.collider == null)
		{
			return false;
		}
		if (raycastHit2D.collider != hitInfo.collider)
		{
			return false;
		}
		Vector2 normal = raycastHit2D.normal;
		bullet.Rebound(normal);
		return true;
	}

	private bool ReboundBulletInvertDir(Bullet bullet, AttackHitInfo hitInfo)
	{
		return true;
	}
}
