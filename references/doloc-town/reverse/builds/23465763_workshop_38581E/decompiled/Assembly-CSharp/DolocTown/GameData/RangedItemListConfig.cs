using System;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct RangedItemListConfig
{
	[SerializeField]
	private string itemListId;

	public RangedItem[] rangedItems => proto?.Items ?? Array.Empty<RangedItem>();

	private RangedItemListInfo proto => DolocConfig.Tables.TbRangedItemList.GetOrDefault(itemListId ?? "");

	public bool isEmpty => rangedItems.IsNullOrEmpty();

	public Item[] GenerateItems()
	{
		return rangedItems.Select((RangedItem x) => DolocAPI.GenerateItem(x.itemName, x.randomCount)).ToArray();
	}

	public CountItem[] GenerateCountItems()
	{
		return rangedItems.Select((RangedItem x) => new CountItem(x.itemName, x.randomCount)).ToArray();
	}
}
