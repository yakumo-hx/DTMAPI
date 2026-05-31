using System;
using System.Collections.Generic;

namespace DolocTown;

public static class AutomateBotPatch
{
	public static PlantCondition GetPlantCondition(this PlantBasin basin)
	{
		(bool, bool, bool) tuple = ParseRoomCondition(basin.CurrentRoom);
		return new PlantCondition(basin.SeedTypeInfo, DolocAPI.archiveHandle.timeData.dateNow.Month, DolocAPI.archiveHandle.CurrentWeatherType, tuple.Item2, tuple.Item3, tuple.Item1);
	}

	private static (bool, bool, bool) ParseRoomCondition(Room room)
	{
		if (!(room is TemplateRoomInHouse templateRoomInHouse))
		{
			return (false, false, false);
		}
		bool ignoreSeason = templateRoomInHouse.RoomEffectInfo.IgnoreSeason;
		bool ignoreSeasonFungus = templateRoomInHouse.RoomEffectInfo.IgnoreSeasonFungus;
		return (!templateRoomInHouse.Building.IsBroken, ignoreSeason, ignoreSeasonFungus);
	}

	public static ItemSeed[] AutomatePatchTakeSeedsFromContainer(this LinearInventory inventory, PlantCondition[] conds)
	{
		(PlantCondition, int)[] array = __CombineConditions(conds);
		List<ItemSeed> list = new List<ItemSeed>();
		(PlantCondition, int)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(PlantCondition, int) tuple = array2[i];
			PlantCondition item = tuple.Item1;
			int item2 = tuple.Item2;
			ItemSeed[] collection = _TakeSeedsFromInventory(inventory, item, item2);
			list.AddRange(collection);
		}
		return list.ToArray();
	}

	public static ItemSeed AutomatePatchTakeSeedFromContainer(this LinearInventory inventory, PlantCondition cond)
	{
		foreach (ItemSeed item in inventory.ReadAll<ItemSeed>())
		{
			if (cond.IsMatchSeed(item))
			{
				return item.CostSelfFromInventory(inventory, showFadeUpIcon: false) as ItemSeed;
			}
		}
		return null;
	}

	private static (PlantCondition, int)[] __CombineConditions(PlantCondition[] conditions)
	{
		Dictionary<PlantCondition, int> dictionary = new Dictionary<PlantCondition, int>();
		foreach (PlantCondition key in conditions)
		{
			dictionary.TryAdd(key, 0);
			dictionary[key]++;
		}
		(PlantCondition, int)[] array = new(PlantCondition, int)[dictionary.Count];
		int num = 0;
		foreach (KeyValuePair<PlantCondition, int> item in dictionary)
		{
			array[num++] = (item.Key, item.Value);
		}
		return array;
	}

	private static ItemSeed[] _TakeSeedsFromInventory(LinearInventory caseInventory, PlantCondition condition, int requireCount)
	{
		List<ItemSeed> list = new List<ItemSeed>();
		foreach (ItemSeed item2 in caseInventory.ReadAll<ItemSeed>())
		{
			if (condition.IsMatchSeed(item2))
			{
				if (item2.count > requireCount)
				{
					ItemSeed item = (ItemSeed)item2.Clone(requireCount);
					item2.count -= requireCount;
					list.Add(item);
					break;
				}
				caseInventory.Take(item2);
				if (item2.count == requireCount)
				{
					list.Add(item2);
					break;
				}
				requireCount -= item2.count;
			}
		}
		return list.ToArray();
	}

	public static T[] AutomatePatchTakeItems<T>(this LinearInventory inventory, int requireCount) where T : Item
	{
		if (requireCount <= 0)
		{
			return Array.Empty<T>();
		}
		List<T> list = new List<T>();
		foreach (T item2 in inventory.ReadAll<T>())
		{
			if (item2.count > requireCount)
			{
				T item = (T)item2.Clone(requireCount);
				item2.count -= requireCount;
				list.Add(item);
				break;
			}
			inventory.Take(item2);
			if (item2.count == requireCount)
			{
				list.Add(item2);
				break;
			}
			requireCount -= item2.count;
		}
		return list.ToArray();
	}

	public static T[] AutomatePatchTakeItems<T>(this LinearInventory inventory, Func<T, bool> predict, int requireCount) where T : Item
	{
		if (requireCount <= 0)
		{
			return Array.Empty<T>();
		}
		List<T> list = new List<T>();
		foreach (T item2 in inventory.ReadAll<T>())
		{
			if (predict(item2))
			{
				if (item2.count > requireCount)
				{
					T item = (T)item2.Clone(requireCount);
					item2.count -= requireCount;
					list.Add(item);
					break;
				}
				inventory.Take(item2);
				if (item2.count == requireCount)
				{
					list.Add(item2);
					break;
				}
				requireCount -= item2.count;
			}
		}
		return list.ToArray();
	}

	public static T AutomatePatchTakeItem<T>(this LinearInventory inventory) where T : Item
	{
		using (IEnumerator<T> enumerator = inventory.ReadAll<T>().GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current.CostSelfFromInventory(inventory, showFadeUpIcon: false) as T;
			}
		}
		return null;
	}

	public static T AutomatePatchTakeItem<T>(this LinearInventory inventory, Func<T, bool> predict) where T : Item
	{
		foreach (T item in inventory.ReadAll<T>())
		{
			if (predict(item))
			{
				return item.CostSelfFromInventory(inventory, showFadeUpIcon: false) as T;
			}
		}
		return null;
	}
}
