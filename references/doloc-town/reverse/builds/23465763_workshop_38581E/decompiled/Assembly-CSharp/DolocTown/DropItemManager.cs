using System.Collections.Generic;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DropItemManager : DataManager<DropItemBase>
{
	public DropItemManager()
	{
	}

	[JsonConstructor]
	protected DropItemManager(IndexList<DropItemBase> datas)
		: base(datas)
	{
	}

	public DropItem CreateDropItem(IDropItemHost host, string itemId, Vector2 position, bool shouldSendMsg)
	{
		DropItem dropItem = new DropItem(itemId, position, shouldSendMsg)
		{
			Host = host
		};
		AddData(dropItem);
		return dropItem;
	}

	public DropItemBase CreateDropItem(IDropItemHost host, Item item, Vector2 position, bool shouldSendMsg)
	{
		if (item == null)
		{
			return null;
		}
		DropItemBase dropItemBase = new SpecialDropItem(item, position, shouldSendMsg);
		dropItemBase.Host = host;
		AddData(dropItemBase);
		return dropItemBase;
	}

	public DropItemMoney CreateMoney(IDropItemHost host, int value, Vector2 pos, bool shouldSendMsg)
	{
		DropItemMoney dropItemMoney = new DropItemMoney(value, pos, shouldSendMsg);
		dropItemMoney.Host = host;
		AddData(dropItemMoney);
		return dropItemMoney;
	}

	public bool RemoveDropItem(DropItemBase item)
	{
		return RemoveData(item);
	}

	public void RemoveDisposableItems()
	{
		List<DropItemBase> list = new List<DropItemBase>();
		foreach (DropItemBase allData in base.AllDatas)
		{
			if (allData.Disposable)
			{
				list.Add(allData);
			}
		}
		foreach (DropItemBase item in list)
		{
			RemoveDropItem(item);
		}
	}
}
