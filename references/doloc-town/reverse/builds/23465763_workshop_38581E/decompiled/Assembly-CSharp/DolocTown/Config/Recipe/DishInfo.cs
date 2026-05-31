using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class DishInfo : BeanBase
{
	public const int __ID__ = -2030263864;

	private RangedItemWithPriceThreshold[] orderedItems;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public bool Unlockable { get; private set; }

	public int CostTime { get; private set; }

	public List<RangedItemWithPriceThreshold> OutputItemByThresholds { get; private set; }

	public int TechPoint { get; private set; }

	public IngredientGroupArray[] InputClass { get; private set; }

	public DishInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["unlockable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Unlockable = _json["unlockable"];
		if (!_json["cost_time"].IsNumber)
		{
			throw new SerializationException();
		}
		CostTime = _json["cost_time"];
		JSONNode jSONNode = _json["output_item_by_thresholds"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		OutputItemByThresholds = new List<RangedItemWithPriceThreshold>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			RangedItemWithPriceThreshold item = RangedItemWithPriceThreshold.DeserializeRangedItemWithPriceThreshold(child);
			OutputItemByThresholds.Add(item);
		}
		if (!_json["tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		TechPoint = _json["tech_point"];
		JSONNode jSONNode2 = _json["input_class"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode2.Count;
		InputClass = new IngredientGroupArray[count];
		int num = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			IngredientGroupArray ingredientGroupArray = IngredientGroupArray.DeserializeIngredientGroupArray(child2);
			InputClass[num++] = ingredientGroupArray;
		}
	}

	public DishInfo(string id, string title, bool unlockable, int cost_time, List<RangedItemWithPriceThreshold> output_item_by_thresholds, int tech_point, IngredientGroupArray[] input_class)
	{
		Id = id;
		Title = title;
		Unlockable = unlockable;
		CostTime = cost_time;
		OutputItemByThresholds = output_item_by_thresholds;
		TechPoint = tech_point;
		InputClass = input_class;
	}

	public static DishInfo DeserializeDishInfo(JSONNode _json)
	{
		return new DishInfo(_json);
	}

	public override int GetTypeId()
	{
		return -2030263864;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RangedItemWithPriceThreshold outputItemByThreshold in OutputItemByThresholds)
		{
			outputItemByThreshold?.Resolve(_tables);
		}
		IngredientGroupArray[] inputClass = InputClass;
		for (int i = 0; i < inputClass.Length; i++)
		{
			inputClass[i]?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		foreach (RangedItemWithPriceThreshold outputItemByThreshold in OutputItemByThresholds)
		{
			outputItemByThreshold?.TranslateText(translator);
		}
		IngredientGroupArray[] inputClass = InputClass;
		for (int i = 0; i < inputClass.Length; i++)
		{
			inputClass[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",Unlockable:" + Unlockable + ",CostTime:" + CostTime + ",OutputItemByThresholds:" + StringUtil.CollectionToString(OutputItemByThresholds) + ",TechPoint:" + TechPoint + ",InputClass:" + StringUtil.CollectionToString(InputClass) + ",}";
	}

	private void PostResolve()
	{
		orderedItems = OutputItemByThresholds.OrderByDescending((RangedItemWithPriceThreshold x) => x.PriceThreshold).ToArray();
	}

	public RangedItem GetItemByPriceThreshold(float priceThreshold)
	{
		RangedItemWithPriceThreshold[] array = orderedItems;
		foreach (RangedItemWithPriceThreshold rangedItemWithPriceThreshold in array)
		{
			if (priceThreshold >= (float)rangedItemWithPriceThreshold.PriceThreshold)
			{
				return rangedItemWithPriceThreshold.RangedItem;
			}
		}
		return orderedItems[0].RangedItem;
	}

	public RangedItem GetMaxQualityItem()
	{
		return orderedItems[0].RangedItem;
	}

	public bool Match(CountItem[] items)
	{
		if (items.IsNullOrEmpty())
		{
			return false;
		}
		List<string> list = new List<string>();
		for (int i = 0; i < items.Length; i++)
		{
			CountItem countItem = items[i];
			for (int j = 0; j < countItem.itemCount; j++)
			{
				list.Add(countItem.itemName);
			}
		}
		int num = items.Length;
		int num2 = InputClass.Length;
		if (num > num2)
		{
			return false;
		}
		for (int k = num; k < num2; k++)
		{
			list.Add("none");
		}
		foreach (List<string> uniquePermutation in list.GetUniquePermutations())
		{
			bool flag = true;
			for (int l = 0; l < num2; l++)
			{
				string text = uniquePermutation[l];
				IngredientGroupArray ingredientGroupArray = InputClass[l];
				bool flag2 = false;
				if (text == "none")
				{
					flag2 = ingredientGroupArray.Array.Contains("none");
				}
				else if (ingredientGroupArray.Array.Contains("any"))
				{
					flag2 = true;
				}
				else
				{
					IngredientGroupInfo[] array_Ref = ingredientGroupArray.Array_Ref;
					foreach (IngredientGroupInfo ingredientGroupInfo in array_Ref)
					{
						flag2 |= ingredientGroupInfo.Items.Contains(text);
						if (flag2)
						{
							break;
						}
					}
				}
				flag = flag && flag2;
				if (!flag)
				{
					break;
				}
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}
}
