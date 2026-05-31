using DolocTown.Config.Drone;
using UnityEngine;

namespace DolocTown;

public class DroneFunctionFarmCollectorHelper : DroneFunction
{
	public static void HandleCrop(Bullet bullet, Component other)
	{
		if (bullet.AffectCrop)
		{
			CropRenderer component = other.GetComponent<CropRenderer>();
			if (!(component == null) && component.Crop != null)
			{
				component.Crop.plantBasin?.Harvest();
			}
		}
	}

	public DroneFunctionFarmCollectorHelper(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
	}

	public override void HandleBullet(Bullet bullet)
	{
		bullet.SetCropInfos();
	}
}
