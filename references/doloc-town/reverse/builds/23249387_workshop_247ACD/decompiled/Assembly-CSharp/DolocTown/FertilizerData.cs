using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public struct FertilizerData
{
	private static Dictionary<ItemInfo, Sprite> fertilizerSpriteCache = new Dictionary<ItemInfo, Sprite>();

	[JsonProperty]
	[DebugInfo("肥料点数")]
	public int duration;

	[JsonProperty]
	[DebugInfo("肥料加成值", Color = "#9bd547")]
	public readonly float growthAddition;

	public ItemInfo fertilizerItemInfo;

	[JsonProperty]
	[DebugInfo("肥料道具ID")]
	public string fertilizerItemId => fertilizerItemInfo?.Id ?? string.Empty;

	public Sprite FertilizerSprite => GetFertilizerSprite(fertilizerItemInfo);

	private static Sprite GetFertilizerSprite(ItemInfo itemInfo)
	{
		if (itemInfo == null)
		{
			return null;
		}
		if (fertilizerSpriteCache.TryGetValue(itemInfo, out var value))
		{
			return value;
		}
		if (!(itemInfo.Function is ItemFunctionFertilizer itemFunctionFertilizer))
		{
			return null;
		}
		value = itemFunctionFertilizer.FertilizerSprite.Asset;
		fertilizerSpriteCache[itemInfo] = value;
		return value;
	}

	public FertilizerData(int duration, float growthAddition, ItemInfo fertilizerItemInfo)
	{
		this.duration = duration;
		this.growthAddition = growthAddition;
		this.fertilizerItemInfo = fertilizerItemInfo;
	}

	[JsonConstructor]
	public FertilizerData(int duration, float growthAddition, string fertilizerItemId = null)
	{
		this.duration = duration;
		this.growthAddition = growthAddition;
		if (duration <= 0)
		{
			fertilizerItemInfo = null;
			return;
		}
		if (fertilizerItemId == null)
		{
			fertilizerItemInfo = DolocConfig.Tables.TbItem.DataList.FirstOrDefault((ItemInfo x) => x.Function is ItemFunctionFertilizer itemFunctionFertilizer && Mathf.Approximately(itemFunctionFertilizer.Addition, growthAddition));
		}
		else
		{
			DolocAPI.QueryItemProto(fertilizerItemId, out fertilizerItemInfo);
		}
		if (fertilizerItemInfo == null)
		{
			fertilizerItemInfo = DolocConfig.Tables.TbItem.DataList.FirstOrDefault((ItemInfo x) => x.Function is ItemFunctionFertilizer);
		}
	}

	public static ItemInfo ValidateTreeFertilizerItemInfo(FertilizerData current)
	{
		if (current.fertilizerItemInfo == null)
		{
			return null;
		}
		if (current.fertilizerItemInfo.Function is ItemFunctionFertilizer { IsTree: not false })
		{
			return current.fertilizerItemInfo;
		}
		ItemInfo itemInfo = DolocConfig.Tables.TbItem.DataList.FirstOrDefault((ItemInfo x) => x.Function is ItemFunctionFertilizer { IsTree: not false } itemFunctionFertilizer3 && Mathf.Approximately(itemFunctionFertilizer3.Addition, current.growthAddition));
		if (itemInfo != null)
		{
			return itemInfo;
		}
		return DolocConfig.Tables.TbItem.DataList.FirstOrDefault((ItemInfo x) => x.Function is ItemFunctionFertilizer itemFunctionFertilizer2 && itemFunctionFertilizer2.IsTree);
	}
}
