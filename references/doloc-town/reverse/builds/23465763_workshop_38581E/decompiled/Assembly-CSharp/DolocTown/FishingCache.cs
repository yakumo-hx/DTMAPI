using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using UnityEngine;

namespace DolocTown;

public class FishingCache
{
	public FishInfo FishProto { get; set; }

	public Item FishItem { get; set; }

	public FishingPool FishingPool { get; set; }

	public ItemFishingRod FishingRod { get; set; }

	public bool IsFailed { get; set; }

	public void Reset()
	{
		IsFailed = true;
		FishItem = null;
		FishProto = null;
	}

	public bool RollFish()
	{
		int level = ((ItemFunctionFishingRod)FishingRod.proto.Function).Level;
		FishProto = FishingPool.RollFishProto(level);
		if (FishProto == null)
		{
			Debug.Log("鱼原型为空!");
			return false;
		}
		FishItem = DolocAPI.GenerateItem(FishProto.Id);
		return true;
	}
}
