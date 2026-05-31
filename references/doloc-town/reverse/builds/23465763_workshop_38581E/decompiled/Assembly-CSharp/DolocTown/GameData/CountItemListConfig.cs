using System;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public struct CountItemListConfig
{
	[SerializeField]
	private string itemListId;

	public string ItemListId => itemListId;

	public CountItem[] countItems => proto?.Items ?? Array.Empty<CountItem>();

	private CountItemListInfo proto => DolocConfig.Tables.TbCountItemList.GetOrDefault(itemListId ?? "");

	public bool isEmpty => countItems.IsNullOrEmpty();

	public Item[] GenerateItems()
	{
		return countItems.Select((CountItem x) => DolocAPI.GenerateItem(x.itemName, x.itemCount)).ToArray();
	}

	public void SetId(string id)
	{
		itemListId = id;
	}
}
