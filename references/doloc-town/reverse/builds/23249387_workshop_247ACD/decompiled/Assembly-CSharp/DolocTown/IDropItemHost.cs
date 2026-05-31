using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public interface IDropItemHost : IBaseHost
{
	[JsonProperty]
	DropItemManager DM_dropitem { get; }

	void RenderAllDropItems()
	{
		DolocAPI.EntitySystem.SetupAll<DropItemRenderer, DropItemBase>(DM_dropitem.AllDatas, RenderDropItem);
	}

	void HideAllDropItems()
	{
		if (DM_dropitem.IsEmpty)
		{
			return;
		}
		foreach (DropItemBase allData in DM_dropitem.AllDatas)
		{
			DolocAPI.EntitySystem.Recycle(allData.Renderer);
		}
	}

	void SetAllShieldCollector(bool shield)
	{
		foreach (DropItemBase allData in DM_dropitem.AllDatas)
		{
			allData.SetShieldCollector(shield);
		}
	}

	void Clear()
	{
		if (DM_dropitem.IsEmpty)
		{
			return;
		}
		Queue<DropItemBase> queue = new Queue<DropItemBase>();
		foreach (DropItemBase allData in DM_dropitem.AllDatas)
		{
			queue.Enqueue(allData);
		}
		while (queue.Count > 0)
		{
			RemoveDropItem(queue.Dequeue());
		}
	}

	void DebugClear()
	{
		if (DM_dropitem.IsEmpty)
		{
			return;
		}
		foreach (DropItemBase allData in DM_dropitem.AllDatas)
		{
			DolocAPI.EntitySystem.Recycle(allData.Renderer);
		}
		DM_dropitem.Clear();
	}

	DropItem CreateDropItem(string itemName, Vector2 start, bool shouldSendMsg, float offset = 0f)
	{
		if (!DolocAPI.QueryItemProto(itemName, out var _))
		{
			return null;
		}
		bool bounce;
		Vector2 randomDropLocation = GetRandomDropLocation(start, offset, out bounce);
		DropItem dropItem = CreateDropItemNoRender(itemName, randomDropLocation, shouldSendMsg);
		RenderDropItem(DolocAPI.EntitySystem.Next<DropItemRenderer>(), dropItem);
		dropItem.Renderer.Raise(start, randomDropLocation, bounce);
		return dropItem;
	}

	DropItemBase CreateDropItemWithCount(CountItem countItem, Vector2 start, bool shouldSendMsg, float offset = 0f)
	{
		if (!DolocAPI.QueryItemProto(countItem.itemName, out var _))
		{
			return null;
		}
		bool bounce;
		Vector2 randomDropLocation = GetRandomDropLocation(start, offset, out bounce);
		Item data = DolocAPI.GenerateItem(countItem.itemName, countItem.itemCount);
		DropItemBase dropItemBase = CreateDropItemNoRender(data, randomDropLocation, shouldSendMsg);
		RenderDropItem(DolocAPI.EntitySystem.Next<DropItemRenderer>(), dropItemBase);
		dropItemBase.Renderer.Raise(start, randomDropLocation, bounce);
		return dropItemBase;
	}

	DropItem[] CreateDropItemsNoRender(CountItem[] countItems, Vector2 targetPosition, bool shouldSendMessage)
	{
		DropItem[] array = new DropItem[countItems.Length];
		for (int i = 0; i < countItems.Length; i++)
		{
			CountItem countItem = countItems[i];
			for (int j = 0; j < countItem.itemCount; j++)
			{
				array[i] = CreateDropItemNoRender(targetPosition: new Vector2(targetPosition.x + (float)Random.Range(-1, 1), targetPosition.y), itemName: countItem.itemName, shouldSendMsg: shouldSendMessage);
			}
		}
		return array;
	}

	DropItem CreateDropItemNoRender(string itemName, Vector2 targetPosition, bool shouldSendMsg)
	{
		return DM_dropitem.CreateDropItem(this, itemName, targetPosition, shouldSendMsg);
	}

	DropItemBase CreateDropItem(Item data, Vector2 start, bool shouldSendMsg, float offset = 0f)
	{
		if (data == null)
		{
			return null;
		}
		bool bounce;
		Vector2 randomDropLocation = GetRandomDropLocation(start, offset, out bounce);
		DropItemBase dropItemBase = CreateDropItemNoRender(data, randomDropLocation, shouldSendMsg);
		RenderDropItem(DolocAPI.EntitySystem.Next<DropItemRenderer>(), dropItemBase);
		dropItemBase.Renderer.Raise(start, randomDropLocation, bounce);
		return dropItemBase;
	}

	DropItemBase CreateDropItemNoRender(Item data, Vector2 targetPosition, bool shouldSendMsg)
	{
		if (data == null)
		{
			return null;
		}
		return DM_dropitem.CreateDropItem(this, data, targetPosition, shouldSendMsg);
	}

	DropItemMoney CreateDropItemMoney(int value, Vector2 start, bool shouldSendMsg)
	{
		bool bounce;
		Vector2 randomDropLocation = GetRandomDropLocation(start, 0f, out bounce);
		DropItemMoney dropItemMoney = CreateDropItemMoneyNoRender(value, randomDropLocation, shouldSendMsg);
		RenderDropItem(DolocAPI.EntitySystem.Next<DropItemRenderer>(), dropItemMoney);
		dropItemMoney.Renderer.Raise(start, randomDropLocation);
		return dropItemMoney;
	}

	DropItemMoney CreateDropItemMoneyNoRender(int value, Vector2 targetPosition, bool shouldSendMsg)
	{
		return DM_dropitem.CreateMoney(this, value, targetPosition, shouldSendMsg);
	}

	void RenderDropItem(DropItemRenderer renderer, DropItemBase dropItem)
	{
		renderer.WorldContent = dropItem;
		dropItem.BaseRenderer = renderer;
		renderer.SetSprite(dropItem.SceneSprite, dropItem.SubscriptSprite);
		renderer.position2d = dropItem.PositionWS;
		if (TryGetBorderColor(dropItem, out var color))
		{
			renderer.BorderColor = color;
		}
	}

	bool TryGetBorderColor(DropItemBase D, out Color color)
	{
		color = default(Color);
		if (!D.IsItem)
		{
			return false;
		}
		DolocAPI.QueryItemProto(D.ItemName, out var proto);
		ItemFunctionBase itemFunctionBase = proto?.Function;
		if (itemFunctionBase is ItemFunctionEquipment || itemFunctionBase is ItemFunctionBuilding || itemFunctionBase is ItemFunctionPlatform)
		{
			color = DolocAPI.eftConfig.BorderColorEquipment;
			return true;
		}
		if (DolocConfig.Tables.TbMissionItem.GetOrDefault(D.ItemName) != null)
		{
			color = DolocAPI.eftConfig.BorderColorMissionItem;
			return true;
		}
		return false;
	}

	bool RemoveDropItem(DropItemBase item)
	{
		bool result = RemoveDropItemNoRender(item);
		DolocAPI.EntitySystem.Recycle(item.Renderer);
		return result;
	}

	bool RemoveDropItemNoRender(DropItemBase item)
	{
		return DM_dropitem.RemoveDropItem(item);
	}

	bool GenerateDropItemsWithBuff(DungeonResourceType type, ItemSpawnEntry entry, Vector2 pos, bool shouldSendMsg, bool useBuff)
	{
		if (entry?.SpawnLut_Ref == null)
		{
			return false;
		}
		int totalCount = (useBuff ? GetCountWithBuff(type, entry.CountRange.RandomCount) : entry.CountRange.RandomCount);
		CountItem[] countItems = entry.SpawnLut_Ref.SpawnItems(totalCount);
		CreateDropItemAnimated(countItems, pos, shouldSendMsg);
		return true;
	}

	int GetCountWithBuff(DungeonResourceType type, int originCount)
	{
		switch (type)
		{
		case DungeonResourceType.TREE:
		case DungeonResourceType.TREE_TRUNK:
			return DolocAPI.AbilitySystem.collectionAbility.GetCollectionWoodsCount(originCount);
		case DungeonResourceType.ORE:
		case DungeonResourceType.SAND:
			return DolocAPI.AbilitySystem.collectionAbility.GetCollectionStoneCount(originCount);
		case DungeonResourceType.WEEDS:
		case DungeonResourceType.WEEDS_SMALL:
			return DolocAPI.AbilitySystem.collectionAbility.GetCollectionWeedsCount(originCount);
		case DungeonResourceType.MECHANICAL_REMAINS:
			return DolocAPI.AbilitySystem.collectionAbility.GetCollectionGarbageCount(originCount);
		default:
			return originCount;
		}
	}

	void CreateDropItemAnimated(CountItem[] countItems, Vector2 startPos, bool shouldSendMsg)
	{
		startPos.y += DolocAPI.eftConfig.dungeonResourceDropItemPopYOffset;
		for (int i = 0; i < countItems.Length; i++)
		{
			CountItem countItem = countItems[i];
			for (int j = 0; j < countItem.itemCount; j++)
			{
				CreateDropItem(countItem.itemName, startPos, shouldSendMsg);
			}
		}
	}

	void CreateDropItemAnimated(CountItem countItem, Vector2 startPos, bool shouldSendMsg)
	{
		startPos.y += DolocAPI.eftConfig.dungeonResourceDropItemPopYOffset;
		for (int i = 0; i < countItem.itemCount; i++)
		{
			CreateDropItem(countItem.itemName, startPos, shouldSendMsg);
		}
	}

	int GetDropItemsCountByName(string itemName)
	{
		int num = 0;
		foreach (DropItemBase allData in DM_dropitem.AllDatas)
		{
			if (allData is SpecialDropItem specialDropItem && allData.ItemName == itemName)
			{
				num += specialDropItem.DropItem.count;
			}
			else if (allData.IsItem && allData.ItemName == itemName)
			{
				num++;
			}
		}
		return num;
	}

	Vector2 GetRandomDropLocation(Vector2 start, float offset, out bool bounce)
	{
		bounce = true;
		Vector2 pos = start;
		pos.x += ((offset == 0f) ? DolocAPI.eftConfig.dropItemRaiseXRange : offset);
		pos = CurrentRoom.Geometry.Constraint(pos, new Vector2(1.5f, 1.5f));
		Vector2 result = Vector2.zero;
		int layerMask = (1 << LayerMask.NameToLayer("Platform")) | (1 << LayerMask.NameToLayer("Water"));
		RaycastHit2D raycastHit2D = Physics2D.Raycast(pos, Vector2.down, pos.y - CurrentRoom.Geometry.roomPosition.y, layerMask);
		bool num = raycastHit2D.collider != null;
		if (num)
		{
			bounce = LayerMask.LayerToName(raycastHit2D.transform.gameObject.layer) == "Platform";
			result = new Vector2(pos.x, raycastHit2D.point.y + 1.5f);
		}
		Vector2Int vector2Int = CurrentRoom.Geometry.CalcCellPosition(pos);
		if (CurrentRoom.Geometry.IsObstacle(vector2Int))
		{
			vector2Int = CurrentRoom.Geometry.GetNearestEmptyPosition(CurrentRoom.Geometry.CalcCellPosition(start));
		}
		CurrentRoom.Geometry.RaycastGround(vector2Int, out var groundPosition);
		groundPosition.y++;
		Vector2 result2 = CurrentRoom.Geometry.CalcWorldPosition(groundPosition);
		if (num && result.y > result2.y)
		{
			return result;
		}
		bounce = true;
		return result2;
	}

	void __AfterLoadDropItems()
	{
		DolocInitAssert.IsTrue(DM_dropitem != null);
		foreach (DropItemBase allData in DM_dropitem.AllDatas)
		{
			allData.Host = this;
		}
	}
}
