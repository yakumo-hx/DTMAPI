using System.Collections.Generic;
using System.Linq;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public static class AutomateProcessingUtils
{
	[Command("check_processing_materials", Desc = "检查配方在当前环境中可以进行多少次合成")]
	private static void Command_TestForCountMaterials(string recipeName)
	{
		if (!(DolocAPI.SelectedEquipment is AutomateBotStation station))
		{
			Debug.LogError("请选中一个自动化工作台");
			return;
		}
		if (!DolocAPI.QueryRecipe(recipeName, out var recipe))
		{
			Debug.LogError("配方\"" + recipeName + "\"不存在");
			return;
		}
		int num = CalcSynthesisMaxCount(station.CountMaterialsInEnv(recipe.InputItems), recipe.InputItems);
		Debug.Log($"配方\"{recipeName}\"在当前环境中可以进行{num}次合成");
	}

	private static Dictionary<string, CountItemContainer[]> CountMaterialsInEnv(this AutomateBotStation station, CountItem[] items)
	{
		Dictionary<string, List<CountItemContainer>> dictionary = new Dictionary<string, List<CountItemContainer>>();
		AutomateStationEnv.RoomEnv[] roomEnvs = station.Env.roomEnvs;
		for (int i = 0; i < roomEnvs.Length; i++)
		{
			foreach (Case equipment in roomEnvs[i].GetEquipments<Case>())
			{
				CountItemContainer[] record2 = CountMaterialsInContainer(items, equipment);
				__Record(dictionary, record2);
			}
		}
		return dictionary.ToDictionary((KeyValuePair<string, List<CountItemContainer>> kv) => kv.Value[0].ItemName, (KeyValuePair<string, List<CountItemContainer>> kv) => kv.Value.ToArray());
		static void __Record(Dictionary<string, List<CountItemContainer>> output, CountItemContainer[] record)
		{
			for (int j = 0; j < record.Length; j++)
			{
				CountItemContainer item = record[j];
				if (!output.ContainsKey(item.ItemName))
				{
					output[item.ItemName] = new List<CountItemContainer>();
				}
				output[item.ItemName].Add(item);
			}
		}
	}

	private static Dictionary<string, int> __Sum(Dictionary<string, CountItemContainer[]> materials)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (KeyValuePair<string, CountItemContainer[]> material in materials)
		{
			material.Deconstruct(out var key, out var value);
			string key2 = key;
			int value2 = value.Sum((CountItemContainer containerInfo) => containerInfo.ItemCount);
			dictionary[key2] = value2;
		}
		return dictionary;
	}

	public static int CalcSynthesisMaxCount(Dictionary<string, CountItemContainer[]> materialDict, CountItem[] recipeItems)
	{
		Dictionary<string, int> dictionary = __Sum(materialDict);
		int num = int.MaxValue;
		for (int i = 0; i < recipeItems.Length; i++)
		{
			CountItem countItem = recipeItems[i];
			if (!dictionary.ContainsKey(countItem.itemName) || dictionary[countItem.itemName] < countItem.itemCount)
			{
				return 0;
			}
			if (num > 1)
			{
				int num2 = ((countItem.itemCount == 1) ? dictionary[countItem.itemName] : (dictionary[countItem.itemName] / countItem.itemCount));
				if (num2 < num)
				{
					num = num2;
				}
			}
		}
		return num;
	}

	public static CountItemContainer[] CountMaterialsInContainer(CountItem[] items, Case container)
	{
		Dictionary<string, int> dictionary = items.ToDictionary((CountItem C) => C.itemName, (CountItem C) => 0);
		Item[] array = container.inventory.ReadAll();
		foreach (Item item in array)
		{
			if (dictionary.ContainsKey(item.name))
			{
				dictionary[item.name] += item.count;
			}
		}
		return dictionary.Select((KeyValuePair<string, int> KV) => new CountItemContainer(container, new CountItem(KV.Key, KV.Value))).ToArray();
	}
}
