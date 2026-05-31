using System;
using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public static class EquipmentPatch
{
	private static bool TryTakeWaterFromPositions(this Equipment equipment, int value, IEnumerable<Vector2Int> positions)
	{
		List<(IWaterContainer, int)> list = new List<(IWaterContainer, int)>();
		IWaterContainer[] equipmentsOfAnyType = equipment.Host.GetEquipmentsOfAnyType<IWaterContainer>(positions);
		foreach (IWaterContainer waterContainer in equipmentsOfAnyType)
		{
			if (waterContainer.Water > value)
			{
				list.Add((waterContainer, value));
				value = 0;
				break;
			}
			list.Add((waterContainer, waterContainer.Water));
			value -= waterContainer.Water;
		}
		if (value > 0)
		{
			return false;
		}
		foreach (var (waterContainer2, require) in list)
		{
			waterContainer2.TakeWater(require);
		}
		return true;
	}

	private static bool TryTakeAgentWater(int value)
	{
		Item[] array = DolocAPI.archiveHandle.InventorySystem.inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is IWaterContainer waterContainer && waterContainer.Water >= value)
			{
				waterContainer.TakeWater(value);
				return true;
			}
		}
		return false;
	}

	public static bool TryCostWater(this Equipment equipment, int value)
	{
		if (!TryTakeAgentWater(value))
		{
			return equipment.TryTakeWaterFromPositions(value, Grid2D.Ring(equipment.Anchor, equipment.proto.CoverSize));
		}
		return true;
	}

	public static void ThunderToRemove(this Equipment equipment)
	{
		equipment.Host.RemoveEquipment(equipment);
	}

	public static Vector2 GetAroundPosition(this Equipment instance, float distance = 2f)
	{
		float num = UnityEngine.Random.Range(0f - distance, distance);
		float value = instance.Position.x + num;
		RoomGeometry geometry = instance.CurrentRoom.Geometry;
		return new Vector2(Math.Clamp(value, geometry.roomPosition.x + 1.5f, geometry.roomPosition.x + geometry.roomSize.x - 1.5f), instance.Position.y + 1.5f);
	}

	public static void CreateDropItem(this Equipment instance, CountItem countItem, bool shouldRender, bool sendMessage)
	{
		if (countItem.isValid)
		{
			for (int i = 0; i < countItem.itemCount; i++)
			{
				instance.CreateDropItem(countItem.itemName, shouldRender, sendMessage);
			}
		}
	}

	public static void CreateDropItem(this Equipment instance, Item item, bool shouldRender, bool sendMessage)
	{
		if (item != null)
		{
			if (shouldRender)
			{
				((IDropItemHost)instance.Host).CreateDropItem(item, instance.PositionCenter, sendMessage);
				return;
			}
			Vector2 targetPosition = (instance.proto.isDecal ? instance.GetPositionAroundHost() : instance.GetAroundPosition());
			((IDropItemHost)instance.Host).CreateDropItemNoRender(item, targetPosition, sendMessage);
		}
	}

	public static void CreateDropItem(this Equipment instance, string itemName, bool shouldRender, bool sendMessage)
	{
		if (shouldRender)
		{
			((IDropItemHost)instance.Host).CreateDropItem(itemName, instance.PositionCenter, sendMessage);
			return;
		}
		Vector2 targetPosition = (instance.proto.isDecal ? instance.GetPositionAroundHost() : instance.GetAroundPosition());
		((IDropItemHost)instance.Host).CreateDropItemNoRender(itemName, targetPosition, sendMessage);
	}

	public static void CreateDropItemMoney(this Equipment instance, int value, bool shouldRender, bool sendMessage)
	{
		if (shouldRender)
		{
			((IDropItemHost)instance.Host).CreateDropItemMoney(value, instance.PositionCenter, sendMessage);
		}
		else
		{
			((IDropItemHost)instance.Host).CreateDropItemMoneyNoRender(value, instance.GetAroundPosition(), sendMessage);
		}
	}

	public static Vector2Int GetRandomAroundSlot(this Equipment equipment, Vector2Int hRange, Vector2Int vRange)
	{
		int minInclusive = equipment.Anchor.x - hRange.x;
		int num = equipment.Anchor.x + equipment.CoveredSize.x + hRange.y;
		int minInclusive2 = equipment.Anchor.y - vRange.x;
		return new Vector2Int(y: UnityEngine.Random.Range(minInclusive2, equipment.Anchor.y + equipment.CoveredSize.y + vRange.y + 1), x: UnityEngine.Random.Range(minInclusive, num + 1));
	}

	public static Vector2Int GetRandomAroundSlot(this Equipment equipment, int hRange, int vRange)
	{
		return equipment.GetRandomAroundSlot(new Vector2Int(-hRange, hRange), new Vector2Int(0, vRange));
	}

	public static Vector2Int GetGlobalAnchor(this Equipment equipment)
	{
		return BuilderUtils.GetRealGridPos(equipment.Anchor, equipment.CurrentRoom.RoomPosition);
	}

	public static void PlaceItemInBagOrCreateDropItem(this Equipment instance, string itemName, bool putInBackpack, bool sendMessage)
	{
		Item item = DolocAPI.GenerateItem(itemName);
		if (putInBackpack)
		{
			DolocAPI.RaiseSpriteFadeUp(instance.PositionCenter, item.uiSprite);
			DolocAPI.RaiseItemObtainTip(item.name, item.uiSprite, item.title);
			item = DolocAPI.PlaceItem(item, DolocAPI.userSettings.autoUseBox);
		}
		if (item != null)
		{
			instance.CreateDropItem(item, instance.IsRender, sendMessage);
		}
	}

	public static void PlaceItemInBagOrCreateDropItem(this Equipment instance, Item item, bool putInBackpack, bool sendMessage)
	{
		if (item != null)
		{
			if (putInBackpack)
			{
				DolocAPI.RaiseSpriteFadeUp(instance.PositionCenter, item.uiSprite);
				DolocAPI.RaiseItemObtainTip(item.name, item.uiSprite, item.title, item.count);
				item = DolocAPI.PlaceItem(item, DolocAPI.userSettings.autoUseBox, useFade: true);
			}
			if (item != null)
			{
				instance.CreateDropItem(item, instance.IsRender, sendMessage);
			}
		}
	}

	public static void CollectAnimalProducts(this Equipment equipment, List<string> products)
	{
		if (equipment == null || products.IsNullOrEmpty())
		{
			return;
		}
		foreach (string product in products)
		{
			equipment.CreateDropItem(product, equipment.IsRender, sendMessage: true);
		}
		products.Clear();
		if (equipment.IsRender)
		{
			equipment.Renderer.Sprite = equipment.EquipmentSprite;
		}
	}

	public static void ReceiveAnimalProducts(this Equipment equipment, List<string> products, CountItem[] items, int capacity)
	{
		if (equipment == null || products == null || items.IsNullOrEmpty() || products.Count >= capacity)
		{
			return;
		}
		for (int i = 0; i < items.Length; i++)
		{
			CountItem countItem = items[i];
			for (int j = 0; j < countItem.itemCount; j++)
			{
				if (products.Count >= capacity)
				{
					return;
				}
				products.Add(countItem.itemName);
			}
		}
		if (equipment.IsRender)
		{
			DolocAPI.RaiseInstantPSEffects(equipment.PositionCenter, InstantParticleEffectsType.BRUST_STARS);
		}
	}
}
