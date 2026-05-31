using DolocTown.Config.Fishing;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class FishingPool : DolocObject
{
	[SerializeField]
	private string poolName;

	public InteractiveWater WaterEntity { get; private set; }

	public string PoolName => poolName;

	public void SetWater(InteractiveWater water)
	{
		WaterEntity = water;
	}

	public void DisposeWater()
	{
		WaterEntity = null;
	}

	public FishInfo RollFishProto(int toolLv)
	{
		return DolocAPI.RollFish(poolName, toolLv);
	}
}
