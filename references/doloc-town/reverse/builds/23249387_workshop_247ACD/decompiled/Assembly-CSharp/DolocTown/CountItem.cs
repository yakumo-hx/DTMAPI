using System;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct CountItem
{
	[SerializeField]
	public string itemName;

	[SerializeField]
	public int itemCount;

	public bool isValid
	{
		get
		{
			if (itemCount > 0)
			{
				return DolocConfig.Tables.TbItem.GetOrDefault(itemName ?? "") != null;
			}
			return false;
		}
	}

	public CountItem(Item item)
	{
		this = default(CountItem);
		itemName = item?.name;
		itemCount = item?.count ?? 0;
	}

	public CountItem(string itemName, int itemCount)
	{
		this = default(CountItem);
		this.itemName = itemName;
		this.itemCount = itemCount;
	}

	public override readonly string ToString()
	{
		return $"{itemName}×{itemCount}";
	}

	public Item GenerateItem()
	{
		return DolocAPI.GenerateItem(itemName, itemCount);
	}

	public bool Equals(Item other)
	{
		if (other == null)
		{
			return itemName == null;
		}
		if (other.name == itemName)
		{
			return other.count == itemCount;
		}
		return false;
	}
}
