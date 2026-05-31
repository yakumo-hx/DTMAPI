using UnityEngine;

namespace DolocTown;

public class DroneEnv
{
	private IMonsterHost MonsterHost;

	public bool TryGetNearestMonsterPosToAgent(float distance, out Vector2 pos)
	{
		pos = default(Vector2);
		if (MonsterHost == null)
		{
			return false;
		}
		if (MonsterHost.TryGetNearestMonsterInRadius(DolocAPI.droneRenderer.position2d, distance, out var monster))
		{
			pos = monster.Controller.position2d;
			return true;
		}
		if (MonsterHost.TryGetNearestTargetInRadius(DolocAPI.droneRenderer.position2d, distance, out var target))
		{
			pos = target.transform.position;
			return true;
		}
		return false;
	}

	public bool TryGetNearestMonsterPosToAgent(float distance, float ballisticWidth, out Vector2 pos)
	{
		pos = default(Vector2);
		if (MonsterHost == null)
		{
			return false;
		}
		if (MonsterHost.TryGetNearestMonsterInRadius(DolocAPI.droneRenderer.position2d, distance, ballisticWidth, out var monster))
		{
			pos = monster.Controller.PositionAttack;
			return true;
		}
		if (MonsterHost.TryGetNearestTargetInRadius(DolocAPI.droneRenderer.position2d, distance, out var target))
		{
			pos = target.transform.position;
			return true;
		}
		return false;
	}

	public IBulletTrackingObject FindNearestTrackingObject(Vector2 viewPosition, float radius = 100f)
	{
		if (MonsterHost == null)
		{
			return null;
		}
		if (MonsterHost.TryGetNearestMonsterInRadius(viewPosition, radius, out var monster))
		{
			return monster;
		}
		if (MonsterHost.TryGetNearestTargetInRadius(viewPosition, radius, out var target))
		{
			return target;
		}
		return null;
	}

	public void SetMonsterHost(IMonsterHost host)
	{
		MonsterHost = host;
	}
}
