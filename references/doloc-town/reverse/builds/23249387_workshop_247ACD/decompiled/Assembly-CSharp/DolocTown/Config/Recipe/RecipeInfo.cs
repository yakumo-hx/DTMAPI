using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class RecipeInfo : BeanBase
{
	public const int __ID__ = -1105053764;

	public string Id { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public int CostTime { get; private set; }

	public RangedItem OutputItem { get; private set; }

	public CountItem[] InputItems { get; private set; }

	public string RecipeSubType { get; private set; }

	public RecipeSubTypeInfo RecipeSubType_Ref { get; private set; }

	public int TechPoint { get; private set; }

	public bool ShowInHandbook { get; private set; }

	public string RecipeTitle
	{
		get
		{
			if (!Title.IsNullOrEmpty())
			{
				return Title;
			}
			return DolocAPI.GetItemTitle(OutputItem.itemName);
		}
	}

	public RecipeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
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
		if (!_json["cost_time"].IsNumber)
		{
			throw new SerializationException();
		}
		CostTime = _json["cost_time"];
		if (!_json["output_item"].IsObject)
		{
			throw new SerializationException();
		}
		OutputItem = ExternalTypeUtil.RangedItemConverter(CfgRangedItem.DeserializeCfgRangedItem(_json["output_item"]));
		JSONNode jSONNode = _json["input_items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		InputItems = new CountItem[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CountItem countItem = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(child));
			InputItems[num++] = countItem;
		}
		if (!_json["recipe_sub_type"].IsString)
		{
			throw new SerializationException();
		}
		RecipeSubType = _json["recipe_sub_type"];
		if (!_json["tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		TechPoint = _json["tech_point"];
		if (!_json["show_in_handbook"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShowInHandbook = _json["show_in_handbook"];
	}

	public RecipeInfo(string id, bool default_unlock, string title, int cost_time, RangedItem output_item, CountItem[] input_items, string recipe_sub_type, int tech_point, bool show_in_handbook)
	{
		Id = id;
		DefaultUnlock = default_unlock;
		Title = title;
		CostTime = cost_time;
		OutputItem = output_item;
		InputItems = input_items;
		RecipeSubType = recipe_sub_type;
		TechPoint = tech_point;
		ShowInHandbook = show_in_handbook;
	}

	public static RecipeInfo DeserializeRecipeInfo(JSONNode _json)
	{
		return new RecipeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1105053764;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		RecipeSubType_Ref = (_tables["Recipe.TbRecipeSubType"] as TbRecipeSubType).GetOrDefault(RecipeSubType);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",DefaultUnlock:" + DefaultUnlock + ",Title:" + Title + ",CostTime:" + CostTime + ",OutputItem:" + OutputItem.ToString() + ",InputItems:" + StringUtil.CollectionToString(InputItems) + ",RecipeSubType:" + RecipeSubType + ",TechPoint:" + TechPoint + ",ShowInHandbook:" + ShowInHandbook + ",}";
	}

	public bool Match(CountItem[] items)
	{
		if (items.IsNullOrEmpty())
		{
			return false;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		CountItem[] inputItems = InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			dictionary.TryAdd(countItem.itemName, 0);
			dictionary[countItem.itemName] += countItem.itemCount;
		}
		inputItems = items;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem2 = inputItems[i];
			if (!dictionary.ContainsKey(countItem2.itemName))
			{
				return false;
			}
			dictionary[countItem2.itemName] -= countItem2.itemCount;
		}
		return dictionary.All((KeyValuePair<string, int> x) => x.Value == 0);
	}
}
