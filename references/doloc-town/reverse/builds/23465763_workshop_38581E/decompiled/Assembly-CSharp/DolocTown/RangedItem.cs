using DolocTown.Config;
using DolocTown.Config.Item;
using UnityEngine;

namespace DolocTown;

public struct RangedItem
{
	public string itemName;

	public int minCount;

	public int maxCount;

	public ItemInfo itemRef => DolocConfig.Tables.TbItem.GetOrDefault(itemName ?? "");

	public int randomCount
	{
		get
		{
			if (maxCount > minCount)
			{
				return Random.Range(minCount, maxCount + 1);
			}
			return minCount;
		}
	}

	public RangedItem(string itemName, int minCount, int maxCount)
	{
		this.itemName = itemName;
		this.minCount = minCount;
		this.maxCount = maxCount;
	}
}
