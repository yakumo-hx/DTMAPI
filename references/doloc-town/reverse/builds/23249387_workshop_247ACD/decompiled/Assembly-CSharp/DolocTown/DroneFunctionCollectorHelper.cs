using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Drone;
using DolocTown.Config.Resource;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionCollectorHelper : DroneFunction
{
	private readonly DroneFunctionProtoCollectorHelper protoCollectorHelper;

	private readonly HashSet<byte> affectedResourceTypes;

	public static void HandleResource(Bullet bullet, Component other)
	{
		if (!bullet.AffectResource)
		{
			return;
		}
		DungeonResourceRenderer component = other.GetComponent<DungeonResourceRenderer>();
		if (!(component == null) && component.DungeonResource != null)
		{
			DungeonResource dungeonResource = component.DungeonResource;
			if (bullet.AffectedResourceTypes.Contains((byte)dungeonResource.ResourceType.GetHashCode()))
			{
				bool levelMatch = bullet.ToolLevel >= dungeonResource.currentLevelData.BulletLevelConstraint;
				dungeonResource._Fell(new ResourceFellData(levelMatch, bullet.ToolLevel, bullet.ChopCount, bullet.positionWS, shouldCounterBack: false, shouldRaiseToolTip: false));
			}
		}
	}

	public DroneFunctionCollectorHelper(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		protoCollectorHelper = (DroneFunctionProtoCollectorHelper)proto;
		affectedResourceTypes = new HashSet<byte>(protoCollectorHelper.Types.Select((DungeonResourceType x) => (byte)x.GetHashCode()));
	}

	public override void HandleBullet(Bullet bullet)
	{
		bullet.SetResourceInfos(affectedResourceTypes, protoCollectorHelper.ToolLevel, protoCollectorHelper.ChopCount);
	}
}
